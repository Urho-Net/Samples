using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Skia;
using Urho.Avalonia;
using Avalonia.Native;

namespace Urho.Avalonia
{
    public class PortableWindowPlatform : PlatformThreadingInterfaceBase, IPlatformSettings, IWindowingPlatform
    {
        private static readonly PortableWindowPlatform s_instance = new PortableWindowPlatform();
      

        public Size DoubleClickSize { get; } = new Size(4, 4);
        public TimeSpan DoubleClickTime { 
            get; 
            } = TimeSpan.FromMilliseconds(500);

        public static void Initialize(AvaloniaUrhoContext context)
        {
            _context = context;

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<IRenderLoop>().ToConstant(new Urho.Avalonia.RenderLoop(_context))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IKeyboardDevice>().ToConstant(context.KeyboardDevice)
                .Bind<IMouseDevice>().ToConstant(context.MouseDevice)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl())
                // .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogImp())
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader());

            SkiaPlatform.Initialize();
            
        }

        public override void EnsureInvokeOnMainThread(Action action)
        {
            AvaloniaUrhoContext.EnsureInvokeOnMainThread(action);
        }

        public override void RunLoop(CancellationToken cancellationToken)
        {
            _context.RunLoop(cancellationToken);
        }

        public IWindowImpl CreateWindow()
        {
            return new UrhoWindowImpl(_context);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            return new UrhoWindowImpl(_context);
        }
    }
}