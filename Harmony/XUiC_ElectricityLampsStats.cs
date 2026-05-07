using System.Globalization;
using UnityEngine;

namespace ElectricityLamps
{
    // Handles the UI controls for editing electricity lamp settings window.
    // Retrieves data from the associated TileEntityElectricityLightBlock and
    // updates UI elements such as labels, stats and controls to reflect the
    // current lamp settings and state.
    public class XUiC_ElectricityLampsStats : XUiController
    {
        public XUiC_ElectricityLampsWindowGroup Owner { get; set; }

        public TileEntityElectricityLightBlock TileEntity
        {
            get => tileEntity;
            set
            {
                tileEntity = value;

                if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer && tileEntity != null)
                {
                    powerItem = tileEntity.GetPowerItem();
                }
            }
        }

        // Finds all child UI controls and registers their value change handlers.
        public override void Init()
        {
            base.Init();

            CbxLightState = RegisterComboBox<LightStateType, XUiC_ComboBoxEnum<LightStateType>>(
                "CbxLightState",
                CbxLightState_OnValueChanged,
                "cbxLightState");
            cbxRate = RegisterComboBox<double, XUiC_ComboBoxFloat>(
                "cbxRate",
                CbxRate_OnValueChanged,
                "cbxRate");
            cbxDelay = RegisterComboBox<double, XUiC_ComboBoxFloat>(
                "cbxDelay",
                CbxDelay_OnValueChanged,
                "cbxDelay");
            uiUseKelvin = RegisterComboBox<bool, XUiC_ComboBoxBool>(
                "uiUseKelvin",
                UiUseKelvin_OnValueChanged,
                "uiUseKelvin");
            uiTemperature = RegisterComboBox<long, XUiC_ComboBoxInt>(
                "uiTemperature",
                UiTemperature_OnValueChanged,
                "uiTemperature");
            uiIntensity = RegisterComboBox<double, XUiC_ComboBoxFloat>(
                "uiIntensity",
                UiIntensity_OnValueChanged,
                "uiIntensity");
            uiBeamAngle = RegisterComboBox<double, XUiC_ComboBoxFloat>(
                "uiBeamAngle",
                UiBeamAngle_OnValueChanged,
                "uiBeamAngle");
            uiRange = RegisterComboBox<double, XUiC_ComboBoxFloat>(
                "uiRange",
                UiRange_OnValueChanged,
                "uiRange");
            uiColorPicker = (XUiC_ColorPicker)GetChildById("uiColorPicker");
            if (uiColorPicker != null)
            {
                uiColorPicker.OnSelectedColorChanged += UiColorPicker_OnValueChanged;
            }
            else
            {
                Debug.LogWarning("ElectricityLampsStats missing uiColorPicker");
            }
        }

