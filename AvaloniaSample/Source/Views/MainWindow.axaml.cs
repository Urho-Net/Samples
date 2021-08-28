using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Urho;
using System.Threading;
using Avalonia.Interactivity;

namespace AvaloniaSample.Views
{
    public partial class MainWindow : Window
    {

        public static Thread RenderThread { get; private set; }

        AvaloniaSample avaloniaSample;
        public async void RunSample()
        {
          
    
                    ApplicationOptions options = new ApplicationOptions("Data;CoreData")
                            {
                                ExternalWindow = this.PlatformImpl.Handle.Handle,
                                DelayedStart = true
                            };

            avaloniaSample = new AvaloniaSample(options);
            // avaloniaSample = Urho.Application.CreateInstance<AvaloniaSample>(options);
            avaloniaSample.Run();

            // RenderThread = new Thread(_ =>
            //             {
            //                 ApplicationOptions options = new ApplicationOptions("Data;CoreData")
            //                 {
            //                     ExternalWindow = this.PlatformImpl.Handle.Handle,
            //                     DelayedStart = true
            //                 };
            //                 avaloniaSample = Urho.Application.CreateInstance<AvaloniaSample>(options);
            //                 avaloniaSample.Run();
            //                 var engine = Urho.Application.Current.Engine;
            //                 engine.RunFrame();
            //                 while (Urho.Application.Current != null && Urho.Application.Current.IsActive)
            //                 {
            //                     engine.RunFrame();
            //                     Thread.Sleep(500);
            //                 }

            //             });


            // RenderThread.Start();
           
        }

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // TBD ELI
            IntPtr handle = this.PlatformImpl.Handle.Handle;

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void MyButton_Click(object sender, RoutedEventArgs e)
        {
            // RunSample();
        }
    }
}