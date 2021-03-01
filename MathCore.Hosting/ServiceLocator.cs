using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

namespace MathCore.Hosting
{
    public abstract class ServiceLocator : DynamicObject
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            var result = new Dictionary<string, (Type? Type, Func<IServiceProvider, object>? Factory)>();

            foreach (var info in services.ToLookup(s => s.ServiceType.Name))
            {
                var service = info.First();
                result.Add(info.Key, (service.ImplementationType, service.ImplementationFactory));
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
