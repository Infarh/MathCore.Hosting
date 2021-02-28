using System;

namespace MathCore.Hosting
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class InjectAttribute : Attribute { }
}