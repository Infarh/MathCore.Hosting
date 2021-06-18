using MathCore.Hosting;

using Microsoft.Extensions.DependencyInjection;

using TestWPF.Services.Interfaces;
using TestWPF.ViewModels.Base;

namespace TestWPF.ViewModels
{
    [Service(ServiceLifetime.Singleton)]
    internal class MainWindowViewModel : ViewModel
    {
        #region Title : string - Заголовок

        /// <summary>Заголовок</summary>
        private string _Title = "Заголовок главного окна";

        /// <summary>Заголовок</summary>
        public string Title { get => _Title; set => Set(ref _Title, value); }

        #endregion

        //[Inject]
        //public IUserDialog UI { get; set; }
        
        //[Inject]
        //private void Initialize(IUserDialog UI) => this.UI = UI;

        //public MainWindowViewModel(IUserDialog UI) => this.UI = UI;
    }
}
