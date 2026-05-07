# Electricity Lamps - 7 Days to Die (v2.4) Mod

A port and extension of [OCB's ElectricityLamps mod][1], updated and expanded for game version 2.4.
Adds a large collection of craftable, powered light blocks with full color, intensity, range and beam angle control.

> You need to disable EAC to use this mod!

---

## Features

- **44 craftable light variants** — industrial lights, ceiling lights, spotlights, desk lamps, porch lights, street lights, lanterns, neon signs and more
- **Color control** — set light color via a color picker or by Kelvin temperature
- **Intensity control** — adjust brightness from dim to very bright
- **Range control** — adjust how far the light reaches
- **Beam angle control** — for spotlights, widen or narrow the light cone
- **Dynamic power consumption** — power draw scales with intensity and range settings
- **Power limit warning** — the light editor warns you when you are approaching or exceeding the power source limit
- **All rotations supported** — lights can be placed and rotated freely in any direction
- **Workbench crafting** — all lights are unlocked via the Electrician skill tree at level 25
- **Compatible with OcbElectricityOverhaul** — works as an optional extension on top of that mod

---

## Requirements

- 7 Days to Die **v2.4**
- EAC must be **disabled**
- [0_TFP_Harmony][2] mod installed

### Optional
- [OcbElectricityOverhaul][3] — the mod is compatible with and extends this electricity overhaul

---

## Installation

1. Download the mod
2. Copy the mod folder into your `7 Days To Die\Mods\` directory
3. The folder structure should look like:
```
Mods\
└── ElectricityLamps\
    ├── Config\
    ├── Harmony\
    ├── ItemIcons\
    ├── ElectricityLamps.dll
    └── ModInfo.xml
```
4. Launch the game with EAC disabled

---

## Crafting

All Electricity Lamps are crafted at the **Workbench** and unlocked by reaching **level 25** in the Electrician skill.

The recipe uses a single **Electricity Lamp** item which opens a radial selection menu, letting you choose which light variant to place. Required materials:

- 12x Electrical Parts
- 10x Forged Iron
- 5x Scrap Polymers
- 1x Duct Tape
- 1x Headlight

---

## Light Options

When you interact with a placed light using the **Edit** command, the light options panel opens:

| Option | Description |
|--------|-------------|
| State | Static, Blinking, or Flux mode |
| Rate | Blink/flux rate (hidden when Static) |
| Delay | Blink/flux delay (hidden when Static) |
| Intensity | Brightness of the light |
| Range | How far the light reaches |
| Beam Angle | Cone width (spotlights only) |
| Mode | Switch between Kelvin temperature and color picker |
| Temperature | Color temperature in Kelvin (Kelvin mode) |
| Color Picker | Direct RGB color selection (color mode) |

> Note: only lights inside your land claim can be configured.

### Power Warning
The intensity and range labels turn **red** and a warning message appears when the total power consumption of the light exceeds the available capacity of the connected power source.

---

## Power Consumption

Power draw is calculated dynamically based on the light's current intensity and range settings:

```
PowerUsed = ceil(BasePower * IntensityFactor * RangeFactor)
```

Where `BasePower` is 5W, `IntensityFactor` is the current intensity value, and `RangeFactor` is `range / 15`. Minimum consumption is 1W when the light is on, 0W when off.

---

## Compatibility

| Mod | Status |
|-----|--------|
| OcbElectricityOverhaul | ✅ Compatible |
| Vanilla (no overhaul) | ✅ Compatible |
| Multiplayer | ✅ Compatible (all clients need the mod) |

---

## Known Limitations

- Light bleeds through walls — this is a Unity engine limitation and cannot be fixed in the mod. Shadow casting can be enabled per block via the `LightShadow` property but comes with a performance cost.
- Very advanced power network topologies (deeply chained consumers) may cause the power warning to be slightly imprecise.

---

## Adding Custom Lights

New light variants can be added by extending the `ElectricityLamps` base block in `blocks.xml`:

```xml
<block name="myCustomLamp">
    <property name="Extends" value="ElectricityLamps"/>
    <property name="Model" value="@:Entities/Lighting/myLampPrefab.prefab"/>
    <property name="Shape" value="ModelEntity"/>
    <property name="CustomIcon" value="myCustomLamp"/>
    <property name="CreativeMode" value="Player"/>
</block>
```

All light properties (`LightIntensity`, `LightRange`, `LightMode` etc.) are inherited from the base block and can be overridden per block.

---

## Additional Block Properties

| Property | Description |
|----------|-------------|
| `LightMode` | 1 = point light with Kelvin, 3 = spotlight |
| `LightIntensity` | Default intensity |
| `LightMinIntensity` / `LightMaxIntensity` | Intensity slider limits |
| `LightIntensityStep` | Intensity slider step size |
| `LightRange` | Default range |
| `LightMinRange` / `LightMaxRange` | Range slider limits |
| `LightRangeStep` | Range slider step size |
| `LightAngle` | Default beam angle (spotlights) |
| `LightMinAngle` / `LightMaxAngle` | Beam angle slider limits |
| `LightAngleStep` | Beam angle slider step size |
| `LightKelvin` | Default color temperature |
| `LightShadow` | Shadow mode: `Soft` or `Hard` |
| `LightShadowStrength` | Shadow strength (0.0 - 1.0) |
| `CanAimSpotlight` | Whether the spotlight can be aimed via camera |
| `LightOrientation` | Override light rotation (Vector3) |

---

## Credits

- Original mod by **OCB** — [ElectricityLamps][1]
- Ported and extended by **Nico**

---

## Changelog

### Version 1.0.1
- Fixed unlock icon not showing in workbench
- Fixed power consumption not updating correctly at power source after reload
- Fixed power consumption not updating correctly in chained consumer networks (E-L-L topology)
- Added dynamic power consumption scaling with intensity and range
- Added power limit warning in light editor (red labels and warning message)
- Added Rate and Delay panels hidden when light mode is Static
- Added OcbElectricityOverhaul compatibility for power limit detection
- Picking up a placed light now returns the Electricity Lamps helper item
- All light variants now support free rotation in any direction
- Localization support for English and German

### Version 1.0.0
- Initial port from OCB's ElectricityLamps to game version 2.4

---

[1]: https://github.com/OCB7D2D/ElectricityLamps
[2]: https://github.com/OCB7D2D/OcbCore
[3]: https://github.com/OCB7D2D/OcbElectricityOverhaul
