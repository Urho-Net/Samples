using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Visuals;
using Urho.Gui;
using Urho.IO;

namespace Urho.Avalonia
{
    public class WindowTitleBar : VGCanvas
    {

        int windowTitleBarHeight;
        UrhoAvaloniaElement  parent;


        public VGLabel windowTitle = null;

        VGButton closeButton = null;
        // VGButton minimizeButton = null;

        VGButton maximizeButton = null;


        public WindowTitleBar(UrhoAvaloniaElement _parent) : base()
        {
            parent = _parent;
            windowTitleBarHeight = Application.Current.Graphics.Height / 40;

            FocusMode = FocusMode.Focusable;
            Enabled = true;

            DragBegin += OnWindowTitleBarDragBegin;
            DragMove += OnWindowTitleBarDragMove;
            DragEnd += OnWindowTitleBarDragEnd;
            Focused += OnFocused;
            Defocused += OnDefocused;

            SetLayout(LayoutMode.Horizontal, 0, new IntRect(0, 0, 0, 0));
            LayoutSpacing = 0;
            MinHeight = windowTitleBarHeight;
            Size = new IntVector2(200, windowTitleBarHeight);
            Position = new IntVector2(0, -windowTitleBarHeight);
            ClearColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
            windowTitle = new VGLabel(this, "", Color.Black, new Color(1.0f, 1.0f, 1.0f, 1.0f));
            windowTitle.CornerRadius = 0f;

            int buttonContainerSize = 2 * (Application.Current.Graphics.Height / 40 + 5);
            int buttonSize = Application.Current.Graphics.Height / 40;

            var buttonsContainer = CreateChild<UIElement>();
            buttonsContainer.MaxWidth = buttonContainerSize;
            // buttonsContainer.MinWidth = buttonContainerSize;
            buttonsContainer.SetLayout(LayoutMode.Horizontal, 0, new IntRect(5, 5, 5, 5));
            buttonsContainer.LayoutSpacing = 5;


            // minimizeButton = new VGButton(this, "__","", new Color(0.5f, 0.5f, 0.5f, 1.0f),6);
            // buttonsContainer.AddChild(minimizeButton);
            // minimizeButton.MaxWidth = buttonSize;

            maximizeButton = new VGButton(this, "[  ]","", new Color(0.5f, 0.5f, 0.5f, 1.0f),6);
            buttonsContainer.AddChild(maximizeButton);
            maximizeButton.MaxWidth = buttonSize;
            maximizeButton.Click += OnToggleMaximizeWindow;

            closeButton = new VGButton(this, "X","", new Color(0.5f, 0.5f, 0.5f, 1.0f),6);
            buttonsContainer.AddChild(closeButton);
            closeButton.MaxWidth = buttonSize;
            closeButton.Click += OnCloseWindow;


        }

        private void OnDefocused(DefocusedEventArgs obj)
        {
            parent.SetFocus(false);
        }

        private void OnFocused(FocusedEventArgs obj)
        {
             parent.SetFocus(true);
        }

        private void OnToggleMaximizeWindow(ClickEventArgs obj)
        {
            parent.ToggleMaximizeWindow(); 
        }

        protected override void Dispose(bool disposing)
        {
     
            if (!Application.isExiting)
            {

                closeButton.Click -= OnCloseWindow;
                maximizeButton.Click -= OnToggleMaximizeWindow;

                windowTitle.Dispose();
                // minimizeButton.Dispose();
                maximizeButton.Dispose();
                closeButton.Dispose();

                DragBegin -= OnWindowTitleBarDragBegin;
                DragMove -= OnWindowTitleBarDragMove;
                DragEnd -= OnWindowTitleBarDragEnd;

                closeButton.Click -= OnCloseWindow;
                maximizeButton.Click -= OnToggleMaximizeWindow;

                Focused -= OnFocused;
                Defocused -= OnDefocused;

                this.Remove();
            }

            base.Dispose(disposing);

        }
     

        private void OnCloseWindow(ClickEventArgs obj)
        {
            var window = parent._windowImpl as UrhoWindowImpl;
            window.Dispose();
        }

        private void OnWindowTitleBarDragBegin(DragBeginEventArgs obj)
        {
            parent.OnWindowTitleDragBegin(obj);
        }

        private void OnWindowTitleBarDragMove(DragMoveEventArgs obj)
        {
           parent.OnWindowTitleDragMove(obj);
        }

        private void OnWindowTitleBarDragEnd(DragEndEventArgs obj)
        {
            parent.OnWindowTitleDragEnd(obj);
        }

      
    }

}