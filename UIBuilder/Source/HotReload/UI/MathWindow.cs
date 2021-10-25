

using Urho.IO; // for Log.Info

namespace Urho.Gui
{
    public class MathWindow : Window
    {
        VGButton buttonAnswerOne;
        VGButton buttonAnswerTwo;
        VGButton buttonAnswerThree;

        public MathWindow()
        {
            Application.Current.UI.Root.AddChild(this);
            var Graphics = Application.Current.Graphics;
            SetStyleAuto();
            // can be modified if the window should be resizable or movebale
            Resizable = false;
            Movable = false;
            LayoutBorder = new IntRect(6, 6, 6, 6);
            MinWidth = (int)(Graphics.Width/1.2);
            LayoutMode = LayoutMode.Vertical;

            var canvas = NewWindowCanvasEntry();
            // height calculated fo title and 3 buttons
            canvas.SetFixedHeight((int)(Graphics.Height / 18)*4);

            var mathLabel = new VGLabel(canvas, "4+5 = " ,"", Color.Yellow, new Color(0.6f, 0.4f, 0.4f, 1.0f));
            mathLabel.CornerRadius = 0f;

            buttonAnswerOne = new VGButton(canvas, "3", "", new Color(0.2f, 0.8f, 0.7f, 1.0f), new Color(1.0f, 1.0f, 0.0f, 1.0f), 0.0f);
            buttonAnswerOne.Click += OnButtonLoginClick;

            buttonAnswerTwo = new VGButton(canvas, "9", "", new Color(0.2f, 0.8f, 0.7f, 1.0f), new Color(1.0f, 1.0f, 0.0f, 1.0f), 0.0f);
            buttonAnswerTwo.Click += OnButtonLoginClick;

            buttonAnswerThree = new VGButton(canvas, "12", "", new Color(0.2f, 0.8f, 0.7f, 1.0f), new Color(1.0f, 1.0f, 0.0f, 1.0f), 0.0f);
            buttonAnswerThree.Click += OnButtonLoginClick;


            Position = new IntVector2((Graphics.Width - Width)/2, Graphics.Height / 2 - Height);

            // must be called at the end
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
            
            if(obj.Element == buttonAnswerOne)
            {
                Log.Info("Button buttonAnswerOne click");
            }
            else if(obj.Element == buttonAnswerTwo)
            {
                Log.Info("Button buttonAnswerTwo click");
            }
            else if (obj.Element == buttonAnswerThree)
            {
                Log.Info("Button buttonAnswerThree click");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            buttonAnswerOne.Click -= OnButtonLoginClick;
            buttonAnswerOne.Dispose();

            buttonAnswerTwo.Click -= OnButtonLoginClick;
            buttonAnswerTwo.Dispose();

            buttonAnswerThree.Click -= OnButtonLoginClick;
            buttonAnswerThree.Dispose();
        }

        VGCanvas NewWindowCanvasEntry(int height = 0)
        {
            var Graphics = Application.Graphics;
            var canvas =  CreateChild<VGCanvas>("WindowCanvasEntry");
            canvas.SetLayout(LayoutMode.Vertical, 0, new IntRect(1, 1, 1, 1));
            if (height != 0)
            {
                canvas.SetFixedHeight(height);
            }
            else
            {
                canvas.SetFixedHeight((int)(Graphics.Height / 18));
            }
            canvas.ClearColor = new Color(0.1f, 0.7f, 0.9f, 1.0f);
            return canvas;
        }
    }

}