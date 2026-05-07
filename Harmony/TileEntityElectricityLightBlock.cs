using System.Globalization;
using System.IO;
using UnityEngine;

namespace ColoredLights_Port
{
    // Custom powered tile entity for electricity-based light blocks.
    // Stores configurable light values such as color, Kelvin temperature, range,
    // intensity, angle, light mode and animation state, then applies them to
    // the block’s Unity light components when the block is updated or powered.
    public class TileEntityElectricityLightBlock : TileEntityPoweredBlock
    {
        public byte LightMode
        {
            get
            {
                return (byte)this.lightMode;
            }
        }

        public bool IsKelvinScale
        {
            get
            {
                return (this.lightMode & 1) == 1;
            }
            set
            {
                if (value)
                {
                    this.lightMode |= 1;
                }
                else
                {
                    this.lightMode &= -2;
                }
            }
        }

        public bool IsColorScale
        {
            get
            {
                return (this.lightMode & 1) != 1;
            }
        }

        public bool IsSpotLight
        {
            get
            {
                return (this.lightMode & 2) == 2;
            }
        }

        public bool IsPointLight
        {
            get
            {
                return (this.lightMode & 2) != 2;
            }
        }

        public float LightIntensity
        {
            get
            {
                return this.lightIntensity;
            }
            set
            {
                this.lightIntensity = value;
                this.UpdateLightState();
            }
        }

        public float LightRange
        {
            get
            {
                return this.lightRange;
            }
            set
            {
                this.lightRange = value;
                this.UpdateLightState();
            }
        }

        public ushort LightKelvin
        {
            get
            {
                return this.lightKelvin;
            }
            set
            {
                this.lightKelvin = value;
                this.UpdateLightState();
            }
        }

        public Color LightColor
        {
            get
            {
                return this.lightColor;
            }
            set
            {
                this.lightColor = value;
                this.UpdateLightState();
            }
        }

        public float LightAngle
        {
            get
            {
                return this.lightAngle;
            }
            set
            {
                this.lightAngle = value;
                this.UpdateLightState();
            }
        }

        // Creates a copy of this custom electricity light tile entity.
        public override TileEntity Clone()
        {
            return new TileEntityElectricityLightBlock(this.chunk)
            {
                lightMode = this.lightMode,
                lightIntensity = this.lightIntensity,
                lightRange = this.lightRange,
                lightColor = this.lightColor,
                lightKelvin = this.lightKelvin,
                lightAngle = this.lightAngle,
                lightRotationRa = this.lightRotationRa,
                lightRotationDec = this.lightRotationDec,
                isLightReRotated = this.isLightReRotated,
                LightState = this.LightState,
                Rate = this.Rate,
                Delay = this.Delay
            };
        }

        // Copies light settings from either a vanilla light tile entity or another custom electricity light tile entity.
        public override void CopyFrom(TileEntity _other)
        {
            TileEntityLight tileEntityLight = _other as TileEntityLight;
            bool isVanillaLightTileEntity = tileEntityLight != null;
            if (isVanillaLightTileEntity)
            {
                this.lightMode = ((tileEntityLight.LightType == LightType.Spot) ? 2 : 0);
                this.lightIntensity = tileEntityLight.LightIntensity;
                this.lightRange = tileEntityLight.LightRange;
                this.lightColor = tileEntityLight.LightColor;
                this.lightKelvin = TileEntityElectricityLightBlock.defKelvin;
                this.lightAngle = tileEntityLight.LightAngle;
                this.lightRotationRa = 0f;
                this.lightRotationDec = 0f;
                this.isLightReRotated = false;
                this.LightState = tileEntityLight.LightState;
                this.Rate = tileEntityLight.Rate;
                this.Delay = tileEntityLight.Delay;
            }
            else
            {
                TileEntityElectricityLightBlock tileEntityElectricityLightBlock = _other as TileEntityElectricityLightBlock;
                bool isElectricityLightTileEntity = tileEntityElectricityLightBlock != null;

                if (isElectricityLightTileEntity)
                {
                    this.lightMode = tileEntityElectricityLightBlock.lightMode;
                    this.lightIntensity = tileEntityElectricityLightBlock.LightIntensity;
                    this.lightRange = tileEntityElectricityLightBlock.LightRange;
                    this.lightColor = tileEntityElectricityLightBlock.LightColor;
                    this.lightKelvin = tileEntityElectricityLightBlock.lightKelvin;
                    this.lightAngle = tileEntityElectricityLightBlock.lightAngle;
                    this.lightRotationRa = tileEntityElectricityLightBlock.lightRotationRa;
                    this.lightRotationDec = tileEntityElectricityLightBlock.lightRotationDec;
                    this.isLightReRotated = tileEntityElectricityLightBlock.isLightReRotated;
                    this.LightState = tileEntityElectricityLightBlock.LightState;
                    this.Rate = tileEntityElectricityLightBlock.Rate;
                    this.Delay = tileEntityElectricityLightBlock.Delay;
                }
            }
        }

