using System.Reflection;

using MathCore.DI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MathCore.Hosting;

public static class Services
{
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Type type)
        => services.AddServicesFromConfiguration(config, type.Assembly);

    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Type type, Action<Type, Type?, ServiceLifetime>? OnServiceAdded)
        => services.AddServicesFromConfiguration(config, type.Assembly, OnServiceAdded);

    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Assembly assembly) =>
        services.AddServicesFromConfiguration(config, assembly, null);

    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Assembly assembly, Action<Type, Type?, ServiceLifetime>? OnServiceAdded)
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

            // ReSharper disable once SettingNotFoundInConfiguration
            if (!Enum.TryParse<ServiceLifetime>(service_config["Mode"], out var mode))
                mode = ServiceLifetime.Transient;

            services.AddService(service_type, implementation_type, mode);
            OnServiceAdded?.Invoke(service_type, implementation_type, mode);
        }

        return services;
    }

    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config) =>
        AddServicesFromConfiguration(services, config, (Action<Type, Type?, ServiceLifetime>?)null);

    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Action<Type, Type?, ServiceLifetime>? OnServiceAdded)
    {
        foreach (var service_config in config.GetChildren())
        {
            var service_type_name = service_config.Key;
            var service_type = Type.GetType(service_type_name)
                ?? throw new InvalidOperationException($"Тип сервиса {service_type_name} не найден");

            var implementation_type_name = service_config["Type"];
            var implementation_type = !string.IsNullOrEmpty(implementation_type_name)
                ? Type.GetType(implementation_type_name) ?? throw new InvalidOperationException($"Тип реализации сервиса {implementation_type_name} не найден")
                : null;

            // ReSharper disable once SettingNotFoundInConfiguration
            if (!Enum.TryParse<ServiceLifetime>(service_config["Mode"], out var mode))
                mode = ServiceLifetime.Transient;

            services.AddService(service_type, implementation_type, mode);
            OnServiceAdded?.Invoke(service_type, implementation_type, mode);
        }

        return services;
    }

    public static IHostBuilder AddServiceLocator(this IHostBuilder Host) => Host.ConfigureServices(ServiceLocator.ConfigureServices);

    public static IHostBuilder AddServices(this IHostBuilder Host, Assembly assembly) =>
        Host.ConfigureServices(services => services.AddServicesFromAssembly(assembly));

    public static IHostBuilder AddServices(this IHostBuilder Host, Type type) =>
        Host.ConfigureServices(services => services.AddServicesFromAssembly(type));

    public static IHostBuilder AddServices(this IHostBuilder Host, params Assembly[] Assemblies)
    {
        foreach (var assembly in Assemblies)
            Host.AddServices(assembly);
        return Host;
    }

    public static IHostBuilder AddServices(this IHostBuilder Host, IEnumerable<Assembly> Assemblies)
    {
        foreach (var assembly in Assemblies)
            Host.AddServices(assembly);
        return Host;
    }

    public static IHostBuilder AddServices(this IHostBuilder Host, params Type[] Types)
    {
        foreach (var assembly in Types)
            Host.AddServices(assembly);
        return Host;
    }

    public static IHostBuilder AddServices(this IHostBuilder Host, IEnumerable<Type> Types)
    {
        foreach (var assembly in Types)
            Host.AddServices(assembly);
        return Host;
    }

    public static IHostBuilder AddServicesFromAssembly(this IHostBuilder Host, string AssemblyName)
    {
        var assembly = Assembly.Load(AssemblyName);
        return Host.AddServices(assembly);
    }

    public static IHostBuilder AddServicesFromAssemblyPath(this IHostBuilder Host, string AssemblyPath)
    {
        var assembly = Assembly.LoadFrom(AssemblyPath);
        return Host.AddServices(assembly);
    }
}