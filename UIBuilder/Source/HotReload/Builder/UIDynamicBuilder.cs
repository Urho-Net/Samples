using Urho;
using System;
using Urho.IO;
using Urho.Gui;


namespace UIBuilder
{
    class UIDynamicBuilder : Component
    {


        // This is the main entry point of the dynamic UI builder
        // You can create new UI Windows and check them by instancing the Window
        private void Start()
        {
            // Only 1 window should be uncommented

            new TreeViewWindow();
            
            // new MathWindow();
            //  new LoginWindow();   
            //   new InviteMatchWindow("Player 2");
            // new RegisterWindow();

        }

        void DisposeElementAndChildren(UIElement uiElement)
        {
            foreach (var child in uiElement.Children)
            {
                DisposeElementAndChildren(child);
                child.Dispose();
            }
        }
        private void Stop()
        {
            if (Application.IsActive)
            {
                var UI = Application.Current.UI;
                DisposeElementAndChildren(UI.Root);
                UI.Root.RemoveAllChildren();
            }
        }

        public UIDynamicBuilder(IntPtr handle) : base(handle)
        {

        }
        public UIDynamicBuilder()
        {

        }

        public override void OnSceneSet(Scene scene)
        {
            try
            {
                if (scene != null)
                {
                    Start();
                }
                else
                {
                    Stop();
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString(),ex);
            }
      
        }

    }
}