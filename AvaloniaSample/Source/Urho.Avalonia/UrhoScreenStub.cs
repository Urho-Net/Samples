using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Platform;

namespace Urho.Avalonia
{
    public class UrhoScreenStub : IScreenImpl, IReadOnlyList<Screen>
    {
        private readonly AvaloniaUrhoContext _context;

        public UrhoScreenStub(AvaloniaUrhoContext context)
        {
            _context = context;
            SetScreenSize(96, new PixelRect(0, 0, (int)Application.Current.Graphics.Width, (int)Application.Current.Graphics.Height));
        }

        public UrhoScreenStub(Screen screen)
        {
            Screen = screen;
        }


        public Screen Screen { get; private set; }

        int IReadOnlyCollection<Screen>.Count => 1;

        int IScreenImpl.ScreenCount => 1;

        IReadOnlyList<Screen> IScreenImpl.AllScreens => this;

        Screen IReadOnlyList<Screen>.this[int index] => Screen;

        public IEnumerator<Screen> GetEnumerator()
        {
            yield return Screen;
        }

        private void SetScreenSize(double pixelDensity, PixelRect framebufferSize)
        {
            Screen = new Screen(pixelDensity, framebufferSize, framebufferSize, true);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Screen ScreenFromWindow(IWindowBaseImpl window)
        {
            return ScreenHelper.ScreenFromWindow(window, AllScreens);
        }

    public IReadOnlyList<Screen> AllScreens { get; } =
            new[] { new Screen(96, new PixelRect(0, 0, 4000, 4000), new PixelRect(0, 0, 4000, 4000), true) };


        public Screen ScreenFromPoint(PixelPoint point)
        {
            return ScreenHelper.ScreenFromPoint(point, AllScreens);
        }

        public Screen ScreenFromRect(PixelRect rect)
        {
            return ScreenHelper.ScreenFromRect(rect, AllScreens);
        }
    }
}