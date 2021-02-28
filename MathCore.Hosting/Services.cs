using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MathCore.Hosting
{
    public static class Services
    {
        private static bool IsSimple(this Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
               .Concat(type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty));
            if (properties.Any(p => p.GetCustomAttribute<InjectAttribute>() is not null))
                return false;

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
               .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic));
            return !methods.Any(m => m.GetCustomAttribute<InjectAttribute>() != null);
        }

        private static ServiceDescriptor CreateDescriptor(this Type Service, Type? Implementation, ServiceLifetime Mode)
        {
            var service_type = Implementation ?? Service;

            var ctor = service_type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
               .Concat(service_type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
               .OrderByDescending(c => c.GetParameters().Length)
               .First();

            var sp = Expression.Parameter(typeof(IServiceProvider), "sp");
            var get_service = sp.Type.GetMethod("GetService") ?? throw new InvalidOperationException();

            var ctor_parameters = ctor.GetParameters()
               .Select(p => Expression.Convert(Expression.Call(sp, get_service, Expression.Constant(p.ParameterType)), p.ParameterType));

            var properties = service_type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
               .Concat(service_type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty))
               .Where(p => p.GetCustomAttribute<InjectAttribute>() != null)
               .Select(p =>
               {
                   var type = Expression.Constant(p.PropertyType);
                   var obj = Expression.Call(sp, get_service, type);
                   var value = Expression.Convert(obj, p.PropertyType);
                   return (MemberBinding)Expression.Bind(p, value);
               })
               .ToArray();

            var ctor_expr = Expression.New(ctor, ctor_parameters);
            Expression instance = properties.Length > 0
                ? Expression.MemberInit(ctor_expr, properties)
                : ctor_expr;

            var result = Expression.Variable(Service, "result");
            var methods = service_type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
               .Concat(service_type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
               .Where(InitMethod => InitMethod.GetCustomAttribute<InjectAttribute>() != null)
               .Select(InitMethod =>
               {
                   var parameters = InitMethod
                      .GetParameters()
                      .Select(p =>
                       {
                           var type = Expression.Constant(p.ParameterType);
                           var obj = Expression.Call(sp, get_service, type);
                           return Expression.Convert(obj, p.ParameterType);
                       });
                   return (Expression)Expression.Call(result, InitMethod, parameters);
               })
               .ToArray();

            var body = methods.Length > 0
                ? Expression.Block(
                    new[] { result },
                    Expression.Assign(result, instance),
                    methods.Length == 1 ? methods[0] : Expression.Block(methods),
                    result)
                : instance;

            var factory_expr = Expression.Lambda<Func<IServiceProvider, object>>(body, sp);
            var factory = factory_expr.Compile();

            return new ServiceDescriptor(Service, factory, Mode);
        }

        public static IServiceCollection AddService(this IServiceCollection services, Type Service, Type? Implementation, ServiceLifetime Mode)
        {
            var service_type = Implementation is null
                ? Service
                : Service.IsAssignableFrom(Implementation)
                    ? Implementation
                    : throw new InvalidOperationException($"Тип {Implementation} не реализует {Service}");

            if (service_type.IsSimple())
                return services.AddSimple(Service, Implementation, Mode);

            services.TryAdd(Service.CreateDescriptor(Implementation, Mode));

            return services;
        }

        private static IServiceCollection AddSimple(this IServiceCollection services, Type Service, Type? Implementation, ServiceLifetime Mode)
        {
            var descriptor = Implementation is null
                ? new ServiceDescriptor(Service, Mode)
                : new ServiceDescriptor(Service, Implementation, Mode);

            services.TryAdd(descriptor);
            return services;
        }

        public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Type AssemblyType) => 
            services.AddServicesFromAssembly(AssemblyType.Assembly);

        public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            foreach (var type in assembly.DefinedTypes)
            {
                var info = type.GetCustomAttribute<ServiceAttribute>();
                if (info is null) continue;
                services.AddService(type, info.Implementation, info.Mode);
            }

            return services;
        }

        public static IServiceCollection AddServicesFromConfiguration(IServiceCollection services, IConfiguration config, Assembly assembly)
        {
            static Type? GetType(string TypeName, Assembly asm) => asm.GetType(TypeName)
                ?? asm.DefinedTypes.FirstOrDefault(t => t.Name == TypeName);

            foreach (var service_config in config.GetChildren())
            {
                var service_type_name = service_config.Key;
                var service_type = GetType(service_type_name, assembly)
                    ?? throw new InvalidOperationException($"Тип сервиса {service_type_name} не найден");

                var implementation_type_name = service_config["Type"];
                var implementation_type = !string.IsNullOrEmpty(implementation_type_name)
                    ? GetType(implementation_type_name, assembly)
                    ?? throw new InvalidOperationException($"Тип реализации сервиса {implementation_type_name} не найден")
                    : null;

                if (!Enum.TryParse<ServiceLifetime>(service_config["Mode"], out var mode))
                    mode = ServiceLifetime.Transient;

                services.AddService(service_type, implementation_type, mode);
            }

            return services;
        }
    }
}
