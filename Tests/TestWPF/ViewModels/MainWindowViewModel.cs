using MathCore.Hosting;
using MathCore.ViewModels;

using Microsoft.Extensions.DependencyInjection;

using TestWPF.Services.Interfaces;

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

        [Inject]
        public IUserDialog UI { get; init; }
    }
}
