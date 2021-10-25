using System;
using Urho.IO;

namespace Urho.Gui
{
    public class VGButton : UIElement
    {
        
        bool isFree = false;
        VGElement vgParent = null;
        bool isPressed = false;
        bool isHover = false;
        public string buttonText;

        string mImageName = "";
        int svgImage = 0;


        NVGcolor buttonColorRelease;
        NVGcolor buttonColorPress;

        NVGcolor buttonColorHover;

        float cornerRadius = 2.0f;

        NVGcolor textColor;




        [Preserve]
        public VGButton(IntPtr handle) : base(handle)
        {

        }
        [Preserve]
        public VGButton(Context context) : base(context)
        {

        }

        public VGButton() : base()
        {

        }


        public VGButton(VGElement parent,float CornerRadius = 6.0f) : this(parent, "","", new Color(0.5f, 0.5f, 0.5f, 1.0f),CornerRadius)
        {

        }

        public VGButton(VGElement parent, string text="",string imageName="", Color colorRelease = new Color(),float CornerRadius = 6.0f) : this(parent, text,imageName,colorRelease, Color.White,CornerRadius)
        {

        }

        public VGButton(VGElement parent, string text,string imageName, Color colorRelease,Color TextColor ,float CornerRadius = 6.0f) : 
        this(parent, text,imageName, colorRelease, new Color(colorRelease.R*0.8f, colorRelease.G*0.8f, colorRelease.B*0.8f, colorRelease.A),TextColor,CornerRadius)
        {
           
        }


        public VGButton(VGElement parent, string text,string imageName, Color colorRelease, Color colorPress,Color TextColor,float CornerRadius = 6.0f) : base()
        {

            var VectorGraphics = Application.Current.VectorGraphics;
           

            if (parent != null)
            {
                parent.AddChild(this);
            }

            vgParent = parent;
            buttonText = text.Trim();
            buttonColorRelease = new NVGcolor(colorRelease.R, colorRelease.G, colorRelease.B, colorRelease.A);
            buttonColorPress = new NVGcolor(colorPress.R, colorPress.G, colorPress.B, colorPress.A);
            float ratio = 1.2f;
            buttonColorHover =  new NVGcolor(Math.Clamp(colorRelease.R*ratio,0,1), Math.Clamp(colorRelease.G*ratio,0,1), Math.Clamp(colorRelease.B*ratio,0,1), colorRelease.A);
            textColor = new NVGcolor(TextColor.R, TextColor.G, TextColor.B, TextColor.A);
            cornerRadius = CornerRadius;
            this.Enabled = true;
            this.Visible = true;

            var Input = Application.Current.Input;
            Input.TouchEnd += OnPrivateTouchEnd;
            Input.MouseButtonUp += OnPrivateMousButtonUp;

            HoverBegin += OnPrivateHoverBegin;
            HoverEnd += OnPrivateHoverEnd;
            DragBegin += OnPrivateDragBegin;
            DragEnd += OnPrivateDragEnd;
            Click += OnPrivateClick;
            ClickEnd += OnPrivateClickEnd;
            DoubleClick += OnPrivateDoubleClick;
            Resized += OnPrivateButonResized;

            imageName = imageName.Trim();
            mImageName = imageName;
            if(mImageName != string.Empty)
            {
                int buttonHeight = (this.Height != 0 ) ?this.Height : Application.Current.Graphics.Height / 20;
                int buttonWidth = buttonHeight;
                svgImage = VectorGraphics.LoadSVGImage(mImageName, buttonWidth, buttonHeight, 0);

            }

        }


        private void OnPrivateButonResized(ResizedEventArgs obj)
        {
            var VectorGraphics = Application.Current.VectorGraphics;
            if(svgImage != 0)
            {
                VectorGraphics.DeleteImage(svgImage);
                if (mImageName != string.Empty)
                {
                    int buttonHeight = (this.Height != 0) ? this.Height :  Application.Current.Graphics.Height / 20;
                    int buttonWidth = buttonHeight;
                    svgImage = VectorGraphics.LoadSVGImage(mImageName, buttonWidth, buttonHeight, 0);
                }
            }
        }
        private void OnPrivateMousButtonUp(MouseButtonUpEventArgs obj)
        {
      
           if(obj.Button == (int)MouseButton.Left)
           {
                 isPressed = false;
                 
           }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!Application.isExiting)
            {
                Free();
            }
        }

        private void Free()
        {
            if(!isFree)
            {
                isFree = true;
                var Input = Application.Current.Input;
                Input.TouchEnd -= OnPrivateTouchEnd;
                Input.MouseButtonUp -= OnPrivateMousButtonUp;
                HoverBegin -= OnPrivateHoverBegin;
                HoverEnd -= OnPrivateHoverEnd;
                DragBegin -= OnPrivateDragBegin;
                DragEnd -= OnPrivateDragEnd;
                Click -= OnPrivateClick;
                ClickEnd -= OnPrivateClickEnd;
                DoubleClick -= OnPrivateDoubleClick;
                Resized -= OnPrivateButonResized;
                if (svgImage != 0)
                {
                    Application.Current.VectorGraphics.DeleteImage(svgImage);
                }
            }
        }

