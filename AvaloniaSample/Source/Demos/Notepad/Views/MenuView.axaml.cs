using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Urho.Avalonia;

namespace Notepad.Views
{
    public class MenuView : UserControl
    {
        public MenuView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

          Window GetWindow() => (Window)this.VisualRoot;
    }
}
