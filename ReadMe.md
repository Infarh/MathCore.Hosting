# Автоматизация сервисов хоста в WPF-приложении

Комплекс инструментов призван упростить добавление сервисов в DI-контейнер при создании и запуске WPF-приложения

## Использование

В коде основного приложения `App.xaml.cs` подключаем сервисы

```C#
public partial class App
{
    private static IHost __Hosting;

    public static IHost Hosting => __Hosting 
        ??= CreateHostBuilder(Environment.GetCommandLineArgs()).Build();

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
       .CreateDefaultBuilder(args)
       .AddServices(typeof(App))              // Добавляем все сервисы из сборки указанного типа
       .ConfigureServices(ConfigureServices)  // Добавляем дополнительные сервисы вручную в методе ниже
       .AddServiceLocator();                  // Подключаем класс ServiceLocator

    private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        // Здесь можно добавить сервисы в контейнер вручную
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        var host = Hosting;
        base.OnStartup(e);
        await host.StartAsync(); // При запуске приложения запускаем хост
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using var host = Hosting; // using - уничтожит хост после остановки приложения
        base.OnExit(e);
        await host.StopAsync();   // При остановке приложения - останавливаем хост
    }
}
```

Если есть необходимость использования Entity Framework Core, то метод `CreateHostBuilder` должен быть публичным, статическим, иметь строго указанное имя и принимать массив строк - аргументов командной строки.

При этом в контейнере сервисов сразу присутствует сконфигурированная система логирования в окно вывода Студии и система конфигурации приложения для файла `appsettings.json`. Файл `appsettings.json` надо добавить в проект вручную и установить для компилятора режим автоматического копирования "более поздней версии" файла в выходной каталог при сборке.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net5.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\MathCore.Hosting\MathCore.Hosting.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory> <!-- !!!! -->
    </None>
  </ItemGroup>

</Project>
```

## Сценарий запуска

При запуске в процессе конфигурирования сервисов приложения при вызове `.AddServices(typeof(App))` система сканирует все типы, определённые в сборке, в которой определён указанный здесь тип `App`. Выбираются все типы для которых указан атрибут `[Service]` (`ServiceAttribute`). Найденые таким образом типы расцениваются как сервисы и добавляются в контейнер сервисов. Параметры добавления каждого сервиса можно определить в параметрах атрибута:

- `Implementation` - в данном свойстве можно указать тип, являющийся реализацией интерфейса сервиса. В этом случае сам атрибут должен быть применён к интерфейсу, либо абстрактному классу.
- `Mode` - режим жизненного цикла сервиса. Можно выбрать из вариантов: `ServiceLifetime.Singleton`, `ServiceLifetime.Scoped` и `ServiceLifetime.Transient`.

Пример добавления модели-представления Главного окна как сервиса прилоежиня

```C#
[Service(ServiceLifetime.Singleton)]
internal class MainWindowViewModel : ViewModel
{
    private string _Title = "Заголовок главного окна";

    public string Title { get => _Title; set => Set(ref _Title, value); }
}
```

Это эквивалентно вызову в `App.xaml.cs`

```C#
private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
{
    services.AddSingleton<MainWindowViewModel>();
}
```

Для добавленя сервиса, построенного на основе интерфейса атрибут `[Service]` должен быть применён к интерфейсу и у него должны быть указаны параметры: тип, реализующий интерфейс; время жизни.

Пример описания интерфейса сервиса диалога с пользователем:

```C#
[Service(Implementation = typeof(WindowUI))]
internal interface IUserDialog
{
    // ...
}
```

И его реализация

```C#
internal class WindowUI : IUserDialog
{
    
}
```

При этом, указанный в атрибуте `[Service(Implementation = typeof(WindowUI))]` тип `WindowUI` должен реализовывать интерфейс `IUserDialog`. Иначе система известит об этом сгенерировав исключение `InvalidOperationException` при сканировании типов сборки.

Добавленный таким образом сервис получи время жизни по умолчанию `ServiceLifetime.Transient`. Если надо указать иное время жизни, то это можно сделать изменив параметры атрибута 
```C#
[Service(Implementation = typeof(WindowUI), Mode = ServiceLifetime.Singleton)]
internal interface IUserDialog
{
    // ...
}
```

## Добавление сервисов через файл конфигурации

Система позволяет выполнить регистрацию сервисов с их описанием в файле конфигурации. Для этого в файле `App.xaml.cs` необходим вызов 

```C#
private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
{
    services.AddServicesFromConfiguration(host.Configuration.GetSection("Services"), typeof(App));
}
```

Это заставит систему прочитать файл конфигурации (указанную его секцию) и выбрать оттуда все определения сервисов зарегистрировав их указанным там образом. Пример файла конфигураци для данного варианта использвоания будет выглядеть следующим образом:

```json
{
  "Services": {
    "IUserDialog": {
      "Type": "WindowUserDialog",
      "Mode": "Transient"
    },
    "MainWindowViewModel": {
      "Mode": "Singleton"
    }
  }
}
```

В файле конфигурации объявляется секция определения сервисов. Имя секции можно задать произвольно.
Внутри секции следует набор определяемых сервисов. Каждый параметр секции - отдельный сервис. Имя параметра в общем случае определяет имя типа сервиса. Внутри секции определения конкретного сервиса задаются его параметры.

- `Type` - определяет тип, выполняющий реализацию сервиса. Если параметр не указан, то сервис будет зарегистрирован по типу, указанному в имени секции.
- `Mode` - определяет время жизни сервиса. Если не указан, то время жизни сервиса будет задано как `ServiceLifetime.Transient`.

Сервисы из файла конфигурации в контейнер сервисов добавляются в режиме `TryAdd` - если сервис уже определён в контейнере, то добавление производиться не будет.

## Внедрение зависимостей через свойство и через метод

Система регистрации сервисов позволяет реализовать дополнительные возможности осуществления внердения зависимостей не только через конструктор, но и через свойство и через метод. Для этого в реализации сервиса для свойств в которые необходимо выполнить внедрение нужно указать атрибут `[Inject]` (`InjectAttribute`)

Пример внедрения сервиса диалога с пользователем в главную модель-представления

Стандартный способ внедрения зависимости через конструктор выглядит следующим образом:

```C#
[Service(ServiceLifetime.Singleton)]
internal class MainWindowViewModel : ViewModel
{
    public IUserDialog UI { get; set; }

