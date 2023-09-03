# VFX Graph properties selector for Odin

![](Images/dropdown.png)

This attribute allows to draw dropdown with exposed properties from Visual Effect component with ability to filter by type.

## Instalation

Put `OdinVFXProperties` folder into Unity project

## Usage

Just add `VFXPropertySelectorAttribute` attribute to `string` or `ExposedProperty`.

If string to resolve `VisualEffect` is not provided it will try to find one from sibling properties.

## Example

![](Images/preview.PNG)

This example code was used for preview screen above

```cs
[SerializeField] private VisualEffect m_visualEffect;

[SerializeField, VFXPropertySelector(VFXPropertySelectorAttribute.Type.Float)]
private string m_string;

[VFXPropertySelector(VFXPropertySelectorAttribute.Type.Vector3)]
[SerializeField] private ExposedProperty m_exposedProperty;

[VFXPropertySelector(VFXPropertySelectorAttribute.Type.Vector2)]
[SerializeField] private ExposedProperty[] m_visualEffectPropertyArray;
```
