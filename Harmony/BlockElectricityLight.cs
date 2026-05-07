using UnityEngine;

namespace ColoredLights_Port
{
    // Custom powered block that represents an electricity light in the world.
    // Handles placement, interaction and links the block to its corresponding
    // TileEntityElectricityLightBlock, which stores and controls all light behavior.
    public class BlockElectricityLight : BlockPoweredLight
    {
        private const byte LightEnabledMetaBit = 2;
        private new readonly BlockActivationCommand[] cmds =
        {
            new BlockActivationCommand("light", "electric_switch", true, false),
            new BlockActivationCommand("edit", "tool", true, false),
            new BlockActivationCommand("aim", "map_cursor", true, false),
            new BlockActivationCommand("take", "hand", false, false)
        };

        public BlockLight BlockLight_Block { get; private set; }

        // Updates the visible light state when the power system activates or deactivates the block.
        public override bool ActivateBlock(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool isOn, bool isPowered)
        {
            UpdateLightState(_world, _cIdx, _blockPos, _blockValue);
            return true;
        }

        // Creates the tile entity used by this powered light block.
        public override TileEntityPowered CreateTileEntity(Chunk chunk)
        {
            return new TileEntityElectricityLightBlock(chunk)
            {
                PowerItemType = (PowerItem.PowerItemTypes)2
            };
        }