    public MainWindowViewModel(IUserDialog UI) => this.UI = UI;
}
```

В том случае если число внедряемых сервисов велико, то конструктор будет выглядеть не лучшим образом в виду большого числа его параметров.

Можно определить свойство (публичное, или приватное) с доступом на запись (или инициализацию) и пометить его атрибутом `[Inject]`:

```C#
[Service(ServiceLifetime.Singleton)]
internal class MainWindowViewModel : ViewModel
{
    [Inject]
    public IUserDialog UI { get; set; }
}
```

В этом случае даже большое число внедряемых сервисов будет выглядеть аккуратно.

Также есть возможность указать метод (или несколько методов), которые необходимо выполнить сразу после создания нового экземпляра. Для этого нужно определить метод (публичный, или приватный) и в его параметрах запросить все необходимые сервисы. Сам метод должен быть помечен атрибутом `[Inject]`.

```C#
[Service(ServiceLifetime.Singleton)]
internal class MainWindowViewModel : ViewModel
{
    public IUserDialog UI { get; private set; }
    
    [Inject]
    private void Initialize(IUserDialog UI) => this.UI = UI;
}
```

## Динамический локатор сервисов

В пакете определён базовый абстрактный класс `ServiceLocator`, позволяющий реализовать шаблон "ServiceLocator". Класс является наследником класса `DynamicObject` (динамический объект). Набор свойств данного объекта является динамическим и определяется набором зарегистрированных в контейнере сервисов. Имя свойства сопоставляется с именем класса сервиса. Значения свойств при обращении к ним формируются контейнером сервисов в соответствии с указанным временем жизни.

Для использования локатора сервисов нужно определить свой класс-наследник

```C#
public class ServiceLocator : MathCore.Hosting.ServiceLocator
{
    protected override IServiceProvider Services => App.Hosting.Services;
}
```

В классе-наследнике надо переопределить свойство, позволяющее осуществить доступ к контейнеру сервисов приложения.

Также необходимо выполнить инициализацию сервисов локатора в файле `App.xaml.cs`

```C#
public partial class App
{
    //...

    public static IHostBuilder CreateHostBuilder(string[] args) => Host
       .CreateDefaultBuilder(args)
       .AddServices(typeof(App))
       .ConfigureServices(ConfigureServices)
       .AddServiceLocator(); // Вызов метода конфигурации локатора должен быть последним

    private static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
    {
        //...
    }

    //...
}
```

Вызов `.AddServiceLocator()` должен быть выполнен после того как все сервисы будут зарегистрированы в контейнере. Иначе локатор не сможет захватить их и использовать для генерации своих динамических свойств.

После этого локатор надо разместить "на видном месте" в ресурсах приложения `App.xaml`

```xml
<Application x:Class="TestWPF.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:l="clr-namespace:TestWPF"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <l:ServiceLocator x:Key="Locator"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

После этого (стоит выполнить пересборку проекта) локатор (несмотря на отсутствие в определении его класса свойства MainWindowViewModel) может быть использован для разрешения типа данных Главной модели-представления Главного окна `MainWindow.xaml`

```xml
<Window x:Class="TestWPF.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" mc:Ignorable="d"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:vm="clr-namespace:TestWPF.ViewModels"
        xmlns:l="clr-namespace:TestWPF"
        DataContext="{Binding MainWindowViewModel, Source={StaticResource Locator}}"
        d:DataContext="{d:DesignInstance vm:MainWindowViewModel}"
        Title="{Binding Title}"
        Width="800" Height="450">
    <Grid>

    </Grid>
</Window>
```

`DataContext="{Binding MainWindowViewModel, Source={StaticResource Locator}}"` возволяет выполнить привязку `DataContext` окна к динамическому свойству локатора `MainWindowViewModel`.

Недостатком данного метода является невозможность для дизайнера Visual Studio определить наличие динамического свойства в классе `ServiceLocator` и вычислить его тип. В результате дизайнер теряет возможность выполнять подсказки в коде разметки окна. Чтобы компенсировать данный недостаток можно воспользоваться возможностями пространства имён `d` добаввив директиву `d:DataContext="{d:DesignInstance vm:MainWindowViewModel}"` насильно указывающую дизайнеру что именно является контекстом данных в окне.

Ещё одним недостатком данного подхода является неспособность Дизайнера Visual Studio выполнять динамические вызовы к динамическим объектам, в виду чего в режиме дизайнера логика класса модели-представления работать не будет и все привязки к ней тоже.