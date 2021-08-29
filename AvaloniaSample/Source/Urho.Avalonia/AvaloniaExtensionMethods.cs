using System;
using Avalonia;
using Avalonia.Dialogs;
using Urho.Avalonia;
using Urho;
using Urho.Gui;
using Urho.Urho2D;
using Avalonia.ReactiveUI;

namespace Urho.Avalonia
{
    public static class AvaloniaExtensionMethods
    {
        public static AvaloniaUrhoContext ConfigureAvalonia<T>(this Context context) where T: global::Avalonia.Application, new()
        {
            var avaloniaUrhoContext = new AvaloniaUrhoContext(context);
            PortableAppBuilder.Configure<T>()
                .UsePortablePlatfrom(avaloniaUrhoContext)
                .UseSkia()
                .UseManagedSystemDialogs()
                .UseReactiveUI()
                .SetupWithoutStarting();

            return avaloniaUrhoContext;
        }

        public static void Show(this global::Avalonia.Controls.Window window, UIElement parent)
        {
            window.GetUIElement().SetParent(parent);
            window.Show();
        }
        
        public static void Show(this global::Avalonia.Controls.Window window, Texture2D dynamicTexture)
        {
            if (!window.TrySetTargetTexture(dynamicTexture))
            {
                throw new ApplicationException("Can't set target texture");
            }
            window.Show();
        }


        public static bool TrySetTargetTexture(this global::Avalonia.Controls.Window window, Texture2D texture)
        {
            if (window.PlatformImpl is UrhoTopLevelImpl impl)
            {
                return impl.TrySetTargetTexture(texture);
            }

            return false;
        }

        public static UIElement GetUIElement(this global::Avalonia.Controls.Window window)
        {
            if (window.PlatformImpl is UrhoTopLevelImpl impl)
            {
                return impl.UrhoUIElement;
            }

            return null;
        }
    }
}