using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaloniaSample
{
    public class AvaloniaApp : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                desktopLifetime.MainWindow = new SampleAvaloniaWindow();
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
                singleViewLifetime.MainView = new SampleAvaloniaWindow();

            base.OnFrameworkInitializationCompleted();
        }
    }
}