using System;

using Microsoft.Extensions.DependencyInjection;
// ReSharper disable ParameterHidesMember

namespace MathCore.Hosting
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceAttribute : Attribute
    {
        public ServiceLifetime Mode { get; set; } = ServiceLifetime.Transient;

        public Type? Implementation { get; set; }

        public ServiceAttribute() { }

        public ServiceAttribute(ServiceLifetime Mode) => this.Mode = Mode;

        public void Deconstruct(out Type? Implementation, out ServiceLifetime Mode)
        {
            Implementation = this.Implementation;
            Mode = this.Mode;
        }
    }
}