        // Converts a Kelvin temperature value into an RGB Unity color.
        public static Color KelvinToColor(ushort kelvin)
        {
            float temperature = (float)kelvin / 100f;
            bool isWarmOrNeutralTemperature = temperature <= 66f;
            float red;
            float green;
            float blue;
            if (isWarmOrNeutralTemperature)
            {
                red = 255f;
                green = temperature;
                green = 99.4708f * Mathf.Log(green) - 161.11957f;
                bool isVeryWarmTemperature = temperature <= 19f;
                if (isVeryWarmTemperature)
                {
                    blue = 0f;
                }
                else
                {
                    blue = temperature - 10f;
                    blue = 138.51773f * Mathf.Log(blue) - 305.0448f;
                }
            }
            else
            {
                red = temperature - 60f;
                red = 329.69873f * Mathf.Pow(red, -0.13320476f);
                green = temperature - 60f;
                green = 288.12216f * Mathf.Pow(green, -0.075514846f);
                blue = 255f;
            }
            return new Color(Mathf.Clamp(red / 255f, 0f, 1f), Mathf.Clamp(green / 255f, 0f, 1f), Mathf.Clamp(blue / 255f, 0f, 1f));
        }

        // Creates a new custom electricity light tile entity for the given chunk.
        public TileEntityElectricityLightBlock(Chunk _chunk)
            : base(_chunk)
        {
        }

        // Reads light orientation values from the block definition and updates the light state.
        public override void SetValuesFromBlock(ushort blockID)
        {
            base.SetValuesFromBlock(blockID);
            DynamicProperties properties = Block.list[(int)blockID].Properties;
            this.isLightReRotated = properties.Values.ContainsKey("LightOrientation");
            this.lightOrientation = ((!this.isLightReRotated) ? TileEntityElectricityLightBlock.nullVector : StringParsers.ParseVector3(properties.Values["LightOrientation"], 0, -1));
            this.UpdateLightState();
            //this.UpdateDynamicRequiredPower();
        }

        // Loads default light values from block XML properties.
        public void PresetDefaultValues(int blockID)
        {
            DynamicProperties properties = Block.list[blockID].Properties;
            this.lightKelvin = ((!properties.Values.ContainsKey("LightKelvin")) ? TileEntityElectricityLightBlock.defKelvin : StringParsers.ParseUInt16(properties.Values["LightKelvin"], 0, -1, NumberStyles.Integer));
            this.lightColor = ((!properties.Values.ContainsKey("LightColor")) ? TileEntityElectricityLightBlock.defColor : StringParsers.ParseColor(properties.Values["LightColor"]));
            this.lightIntensity = ((!properties.Values.ContainsKey("LightIntensity")) ? 1f : StringParsers.ParseFloat(properties.Values["LightIntensity"], 0, -1, NumberStyles.Any));
            this.lightRange = ((!properties.Values.ContainsKey("LightRange")) ? 1f : StringParsers.ParseFloat(properties.Values["LightRange"], 0, -1, NumberStyles.Any));
            this.lightAngle = ((!properties.Values.ContainsKey("LightAngle")) ? 60f : StringParsers.ParseFloat(properties.Values["LightAngle"], 0, -1, NumberStyles.Any));
            this.lightMode = (int)((!properties.Values.ContainsKey("LightMode")) ? 0 : StringParsers.ParseUInt8(properties.Values["LightMode"], 0, -1, NumberStyles.Integer));
            this.UpdateLightState();
        }

        // Returns the custom tile entity type used by this mod.
        public override TileEntityType GetTileEntityType()
        {
            return (TileEntityType)244;
        }

