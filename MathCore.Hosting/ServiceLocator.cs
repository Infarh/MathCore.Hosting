using System.Dynamic;

using Microsoft.Extensions.DependencyInjection;

namespace MathCore.Hosting;

/// <summary>Базовый локатор сервисов с доступом к зависимостям через dynamic</summary>
public abstract class ServiceLocator : DynamicObject
{
    /// <summary>Заполняет внутренний кэш доступных сервисов на основе текущих регистраций</summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <remarks>При совпадении имён типов берётся первая регистрация</remarks>
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

    /// <summary>Кэш соответствий имён сервисов их типам или фабрикам</summary>
    private static Dictionary<string, (Type? Type, Func<IServiceProvider, object>? Factory)> __Services = new();

    /// <summary>Провайдер сервисов, используемый локатором для разрешения зависимостей</summary>
    protected abstract IServiceProvider Services { get; }

    /// <summary>Пытается получить сервис по имени динамического члена</summary>
    /// <param name="binder">Объект привязки динамического члена</param>
    /// <param name="result">Найденный экземпляр сервиса или null</param>
    /// <returns>Истина, если сервис получен успешно</returns>
    /// <exception cref="InvalidOperationException">Если тип найден, но экземпляр не может быть разрешён контейнером</exception>
    /// <remarks>Сначала используется фабрика реализации, затем тип и провайдер сервисов</remarks>
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
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

    /// <summary>Возвращает имена доступных динамических членов</summary>
    /// <returns>Перечень имён сервисов, доступных через локатор</returns>
    public override IEnumerable<string> GetDynamicMemberNames() => __Services.Keys;
}