using System;
using System.Collections.Generic;
using System.Text;

namespace AvaloniaEasyProperties
{
	//=============================================================================
	/// <summary>Simplifies code like
	/// 
	/// public static readonly AttachedProperty<IBrush> CustomBackgroundProperty = AvaloniaProperty.RegisterAttached<DeviceInfoButton, TemplatedControl, IBrush> (nameof (CustomBackground));
	///  + the getter/setter
	///  with
	///  
	/// [SimpleAttachedProperty] private IBrush _customBackground;
	/// </summary>
	[AttributeUsage (AttributeTargets.Field)]
	public class SimpleAttachedProperty : Attribute
	{

	}

}
