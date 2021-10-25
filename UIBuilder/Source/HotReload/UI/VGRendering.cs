using Urho;
using System;
using System.Collections.Generic;
using Urho.Gui;

namespace Urho
{

    public static class VGRendering
    {
        public static int fontNormal, fontBold, fontIcons, fontEmoji;

        
        public static void LoadResources()
        {
            fontIcons = Application.Current.VectorGraphics.CreateFont("icons", "nanovg/fonts/entypo.ttf");
            fontNormal = Application.Current.VectorGraphics.CreateFont("sans", "nanovg/fonts/Roboto-Regular.ttf");
            fontBold = Application.Current.VectorGraphics.CreateFont("sans-bold", "nanovg/fonts/Roboto-Bold.ttf");
            fontEmoji = Application.Current.VectorGraphics.CreateFont("emoji", "nanovg/fonts/NotoEmoji-Regular.ttf");

            Application.Current.VectorGraphics.AddFallbackFontId(fontNormal, fontEmoji);
            Application.Current.VectorGraphics.AddFallbackFontId(fontBold, fontEmoji);
        }

        public static NVGcolor ColorToNVGColor(Color color)
        {
            return new NVGcolor(color.R, color.G, color.B, color.A);
        }

        public static Color NVGColorToColor(NVGcolor color)
        {
            return new Color(color.r, color.g, color.b, color.a);
        }

        public static void drawSVGImage(VGFrameBuffer vge, int imageID, float x, float y, float w, float h, float a)
        {
            NVGpaint imgPaint;

            imgPaint = vge.ImagePattern(x, y, w, h, 0, imageID, a);
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, 3.0f);
            vge.FillPaint(imgPaint);
            vge.Fill();
        }

        public static void drawSVGImage(VGElement vge, int imageID, float x, float y, float w, float h, float a)
        {
            NVGpaint imgPaint;

            imgPaint = vge.ImagePattern(x, y, w, h, 0, imageID, a);
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, 3.0f);
            vge.FillPaint(imgPaint);
            vge.Fill();
        }

    }

}