        // Applies the configured light values to a LightLOD component.
        private void UpdateLightLOD(LightLOD lod)
        {
            Color color = this.LightColor;
            bool usesKelvinColor = this.IsKelvinScale;
            if (usesKelvinColor)
            {
                color = TileEntityElectricityLightBlock.KelvinToColor(this.LightKelvin);
            }
            lod.EmissiveColor = color * this.LightIntensity;
            lod.MaxIntensity = this.LightIntensity;
            lod.LightStateType = this.LightState;
            lod.StateRate = this.Rate;
            lod.FluxDelay = this.Delay;
            Light light = lod.GetLight();
            bool hasUnityLight = light != null;
            if (hasUnityLight)
            {
                lod.SetRange(this.LightRange);
                light.spotAngle = this.LightAngle;
                light.color = color;
                light.type = (this.IsPointLight ? LightType.Point : LightType.Spot);
                BlockValue blockValue = base.blockValue;
                bool hasValidBlockValue = blockValue.type != BlockValue.Air.type;
                if (hasValidBlockValue)
                {
                    DynamicProperties properties = Block.list[blockValue.type].Properties;
                    bool hasLightShadowProperty = properties.Values.ContainsKey("LightShadow");
                    if (hasLightShadowProperty)
                    {
                        light.shadows = (properties.Values["LightShadow"].Equals("Soft") ? LightShadows.Soft : LightShadows.Hard);
                    }
                    bool hasLightShadowBiasProperty = properties.Values.ContainsKey("LightShadowBias");
                    if (hasLightShadowBiasProperty)
                    {
                        light.shadowBias = StringParsers.ParseFloat(properties.Values["LightShadowBias"], 0, -1, NumberStyles.Any);
                    }
                    bool hasLightShadowStrengthProperty = properties.Values.ContainsKey("LightShadowStrength");
                    if (hasLightShadowStrengthProperty)
                    {
                        light.shadowStrength = StringParsers.ParseFloat(properties.Values["LightShadowStrength"], 0, -1, NumberStyles.Any);
                    }
                    bool hasLightPositionProperty = properties.Values.ContainsKey("LightPosition");
                    if (hasLightPositionProperty)
                    {
                        light.transform.localPosition = StringParsers.ParseVector3(properties.Values["LightPosition"], 0, -1);
                    }
                    bool hasLightShadowNearPlaneProperty = properties.Values.ContainsKey("LightShadowNearPlane");
                    if (hasLightShadowNearPlaneProperty)
                    {
                        light.shadowNearPlane = StringParsers.ParseFloat(properties.Values["LightShadowNearPlane"], 0, -1, NumberStyles.Any);
                    }
                }
            }
        }

        // Periodically refreshes the light state while the tile entity is loaded.
        public override void UpdateTick(World world)
        {
            bool isChunkMissing = this.chunk == null;
            if (!isChunkMissing)
            {
                bool shouldRefreshLightState = Time.fixedTime > this.nextTickCheck;

                if (shouldRefreshLightState)
                {
                    this.UpdateLightState();
                    this.nextTickCheck = Time.fixedTime + 0.1f + world.GetGameRandom().RandomRange(90f);
                }
            }
        }

        // Finds the block entity in the world and updates its visual light state.
        public void UpdateLightState()
        {
            bool isChunkMissing = this.chunk == null;
            if (!isChunkMissing)
            {
                BlockEntityData blockEntity = this.chunk.GetBlockEntity(base.ToWorldPos());
                bool hasBlockEntity = blockEntity != null;

                if (hasBlockEntity)
                {
                    this.UpdateLightState(blockEntity);
                }
            }
        }

