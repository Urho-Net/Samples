using System;

namespace Urho.Gui
{

    public class VGLabel : UIElement
    {
        VGElement vgParent = null;
        
        string labelText;

        int svgImage = 0;

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

        public float CornerRadius  = 0.0f;

        public VGLabel(VGElement parent, string text,string imageName, Color textColor,Color backGround) : base()
        {
            if (parent != null)
            {
                parent.AddChild(this);
            }
            vgParent = parent;
            labelText = text;

            labelTextColor = new NVGcolor(textColor.R, textColor.G, textColor.B, textColor.A);
            labelBackgroundColor = new NVGcolor(backGround.R, backGround.G, backGround.B, backGround.A);


            if (imageName != "")
            {
                var VectorGraphics = Application.Current.VectorGraphics;
                int imageSize = (this.Height != 0) ? this.Height : Application.Current.Graphics.Height / 20;
                svgImage = VectorGraphics.LoadSVGImage(imageName, imageSize, imageSize, 0);
            }

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!Application.isExiting)
            {
                if (svgImage != 0)
                {
                    Application.Current.VectorGraphics.DeleteImage(svgImage);
                }
            }
        }


        public override void OnVGRenderUpdate(float timeStep)
        {
            if (vgParent != null)
            {
                var position = this.ScreenPosition - vgParent.ScreenPosition;
                drawLabel(vgParent, svgImage, labelText, position.X, position.Y, this.Size.X, this.Size.Y);
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


        void drawLabel(VGElement vge,int image, string text, float x, float y, float w, float h)
        {
            float iw = 0;
            NVGpaint bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(255, 255, 255, (byte)(isBlack(labelBackgroundColor) ? 16 : 64)),
                                          vge.RGBA(0, 0, 0, (byte)(isBlack(labelBackgroundColor) ? 16 : 64)));
            vge.BeginPath();
            vge.RoundedRect(x , y , w  , h, CornerRadius - 1);
            if (!isBlack(labelBackgroundColor))
            {
                vge.FillColor(labelBackgroundColor);
                vge.Fill();
            }
            vge.FillPaint(bg);
            vge.Fill();

            vge.FontSize(h * 0.8f);
            vge.FontFace("sans-bold");
      

            float tw = vge.TextBounds(0, 0, text, out Rect bounds);

            if (image != 0)
            {
                iw = h;
                if (text == string.Empty)
                {
                    VGRendering.drawSVGImage(vge, image, x + w * 0.5f - iw * 0.5f, y + 2, iw, h - 4, 1.0f);
                }
                else
                {
                    VGRendering.drawSVGImage(vge, image, x + w * 0.5f - tw * 0.5f - iw * 0.8f, y + 2, iw, h - 4, 1.0f);
                }
            }

            vge.FillColor(labelTextColor);
            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
           
            vge.Text(x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f, text);
        }

    }

}
