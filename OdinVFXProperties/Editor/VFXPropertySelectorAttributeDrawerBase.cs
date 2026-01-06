using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

public abstract class VFXPropertySelectorAttributeDrawerBase<T> : OdinAttributeDrawer<VFXPropertySelectorAttribute, T>
{
	private ValueResolver<VisualEffect> m_visualEffectResolver;
	private GUIContent m_buttonContent = new GUIContent();

	protected override void Initialize()
	{
		base.Initialize();

		var (propertyToResolve, referenceToResolve) =
			GetValueResolverOverride(Property, Attribute.m_reference, typeof(VisualEffect));

		m_visualEffectResolver = ValueResolver.Get<VisualEffect>(propertyToResolve, referenceToResolve);
		
		UpdateExposedProperties();
		
		UpdateValue();

		Property.ValueEntry.OnValueChanged += ValeChanged;
		Property.ValueEntry.OnChildValueChanged += ValeChanged;
	}

	private void UpdateValue()
	{
		// If value is empty or not of correct type, set first one from exposed values that is valid 
		if (string.IsNullOrWhiteSpace(GetValue()) || m_exposedProperties
			    .Where(x => VFXPropertySelector.IsCorrectType(x.type, Attribute.m_type))
			    .All(x => x.name != GetValue()))
		{
			SetValue(m_exposedProperties
				.FirstOrDefault(x => VFXPropertySelector.IsCorrectType(x.type, Attribute.m_type)).name);
		}

		UpdateButtonContent();
	}

	private void ValeChanged(int obj) => UpdateValue();

	public static (InspectorProperty property, string attribute) 
		GetValueResolverOverride(InspectorProperty property, string reference, Type type)
	{
		var currentProperty = property;

		if (currentProperty.ParentType.IsArray)
			currentProperty = currentProperty.ParentValueProperty;

		var foundReference = reference;
		
		if (!string.IsNullOrWhiteSpace(foundReference) || currentProperty.ParentValueProperty == null)
			return (currentProperty, foundReference);

		// If reference is null or empty, find first property of correct type
		foundReference = FindFirstSiblingProperty(currentProperty, foundReference, type);

		return (currentProperty, foundReference);
	}
	
	public static string FindFirstSiblingProperty(InspectorProperty currentProperty, string foundReference, Type type)
	{
		foreach (var child in currentProperty.ParentValueProperty.Children)
		{
			if (child.ValueEntry == null)
				continue;

			var value = child.ValueEntry.TypeOfValue;

			if (!value.InheritsFrom(type)) 
				continue;

			foundReference = child.Name;
			return foundReference;
		}

		return foundReference;
	}
	
	private static List<VFXExposedProperty> m_exposedProperties = new(1);
	
	private void UpdateButtonContent()
	{
		if (m_exposedProperties.Any(x => x.name == GetValue()))
		{
			var exposedProperty = m_exposedProperties.Find(x => x.name == GetValue());

			m_buttonContent.text = $"{exposedProperty.name.Replace('/', '\\')} ({exposedProperty.type.Name})";
		}
		else
			m_buttonContent.text = GetValue();
	}

	private void UpdateExposedProperties()
	{
		var visualEffect = m_visualEffectResolver.GetValue();

		if (visualEffect == null || visualEffect.visualEffectAsset == null)
			return;

		visualEffect.visualEffectAsset.GetExposedProperties(m_exposedProperties);
	}

	protected override void DrawPropertyLayout(GUIContent label)
	{
		if (m_visualEffectResolver.HasError)
		{
			m_visualEffectResolver.DrawError();
			return;
		}

		var visualEffect = m_visualEffectResolver.GetValue();

		if (visualEffect == null || visualEffect.visualEffectAsset == null)
		{
			SirenixEditorGUI.ErrorMessageBox("Provided VFX is null");
			return;
		}

		VFXPropertySelector.DrawSelectorDropdown(label, m_buttonContent,
			rect => DrawSelector(visualEffect.visualEffectAsset, rect));
	}

