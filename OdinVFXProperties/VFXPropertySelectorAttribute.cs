using System;

public class VFXPropertySelectorAttribute : Attribute
{
	public enum Type
	{
		Any,
		Float,
		Vector2,
		Vector3,
		Vector4,
		Int,
		Uint,
		Texture,
		Matrix4x4,
		AnimationCurve,
		Gradient,
		Mesh,
		Bool,
		ComputeBuffer
	}

	public readonly Type m_type;
	public readonly string m_reference;

	/// <summary>
	/// VFXPropertySelectorAttribute constructor. VisualEffect will be found automatically from sibling properties
	/// </summary>
	/// <param name="type">Desired type of property</param>
	public VFXPropertySelectorAttribute(Type type = Type.Any) : this(null, type){}

	/// <summary>
	/// VFXPropertySelectorAttribute constructor
	/// </summary>
	/// <param name="reference">String to resolve VisualEffect object.
	/// If left empty or null VisualEffect will be found automatically from sibling properties</param>
	/// <param name="type">Desired type of property</param>
	public VFXPropertySelectorAttribute(string reference, Type type = Type.Any)
	{
		m_reference = reference;
		m_type = type;
	}
}