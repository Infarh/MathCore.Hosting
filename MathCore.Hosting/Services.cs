using System.Reflection;

using MathCore.DI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MathCore.Hosting;

/// <summary>Методы‑расширения для регистрации сервисов из конфигурации и добавления служб в хост</summary>
public static class Services
{
    /// <summary>Регистрирует службы из конфигурации, используя сборку, содержащую указанный тип</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <param name="type">Тип, чья сборка используется для поиска типов сервисов и реализаций</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    /// <example>
    /// var section = config.GetSection("Services");
    /// services.AddServicesFromConfiguration(section, typeof(Program));
    /// </example>
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Type type)
        => services.AddServicesFromConfiguration(config, type.Assembly);

    /// <summary>Регистрирует службы из конфигурации и вызывает обработчик после каждой регистрации, используя сборку указанного типа</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <param name="type">Тип, чья сборка используется для поиска типов сервисов и реализаций</param>
    /// <param name="OnServiceAdded">Обработчик, вызываемый после регистрации каждой пары сервис/реализация</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    /// <example>
    /// services.AddServicesFromConfiguration(config.GetSection("Services"), typeof(Program),
    ///     (svc, impl, lifetime) => { /* логирование регистрации */ });
    /// </example>
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Type type, Action<Type, Type?, ServiceLifetime>? OnServiceAdded)
        => services.AddServicesFromConfiguration(config, type.Assembly, OnServiceAdded);

    /// <summary>Регистрирует службы из конфигурации, используя указанную сборку для разрешения типов</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <param name="assembly">Сборка, используемая для поиска типов сервисов и реализаций</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Assembly assembly) =>
        services.AddServicesFromConfiguration(config, assembly, null);

    /// <summary>Регистрирует службы из конфигурации и вызывает обработчик после каждой регистрации, используя указанную сборку</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <param name="assembly">Сборка, используемая для поиска типов сервисов и реализаций</param>
    /// <param name="OnServiceAdded">Обработчик, вызываемый после регистрации каждой пары сервис/реализация</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config, Assembly assembly, Action<Type, Type?, ServiceLifetime>? OnServiceAdded)
    {
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

        static Type? GetType(string TypeName, Assembly asm) => asm.GetType(TypeName) ?? asm.DefinedTypes.FirstOrDefault(t => t.Name == TypeName);
    }

    /// <summary>Регистрирует службы из конфигурации, разрешая типы по полным именам с использованием загрузчика типов</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    public static IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration config) =>
        AddServicesFromConfiguration(services, config, (Action<Type, Type?, ServiceLifetime>?)null);

    /// <summary>Регистрирует службы из конфигурации и вызывает обработчик после каждой регистрации, разрешая типы по полным именам</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="config">Раздел конфигурации со списком сервисов</param>
    /// <param name="OnServiceAdded">Обработчик, вызываемый после регистрации каждой пары сервис/реализация</param>
    /// <returns>Коллекция сервисов с добавленными регистрациями</returns>
    /// <exception cref="InvalidOperationException">Если указанный в конфигурации тип сервиса или реализации не найден</exception>
    /// <example>
    /// services.AddServicesFromConfiguration(config.GetSection("Services"),
    ///     (svc, impl, lifetime) => { /* пост‑обработка регистрации */ });
    /// </example>
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

    /// <summary>Подключает локатор сервисов к контейнеру хоста</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServiceLocator(this IHostBuilder Host) => Host.ConfigureServices(ServiceLocator.ConfigureServices);

    /// <summary>Добавляет службы, найденные в указанной сборке</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="assembly">Сборка, из которой регистрируются службы</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, Assembly assembly) =>
        Host.ConfigureServices(services => services.AddServicesFromAssembly(assembly));

    /// <summary>Добавляет службы из сборки, содержащей указанный тип</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="type">Тип, чья сборка используется для поиска служб</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, Type type) =>
        Host.ConfigureServices(services => services.AddServicesFromAssembly(type));

    /// <summary>Добавляет службы из набора сборок</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="Assemblies">Массив сборок для регистрации служб</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, params Assembly[] Assemblies)
    {
        foreach (var assembly in Assemblies)
            Host.AddServices(assembly);
        return Host;
    }

    /// <summary>Добавляет службы из перечисления сборок</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="Assemblies">Перечисление сборок для регистрации служб</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, IEnumerable<Assembly> Assemblies)
    {
        foreach (var assembly in Assemblies)
            Host.AddServices(assembly);
        return Host;
    }

    /// <summary>Добавляет службы из сборок, содержащих указанные типы</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="Types">Массив типов, чьи сборки используются для регистрации служб</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, params Type[] Types)
    {
        foreach (var assembly in Types)
            Host.AddServices(assembly);
        return Host;
    }

    /// <summary>Добавляет службы из сборок, содержащих указанные типы</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="Types">Перечисление типов, чьи сборки используются для регистрации служб</param>
    /// <returns>Исходный построитель хоста</returns>
    public static IHostBuilder AddServices(this IHostBuilder Host, IEnumerable<Type> Types)
    {
        foreach (var assembly in Types)
            Host.AddServices(assembly);
        return Host;
    }

    /// <summary>Загружает сборку по имени и добавляет найденные службы</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="AssemblyName">Полное или простое имя сборки</param>
    /// <returns>Исходный построитель хоста</returns>
    /// <exception cref="System.IO.FileNotFoundException">Если сборка с указанным именем не найдена</exception>
    /// <exception cref="System.IO.FileLoadException">Если сборка найдена, но не может быть загружена</exception>
    /// <exception cref="BadImageFormatException">Если файл не является корректной сборкой</exception>
    public static IHostBuilder AddServicesFromAssembly(this IHostBuilder Host, string AssemblyName)
    {
        var assembly = Assembly.Load(AssemblyName);
        return Host.AddServices(assembly);
    }

    /// <summary>Загружает сборку по пути и добавляет найденные службы</summary>
    /// <param name="Host">Построитель хоста</param>
    /// <param name="AssemblyPath">Путь к файлу сборки</param>
    /// <returns>Исходный построитель хоста</returns>
    /// <exception cref="System.IO.FileNotFoundException">Если файл по указанному пути не найден</exception>
    /// <exception cref="System.IO.FileLoadException">Если файл найден, но сборка не может быть загружена</exception>
    /// <exception cref="BadImageFormatException">Если файл не является корректной сборкой</exception>
    public static IHostBuilder AddServicesFromAssemblyPath(this IHostBuilder Host, string AssemblyPath)
    {
        var assembly = Assembly.LoadFrom(AssemblyPath);
        return Host.AddServices(assembly);
    }
}