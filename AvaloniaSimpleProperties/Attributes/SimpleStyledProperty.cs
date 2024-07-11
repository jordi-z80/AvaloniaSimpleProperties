using System;

namespace AvaloniaEasyProperties
{

	//=============================================================================
	/// <summary>Simplifies code like
	/// 
	/// public static readonly StyledProperty<int> DirectionProperty = AvaloniaProperty.Register<ArrowButton, int> (nameof (Direction));
	///  + the getter/setter
	///  
	/// with
	/// 
	/// [SimpleStyledProperty] private int _direction;
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public class SimpleStyledProperty : Attribute
	{

	}

}