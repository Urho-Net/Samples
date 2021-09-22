using System;

namespace Urho.Gui
{

    public class VGLabel : UIElement
    {
        VGElement vgParent = null;
        
        string labelText;

        public string Text
        {
            set
            {
                labelText = value;
            }

            get
            {
                return labelText;
            }
        }

        NVGcolor labelTextColor;

        NVGcolor labelBackgroundColor;

        public float CornerRadius  = 6.0f;

        public VGLabel(VGElement parent, string text, Color textColor,Color backGround) : base()
        {
            if (parent != null)
            {
                parent.AddChild(this);
            }
            vgParent = parent;
            labelText = text;

            labelTextColor = new NVGcolor(textColor.R, textColor.G, textColor.B, textColor.A);
            labelBackgroundColor = new NVGcolor(backGround.R, backGround.G, backGround.B, backGround.A);

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        public override void OnVGRenderUpdate(float timeStep)
        {
            if (vgParent != null)
            {
                drawLabel(vgParent, labelText, this.Position.X, this.Position.Y, this.Size.X, this.Size.Y);
            }
        }

        private bool isBlack(NVGcolor col)
        {
            if (col.r == 0.0f && col.g == 0.0f && col.b == 0.0f && col.a == 0.0f)
            {
                return true;
            }
            return false;
        }


        void drawLabel(VGElement vge, string text, float x, float y, float w, float h)
        {
            NVGpaint bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(255, 255, 255, (byte)(isBlack(labelBackgroundColor) ? 16 : 64)),
                                          vge.RGBA(0, 0, 0, (byte)(isBlack(labelBackgroundColor) ? 16 : 64)));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + 1, w - 2, h - 2, CornerRadius - 1);
            if (!isBlack(labelBackgroundColor))
            {
                vge.FillColor(labelBackgroundColor);
                vge.Fill();
            }
            vge.FillPaint(bg);
            vge.Fill();


            vge.FontSize(h * 0.8f);
            vge.FontFace("sans-bold");
            vge.FillColor(labelTextColor);

            float tw = vge.TextBounds(0, 0, text, out Rect bounds);

            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x+w/2 - tw/2, y + h * 0.5f, text);
        }

    }

}