        // Updates the lamp delay when the delay combo box changes.
        private void CbxDelay_OnValueChanged(XUiController _sender, double _oldValue, double _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.Delay = (float)_newValue);
        }

        // Updates the lamp mode when the light state combo box changes.
        private void CbxLightState_OnValueChanged(XUiController _sender, LightStateType _oldValue, LightStateType _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.LightState = _newValue);
            ApplyVisibilityToUi();
        }

        // Updates the lamp blinking rate when the rate combo box changes.
        private void CbxRate_OnValueChanged(XUiController _sender, double _oldValue, double _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.Rate = (float)_newValue);
        }

        // Updates the lamp color when the color picker changes.
        private void UiColorPicker_OnValueChanged(Color _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.LightColor = _newValue);
        }

        // Updates the spotlight beam angle when the beam angle combo box changes.
        private void UiBeamAngle_OnValueChanged(XUiController _sender, double _oldValue, double _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.LightAngle = (float)_newValue);
        }

        // Updates the lamp range when the range combo box changes.
        private void UiRange_OnValueChanged(XUiController _sender, double _oldValue, double _newValue)
        {
            UpdateTileEntityLight(tileEntity =>
            {
                tileEntity.LightRange = (float)_newValue;
                tileEntity.UpdateDynamicRequiredPower();    // range changes affect power requirements, so update power requirements as well
                tileEntity.UpdateLightState();              // updates visuals + logic
            });
            ApplyPowerWarningColors();
            //GameManager.Instance.StartCoroutine(ApplyPowerWarningColorsDelayed());
        }

        // Updates the lamp intensity when the intensity combo box changes.
        private void UiIntensity_OnValueChanged(XUiController _sender, double _oldValue, double _newValue)
        {
            UpdateTileEntityLight(tileEntity =>
            {
                tileEntity.LightIntensity = (float)_newValue;
                tileEntity.UpdateDynamicRequiredPower();    // intensity changes affect power requirements, so update power requirements as well
                tileEntity.UpdateLightState();              // updates visuals + logic
            });
            ApplyPowerWarningColors();
            //GameManager.Instance.StartCoroutine(ApplyPowerWarningColorsDelayed());
        }

        // Updates the Kelvin temperature when the temperature combo box changes.
        private void UiTemperature_OnValueChanged(XUiController _sender, long _oldValue, long _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.LightKelvin = (ushort)_newValue);
        }

        // Switches between Kelvin and RGB color mode.
        private void UiUseKelvin_OnValueChanged(XUiController _sender, bool _oldValue, bool _newValue)
        {
            UpdateTileEntityLight(tileEntity => tileEntity.IsKelvinScale = _newValue);
            ApplyVisibilityToUi();
        }

        // Reads a block property and returns a fallback value when the property is missing.
        public string GetBlockProperty(string name, string fallback)
        {
            var properties = Block.list[BlockType].Properties;
            if (properties == null || !properties.Values.ContainsKey(name))
            {
                return fallback;
            }
            return properties.Values[name];
        }

        // Applies min, max, and step values from blocks.xml directly to the float controls.
        private void ApplyBlockPropertyLimitsToUi()
        {
            if (this.uiIntensity != null)
            {
                this.uiIntensity.Min = GetBlockPropertyDouble("LightMinIntensity", 0.2);
                this.uiIntensity.Max = GetBlockPropertyDouble("LightMaxIntensity", 8.0);
                this.uiIntensity.IncrementSize = GetBlockPropertyDouble("LightIntensityStep", 0.1);
            }
            if (this.uiRange != null)
            {
                this.uiRange.Min = GetBlockPropertyDouble("LightMinRange", 0.0);
                this.uiRange.Max = GetBlockPropertyDouble("LightMaxRange", 50.0);
                this.uiRange.IncrementSize = GetBlockPropertyDouble("LightRangeStep", 1.0);
            }
            if (this.uiBeamAngle != null)
            {
                this.uiBeamAngle.Min = GetBlockPropertyDouble("LightMinAngle", 30.0);
                this.uiBeamAngle.Max = GetBlockPropertyDouble("LightMaxAngle", 180.0);
                this.uiBeamAngle.IncrementSize = GetBlockPropertyDouble("LightAngleStep", 3.0);
            }
        }

        // Reads a double value from the current block's properties.
        private double GetBlockPropertyDouble(string propertyName, double fallback)
        {
            return StringParsers.ParseDouble(
                GetBlockProperty(propertyName, fallback.ToCultureInvariantString()),
                0,
                -1,
                NumberStyles.Any
            );
        }

        // Refreshes UI bindings while the lamp settings window is active.
        public override void Update(float _dt)
        {
            if (GameManager.Instance == null || GameManager.Instance.World == null || tileEntity == null)
            {
                return;
            }
            base.Update(_dt);
            RefreshBindings(false);
        }

        // Shows or hides certain UI controls based on the current lamp mode and settings.
        private void ApplyVisibilityToUi()
        {
            bool isKelvinScale = this.TileEntity != null && this.TileEntity.IsKelvinScale;
            bool isColorScale = this.TileEntity != null && this.TileEntity.IsColorScale;
            bool isSpotLight = this.TileEntity != null && this.TileEntity.IsSpotLight;
            bool isNotStatic = this.TileEntity != null && this.TileEntity.LightState != LightStateType.Static;

            XUiController colorPicker = GetChildById("uiColorPicker");
            if (colorPicker != null && colorPicker.ViewComponent != null)
                colorPicker.ViewComponent.IsVisible = isColorScale;

            XUiController temperaturePanel = GetChildById("panelTemperature");
            if (temperaturePanel != null && temperaturePanel.ViewComponent != null)
                temperaturePanel.ViewComponent.IsVisible = isKelvinScale;

            XUiController beamAnglePanel = GetChildById("panelBeamAngle");
            if (beamAnglePanel != null && beamAnglePanel.ViewComponent != null)
                beamAnglePanel.ViewComponent.IsVisible = isSpotLight;

            // Hide rate and delay panels when light state is static
            XUiController ratePanel = GetChildById("panelRate");
            if (ratePanel != null && ratePanel.ViewComponent != null)
                ratePanel.ViewComponent.IsVisible = isNotStatic;

            XUiController delayPanel = GetChildById("panelDelay");
            if (delayPanel != null && delayPanel.ViewComponent != null)
                delayPanel.ViewComponent.IsVisible = isNotStatic;
        }

        // Loads the current lamp values into the UI when the window opens.
        public override void OnOpen()
        {
            base.OnOpen();
            // Debug.Log("ElectricityLampsStats OnOpen | TileEntity: " + (this.TileEntity != null));
            if (this.TileEntity == null)
            {
                return;
            }
            //Debug.Log("LightRange: " + this.TileEntity.LightRange + " | LightIntensity: " + this.TileEntity.LightIntensity);
            //Debug.Log("BlockType: " + this.TileEntity.GetChunk().GetBlock(this.TileEntity.localChunkPos).type);
            BlockType = this.TileEntity.GetChunk().GetBlock(this.TileEntity.localChunkPos).type;
            ApplyBlockPropertyLimitsToUi();

            //ApplyPowerLimitsToSliders();
            ApplyPowerWarningColors();

            ApplyTileEntityValuesToUi();
            ApplyVisibilityToUi();
            this.TileEntity.SetUserAccessing(true);
            this.TileEntity.SetModified();
            RefreshBindings(false);
        }

        // Saves UI changes and unlocks the tile entity when the window closes.
        public override void OnClose()
        {
            GameManager instance = GameManager.Instance;
            Vector3i worldPosition = tileEntity.ToWorldPos();

            if (!XUiC_CameraWindow.hackyIsOpeningMaximizedWindow)
            {
                tileEntity.SetUserAccessing(false);

                if (GetChildById("uiColorPicker") is XUiC_ColorPicker colorPicker)
                {
                    tileEntity.LightColor = colorPicker.SelectedColor;
                }
                instance.TEUnlockServer(tileEntity.GetClrIdx(), worldPosition, tileEntity.entityId, true);
                tileEntity.SetModified();
                powerItem = null;
            }

            base.OnClose();
        }

        // Registers a combo box and logs a warning if the control is missing.
        private TComboBox RegisterComboBox<TValue, TComboBox>(
            string childId,
            XUiC_ComboBox<TValue>.XUiEvent_ValueChanged valueChangedHandler,
            string warningName)
            where TComboBox : XUiC_ComboBox<TValue>
        {
            TComboBox comboBox = (TComboBox)GetChildById(childId);
            if (comboBox != null)
            {
                comboBox.OnValueChanged += valueChangedHandler;
            }
            else
            {
                Debug.LogWarning($"ElectricityLampsStats missing {warningName}");
            }
            return comboBox;
        }

        // Applies a change to the tile entity, updates the visible light, and refreshes UI bindings.
        private void UpdateTileEntityLight(System.Action<TileEntityElectricityLightBlock> applyChange)
        {
            if (TileEntity != null)
            {
                applyChange(TileEntity);

                BlockEntityData blockEntity = TileEntity.GetChunk().GetBlockEntity(TileEntity.ToWorldPos());
                if (blockEntity != null)
                {
                    TileEntity.UpdateLightState(blockEntity);
                }
            }
            RefreshBindings(false);
        }

        // Copies the tile entity state into the matching UI controls.
        private void ApplyTileEntityValuesToUi()
        {
            if (CbxLightState != null)
            {
                CbxLightState.Value = tileEntity.LightState;
            }
            if (cbxRate != null)
            {
                cbxRate.Value = tileEntity.Rate;
            }
            if (cbxDelay != null)
            {
                cbxDelay.Value = tileEntity.Delay;
            }
            if (uiColorPicker != null)
            {
                uiColorPicker.SelectedColor = tileEntity.LightColor;
            }
            if (uiUseKelvin != null)
            {
                uiUseKelvin.Value = TileEntity.IsKelvinScale;
            }
            if (uiIntensity != null)
            {
                uiIntensity.Value = tileEntity.LightIntensity;
            }
            if (uiTemperature != null)
            {
                uiTemperature.Value = tileEntity.LightKelvin;
            }
            if (uiRange != null)
            {
                uiRange.Value = tileEntity.LightRange;
            }
            if (uiBeamAngle != null)
            {
                uiBeamAngle.Value = tileEntity.LightAngle;
            }
        }

        // Converts a bool to the lowercase string expected by XUi bindings.
        private static string BoolToBindingValue(bool value)
        {
            return value ? "true" : "false";
        }

        // Returns the remaining power headroom available on the network in watts.
        private int GetAvailablePowerHeadroom()
        {
            if (this.TileEntity == null)
            {
                //Debug.Log("[ElectricityLamps] Headroom: TileEntity is null");
                return int.MaxValue;
            }

            PowerConsumer powerConsumer = this.TileEntity.PowerItem as PowerConsumerToggle
                                       ?? this.TileEntity.PowerItem as PowerConsumer;
            if (powerConsumer == null)
            {
                //Debug.Log("[ElectricityLamps] Headroom: PowerConsumer is null");
                return int.MaxValue;
            }

            if (powerConsumer.Parent == null)
            {
                //Debug.Log("[ElectricityLamps] Headroom: Parent is null");
                return int.MaxValue;
            }

            // Walk up the parent chain until we find a PowerSource
            PowerItem current = powerConsumer.Parent;
            PowerSource source = null;
            while (current != null)
            {
                if (current is PowerSource ps)
                {
                    source = ps;
                    break;
                }
                current = current.Parent;
            }

            if (source == null)
            {
                //Debug.Log("[ElectricityLamps] Headroom: No PowerSource found in parent chain");
                return int.MaxValue;
            }

            // Walk up to root
            while (source.Parent is PowerSource parentSource)
                source = parentSource;

            //Debug.Log($"[ElectricityLamps] Root type: {source.GetType().Name}");

            // Try OCB path via reflection
            var sourceType = source.GetType();
            var maxGridField = sourceType.GetField("MaxGridProduction");
            var gridDemandField = sourceType.GetField("GridConsumerDemand");
            var consumerDemandField = sourceType.GetField("ConsumerDemand");

            //Debug.Log($"[ElectricityLamps] OCB fields found: MaxGridProduction={maxGridField != null}, GridConsumerDemand={gridDemandField != null}, ConsumerDemand={consumerDemandField != null}");

            if (maxGridField != null && gridDemandField != null && consumerDemandField != null)
            {
                int maxGrid = (int)(ushort)maxGridField.GetValue(source);
                int gridDemand = (int)(ushort)gridDemandField.GetValue(source);
                int consumerDemand = (int)(ushort)consumerDemandField.GetValue(source);
                int totalDemand = gridDemand + consumerDemand;
                int otherDemand = totalDemand - (int)powerConsumer.RequiredPower;
                int headroom = maxGrid - otherDemand - (int)this.TileEntity.PowerUsed;
                //Debug.Log($"[ElectricityLamps] OCB path: MaxGrid={maxGrid}, GridDemand={gridDemand}, ConsumerDemand={consumerDemand}, RequiredPower={powerConsumer.RequiredPower}, PowerUsed={this.TileEntity.PowerUsed}, Headroom={headroom}");
                return headroom;
            }

            // Vanilla path
            int otherConsumers = (int)source.LastPowerUsed - (int)powerConsumer.RequiredPower;
            int vanillaHeadroom = (int)source.MaxOutput - otherConsumers - (int)this.TileEntity.PowerUsed;
            //Debug.Log($"[ElectricityLamps] Vanilla path: MaxOutput={source.MaxOutput}, LastPowerUsed={source.LastPowerUsed}, RequiredPower={powerConsumer.RequiredPower}, PowerUsed={this.TileEntity.PowerUsed}, Headroom={vanillaHeadroom}");
            return vanillaHeadroom;
        }

        // Colors the intensity and range labels red when power consumption exceeds
        // the available network headroom, restoring white when within limits.
        private void ApplyPowerWarningColors()
        {
            int headroom = GetAvailablePowerHeadroom();

            //Debug.Log($"[ElectricityLamps] Headroom: {headroom}, PowerUsed: {this.TileEntity?.PowerUsed ?? 0}");

            // Warn when there is no headroom left (network is at or over capacity)
            bool isExceeding = headroom != int.MaxValue && headroom <= 0;
            Color warningColor = isExceeding ? Color.red : Color.white;

            XUiV_Label lblIntensity = GetChildById("lblIntensity")?.ViewComponent as XUiV_Label;
            if (lblIntensity != null)
                lblIntensity.Color = warningColor;

            XUiV_Label lblRange = GetChildById("lblRange")?.ViewComponent as XUiV_Label;
            if (lblRange != null)
                lblRange.Color = warningColor;

            // Show or hide the power warning label
            XUiController lblPowerWarning = GetChildById("lblPowerWarning");
            if (lblPowerWarning?.ViewComponent != null)
                lblPowerWarning.ViewComponent.IsVisible = isExceeding;
        }

        // Delays the power warning color update by one frame to allow the power
        // network to recalculate LastPowerUsed before we read it.
        private System.Collections.IEnumerator ApplyPowerWarningColorsDelayed()
        {
            yield return null;
            ApplyPowerWarningColors();
        }

        private int BlockType;
        private TileEntityElectricityLightBlock tileEntity;
        private PowerItem powerItem;
        private XUiC_ComboBoxEnum<LightStateType> CbxLightState;
        private XUiC_ComboBoxFloat cbxRate;
        private XUiC_ComboBoxFloat cbxDelay;
        private XUiC_ColorPicker uiColorPicker;
        private XUiC_ComboBoxBool uiUseKelvin;
        private XUiC_ComboBoxInt uiTemperature;
        private XUiC_ComboBoxFloat uiIntensity;
        private XUiC_ComboBoxFloat uiBeamAngle;
        private XUiC_ComboBoxFloat uiRange;
    }
}
