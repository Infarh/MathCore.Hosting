using System;
using System.ComponentModel;

namespace MathCore.Hosting;

public class DynamicPropertyManager<T> : IDisposable
{
    private DynamicTypeDescriptionProvider _Provider;
    private class DynamicTypeDescriptionProvider : TypeDescriptionProvider
    {

    }

    public DynamicPropertiesCollection<T> Properties { get; } = new();

    public DynamicPropertyManager() => TypeDescriptor.AddProvider(_Provider = new DynamicTypeDescriptionProvider(), typeof(T));

    public void Dispose() => TypeDescriptor.RemoveProvider(_Provider, typeof(T));
}

public class DynamicPropertiesCollection<T>
{
    private class DynamicPropertyDescriptor<TProperty>
    {

    }

    public void Add<TProperty>(string Name, Func<TProperty> Getter, Action<TProperty>? Setter = null, Attribute[]? Attributes = null)
    {

    }
}