        // Updates all visual light objects and emission materials on the block entity.
        public void UpdateLightState(BlockEntityData blockEntity)
        {
            bool isBlockEntityMissing = blockEntity == null;

            if (!isBlockEntityMissing)
            {
                bool isTransformMissing = blockEntity.transform == null;

                if (!isTransformMissing)
                {
                    bool shouldLightBeOn = (this.IsPoweredPOI || base.IsPowered) && base.IsPowered;
                    bool shouldCheckClaimState = !GameManager.IsDedicatedServer && this.IsPoweredPOI;

                    if (shouldCheckClaimState)
                    {
                        this.IsClaimed = GameManager.Instance.World.IsMyLandProtectedBlock(base.ToWorldPos(), GameManager.Instance.World.GetGameManager().GetPersistentLocalPlayer(), false);

                        bool isClaimedButNotPowered = this.IsClaimed && !base.IsPowered;
                        if (isClaimedButNotPowered)
                        {
                            shouldLightBeOn = false;
                        }
                    }

                    Color color = this.LightColor;

                    bool usesKelvinColor = this.IsKelvinScale;
                    if (usesKelvinColor)
                    {
                        color = TileEntityElectricityLightBlock.KelvinToColor(this.LightKelvin);
                    }

                    float lightRange = this.LightRange;
                    float lightAngle = this.LightAngle;
                    float lightIntensity = this.LightIntensity;

                    Transform mainLightTransform = blockEntity.transform.Find("MainLight");
                    bool hasMainLightTransform = mainLightTransform != null;

                    if (hasMainLightTransform)
                    {
                        bool shouldApplyCustomLightOrientation = this.isLightReRotated;
                        if (shouldApplyCustomLightOrientation)
                        {
                            mainLightTransform.localEulerAngles = this.lightOrientation;
                        }

                        LightLOD mainLightLod = mainLightTransform.GetComponent<LightLOD>();
                        bool hasMainLightLod = mainLightLod != null;

                        if (hasMainLightLod)
                        {
                            this.UpdateLightLOD(mainLightLod);
                            mainLightLod.SwitchOnOff(shouldLightBeOn);
                        }
                    }

                    Transform separatedLensFlareTransform = blockEntity.transform.Find("SeparatedLensFlare");
                    bool hasSeparatedLensFlareTransform = separatedLensFlareTransform != null;

                    if (hasSeparatedLensFlareTransform)
                    {
                        LightLOD separatedLensFlareLod = separatedLensFlareTransform.GetComponent<LightLOD>();
                        bool hasSeparatedLensFlareLod = separatedLensFlareLod != null;

                        if (hasSeparatedLensFlareLod)
                        {
                            separatedLensFlareLod.SwitchOnOff(shouldLightBeOn);
                        }
                    }

                    Transform bulbGlowTransform = blockEntity.transform.Find("BulbGlow");
                    bool hasBulbGlowTransform = bulbGlowTransform != null;

                    if (hasBulbGlowTransform)
                    {
                        MeshRenderer bulbGlowRenderer = bulbGlowTransform.GetComponent<MeshRenderer>();
                        bool hasBulbGlowRenderer = bulbGlowRenderer != null;

                        if (hasBulbGlowRenderer)
                        {
                            bool hasBulbGlowMaterial = bulbGlowRenderer.material != null;

                            if (hasBulbGlowMaterial)
                            {
                                bulbGlowRenderer.material.SetColor("_EmissionColor", color * lightIntensity * 1.5f);

                                bool shouldEnableEmission = shouldLightBeOn;
                                if (shouldEnableEmission)
                                {
                                    bulbGlowRenderer.material.EnableKeyword("_EMISSION");
                                }
                                else
                                {
                                    bulbGlowRenderer.material.DisableKeyword("_EMISSION");
                                }
                            }

                            bulbGlowRenderer.enabled = true;
                        }
                    }

                    Transform extraPointLightTransform = blockEntity.transform.Find("ExtraPointLight");
                    bool hasExtraPointLightTransform = extraPointLightTransform != null;

                    if (hasExtraPointLightTransform)
                    {
                        bool shouldWarnAboutExtraPointLight = TileEntityElectricityLightBlock.warnOnce;
                        if (shouldWarnAboutExtraPointLight)
                        {
                            Debug.LogWarning("LightLOD => Light Model has ExtraPointLight");
                        }

                        LightLOD extraPointLightLod = extraPointLightTransform.GetComponent<LightLOD>();
                        bool hasExtraPointLightLod = extraPointLightLod != null;

                        if (hasExtraPointLightLod)
                        {
                            bool shouldApplyCustomLightOrientation = this.isLightReRotated;
                            if (shouldApplyCustomLightOrientation)
                            {
                                extraPointLightTransform.localEulerAngles = this.lightOrientation;
                            }

                            this.UpdateLightLOD(extraPointLightLod);
                            extraPointLightLod.SwitchOnOff(shouldLightBeOn);
                        }

                        TileEntityElectricityLightBlock.warnOnce = false;
                    }

                    Transform pointLightTransform = blockEntity.transform.Find("Point light");
                    bool hasPointLightTransform = pointLightTransform != null;

                    if (hasPointLightTransform)
                    {
                        bool shouldWarnAboutPointLight = TileEntityElectricityLightBlock.warnOnce;
                        if (shouldWarnAboutPointLight)
                        {
                            Debug.LogWarning("LightLOD => Light Model has Point Light");
                        }

                        LightLOD pointLightLod = pointLightTransform.GetComponent<LightLOD>();
                        bool hasPointLightLod = pointLightLod != null;

                        if (hasPointLightLod)
                        {
                            bool shouldApplyCustomLightOrientation = this.isLightReRotated;
                            if (shouldApplyCustomLightOrientation)
                            {
                                pointLightTransform.localEulerAngles = this.lightOrientation;
                            }

                            this.UpdateLightLOD(pointLightLod);
                            pointLightLod.SwitchOnOff(shouldLightBeOn);
                        }

                        TileEntityElectricityLightBlock.warnOnce = false;
                    }
                }
            }
        }

