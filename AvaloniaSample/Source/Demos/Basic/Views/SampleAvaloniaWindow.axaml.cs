using Avalonia.Markup.Xaml;

namespace AvaloniaSample
{
    public class SampleAvaloniaWindow : Avalonia.Controls.Window
    {
        public SampleAvaloniaWindow()
        {
            InitializeComponent();
#if DEBUG
            //this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}