using System;
using Urho.IO;
using Urho.Resources;

namespace Urho.Gui
{
    public class LoginWindow : Window
    {
        VGButton buttonLogin;

        public LoginWindow()
        {
            Application.Current.UI.Root.AddChild(this);
            var Graphics = Application.Current.Graphics;
            SetStyleAuto();
            SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Top);
            Resizable = false;
            Movable = false;
            LayoutBorder = new IntRect(0, 6, 0, 6);
            MinWidth = (int)(Graphics.Width/1.2);
            LayoutMode = LayoutMode.Vertical;

            var canvas = NewWindowCanvasEntry(this) as VGCanvas;
            var windowTitle = new VGLabel(canvas, "Login","", Color.Yellow, new Color(0.6f, 0.4f, 0.4f, 1.0f));
            windowTitle.CornerRadius = 0f;

            CreatePlayerInputEntry(this, " Player ", "").SetFocus(true);
            CreatePlayerInputEntry(this, " Password ", "");

            canvas = NewWindowCanvasEntry(this) as VGCanvas;
            buttonLogin = new VGButton(canvas, "Let's Play", "", new Color(0.7f, 0.5f, 0.5f, 1.0f), new Color(1.0f, 1.0f, 0.0f, 1.0f), 0.0f);
            buttonLogin.Click += OnButtonLoginClick;

            Position = new IntVector2((Graphics.Width - Width)/2, Graphics.Height / 2 - Height);

            UpdateLayout();
        }

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        private void OnButtonLoginClick(ClickEventArgs obj)
        {
            Log.Info("Button Login click");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            buttonLogin.Click -= OnButtonLoginClick;
            buttonLogin.Dispose();
        }
        LineEdit CreatePlayerInputEntry(Window window, string text, string lineEditText)
        {
            var container = NewWindowCanvasEntry(window);

            var canvas = container as VGCanvas;
            var windowTitle = new VGLabel(canvas, text,"", Color.Yellow, new Color(0.8f, 0.6f, 0.6f, 1.0f));
            windowTitle.CornerRadius = 0f;
            canvas.LayoutSpacing = 5;

            LineEdit lineEdit = container.CreateLineEdit();
            lineEdit.SetStyleAuto();
            lineEdit.Text = lineEditText;
            lineEdit.CursorPosition = 0;
            foreach (var child in lineEdit.Children)
            {
                if (child is Text)
                {
                    Text textChild = child as Text;
                    Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf");
                    int FontSize = Application.Current.Graphics.Height / 30;
                    textChild.SetFont(Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), FontSize);
                }
            }
            return lineEdit;
        }
        UIElement NewWindowCanvasEntry(Window window)
        {
            var Graphics = Application.Graphics;
            var canvas =  CreateChild<VGCanvas>("WindowCanvasEntry");
            canvas.SetLayout(LayoutMode.Horizontal, 0, new IntRect(1, 1, 1, 1));
            canvas.SetFixedHeight((int)(Graphics.Height / 18));
            canvas.MinWidth = window.Width;
            canvas.ClearColor = new Color(0.1f, 0.7f, 0.9f, 1.0f);
            return canvas;
        }
    }

}