# AvaloniaSimpleProperties

Quick and dirty code generator to simplify Avalonia simple Styled and Attached properties creation.

Some work needs to be done yet, like proper error generation (and not just throwing exceptions around).


Adds [SimpleAttachedProperty] and [SimpleStyledProperty].

Use:


```csharp
[SimpleAttachedProperty] private int _value;
```

to generate

```csharp
public static readonly AttachedProperty<int> ValueProperty = AvaloniaProperty.RegisterAttached<SomeType, int>("Value", typeof(SomeType));

public int Value
{
	get => GetValue(ValueProperty);
	set => SetValue(ValueProperty, value);
}

```

This covers 99% of my Avalonia use cases. If you need more complex properties, just use the standard code.

