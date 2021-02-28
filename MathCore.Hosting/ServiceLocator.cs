using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MathCore.Hosting
{
    public abstract class ServiceLocator : DynamicObject
    {
        public static void ConfigureServices(IServiceCollection services) =>
            __Services = services
               .ToLookup(s => s.ServiceType.Name)
               .ToDictionary(s => s.Key, s => s.First().ImplementationType!);

        private static Dictionary<string, Type> __Services = new();

        protected abstract IServiceProvider Services { get; }

        /// <inheritdoc />
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (base.TryGetMember(binder, out result))
                return true;

            if (!__Services.TryGetValue(binder.Name, out var type)) return false;
            result = Services.GetRequiredService(type);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => __Services.Keys;
    }
}