        // Creates and registers the custom tile entity when this block is added to the world.
        public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue, PlatformUserIdentifierAbs _addedByPlayer)
           {
            base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue, _addedByPlayer);
            if (_world.GetTileEntity(_chunk.ClrIdx, _blockPos) is TileEntityElectricityLightBlock)
            {
                return;
            }
            TileEntityPowered tileEntity = this.CreateTileEntity(_chunk);
            tileEntity.localChunkPos = World.toBlock(_blockPos);
            tileEntity.InitializePowerData();
            if (tileEntity is TileEntityElectricityLightBlock lightTileEntity)
            {
                lightTileEntity.PresetDefaultValues(_blockValue.type);
                lightTileEntity.IsToggled = IsLightEnabledInMeta(_blockValue.meta);

                if (lightTileEntity.PowerItem is PowerConsumerToggle powerConsumerToggle)
                {
                    powerConsumerToggle.IsToggled = lightTileEntity.IsToggled;
                }
            }
            _chunk.AddTileEntity(tileEntity);
            //Debug.Log("Created TileEntityElectricityLightBlock at: " + _blockPos + " | cIdx: " + _chunk.ClrIdx);
        }

        // Returns the interaction text for switching the light on or off.
        public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            if (!(_world.GetTileEntity(_clrIdx, _blockPos) is TileEntityElectricityLightBlock lightTileEntity))
            {
                return null;
            }
            PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
            string activateKeyText = XUiUtils.GetBindingXuiMarkupString(playerInput.Activate, 0, 0, null)
                                   + XUiUtils.GetBindingXuiMarkupString(playerInput.PermanentActions.Activate, 0, 0, null);
            if (lightTileEntity.IsSpotLight)
            {
                return string.Format(
                    Localization.Get("tooltipInteract"),
                    activateKeyText,
                    Localization.Get("spotlightPlayer")
                );
            }
            string localizationKey = IsLightEnabledInMeta(_blockValue.meta) ? "useSwitchLightOff" : "useSwitchLightOn";
            return string.Format(Localization.Get(localizationKey), activateKeyText);
        }

        // Enables or disables the available radial menu commands for this light block.
        public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            bool canTakeBlock = _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer(), false)
                             && TakeDelay > 0f;
            bool isSpotLight = false;
            TileEntityElectricityLightBlock lightTileEntity = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityElectricityLightBlock;
            if (lightTileEntity != null)
            {
                isSpotLight = lightTileEntity.IsSpotLight;
            }
            cmds[0].enabled = true;                         // light
            cmds[1].enabled = true;                         // edit
            cmds[2].enabled = CanAimSpotlight(_blockValue); // aim
            cmds[3].enabled = canTakeBlock;                 // take
            return cmds;
        }

        // Returns the emitted light value only when the light is switched on.
        public override byte GetLightValue(BlockValue _blockValue)
        {
            return IsLightEnabledInMeta(_blockValue.meta) ? base.GetLightValue(_blockValue) : (byte)0;
        }

        // Allows this block to always expose activation commands.
        public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            return true;
        }

        // Runs the base block initialization.
        public override void Init()
        {
            base.Init();
        }

        // Keeps the tile entity saved when the block is stored in a prefab.
        public override bool IsTileEntitySavedInPrefab()
        {
            return true;
        }

        // Handles the light, edit, and take activation commands.
        public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
        {
            switch (_commandName)
            {
                case "light":
                    bool shouldSwitchOn = !IsLightEnabledInMeta(_blockValue.meta);
                    //return ActivateBlock(_world, _cIdx, _blockPos, _blockValue, shouldSwitchOn, true);
                    return SetLightSwitchState(_world, _cIdx, _blockPos, _blockValue, shouldSwitchOn);
                case "edit":
                    return TryOpenLightEditor(_world, _cIdx, _blockPos, _player);
                case "aim":
                    return CanAimSpotlight(_blockValue) && TryAimSpotlight(_world, _cIdx, _blockPos, _player);
                case "take":
                    TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                    return true;
                default:
                    return false;
            }
        }

        // Stores the player's switch state and updates power/light behavior.
        private bool SetLightSwitchState(WorldBase world, int chunkIndex, Vector3i blockPos, BlockValue blockValue, bool switchOn)
        {
            blockValue.meta = SetLightEnabledMeta(blockValue.meta, switchOn);
            world.SetBlockRPC(chunkIndex, blockPos, blockValue);

            if (world.GetTileEntity(chunkIndex, blockPos) is TileEntityElectricityLightBlock lightTileEntity)
            {
                lightTileEntity.IsToggled = switchOn;

                if (lightTileEntity.PowerItem is PowerConsumerToggle powerConsumerToggle)
                {
                    powerConsumerToggle.IsToggled = switchOn;
                }

                lightTileEntity.UpdateDynamicRequiredPower();
                lightTileEntity.SetModified();
            }

            UpdateLightState(world, chunkIndex, blockPos, blockValue);
            return true;
        }

        // Opens the vanilla-style aiming camera for spotlight blocks.
        private static bool TryAimSpotlight(WorldBase world, int chunkIndex, Vector3i blockPos, EntityPlayerLocal player)
        {
            if (!(world.GetTileEntity(chunkIndex, blockPos) is TileEntityElectricityLightBlock lightTileEntity))
            {
                return false;
            }
            if (!lightTileEntity.IsSpotLight)
            {
                return false;
            }
            player.AimingGun = false;
            lightTileEntity.WindowGroupToOpen = XUiC_PoweredSpotlightWindowGroup.ID;
            world.GetGameManager().TELockServer(
                chunkIndex,
                lightTileEntity.ToWorldPos(),
                lightTileEntity.entityId,
                player.entityId,
                null
            );
            return true;
        }

        // Updates the light after the block entity transform has been activated.
        public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
        {
            base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);

            if (_world.GetTileEntity(_cIdx, _blockPos) is TileEntityElectricityLightBlock lightTileEntity && lightTileEntity.IsSpotLight)
            {
                SpotlightController spotlightController = _ebcd.transform.gameObject.GetComponent<SpotlightController>();

                if (spotlightController != null)
                {
                    spotlightController.Init(base.Properties);
                    spotlightController.TileEntity = lightTileEntity;
                }
            }

            UpdateLightState(_world, _cIdx, _blockPos, _blockValue);
        }

        private static bool CanAimSpotlight(BlockValue blockValue)
        {
            DynamicProperties properties = Block.list[blockValue.type].Properties;

            return properties.Values.ContainsKey("CanAimSpotlight")
                && StringParsers.ParseBool(properties.Values["CanAimSpotlight"], 0, -1, true);
        }

        // Updates the light when the block value changes.
        public override void OnBlockValueChanged(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
        {
            base.OnBlockValueChanged(_world, _chunk, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
            UpdateLightState(_world, _clrIdx, _blockPos, _newBlockValue);
        }

        // Updates both Unity light components according to the stored switch state and power state.
        private bool UpdateLightState(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool switchLight = false)
        {
            ChunkCluster chunkCluster = _world.ChunkClusters[_cIdx];
            if (chunkCluster == null)
            {
                return false;
            }
            IChunk chunk = chunkCluster.GetChunkFromWorldPos(_blockPos);
            if (chunk == null)
            {
                return false;
            }
            BlockEntityData blockEntity = chunk.GetBlockEntity(_blockPos);
            if (blockEntity == null || !blockEntity.bHasTransform)
            {
                return false;
            }
            bool shouldLightBeOn = IsLightEnabledInMeta(_blockValue.meta);
            //Debug.Log("shouldLightBeOn (from meta): " + shouldLightBeOn);
            TileEntity rawTE = _world.GetTileEntity(_cIdx, _blockPos);
            if (rawTE is TileEntityElectricityLightBlock lightTileEntity)
            {
                shouldLightBeOn = shouldLightBeOn && lightTileEntity.IsPowered;
                //Debug.Log("rawTE is TileEntityElectricityLightBlock | shouldLightBeOn: " + shouldLightBeOn);
                //Debug.Log("Powered: " + lightTileEntity.IsPowered);
            }
            else
            {
                shouldLightBeOn = false;
                //Debug.Log("rawTE is NOT TileEntityElectricityLightBlock | shouldLightBeOn: " + shouldLightBeOn);
            }
            if (switchLight)
            {
                shouldLightBeOn = !shouldLightBeOn;
                _blockValue.meta = SetLightEnabledMeta(_blockValue.meta, shouldLightBeOn);
                _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
            }
            SetLightLodState(blockEntity.transform, "MainLight", shouldLightBeOn);
            SetLightLodState(blockEntity.transform, "Point light", shouldLightBeOn);
            if (rawTE is TileEntityElectricityLightBlock lightTileEntityForController && lightTileEntityForController.IsSpotLight)
            {
                SpotlightController spotlightController = blockEntity.transform.gameObject.GetComponent<SpotlightController>();

                if (spotlightController != null)
                {
                    spotlightController.IsOn = shouldLightBeOn;
                }
            }
            return true;
        }

        // Opens the light editor window for this block if its tile entity exists.
        private static bool TryOpenLightEditor(WorldBase world, int chunkIndex, Vector3i blockPos, EntityPlayerLocal player)
        {
            if (!(world.GetTileEntity(chunkIndex, blockPos) is TileEntityElectricityLightBlock lightTileEntity))
            {
                return false;
            }
            player.AimingGun = false;
            lightTileEntity.WindowGroupToOpen = "";
            Vector3i tileEntityWorldPos = lightTileEntity.ToWorldPos();
            world.GetGameManager().TELockServer(chunkIndex, tileEntityWorldPos, lightTileEntity.entityId, player.entityId, null);
            return true;
        }

        // Switches a named child LightLOD component on or off when it exists.
        private static void SetLightLodState(Transform parentTransform, string childName, bool isOn)
        {
            Transform lightTransform = parentTransform.Find(childName);
            if (lightTransform == null)
            {
                return;
            }
            LightLOD lightComponent = lightTransform.GetComponent<LightLOD>();
            if (lightComponent != null)
            {
                lightComponent.SwitchOnOff(isOn);
            }
        }

        // Checks whether the light enabled bit is set in the block metadata.
        private static bool IsLightEnabledInMeta(byte meta)
        {
            return (meta & LightEnabledMetaBit) != 0;
        }

        // Sets or clears the light enabled bit in the block metadata.
        private static byte SetLightEnabledMeta(byte meta, bool isEnabled)
        {
            return (byte)((meta & ~LightEnabledMetaBit) | (isEnabled ? LightEnabledMetaBit : 0));
        }

        // Picks up the light block and returns the ColoredLightsVariantHelper
        // instead of the specific light variant, allowing the player to
        // choose a different light type on next placement.
        private void TakeItemWithTimerAsHelper(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
        {
            if (_blockValue.damage > 0)
            {
                GameManager.ShowTooltip(_player, Localization.Get("ttRepairBeforePickup"), false, false, 0f);
                return;
            }
            LocalPlayerUI playerUI = _player.PlayerUI;
            playerUI.windowManager.Open("timer", true, false, true);
            XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
            TimerEventData timerEventData = new TimerEventData();
            timerEventData.Data = new object[] { _cIdx, _blockValue, _blockPos, _player };
            timerEventData.Event += this.TakeAsHelperEvent;
            childByType.SetTimer(this.TakeDelay, timerEventData, -1f, "");
        }

        // Timer callback that replaces the block with the ColoredLightsVariantHelper item.
        private void TakeAsHelperEvent(TimerEventData timerData)
        {
            World world = GameManager.Instance.World;
            object[] array = (object[])timerData.Data;
            int cIdx = (int)array[0];
            BlockValue blockValue = (BlockValue)array[1];
            Vector3i blockPos = (Vector3i)array[2];
            EntityPlayerLocal player = array[3] as EntityPlayerLocal;

            BlockValue currentBlock = world.GetBlock(blockPos);
            if (currentBlock.damage > 0)
            {
                GameManager.ShowTooltip(player, Localization.Get("ttRepairBeforePickup"), false, false, 0f);
                return;
            }
            if (currentBlock.type != blockValue.type)
            {
                GameManager.ShowTooltip(player, Localization.Get("ttBlockMissingPickup"), false, false, 0f);
                return;
            }

            LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(player);
            // Return ColoredLightsVariantHelper instead of the specific block
            ItemStack itemStack = new ItemStack(
                new ItemValue(Block.GetBlockByName("ColoredLightsVariantHelper").blockID), 1);
            if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack))
                uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
            world.SetBlockRPC(cIdx, blockPos, BlockValue.Air);
        }
    }
}