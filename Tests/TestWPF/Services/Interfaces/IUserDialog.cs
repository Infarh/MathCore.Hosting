using MathCore.Hosting;

namespace TestWPF.Services.Interfaces
{
    [Service(Implementation = typeof(WindowUI))]
    internal interface IUserDialog
    {
        
    }
}
