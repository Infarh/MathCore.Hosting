using System;

namespace TestWPF
{
    public class ServiceLocator : MathCore.Hosting.ServiceLocator
    {
        protected override IServiceProvider Services => App.Hosting.Services;
    }
}