	private VFXPropertySelector DrawSelector(VisualEffectAsset visualEffectAsset, Rect rect)
	{
		var selector = new VFXPropertySelector(visualEffectAsset, Attribute.m_type);
		selector.SetSelection(GetValue());
		selector.ShowInPopup(rect.position + rect.height * Vector2.up);

		selector.SelectionConfirmed += x =>
		{
			ValueEntry.Property.Tree.DelayAction(() =>
			{
				SetValue(x.FirstOrDefault());
				
				UpdateExposedProperties();
				UpdateButtonContent();
			});
		};

		return selector;
	}

	protected abstract string GetValue();
	protected abstract void SetValue(string name);
}

public class ExposedPropertyVFXPropertySelectorAttributeDrawer : VFXPropertySelectorAttributeDrawerBase<ExposedProperty>
{
	protected override string GetValue() => ValueEntry.SmartValue.ToString();

	protected override void SetValue(string name) => ValueEntry.SmartValue = name;
}

public class StringVFXPropertySelectorAttributeDrawer : VFXPropertySelectorAttributeDrawerBase<string>
{
	protected override string GetValue() => ValueEntry.SmartValue;

	protected override void SetValue(string name) => ValueEntry.SmartValue = name;
}

public class VFXPropertySelector : OdinSelector<string>
{
	private VisualEffectAsset m_visualEffectAsset;
	private VFXPropertySelectorAttribute.Type m_type;
	
	public static bool IsCorrectType(Type propertyType, VFXPropertySelectorAttribute.Type type)
	{
		return type switch
		{
			VFXPropertySelectorAttribute.Type.Float => propertyType == typeof(float),
			VFXPropertySelectorAttribute.Type.Vector2 => propertyType == typeof(Vector2),
			VFXPropertySelectorAttribute.Type.Vector3 => propertyType == typeof(Vector3),
			VFXPropertySelectorAttribute.Type.Vector4 => propertyType == typeof(Vector4),
			VFXPropertySelectorAttribute.Type.Int => propertyType == typeof(int),
			VFXPropertySelectorAttribute.Type.Uint => propertyType == typeof(uint),
			VFXPropertySelectorAttribute.Type.Texture => propertyType == typeof(Texture),
			VFXPropertySelectorAttribute.Type.Matrix4x4 => propertyType == typeof(Matrix4x4),
			VFXPropertySelectorAttribute.Type.AnimationCurve => propertyType == typeof(AnimationCurve),
			VFXPropertySelectorAttribute.Type.Gradient => propertyType == typeof(Gradient),
			VFXPropertySelectorAttribute.Type.Mesh => propertyType == typeof(Mesh),
			VFXPropertySelectorAttribute.Type.Bool => propertyType == typeof(bool),
			VFXPropertySelectorAttribute.Type.GraphicsBuffer => propertyType == typeof(GraphicsBuffer),
			VFXPropertySelectorAttribute.Type.SkinnedMeshRenderer => propertyType == typeof(SkinnedMeshRenderer),
			_ => propertyType != null
		};
	}

	public VFXPropertySelector(VisualEffectAsset visualEffectAsset, VFXPropertySelectorAttribute.Type type)
	{
		m_visualEffectAsset = visualEffectAsset;
		m_type = type;

		SelectionTree.Selection.SupportsMultiSelect = false;

		// Needed in versions of Odin 3.13+, you can remove it when using in earlier one
		SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
	}

	private static List<VFXExposedProperty> m_properties = new(1);

	protected override void BuildSelectionTree(OdinMenuTree tree)
	{
		m_properties.Clear();
		m_visualEffectAsset.GetExposedProperties(m_properties);

		for (int i = 0; i < m_properties.Count; i++)
		{
			var property = m_properties[i];
			
			if (!IsCorrectType(property.type, m_type))
				continue;

			tree.Add($"{property.name.Replace('/', '\\')} ({property.type.Name})", property.name);
		}
	}
}