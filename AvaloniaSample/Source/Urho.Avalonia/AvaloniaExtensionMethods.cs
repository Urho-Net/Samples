using System;
using Avalonia;
using Avalonia.Dialogs;
using Urho.Gui;
using Urho.Urho2D;
using Avalonia.ReactiveUI;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Controls.Platform;
using Avalonia.Native;
using Avalonia.FreeDesktop;
using Avalonia.Win32;

namespace Urho.Avalonia
{
    public static class AvaloniaExtensionMethods
    {
        // [DllImport("libAvaloniaNative")]
        // static extern IntPtr CreateAvaloniaNative();
        public static AvaloniaUrhoContext ConfigureAvalonia<T>(this Context context) where T: global::Avalonia.Application, new()
        {
            var avaloniaUrhoContext = new AvaloniaUrhoContext(context);
            var builder = PortableAppBuilder.Configure<T>()
                .UsePlatformDetect()
                .UsePortablePlatfrom(avaloniaUrhoContext)
                .UseSkia()
                .UseReactiveUI()
                .UseManagedSystemDialogs()
                .SetupWithoutStarting();


            // IntPtr _native =  CreateAvaloniaNative();


            var os = builder.RuntimePlatform.GetRuntimeInfo().OperatingSystem;
            AvaloniaUrhoContext.OperatingSystemType = os;

            if (os == OperatingSystemType.WinNT)
            {
                AvaloniaLocator.CurrentMutable.Bind<IMountedVolumeInfoProvider>().ToConstant(new WindowsMountedVolumeInfoProvider());
            }
            else if (os == OperatingSystemType.OSX)
            {
                AvaloniaLocator.CurrentMutable.Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider());
            }
            else
            {
                AvaloniaLocator.CurrentMutable.Bind<IMountedVolumeInfoProvider>().ToConstant(new LinuxMountedVolumeInfoProvider());
            }

            

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