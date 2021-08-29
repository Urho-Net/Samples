using Avalonia.Controls;
using Urho.AvaloniaAdapter;

namespace Urho
{
    public sealed class PortableAppBuilder : AppBuilderBase<PortableAppBuilder>
    {
        public PortableAppBuilder() : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType()?.Assembly))
        {
        }
    }
}