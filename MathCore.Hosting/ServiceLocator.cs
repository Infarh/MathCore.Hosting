using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

namespace MathCore.Hosting
{
    public abstract class ServiceLocator : DynamicObject
    {
        private class ServicePropertyDescriptor : PropertyDescriptor
        {
            public override Type ComponentType { get; }
            public override bool IsReadOnly { get; } = true;
            public override Type PropertyType { get; }

            public ServicePropertyDescriptor(Type ServiceType, Type LocatorType) 
                : base(ServiceType.Name, Array.Empty<Attribute>())
            {
                PropertyType = ServiceType;
                ComponentType = LocatorType;
            }

            public override object GetValue(object component) => ((ServiceLocator)component).Services.GetRequiredService(PropertyType);
            public override void SetValue(object component, object value) { }
            public override void ResetValue(object component) { }
            public override bool CanResetValue(object component) => false;
            public override bool ShouldSerializeValue(object component) => false;
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            var result = new Dictionary<string, (Type? Type, Func<IServiceProvider, object>? Factory)>();

            foreach (var info in services.ToLookup(s => s.ServiceType.Name))
            {
                var service = info.First();
                result.Add(info.Key, (service.ImplementationType, service.ImplementationFactory));

                TypeDescriptor.CreateProperty(
                    typeof(ServiceLocator),
                    new ServicePropertyDescriptor(service.ServiceType, typeof(ServiceLocator)));
            }

            __Services = result;
        }

        private static Dictionary<string, (Type? Type, Func<IServiceProvider, object>? Factory)> __Services = new();

        protected abstract IServiceProvider Services { get; }

        /// <inheritdoc />
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (base.TryGetMember(binder, out result))
                return true;

            if (!__Services.TryGetValue(binder.Name, out var info)) return false;
            if (info.Factory is { } factory)
            {
                result = factory(Services);
                return true;
            }

            if (info.Type is not { } type) return false;

            result = Services.GetRequiredService(type);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => __Services.Keys;
    }
}