        // Reads saved light data from the binary stream.
        public override void read(PooledBinaryReader _br, TileEntity.StreamModeRead _eStreamMode)
        {
            base.read(_br, _eStreamMode);
            BinaryReader br = _br;
            if (_eStreamMode == 0)
            {
                br.ReadByte();
            }
            this.lightMode = (int)br.ReadByte();
            this.lightRange = br.ReadSingle();
            this.lightIntensity = br.ReadSingle();
            this.lightKelvin = br.ReadUInt16();
            this.lightColor = StreamUtils.ReadColor32(_br);
            this.LightState = (LightStateType)br.ReadByte();
            this.Rate = br.ReadSingle();
            this.Delay = br.ReadSingle();
            if (this.IsSpotLight)
            {
                this.lightAngle = br.ReadSingle();
                this.lightRotationRa = br.ReadSingle();
                this.lightRotationDec = br.ReadSingle();
            }
            this.UpdateLightState();
        }

        // Writes light data to the binary stream for saving or synchronization.
        public override void write(PooledBinaryWriter _bw, TileEntity.StreamModeWrite _eStreamMode)
        {
            base.write(_bw, _eStreamMode);
            BinaryWriter bw = _bw;
            if (_eStreamMode == 0)
            {
                bw.Write((byte)0);
            }
            bw.Write(this.LightMode);
            bw.Write(this.lightRange);
            bw.Write(this.LightIntensity);
            bw.Write(this.LightKelvin);
            StreamUtils.WriteColor32(_bw, this.LightColor);
            bw.Write((byte)this.LightState);
            bw.Write(this.Rate);
            bw.Write(this.Delay);
            if (this.IsSpotLight)
            {
                bw.Write(this.lightAngle);
                bw.Write(this.lightRotationRa);
                bw.Write(this.lightRotationDec);
            }
        }

        // Creates the power item used by this powered tile entity.
        public override PowerItem CreatePowerItem()
        {
            return base.CreatePowerItem();
        }

        // Marks the tile entity as modified and refreshes the light state.
        public override void setModified()
        {
            base.setModified();
            this.UpdateLightState();
        }

        // Calculates the power usage based on the light's intensity and range, returning 0 if the light is not toggled on.
        public override int PowerUsed
        {
            get
            {
                if (!this.IsToggled)
                {
                    return 0;
                }

                float intensityFactor = Mathf.Max(0.1f, this.LightIntensity);
                float rangeFactor = Mathf.Max(0.1f, this.LightRange / 15f);

                int calculatedPower = Mathf.CeilToInt(base.PowerUsed * intensityFactor * rangeFactor);

                return Mathf.Max(1, calculatedPower);
            }
        }

        public void UpdateDynamicRequiredPower()
        {
            PowerConsumer powerConsumer = this.PowerItem as PowerConsumerToggle
                                       ?? this.PowerItem as PowerConsumer;
            if (powerConsumer != null)
            {
                ushort newPower = (ushort)this.PowerUsed;
                if (powerConsumer.RequiredPower != newPower)
                {
                    powerConsumer.RequiredPower = newPower;
                    this.SetModified();

                    // Force the power network to recalculate by simulating a toggle
                    if (powerConsumer is PowerConsumerToggle toggle)
                    {
                        toggle.IsToggled = toggle.IsToggled;
                    }
                }
            }
        }

        private static ushort defKelvin = 3200;
        private static Color defColor = TileEntityElectricityLightBlock.KelvinToColor(TileEntityElectricityLightBlock.defKelvin);
        private static Vector3 nullVector = new Vector3(0f, 0f, 0f);
        public LightStateType LightState;
        public float Rate = 1f;
        public float Delay = 1f;
        public bool IsClaimed = false;
        public bool IsPoweredPOI = false;
        private float nextTickCheck = 0f;
        private static bool warnOnce = true;
        private int lightMode = 0;
        private float lightIntensity = 1f;
        private float lightRange = 15f;
        private Color lightColor = TileEntityElectricityLightBlock.defColor;
        private ushort lightKelvin = TileEntityElectricityLightBlock.defKelvin;
        private float lightAngle = 60f;
        private float lightRotationRa = 0f;
        private float lightRotationDec = 0f;
        private bool isLightReRotated = false;
        private Vector3 lightOrientation = default(Vector3);
    }
}