        private void OnPrivateTouchEnd(TouchEndEventArgs obj)
        {
              isPressed = false;
        }


        private void OnPrivateDoubleClick(DoubleClickEventArgs obj)
        {

        }

        private void OnPrivateClick(ClickEventArgs obj)
        {
        
            isPressed = true;

            // Log.Info("OnClick");
        }


        private void OnPrivateClickEnd(ClickEndEventArgs obj)
        {
             isPressed = false;
            //  Log.Info("OnClickEnd");
        }

        private void OnPrivateHoverBegin(HoverBeginEventArgs obj)
        {
            isHover = true;
        }

        private void OnPrivateHoverEnd(HoverEndEventArgs obj)
        {
            isHover = false;
        }


        private void OnPrivateDragBegin(DragBeginEventArgs obj)
        {
     
            isPressed = true;
            //  Log.Info("OnDragBegin");
        }

        
        private void OnPrivateDragEnd(DragEndEventArgs obj)
        {
      
            isPressed = false;
            // Log.Info("OnDragEnd");
        }

        public void SetParent(VGElement parent)
        {
            vgParent = parent;
        }

        public override void OnVGRenderUpdate(float timeStep)
        {
        //   string name =   vgParent.Name ;
              
            if (vgParent != null && vgParent.Visible == true)
            {
                 var position = this.ScreenPosition - vgParent.ScreenPosition;
 
                if (isPressed)
                {
                
                    drawButton(vgParent, svgImage, buttonText, position.X, position.Y, this.Size.X, this.Size.Y, buttonColorPress );
                }
                else
                {
                    if(isHover)
                    {
                        drawButton(vgParent, svgImage, buttonText, position.X, position.Y, this.Size.X, this.Size.Y, buttonColorHover);
                    }
                    else
                    {
                       
                        drawButton(vgParent, svgImage, buttonText, position.X, position.Y, this.Size.X, this.Size.Y, buttonColorRelease);
                    }
                   
                }
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

        private string cpToUTF8(int cp, byte[] str)
        {
            int n = 0;
            if (cp < 0x80)
                n = 1;
            else if (cp < 0x800)
                n = 2;
            else if (cp < 0x10000)
                n = 3;
            else if (cp < 0x200000)
                n = 4;
            else if (cp < 0x4000000)
                n = 5;
            else if (cp <= 0x7fffffff)
                n = 6;
            str[n] = (byte)'\0';
            switch (n)
            {
                case 6:
                    str[5] = (byte)(0x80 | (cp & 0x3f));
                    cp = cp >> 6;
                    cp |= 0x4000000;
                    goto case 5;
                case 5:
                    str[4] = (byte)(0x80 | (cp & 0x3f));
                    cp = cp >> 6;
                    cp |= 0x200000;
                    goto case 4;
                case 4:
                    str[3] = (byte)(0x80 | (cp & 0x3f));
                    cp = cp >> 6;
                    cp |= 0x10000;
                    goto case 3;
                case 3:
                    str[2] = (byte)(0x80 | (cp & 0x3f));
                    cp = cp >> 6;
                    cp |= 0x800;
                    goto case 2;
                case 2:
                    str[1] = (byte)(0x80 | (cp & 0x3f));
                    cp = cp >> 6;
                    cp |= 0xc0;
                    goto case 1;
                case 1:
                    str[0] = (byte)cp;
                    break;
            }

            return System.Text.Encoding.UTF8.GetString(str, 0, n);

        }


        private void drawButton(VGElement vge, int image, string text, float x, float y, float w, float h, NVGcolor col)
        {
            NVGpaint bg;
      
            float tw = 0, iw = 0;
            vge.FontSize(h * 0.7f);
            vge.FontFace("sans-bold");
      
            tw = vge.TextBounds(0, 0, text, out Rect bounds);
      
            bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(255, 255, 255, (byte)(isBlack(col) ? 16 : 100)),
                                     vge.RGBA(0, 0, 0, (byte)(isBlack(col) ? 16 : 100)));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + 1, w - 2, h - 2, cornerRadius - 1);
            if (!isBlack(col))
            {
                vge.FillColor(col);
                vge.Fill();
            }
            vge.FillPaint(bg);
            vge.Fill();

            vge.BeginPath();
            vge.RoundedRect(x + 0.5f, y + 0.5f, w - 1, h - 1, cornerRadius - 0.5f);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 48));
            vge.Stroke();

            if (image != 0)
            {
                iw = h;
                if (text == string.Empty)
                {
                    VGRendering.drawSVGImage(vge, image, x + w * 0.5f - iw * 0.5f, y+2, iw, h-4, 1.0f);
                }
                else
                {
                    VGRendering.drawSVGImage(vge, image, x + w * 0.5f - tw * 0.5f - iw * 0.8f, y+2, iw, h-4, 1.0f);
                }
            }

            vge.FontSize(h * 0.7f);
            vge.FontFace("sans-bold");
            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.FillColor(vge.RGBA(0, 0, 0, 160));
            vge.Text(x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f - 1, text);
            vge.FillColor(textColor);
            vge.Text(x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f, text);
        }

    }


}
