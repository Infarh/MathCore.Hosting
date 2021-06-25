using MathCore.Hosting;
using MathCore.WPF.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using TestWPF.Services.Interfaces;

namespace TestWPF.ViewModels
{
    [Service(ServiceLifetime.Singleton)]
    internal class MainWindowViewModel : TitledViewModel
    {
        public MainWindowViewModel() => Title = "Заголовок главного окна";

        [Inject]
        public IUserDialog UI { get; set; }

        //[Inject]
        //private void Initialize(IUserDialog UI) => this.UI = UI;

        //public MainWindowViewModel(IUserDialog UI) => this.UI = UI;
    }
}
