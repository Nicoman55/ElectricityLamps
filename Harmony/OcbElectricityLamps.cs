using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ElectricityLamps
{
    // Entry point for the mod that initializes all Harmony patches.
    // Hooks into the game’s mod loading system and applies custom behavior
    // for electricity light blocks, tile entities and UI handling.
    public class OcbElectricityLamps : IModApi
    {
        // Called by the game when the mod is loaded.
        public void InitMod(Mod mod)
        {
            // Write a log entry so the patch loading can be seen in the game log.
            Debug.Log("Loading OCB Electricity Lamps Patch: " + base.GetType().ToString());

            // Create a Harmony instance using this class name as the patch ID.
            Harmony harmony = new Harmony(base.GetType().ToString());

            // Apply all Harmony patches found in this assembly.
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        // Empty constructor, kept for normal class instantiation.
        public OcbElectricityLamps()
        {
        }

        // Patch TileEntity.Instantiate so tile entity type 244 creates the custom lamp tile entity.
        [HarmonyPatch(typeof(TileEntity))]
        [HarmonyPatch("Instantiate")]
        public class TileEntity_Instantiate
        {
            // Runs before TileEntity.Instantiate.
            public static bool Prefix(ref TileEntity __result, TileEntityType type, Chunk _chunk)
            {
                // Check whether the requested tile entity type is the electricity light type.
                bool isElectricityLightType = (int)type == 244;

                // Controls whether the original Instantiate method should still run.
                bool shouldRunOriginalInstantiate;

                if (isElectricityLightType)
                {
                    // Return the custom electricity light tile entity instead of the vanilla one.
                    __result = new TileEntityElectricityLightBlock(_chunk);

                    // Skip the original Instantiate method because the result was already set.
                    shouldRunOriginalInstantiate = false;
                }
                else
                {
                    // Let the original Instantiate method handle all other tile entity types.
                    shouldRunOriginalInstantiate = true;
                }

                // Harmony uses false to skip the original method, true to continue normally.
                return shouldRunOriginalInstantiate;
            }

            // Empty constructor, not required for the patch logic.
            public TileEntity_Instantiate()
            {
            }
        }

        // Patch GameManager.OpenTileEntityUi to open the custom lamp UI for custom lamp tile entities.
        [HarmonyPatch(typeof(GameManager))]
        [HarmonyPatch("OpenTileEntityUi")]
        public class GameManager_OpenTileEntityUi
        {
            // Runs after GameManager.OpenTileEntityUi.
            public static void Postfix(GameManager __instance, int _entityIdThatOpenedIt, TileEntity _te, string _customUi, World ___m_World)
            {
                // Get the local player UI for the entity that opened the tile entity.
                LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(___m_World.GetEntity(_entityIdThatOpenedIt) as EntityPlayerLocal);

                // Try to treat the opened tile entity as a custom electricity light block.
                TileEntityElectricityLightBlock tileEntityElectricityLightBlock = _te as TileEntityElectricityLightBlock;

                // Check whether the opened tile entity is actually one of the custom light blocks.
                bool isElectricityLightBlock = tileEntityElectricityLightBlock != null;

                if (isElectricityLightBlock)
                {
                    if (tileEntityElectricityLightBlock.WindowGroupToOpen == XUiC_PoweredSpotlightWindowGroup.ID)
                    {
                        return;
                    }
                    
                    // Check whether the player UI could not be found.
                    bool isPlayerUiMissing = uiforPlayer == null;

                    if (!isPlayerUiMissing)
                    {
                        // Pass the selected tile entity into the custom electricity lamps window controller.
                        ((XUiC_ElectricityLampsWindowGroup)((XUiWindowGroup)uiforPlayer.windowManager.GetWindow("electricitylamps")).Controller).TileEntity = tileEntityElectricityLightBlock;

                        // Open the custom electricity lamps UI window.
                        uiforPlayer.windowManager.Open("electricitylamps", true, false, true);
                    }
                }
            }

            // Empty constructor, not required for the patch logic.
            public GameManager_OpenTileEntityUi()
            {
            }
        }

        // Patch TileEntityPowered.CanHaveParent so custom lamp blocks can always have a power parent.
        [HarmonyPatch(typeof(TileEntityPowered))]
        [HarmonyPatch("CanHaveParent")]
        public class TileEntityPowered_CanHaveParent
        {
            // Runs before TileEntityPowered.CanHaveParent.
            private static bool Prefix(TileEntityPowered __instance, ref bool __result, IPowered powered)
            {
                // Only override behaviour for custom light blocks.
                // Let the base game handle all other powered blocks normally.
                if (!(__instance is TileEntityElectricityLightBlock))
                {
                    // Run the original method.
                    return true;
                }

                // Allow the custom electricity light block to have a parent power source.
                __result = true;

                // Skip the original method because the result was already set.
                return false;
            }

            // Empty constructor, not required for the patch logic.
            public TileEntityPowered_CanHaveParent()
            {
            }
        }

        // Patch ItemActionConnectPower.OnHoldingUpdate.
        [HarmonyPatch(typeof(ItemActionConnectPower))]
        [HarmonyPatch("OnHoldingUpdate")]
        public class ItemActionConnectPower_OnHoldingUpdate
        {
            // Runs before ItemActionConnectPower.OnHoldingUpdate.
            private static bool Prefix(ItemActionConnectPower __instance, ItemActionData _actionData)
            {
                // Return true, so the original method still runs unchanged.
                return true;
            }

            // Empty constructor, not required for the patch logic.
            public ItemActionConnectPower_OnHoldingUpdate()
            {
            }
        }

        // Postfix patch on TileEntityPowered.InitializePowerData to restore the correct
        // dynamic power consumption after a game reload. Without this, the power source
        // would display only the base 5W from blocks.xml instead of the actual calculated
        // value based on the light's current intensity and range settings.
        [HarmonyPatch(typeof(TileEntityPowered))]
        [HarmonyPatch("InitializePowerData")]
        public class TileEntityPowered_InitializePowerData
        {
            public static void Postfix(TileEntityPowered __instance)
            {
                if (__instance is TileEntityElectricityLightBlock lightTileEntity)
                {
                    lightTileEntity.UpdateDynamicRequiredPower();
                }
            }
        }
    }
}