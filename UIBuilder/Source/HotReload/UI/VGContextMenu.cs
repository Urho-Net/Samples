using System;
using System.Collections.Generic;
using System.Linq;
using Urho.IO;
using Urho.Resources;


namespace Urho.Gui
{
    public delegate void CallbackContextMenuEntry(ContextMenuEntry contextMenu);
    public class ContextMenuEntry
    {
        string _name ;
     
        public VGButton uiEntry = null;

        public VGButton UIEntry
        {
            set{
                uiEntry = value;
                if(uiEntry != null)
                {
                    uiEntry.Click += OnEntryClicked;
                }
            }
        }

        private void OnEntryClicked(ClickEventArgs obj)
        {
            _callback?.Invoke(this);
        }

        private CallbackContextMenuEntry _callback = null;

        public CallbackContextMenuEntry Callback
        {
            get
            {
                return _callback;
            }
            set
            {
                _callback = value;
            }
        }
        public string  Name
        {
            get
            {
                return _name;
            }
        }
        public ContextMenuEntry(string name)
        {
            _name = name;
        }

        public ContextMenuEntry(string name,CallbackContextMenuEntry cb)
        {
            _name = name;
            Callback = cb;
        }

        public void Free()
        {
            if (uiEntry != null)
            {
                uiEntry.Click -= OnEntryClicked;
            }
        }

    }
    public class VGContextMenu : Window
    {

        Window parentWindow = null;
        VGCanvas canvas = null;

        VGLabel title = null;
        string titleText ;
        bool  isMobile = (Application.Platform == Platforms.iOS || Application.Platform == Platforms.Android);

        List<ContextMenuEntry> entries = new List<ContextMenuEntry>();

        int entryHeight = (int)(Application.Current.Graphics.Height / 17);
        public VGContextMenu(Window parent)
        {
            parentWindow = parent;

            SetParent(Application.Current.UI.Root);
            SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Top);
            FocusMode = FocusMode.Focusable;
            LayoutMode = LayoutMode.Vertical;
            Visible = false;
            Defocused += ContextMenuDefocused;
            MinWidth = (int)((float)parent.Width / 1.2);

            entryHeight = (int)(Application.Current.Graphics.Height / 17);

            var Graphics = Application.Current.Graphics;
            canvas = CreateChild<VGCanvas>("WindowContextMenuCanvas");
            canvas.SetLayout(LayoutMode.Vertical, 0, new IntRect(1, 1, 1, 1));
            canvas.MinWidth = Width;
            canvas.ClearColor = new Color(0.1f, 0.7f, 0.9f, 1.0f);

            title = new VGLabel(canvas, "Title","", Color.Yellow, new Color(0.5f, 0.7f, 0.9f, 1.0f));
            UpdateLayout();
        }

        public void SetTitle(string titleName)
        {
            title.Text = titleName;
            titleText = titleName;
        }

        public void AddEntry(ContextMenuEntry entry)
        {
            entries.Add(entry);
            VGButton entryButton = new VGButton(canvas, entry.Name, "", new Color(0.8f, 0.6f, 0.6f, 1.0f), Color.Yellow, 0);
            entry.UIEntry = entryButton;
            SetFixedHeight(entryHeight * (entries.Count + 1));
            UpdateLayout();
        }

        public void RemoveAllEntries()
        {
            foreach (var entry in entries)
            {
                entry.Free();
            }

            entries.Clear();

            foreach(var child in canvas.Children )
            {
                child.Dispose();
            }
            canvas.RemoveAllChildren();
         
           
            title = new VGLabel(canvas, titleText,"", Color.Yellow, new Color(0.6f, 0.4f, 0.4f, 1.0f));
            SetFixedHeight(entryHeight * (entries.Count + 1));
            UpdateLayout();
        }
        
        public void Show()
        {
            var Graphics = Application.Current.Graphics;
            BringToFront();
            Visible = true;
            SetFocus(true);
            IntVector2 mousePosition = IntVector2.Zero;
            if (isMobile)
            {
                if (Application.Current.Input.NumTouches > 0)
                {
                    var touch = Application.Current.Input.GetTouch(0);
                    mousePosition = touch.Position;
                }
            }
            else
            {
                mousePosition = Application.Current.Input.MousePosition;
            }

            Position = new IntVector2((Graphics.Width - Width)/2, mousePosition.Y);
            if (Position.Y + Height >= Graphics.Height)
            {
                Position = new IntVector2(parentWindow.Position.X + 20, mousePosition.Y - Height);
            }
        }

        public void Hide()
        {
            Visible = false;
        }
        private void ContextMenuDefocused(DefocusedEventArgs obj)
        {
            Hide();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
          
            if (!Application.isExiting)
            {
                Defocused -= ContextMenuDefocused;
            }
        }
    }
}
