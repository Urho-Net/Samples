using Avalonia.Controls;
using Urho.Avalonia;

namespace Urho.Avalonia
{
    public sealed class PortableAppBuilder : AppBuilderBase<PortableAppBuilder>
    {
        public PortableAppBuilder() : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(Application.Current.GetType()?.Assembly))
        {
        }
    }
}