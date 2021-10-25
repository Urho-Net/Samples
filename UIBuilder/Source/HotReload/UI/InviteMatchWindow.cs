using System;
using Urho.IO;
using Urho.Resources;

namespace Urho.Gui
{
    public class InviteMatchWindow : Window
    {

        int FontSize = Application.Current.Graphics.Height / 30;
        public static VGButton buttonInviteMatch;

        private static InviteMatchWindow instance = null;

        public static void Show(string title)
        {
            if (instance != null)
            {

                instance.Dispose();
                instance = null;
            }

            instance = new InviteMatchWindow(title);
        }

        public static void Hide()
        {
            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }
        }

        public InviteMatchWindow(string title)
        {
            Application.Current.UI.Root.AddChild(this);
            var Graphics = Application.Current.Graphics;
            SetStyleAuto();
            SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Top);
            Resizable = false;
            Movable = false;
            LayoutBorder = new IntRect(0, 6, 0, 6);
            MinWidth = (int)(Graphics.Width/1.3);
            LayoutMode = LayoutMode.Vertical;

            var canvas = NewWindowCanvasEntry(this) as VGCanvas;
            var windowTitle = new VGLabel(canvas, title,"", Color.Yellow, new Color(0.6f, 0.4f, 0.4f, 1.0f));
            windowTitle.CornerRadius = 0f;

            var dropDownGameCount = CreatePlayerDropListEntry(this, "Games", "");
            for (int i = 3; i <= 7; i += 2)
            {
                 AddDropDownEntry(dropDownGameCount , i.ToString() + "  ");
            }

            dropDownGameCount.SetFocus(true);


            var DropDownGameDifficulty = CreatePlayerDropListEntry(this, "Difficulty", "");
            AddDropDownEntry(DropDownGameDifficulty, "Easy ");
            AddDropDownEntry(DropDownGameDifficulty, "Medium ");
            AddDropDownEntry(DropDownGameDifficulty, "Hard ");

            canvas = NewWindowCanvasEntry(this) as VGCanvas;
            buttonInviteMatch = new VGButton(canvas, "Invite player", "", new Color(0.7f, 0.5f, 0.5f, 1.0f), new Color(1.0f, 1.0f, 0.0f, 1.0f), 0.0f);
            buttonInviteMatch.Click += OnButtonInviteMatchClick;

            Position = new IntVector2((Graphics.Width-Width)/2, Graphics.Height / 2 - Height);

            UpdateLayout();
        }

        void AddDropDownEntry(DropDownList dropDown , string entryString)
        {
                var entry = new Text();
                dropDown.AddItem(entry);
                entry.SetStyleAuto();
                entry.SetFont(Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), FontSize);

                entry.Value = entryString;
        }

        private void OnButtonInviteMatchClick(ClickEventArgs obj)
        {
            Log.Info("Button invite match click");
        }

        protected override void Dispose(bool disposing)
        {
            Visible = false;
            base.Dispose(disposing);
            buttonInviteMatch.Click -= OnButtonInviteMatchClick;
            buttonInviteMatch.Dispose();
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

        DropDownList CreatePlayerDropListEntry(Window window, string text, string lineEditText)
        {
            var container = NewWindowCanvasEntry(window);

            var canvas = container as VGCanvas;
            var windowTitle = new VGLabel(canvas, text,"", Color.Yellow, new Color(0.8f, 0.6f, 0.6f, 1.0f));
            windowTitle.CornerRadius = 0f;
            canvas.LayoutSpacing = 5;

            DropDownList dropDown = container.CreateDropDownList();
            dropDown.SetStyleAuto();
      
            foreach (var child in dropDown.Children)
            {
                if (child is Text)
                {
                    Text textChild = child as Text;
                    Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf");
                    textChild.SetFont(Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), FontSize);
                }
            }

            return dropDown;
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