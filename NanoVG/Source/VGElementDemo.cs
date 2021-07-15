using Urho;
using System;
using Urho.Gui;

namespace NanoVGSample
{

    public partial class NanoVGSample : Sample
    {

        const int ICON_SEARCH = 0x1F50D;
        const int ICON_CIRCLED_CROSS = 0x2716;
        const int ICON_CHEVRON_RIGHT = 0xE75E;
        const int ICON_CHECK = 0x2713;
        const int ICON_LOGIN = 0xE740;
        const int ICON_TRASH = 0xE729;
        void loadDemoData()
        {

            for (int i = 0; i < 12; i++)
            {
                string file = String.Format("nanovg/images/image{0}.jpg", i + 1);
                demoData_.images[i] = VectorGraphics.CreateImage(file, 0);
            }

            demoData_.svgImage = VectorGraphics.CreateImage("nanosvg/23_modified.svg", 0);

            demoData_.fontIcons = VectorGraphics.CreateFont("icons", "nanovg/fonts/entypo.ttf");
            demoData_.fontNormal = VectorGraphics.CreateFont("sans", "nanovg/fonts/Roboto-Regular.ttf");
            demoData_.fontBold = VectorGraphics.CreateFont("sans-bold", "nanovg/fonts/Roboto-Bold.ttf");
            demoData_.fontEmoji = VectorGraphics.CreateFont("emoji", "nanovg/fonts/NotoEmoji-Regular.ttf");

            VectorGraphics.AddFallbackFontId(demoData_.fontNormal, demoData_.fontEmoji);
            VectorGraphics.AddFallbackFontId(demoData_.fontBold, demoData_.fontEmoji);
        }


        void renderVGElement(VGElement vge, float mx, float my, float width, float height, float t, int blowup, DemoData data)
        {
            float x, y, popy;
            drawEyes(vge, width - 250, 50, 150, 100, mx, my, t);
            drawParagraph(vge, width - 450, 50, 150, 100, mx, my);
            drawGraph(vge, 0, height / 2, width, height / 2, t);
            drawColorwheel(vge, width - 300, height - 300, 250.0f, 250.0f, t);
            drawLines(vge, 120, height - 50, 600, 50, t);
            drawWidths(vge, 10, 50, 30);
            drawCaps(vge, 10, 300, 30);
            drawScissor(vge, 50, height - 80, t);


            vge.SaveState();
            if (blowup == 1)
            {
                vge.Rotate(MathF.Sin(t * 0.3f) * 5.0f / 180.0f * MathF.PI);
                vge.Scale(2.0f, 2.0f);
            }

            // Widgets
            drawWindow(vge, "Widgets `n Stuff", 50, 50, 300, 400);
            x = 60;
            y = 95;
            drawSearchBox(vge, "Search", x, y, 280, 25);
            y += 40;
            drawDropDown(vge, "Effects", x, y, 280, 28);
            popy = y + 14;
            y += 45;

            // Form
            drawLabel(vge, "Login", x, y, 280, 20);
            y += 25;
            drawEditBox(vge, "Email", x, y, 280, 28);
            y += 35;
            drawEditBox(vge, "Password", x, y, 280, 28);
            y += 38;
            drawCheckBox(vge, "Remember me", x, y, 140, 28);
            drawButton(vge, ICON_LOGIN, "Sign in", x + 138, y, 140, 28, vge.RGBA(0, 96, 128, 255));
            y += 45;

            // Slider
            drawLabel(vge, "Diameter", x, y, 280, 20);
            y += 25;
            drawEditBoxNum(vge, "123.00", "px", x + 180, y, 100, 28);
            drawSlider(vge, 0.4f, x, y, 170, 28);
            y += 55;

            drawButton(vge, ICON_TRASH, "Delete", x, y, 160, 28, vge.RGBA(128, 16, 8, 255));
            drawButton(vge, 0, "Cancel", x + 170, y, 110, 28, vge.RGBA(0, 0, 0, 0));

            // Thumbnails box
            drawThumbnails(vge, 365, popy - 30, 160, 300, data.images, 12, t);

            float svgSize = Math.Min(width / 3.0f, height / 3.0f);
            drawSVGImage(vge, data.svgImage, width - svgSize, svgSize, svgSize, svgSize, 1.0f);

            vge.RestoreState();
        }

        void drawEyes(VGElement vge, float x, float y, float w, float h, float mx, float my, float t)
        {
            NVGpaint gloss, bg;
            float ex = w * 0.23f;
            float ey = h * 0.5f;
            float lx = x + ex;
            float ly = y + ey;
            float rx = x + w - ex;
            float ry = y + ey;
            float dx, dy, d;
            float br = (ex < ey ? ex : ey) * 0.5f;
            float blink = 1 - MathF.Pow(MathF.Sin(t * 0.5f), 200) * 0.8f;

            bg = vge.LinearGradient(x, y + h * 0.5f, x + w * 0.1f, y + h, vge.RGBA(0, 0, 0, 32), vge.RGBA(0, 0, 0, 16));
            vge.BeginPath();
            vge.Ellipse(lx + 3.0f, ly + 16.0f, ex, ey);
            vge.Ellipse(rx + 3.0f, ry + 16.0f, ex, ey);
            vge.FillPaint(bg);
            vge.Fill();

            bg = vge.LinearGradient(x, y + h * 0.25f, x + w * 0.1f, y + h, vge.RGBA(220, 220, 220, 255),
                                     vge.RGBA(128, 128, 128, 255));
            vge.BeginPath();
            vge.Ellipse(lx, ly, ex, ey);
            vge.Ellipse(rx, ry, ex, ey);
            vge.FillPaint(bg);
            vge.Fill();


            dx = (mx - rx) / (ex * 10);
            dy = (my - ry) / (ey * 10);
            d = MathF.Sqrt(dx * dx + dy * dy);
            if (d > 1.0f)
            {
                dx /= d;
                dy /= d;
            }
            dx *= ex * 0.4f;
            dy *= ey * 0.5f;
            vge.BeginPath();
            vge.Ellipse(lx + dx, ly + dy + ey * 0.25f * (1 - blink), br, br * blink);
            vge.FillColor(vge.RGBA(32, 32, 32, 255));
            vge.Fill();

            dx = (mx - rx) / (ex * 10);
            dy = (my - ry) / (ey * 10);
            d = MathF.Sqrt(dx * dx + dy * dy);
            if (d > 1.0f)
            {
                dx /= d;
                dy /= d;
            }
            dx *= ex * 0.4f;
            dy *= ey * 0.5f;
            vge.BeginPath();
            vge.Ellipse(rx + dx, ry + dy + ey * 0.25f * (1 - blink), br, br * blink);
            vge.FillColor(vge.RGBA(32, 32, 32, 255));
            vge.Fill();

            gloss = vge.RadialGradient(lx - ex * 0.25f, ly - ey * 0.5f, ex * 0.1f, ex * 0.75f, vge.RGBA(255, 255, 255, 128),
                                        vge.RGBA(255, 255, 255, 0));
            vge.BeginPath();
            vge.Ellipse(lx, ly, ex, ey);
            vge.FillPaint(gloss);
            vge.Fill();

            gloss = vge.RadialGradient(rx - ex * 0.25f, ry - ey * 0.5f, ex * 0.1f, ex * 0.75f, vge.RGBA(255, 255, 255, 128),
                                        vge.RGBA(255, 255, 255, 0));
            vge.BeginPath();
            vge.Ellipse(rx, ry, ex, ey);
            vge.FillPaint(gloss);
            vge.Fill();
        }

        void drawGraph(VGElement vge, float x, float y, float w, float h, float t)
        {
            NVGpaint bg;
            float[] samples = new float[6];
            float[] sx = new float[6], sy = new float[6];
            float dx = w / 5.0f;
            int i;

            samples[0] = (1 + MathF.Sin(t * 1.2345f + MathF.Cos(t * 0.33457f) * 0.44f)) * 0.5f;
            samples[1] = (1 + MathF.Sin(t * 0.68363f + MathF.Cos(t * 1.3f) * 1.55f)) * 0.5f;
            samples[2] = (1 + MathF.Sin(t * 1.1642f + MathF.Cos(t * 0.33457f) * 1.24f)) * 0.5f;
            samples[3] = (1 + MathF.Sin(t * 0.56345f + MathF.Cos(t * 1.63f) * 0.14f)) * 0.5f;
            samples[4] = (1 + MathF.Sin(t * 1.6245f + MathF.Cos(t * 0.254f) * 0.3f)) * 0.5f;
            samples[5] = (1 + MathF.Sin(t * 0.345f + MathF.Cos(t * 0.03f) * 0.6f)) * 0.5f;

            for (i = 0; i < 6; i++)
            {
                sx[i] = x + i * dx;
                sy[i] = y + h * samples[i] * 0.8f;
            }

            // Graph background
            bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(0, 160, 192, 0), vge.RGBA(0, 160, 192, 64));
            vge.BeginPath();
            vge.MoveTo(sx[0], sy[0]);
            for (i = 1; i < 6; i++)
                vge.BezierTo(sx[i - 1] + dx * 0.5f, sy[i - 1], sx[i] - dx * 0.5f, sy[i], sx[i], sy[i]);
            vge.LineTo(x + w, y + h);
            vge.LineTo(x, y + h);
            vge.FillPaint(bg);
            vge.Fill();

            // Graph line
            vge.BeginPath();
            vge.MoveTo(sx[0], sy[0] + 2);
            for (i = 1; i < 6; i++)
                vge.BezierTo(sx[i - 1] + dx * 0.5f, sy[i - 1] + 2, sx[i] - dx * 0.5f, sy[i] + 2, sx[i], sy[i] + 2);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 32));
            vge.StrokeWidth(3.0f);
            vge.Stroke();

            vge.BeginPath();
            vge.MoveTo(sx[0], sy[0]);
            for (i = 1; i < 6; i++)
                vge.BezierTo(sx[i - 1] + dx * 0.5f, sy[i - 1], sx[i] - dx * 0.5f, sy[i], sx[i], sy[i]);
            vge.StrokeColor(vge.RGBA(0, 160, 192, 255));
            vge.StrokeWidth(3.0f);
            vge.Stroke();

            // Graph sample pos
            for (i = 0; i < 6; i++)
            {
                bg = vge.RadialGradient(sx[i], sy[i] + 2, 3.0f, 8.0f, vge.RGBA(0, 0, 0, 32), vge.RGBA(0, 0, 0, 0));
                vge.BeginPath();
                vge.Rect(sx[i] - 10, sy[i] - 10 + 2, 20, 20);
                vge.FillPaint(bg);
                vge.Fill();
            }

            vge.BeginPath();
            for (i = 0; i < 6; i++)
                vge.Circle(sx[i], sy[i], 4.0f);
            vge.FillColor(vge.RGBA(0, 160, 192, 255));
            vge.Fill();
            vge.BeginPath();
            for (i = 0; i < 6; i++)
                vge.Circle(sx[i], sy[i], 2.0f);
            vge.FillColor(vge.RGBA(220, 220, 220, 255));
            vge.Fill();

            vge.StrokeWidth(1.0f);
        }


        void drawColorwheel(VGElement vge, float x, float y, float w, float h, float t)
        {
            int i;
            float r0, r1, ax, ay, bx, by, cx, cy, aeps, r;
            float hue = MathF.Sin(t * 0.12f);
            NVGpaint paint;

            vge.SaveState();

            cx = x + w * 0.5f;
            cy = y + h * 0.5f;
            r1 = (w < h ? w : h) * 0.5f - 5.0f;
            r0 = r1 - 20.0f;
            aeps = 0.5f / r1; // half a pixel arc length in radians (2pi cancels out).

            for (i = 0; i < 6; i++)
            {
                float a0 = (float)i / 6.0f * MathF.PI * 2.0f - aeps;
                float a1 = (float)(i + 1.0f) / 6.0f * MathF.PI * 2.0f + aeps;
                vge.BeginPath();
                vge.Arc(cx, cy, r0, a0, a1, NVGwinding.CW);
                vge.Arc(cx, cy, r1, a1, a0, NVGwinding.CCW);
                vge.ClosePath();
                ax = cx + MathF.Cos(a0) * (r0 + r1) * 0.5f;
                ay = cy + MathF.Sin(a0) * (r0 + r1) * 0.5f;
                bx = cx + MathF.Cos(a1) * (r0 + r1) * 0.5f;
                by = cy + MathF.Sin(a1) * (r0 + r1) * 0.5f;
                paint = vge.LinearGradient(ax, ay, bx, by, vge.HSLA(a0 / (MathF.PI * 2), 1.0f, 0.55f, 255),
                                            vge.HSLA(a1 / (MathF.PI * 2), 1.0f, 0.55f, 255));
                vge.FillPaint(paint);
                vge.Fill();
            }

            vge.BeginPath();
            vge.Circle(cx, cy, r0 - 0.5f);
            vge.Circle(cx, cy, r1 + 0.5f);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 64));
            vge.StrokeWidth(1.0f);
            vge.Stroke();

            // Selector
            vge.SaveState();
            vge.Translate(cx, cy);
            vge.Rotate(hue * MathF.PI * 2);

            // Marker on
            vge.StrokeWidth(2.0f);
            vge.BeginPath();
            vge.Rect(r0 - 1, -3, r1 - r0 + 2, 6);
            vge.StrokeColor(vge.RGBA(255, 255, 255, 192));
            vge.Stroke();

            paint = vge.BoxGradient(r0 - 3, -5, r1 - r0 + 6, 10, 2, 4, vge.RGBA(0, 0, 0, 128), vge.RGBA(0, 0, 0, 0));
            vge.BeginPath();
            vge.Rect(r0 - 2 - 10, -4 - 10, r1 - r0 + 4 + 20, 8 + 20);
            vge.Rect(r0 - 2, -4, r1 - r0 + 4, 8);
            vge.PathWinding(NVGwinding.Hole);
            vge.FillPaint(paint);
            vge.Fill();

            // Center triangle
            r = r0 - 6;
            ax = MathF.Cos(120.0f / 180.0f * MathF.PI) * r;
            ay = MathF.Sin(120.0f / 180.0f * MathF.PI) * r;
            bx = MathF.Cos(-120.0f / 180.0f * MathF.PI) * r;
            by = MathF.Sin(-120.0f / 180.0f * MathF.PI) * r;
            vge.BeginPath();
            vge.MoveTo(r, 0);
            vge.LineTo(ax, ay);
            vge.LineTo(bx, by);
            vge.ClosePath();
            paint = vge.LinearGradient(r, 0, ax, ay, vge.HSLA(hue, 1.0f, 0.5f, 255), vge.RGBA(255, 255, 255, 255));
            vge.FillPaint(paint);
            vge.Fill();
            paint = vge.LinearGradient((r + ax) * 0.5f, (0 + ay) * 0.5f, bx, by, vge.RGBA(0, 0, 0, 0), vge.RGBA(0, 0, 0, 255));
            vge.FillPaint(paint);
            vge.Fill();
            vge.StrokeColor(vge.RGBA(0, 0, 0, 64));
            vge.Stroke();

            // Select circle on triangle
            ax = MathF.Cos(120.0f / 180.0f * MathF.PI) * r * 0.3f;
            ay = MathF.Sin(120.0f / 180.0f * MathF.PI) * r * 0.4f;
            vge.StrokeWidth(2.0f);
            vge.BeginPath();
            vge.Circle(ax, ay, 5);
            vge.StrokeColor(vge.RGBA(255, 255, 255, 192));
            vge.Stroke();

            paint = vge.RadialGradient(ax, ay, 7, 9, vge.RGBA(0, 0, 0, 64), vge.RGBA(0, 0, 0, 0));
            vge.BeginPath();
            vge.Rect(ax - 20, ay - 20, 40, 40);
            vge.Circle(ax, ay, 7);
            vge.PathWinding(NVGwinding.Hole);
            vge.FillPaint(paint);
            vge.Fill();

            vge.RestoreState();

            vge.RestoreState();
        }

        void drawLines(VGElement vge, float x, float y, float w, float h, float t)
        {
            int i, j;
            float pad = 5.0f, s = w / 9.0f - pad * 2;
            float[] pts = new float[4 * 2];
            float fx, fy;

            int[] joins = new int[3] { NVGlineCap.Miter, NVGlineCap.Round, NVGlineCap.Bevel };
            int[] caps = new int[3] { NVGlineCap.Butt, NVGlineCap.Round, NVGlineCap.Square };


            vge.SaveState();
            pts[0] = -s * 0.25f + MathF.Cos(t * 0.3f) * s * 0.5f;
            pts[1] = MathF.Sin(t * 0.3f) * s * 0.5f;
            pts[2] = -s * 0.25F;
            pts[3] = 0;
            pts[4] = s * 0.25f;
            pts[5] = 0;
            pts[6] = s * 0.25f + MathF.Cos(-t * 0.3f) * s * 0.5f;
            pts[7] = MathF.Sin(-t * 0.3f) * s * 0.5f;

            for (i = 0; i < 3; i++)
            {
                for (j = 0; j < 3; j++)
                {
                    fx = x + s * 0.5f + (i * 3 + j) / 9.0f * w + pad;
                    fy = y - s * 0.5f + pad;

                    vge.LineCap(caps[i]);
                    vge.LineJoin(joins[j]);

                    vge.StrokeWidth(s * 0.3f);
                    vge.StrokeColor(vge.RGBA(0, 0, 0, 160));
                    vge.BeginPath();
                    vge.MoveTo(fx + pts[0], fy + pts[1]);
                    vge.LineTo(fx + pts[2], fy + pts[3]);
                    vge.LineTo(fx + pts[4], fy + pts[5]);
                    vge.LineTo(fx + pts[6], fy + pts[7]);
                    vge.Stroke();

                    vge.LineCap(NVGlineCap.Butt);
                    vge.LineJoin(NVGlineCap.Bevel);

                    vge.StrokeWidth(1.0f);
                    vge.StrokeColor(vge.RGBA(0, 192, 255, 255));
                    vge.BeginPath();
                    vge.MoveTo(fx + pts[0], fy + pts[1]);
                    vge.LineTo(fx + pts[2], fy + pts[3]);
                    vge.LineTo(fx + pts[4], fy + pts[5]);
                    vge.LineTo(fx + pts[6], fy + pts[7]);
                    vge.Stroke();
                }
            }

            vge.RestoreState();
        }

        void drawWidths(VGElement vge, float x, float y, float width)
        {
            int i;

            vge.SaveState();

            vge.StrokeColor(vge.RGBA(0, 0, 0, 255));

            for (i = 0; i < 20; i++)
            {
                float w = (i + 0.5f) * 0.1f;
                vge.StrokeWidth(w);
                vge.BeginPath();
                vge.MoveTo(x, y);
                vge.LineTo(x + width, y + width * 0.3f);
                vge.Stroke();
                y += 10;
            }

            vge.RestoreState();
        }

        void drawCaps(VGElement vge, float x, float y, float width)
        {
            int i;
            int[] caps = new int[3] { NVGlineCap.Butt, NVGlineCap.Round, NVGlineCap.Square };

            float lineWidth = 8.0f;

            vge.SaveState();

            vge.BeginPath();
            vge.Rect(x - lineWidth / 2, y, width + lineWidth, 40);
            vge.FillColor(vge.RGBA(255, 255, 255, 32));
            vge.Fill();

            vge.BeginPath();
            vge.Rect(x, y, width, 40);
            vge.FillColor(vge.RGBA(255, 255, 255, 32));
            vge.Fill();

            vge.StrokeWidth(lineWidth);
            for (i = 0; i < 3; i++)
            {
                vge.LineCap(caps[i]);
                vge.StrokeColor(vge.RGBA(0, 0, 0, 255));
                vge.BeginPath();
                vge.MoveTo(x, y + i * 10 + 5);
                vge.LineTo(x + width, y + i * 10 + 5);
                vge.Stroke();
            }

            vge.RestoreState();
        }

        void drawScissor(VGElement vge, float x, float y, float t)
        {
            vge.SaveState();

            // Draw first rect and set scissor to it's area.
            vge.Translate(x, y);
            vge.Rotate(vge.DegToRad(5));
            vge.BeginPath();
            vge.Rect(-20, -20, 60, 40);
            vge.FillColor(vge.RGBA(255, 0, 0, 255));
            vge.Fill();
            vge.Scissor(-20, -20, 60, 40);

            // Draw second rectangle with offset and rotation.
            vge.Translate(40, 0);
            vge.Rotate(t);

            // Draw the intended second rectangle without any scissoring.
            vge.SaveState();
            vge.ResetScissor();
            vge.BeginPath();
            vge.Rect(-20, -10, 60, 30);
            vge.FillColor(vge.RGBA(255, 128, 0, 64));
            vge.Fill();
            vge.RestoreState();

            // Draw second rectangle with combined scissoring.
            vge.IntersectScissor(-20, -10, 60, 30);
            vge.BeginPath();
            vge.Rect(-20, -10, 60, 30);
            vge.FillColor(vge.RGBA(255, 128, 0, 255));
            vge.Fill();

            vge.RestoreState();
        }


        void drawParagraph(VGElement vge, float x, float y, float width, float height, float mx, float my)
        {
            int lnum = 0;
            float caretx, px;
            int gutter = 0;
            float a = 1.0f;
            float gx = 0.0f, gy = 0.0f;
            string text = "This is longer chunk of text.\n  \n  Would have used lorem ipsum but she    was busy jumping over the lazy dog with the fox and all the men who came to the aid of the party.🎉";
            string hoverText = "Hover your mouse over the text to see calculated caret position.";

            vge.SaveState();

            vge.FontSize(15.0f);
            vge.FontFace("sans");
            vge.TextAlign(NVGalign.Left | NVGalign.Top);
            float t1, t2, lineh;
            vge.TextMetrics(out t1, out t2, out lineh);

            

            VGTextRowBuffer vgTextRowBuffer = vge.TextBreakLines(text, width);

            for (int i = 0; i < vgTextRowBuffer.Size; i++)
            {
                VGTextRow row = vgTextRowBuffer.GetRow(i);
                bool hit = mx > x && mx < (x + width) && my >= y && my < (y + lineh);

                vge.BeginPath();
                vge.FillColor(vge.RGBA(255, 255, 255, hit ? (byte)64 : (byte)16));
                vge.Rect(x + row.Min, y, row.Max - row.Min, lineh);
                vge.Fill();

                vge.FillColor(vge.RGBA(255, 255, 255, 255));
                vge.Text(x, y, row.Text);
                if (hit)
                {
                              caretx = (mx < x + row.Width / 2) ? x : x + row.Width;
                px = x;
                int nglyphs = vge.TextGlyphPositions(x, y, row.Text, out NVGglyphPosition [] glyphs, 100);
                for (int j = 0; j < nglyphs; j++)
                {
                    float x0 = glyphs[j].X;
                    float x1 = (j + 1 < nglyphs) ? glyphs[j + 1].X : x + row.Width;
                    gx = x0 * 0.3f + x1 * 0.7f;
                    if (mx >= px && mx < gx)
                        caretx = glyphs[j].X;
                    px = gx;
                }
                vge.BeginPath();
                vge.FillColor(vge.RGBA(255, 192, 0, 255));
                vge.Rect(caretx, y, 1, lineh);
                vge.Fill();

                gutter = lnum + 1;
                gx = x - 10;
                gy = y + lineh / 2;
                }
                lnum++;
                y += lineh;
            }

            if (gutter != 0)
            {
                string txt = String.Format("{0}", gutter);

                vge.FontSize(12.0f);
                vge.TextAlign(NVGalign.Right | NVGalign.Middle);

                vge.TextBounds(gx, gy, txt, out float[] textBounds);

                vge.BeginPath();
                vge.FillColor(vge.RGBA(255, 192, 0, 255));
                vge.RoundedRect((int)textBounds[0] - 4, (int)textBounds[1] - 2, (int)(textBounds[2] - textBounds[0]) + 8,
                                 (int)(textBounds[3] - textBounds[1]) + 4, ((int)(textBounds[3] - textBounds[1]) + 4) / 2 - 1);
                vge.Fill();

                vge.FillColor(vge.RGBA(32, 32, 32, 255));
                vge.Text(gx, gy, txt);
            }


            y += 20.0f;

            vge.FontSize(11.0f);
            vge.TextAlign(NVGalign.Left | NVGalign.Top);
            vge.TextLineHeight(1.2f);

            float[] bounds;
            vge.TextBoxBounds(x, y, 150, hoverText, out bounds);

            // Fade the tooltip out when close to it.

            gx = Math.Clamp(mx, bounds[0], bounds[2]) - mx;
            gy = Math.Clamp(my, bounds[1], bounds[3]) - my;
            a = MathF.Sqrt(gx * gx + gy * gy) / 30.0f;
            a = Math.Clamp(a, 0, 1);
            vge.GlobalAlpha(a);

            vge.BeginPath();
            vge.FillColor(vge.RGBA(220, 220, 220, 255));
            vge.RoundedRect(bounds[0] - 2, bounds[1] - 2, (int)(bounds[2] - bounds[0]) + 4, (int)(bounds[3] - bounds[1]) + 4,
                             3);
            px = (int)((bounds[2] + bounds[0]) / 2);
            vge.MoveTo(px, bounds[1] - 10);
            vge.LineTo(px + 7, bounds[1] + 1);
            vge.LineTo(px - 7, bounds[1] + 1);
            vge.Fill();

            vge.FillColor(vge.RGBA(0, 0, 0, 220));
            vge.TextBox(x, y, 150, hoverText);

            vge.RestoreState();

        }
        void drawWindow(VGElement vge, string title, float x, float y, float w, float h)
        {
            float cornerRadius = 3.0f;
            NVGpaint shadowPaint;
            NVGpaint headerPaint;

            vge.SaveState();

            // Window
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, cornerRadius);
            vge.FillColor(vge.RGBA(28, 30, 34, 192));
            //	nvgFillColor( vge.RGBA(0,0,0,128));
            vge.Fill();

            // Drop shadow
            shadowPaint = vge.BoxGradient(x, y + 2, w, h, cornerRadius * 2, 10, vge.RGBA(0, 0, 0, 128), vge.RGBA(0, 0, 0, 0));
            vge.BeginPath();
            vge.Rect(x - 10, y - 10, w + 20, h + 30);
            vge.RoundedRect(x, y, w, h, cornerRadius);
            vge.PathWinding(NVGwinding.Hole);
            vge.FillPaint(shadowPaint);
            vge.Fill();

            // Header
            headerPaint = vge.LinearGradient(x, y, x, y + 15, vge.RGBA(255, 255, 255, 8), vge.RGBA(0, 0, 0, 16));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + 1, w - 2, 30, cornerRadius - 1);
            vge.FillPaint(headerPaint);
            vge.Fill();
            vge.BeginPath();
            vge.MoveTo(x + 0.5f, y + 0.5f + 30);
            vge.LineTo(x + 0.5f + w - 1, y + 0.5f + 30);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 32));
            vge.Stroke();

            vge.FontSize(15.0f);
            vge.FontFace("sans-bold");
            vge.TextAlign(NVGalign.Center | NVGalign.Middle);

            vge.FontBlur(2);
            vge.FillColor(vge.RGBA(0, 0, 0, 128));
            vge.Text(x + w / 2, y + 16 + 1, title);

            vge.FontBlur(0);
            vge.FillColor(vge.RGBA(220, 220, 220, 160));
            vge.Text(x + w / 2, y + 16, title);

            vge.RestoreState();
        }

        void drawSearchBox(VGElement vge, string text, float x, float y, float w, float h)
        {
            NVGpaint bg;
            byte[] icon = new byte[8];
            float cornerRadius = h / 2 - 1;

            // Edit
            bg = vge.BoxGradient(x, y + 1.5f, w, h, h / 2, 5, vge.RGBA(0, 0, 0, 16), vge.RGBA(0, 0, 0, 92));
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, cornerRadius);
            vge.FillPaint(bg);
            vge.Fill();

            vge.FontSize(h * 1.3f);
            vge.FontFace("icons");
            vge.FillColor(vge.RGBA(255, 255, 255, 64));
            vge.TextAlign(NVGalign.Center | NVGalign.Middle);
            vge.Text(x + h * 0.55f, y + h * 0.55f, cpToUTF8(ICON_SEARCH, icon));

            vge.FontSize(17.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 32));

            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x + h * 1.05f, y + h * 0.5f, text);

            vge.FontSize(h * 1.3f);
            vge.FontFace("icons");
            vge.FillColor(vge.RGBA(255, 255, 255, 32));
            vge.TextAlign(NVGalign.Center | NVGalign.Middle);
            vge.Text(x + w - h * 0.55f, y + h * 0.55f, cpToUTF8(ICON_CIRCLED_CROSS, icon));
        }

        void drawDropDown(VGElement vge, string text, float x, float y, float w, float h)
        {
            NVGpaint bg;
            byte[] icon = new byte[8];
            float cornerRadius = 4.0f;

            bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(255, 255, 255, 16), vge.RGBA(0, 0, 0, 16));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + 1, w - 2, h - 2, cornerRadius - 1);
            vge.FillPaint(bg);
            vge.Fill();

            vge.BeginPath();
            vge.RoundedRect(x + 0.5f, y + 0.5f, w - 1, h - 1, cornerRadius - 0.5f);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 48));
            vge.Stroke();

            vge.FontSize(17.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 160));
            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x + h * 0.3f, y + h * 0.5f, text);

            vge.FontSize(h * 1.3f);
            vge.FontFace("icons");
            vge.FillColor(vge.RGBA(255, 255, 255, 64));
            vge.TextAlign(NVGalign.Center | NVGalign.Middle);
            vge.Text(x + w - h * 0.5f, y + h * 0.5f, cpToUTF8(ICON_CHEVRON_RIGHT, icon));
        }


        string cpToUTF8(int cp, byte[] str)
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

            return System.Text.Encoding.UTF8.GetString(str, 0, 8);

        }


        void drawEditBoxBase(VGElement vge, float x, float y, float w, float h)
        {
            NVGpaint bg;
            // Edit
            bg = vge.BoxGradient(x + 1, y + 1 + 1.5f, w - 2, h - 2, 3, 4, vge.RGBA(255, 255, 255, 32), vge.RGBA(32, 32, 32, 32));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + 1, w - 2, h - 2, 4 - 1);
            vge.FillPaint(bg);
            vge.Fill();

            vge.BeginPath();
            vge.RoundedRect(x + 0.5f, y + 0.5f, w - 1, h - 1, 4 - 0.5f);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 48));
            vge.Stroke();
        }

        void drawEditBox(VGElement vge, string text, float x, float y, float w, float h)
        {

            drawEditBoxBase(vge, x, y, w, h);

            vge.FontSize(17.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 64));
            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x + h * 0.3f, y + h * 0.5f, text);
        }

        void drawEditBoxNum(VGElement vge, string text, string units, float x, float y, float w, float h)
        {
            float uw;

            drawEditBoxBase(vge, x, y, w, h);

            float[] dummy;
            uw = vge.TextBounds(0, 0, units, out dummy);

            vge.FontSize(15.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 64));
            vge.TextAlign(NVGalign.Right | NVGalign.Middle);
            vge.Text(x + w - h * 0.3f, y + h * 0.5f, units);

            vge.FontSize(17.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 128));
            vge.TextAlign(NVGalign.Right | NVGalign.Middle);
            vge.Text(x + w - uw - h * 0.5f, y + h * 0.5f, text);
        }

        void drawCheckBox(VGElement vge, string text, float x, float y, float w, float h)
        {
            NVGpaint bg;
            byte[] icon = new byte[8];

            vge.FontSize(15.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 160));

            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x + 28, y + h * 0.5f, text);

            bg = vge.BoxGradient(x + 1, y + (int)(h * 0.5f) - 9 + 1, 18, 18, 3, 3, vge.RGBA(0, 0, 0, 32), vge.RGBA(0, 0, 0, 92));
            vge.BeginPath();
            vge.RoundedRect(x + 1, y + (int)(h * 0.5f) - 9, 18, 18, 3);
            vge.FillPaint(bg);
            vge.Fill();

            vge.FontSize(33);
            vge.FontFace("icons");
            vge.FillColor(vge.RGBA(255, 255, 255, 128));
            vge.TextAlign(NVGalign.Center | NVGalign.Middle);
            vge.Text(x + 9 + 2, y + h * 0.5f, cpToUTF8(ICON_CHECK, icon));
        }

        void drawButton(VGElement vge, int preicon, string text, float x, float y, float w, float h, NVGcolor col)
        {
            NVGpaint bg;
            byte[] icon = new byte[8];
            float cornerRadius = 4.0f;
            float tw = 0, iw = 0;

            bg = vge.LinearGradient(x, y, x, y + h, vge.RGBA(255, 255, 255, (byte)(isBlack(col) ? 16 : 32)),
                                     vge.RGBA(0, 0, 0, (byte)(isBlack(col) ? 16 : 32)));
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

            vge.FontSize(17.0f);
            vge.FontFace("sans-bold");
            float[] dummy;
            tw = vge.TextBounds(0, 0, text, out dummy);
            if (preicon != 0)
            {
                vge.FontSize(h * 1.3f);
                vge.FontFace("icons");
                iw = vge.TextBounds(0, 0, cpToUTF8(preicon, icon), out dummy);
                iw += h * 0.15f;
            }

            if (preicon != 0)
            {
                vge.FontSize(h * 1.3f);
                vge.FontFace("icons");
                vge.FillColor(vge.RGBA(255, 255, 255, 96));
                vge.TextAlign(NVGalign.Left | NVGalign.Middle);
                vge.Text(x + w * 0.5f - tw * 0.5f - iw * 0.75f, y + h * 0.5f, cpToUTF8(preicon, icon));
            }

            vge.FontSize(17.0f);
            vge.FontFace("sans-bold");
            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.FillColor(vge.RGBA(0, 0, 0, 160));
            vge.Text(x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f - 1, text);
            vge.FillColor(vge.RGBA(255, 255, 255, 160));
            vge.Text(x + w * 0.5f - tw * 0.5f + iw * 0.25f, y + h * 0.5f, text);
        }

        void drawSlider(VGElement vge, float pos, float x, float y, float w, float h)
        {
            NVGpaint bg, knob;
            float cy = y + (int)(h * 0.5f);
            float kr = (int)(h * 0.25f);

            vge.SaveState();
            //	nvgClearState();

            // Slot
            bg = vge.BoxGradient(x, cy - 2 + 1, w, 4, 2, 2, vge.RGBA(0, 0, 0, 32), vge.RGBA(0, 0, 0, 128));
            vge.BeginPath();
            vge.RoundedRect(x, cy - 2, w, 4, 2);
            vge.FillPaint(bg);
            vge.Fill();

            // Knob Shadow
            bg = vge.RadialGradient(x + (int)(pos * w), cy + 1, kr - 3, kr + 3, vge.RGBA(0, 0, 0, 64), vge.RGBA(0, 0, 0, 0));
            vge.BeginPath();
            vge.Rect(x + (int)(pos * w) - kr - 5, cy - kr - 5, kr * 2 + 5 + 5, kr * 2 + 5 + 5 + 3);
            vge.Circle(x + (int)(pos * w), cy, kr);
            vge.PathWinding(NVGwinding.Hole);
            vge.FillPaint(bg);
            vge.Fill();

            // Knob
            knob = vge.LinearGradient(x, cy - kr, x, cy + kr, vge.RGBA(255, 255, 255, 16), vge.RGBA(0, 0, 0, 16));
            vge.BeginPath();
            vge.Circle(x + (int)(pos * w), cy, kr - 1);
            vge.FillColor(vge.RGBA(40, 43, 48, 255));
            vge.Fill();
            vge.FillPaint(knob);
            vge.Fill();

            vge.BeginPath();
            vge.Circle(x + (int)(pos * w), cy, kr - 0.5f);
            vge.StrokeColor(vge.RGBA(0, 0, 0, 92));
            vge.Stroke();

            vge.RestoreState();
        }

        void drawSpinner(VGElement vge, float cx, float cy, float r, float t)
        {
            float a0 = 0.0f + t * 6;
            float a1 = MathF.PI + t * 6;
            float r0 = r;
            float r1 = r * 0.75f;
            float ax, ay, bx, by;
            NVGpaint paint;

            vge.SaveState();

            vge.BeginPath();
            vge.Arc(cx, cy, r0, a0, a1, NVGwinding.CW);
            vge.Arc(cx, cy, r1, a1, a0, NVGwinding.CCW);
            vge.ClosePath();
            ax = cx + MathF.Cos(a0) * (r0 + r1) * 0.5f;
            ay = cy + MathF.Sin(a0) * (r0 + r1) * 0.5f;
            bx = cx + MathF.Cos(a1) * (r0 + r1) * 0.5f;
            by = cy + MathF.Sin(a1) * (r0 + r1) * 0.5f;
            paint = vge.LinearGradient(ax, ay, bx, by, vge.RGBA(0, 0, 0, 0), vge.RGBA(0, 0, 0, 128));
            vge.FillPaint(paint);
            vge.Fill();

            vge.RestoreState();
        }

        void drawLabel(VGElement vge, string text, float x, float y, float w, float h)
        {

            vge.FontSize(15.0f);
            vge.FontFace("sans");
            vge.FillColor(vge.RGBA(255, 255, 255, 128));

            vge.TextAlign(NVGalign.Left | NVGalign.Middle);
            vge.Text(x, y + h * 0.5f, text);
        }

        void drawThumbnails(VGElement vge, float x, float y, float w, float h, int[] images, int nimages, float t)
        {
            float cornerRadius = 3.0f;
            NVGpaint shadowPaint, imgPaint, fadePaint;
            float ix, iy, iw, ih;
            float thumb = 60.0f;
            float arry = 30.5f;
            int imgw, imgh;
            float stackh = (nimages / 2) * (thumb + 10) + 10;
            int i;
            float u = (1 + MathF.Cos(t * 0.5f)) * 0.5f;
            float u2 = (1 - MathF.Cos(t * 0.2f)) * 0.5f;
            float scrollh, dv;

            vge.SaveState();

            // Drop shadow
            shadowPaint = vge.BoxGradient(x, y + 4, w, h, cornerRadius * 2, 20, vge.RGBA(0, 0, 0, 128), vge.RGBA(0, 0, 0, 0));
            vge.BeginPath();
            vge.Rect(x - 10, y - 10, w + 20, h + 30);
            vge.RoundedRect(x, y, w, h, cornerRadius);
            vge.PathWinding(NVGwinding.Hole);
            vge.FillPaint(shadowPaint);
            vge.Fill();

            // Window
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, cornerRadius);
            vge.MoveTo(x - 10, y + arry);
            vge.LineTo(x + 1, y + arry - 11);
            vge.LineTo(x + 1, y + arry + 11);
            vge.FillColor(vge.RGBA(200, 200, 200, 255));
            vge.Fill();

            vge.SaveState();
            vge.Scissor(x, y, w, h);
            vge.Translate(0, -(stackh - h) * u);

            dv = 1.0f / (float)(nimages - 1);

            for (i = 0; i < nimages; i++)
            {
                float tx, ty, v, a;
                tx = x + 10;
                ty = y + 10;
                tx += (i % 2) * (thumb + 10);
                ty += (i / 2) * (thumb + 10);

                vge.ImageSize(images[i], out imgw, out imgh);
                if (imgw < imgh)
                {
                    iw = thumb;
                    ih = iw * (float)imgh / (float)imgw;
                    ix = 0;
                    iy = -(ih - thumb) * 0.5f;
                }
                else
                {
                    ih = thumb;
                    iw = ih * (float)imgw / (float)imgh;
                    ix = -(iw - thumb) * 0.5f;
                    iy = 0;
                }

                v = i * dv;
                a = Math.Clamp((u2 - v) / dv, 0, 1);

                if (a < 1.0f)
                    drawSpinner(vge, tx + thumb / 2, ty + thumb / 2, thumb * 0.25f, t);

                imgPaint = vge.ImagePattern(tx + ix, ty + iy, iw, ih, 0.0f / 180.0f * MathF.PI, images[i], a);
                vge.BeginPath();
                vge.RoundedRect(tx, ty, thumb, thumb, 5);
                vge.FillPaint(imgPaint);
                vge.Fill();

                shadowPaint =
                    vge.BoxGradient(tx - 1, ty, thumb + 2, thumb + 2, 5, 3, vge.RGBA(0, 0, 0, 128), vge.RGBA(0, 0, 0, 0));
                vge.BeginPath();
                vge.Rect(tx - 5, ty - 5, thumb + 10, thumb + 10);
                vge.RoundedRect(tx, ty, thumb, thumb, 6);
                vge.PathWinding(NVGwinding.Hole);
                vge.FillPaint(shadowPaint);
                vge.Fill();

                vge.BeginPath();
                vge.RoundedRect(tx + 0.5f, ty + 0.5f, thumb - 1, thumb - 1, 4 - 0.5f);
                vge.StrokeWidth(1.0f);
                vge.StrokeColor(vge.RGBA(255, 255, 255, 192));
                vge.Stroke();
            }
            vge.RestoreState();

            // Hide fades
            fadePaint = vge.LinearGradient(x, y, x, y + 6, vge.RGBA(200, 200, 200, 255), vge.RGBA(200, 200, 200, 0));
            vge.BeginPath();
            vge.Rect(x + 4, y, w - 8, 6);
            vge.FillPaint(fadePaint);
            vge.Fill();

            fadePaint = vge.LinearGradient(x, y + h, x, y + h - 6, vge.RGBA(200, 200, 200, 255), vge.RGBA(200, 200, 200, 0));
            vge.BeginPath();
            vge.Rect(x + 4, y + h - 6, w - 8, 6);
            vge.FillPaint(fadePaint);
            vge.Fill();

            // Scroll bar
            shadowPaint =
                vge.BoxGradient(x + w - 12 + 1, y + 4 + 1, 8, h - 8, 3, 4, vge.RGBA(0, 0, 0, 32), vge.RGBA(0, 0, 0, 92));
            vge.BeginPath();
            vge.RoundedRect(x + w - 12, y + 4, 8, h - 8, 3);
            vge.FillPaint(shadowPaint);
            //	nvgFillColor( vge.RGBA(255,0,0,128));
            vge.Fill();

            scrollh = (h / stackh) * (h - 8);
            shadowPaint = vge.BoxGradient(x + w - 12 - 1, y + 4 + (h - 8 - scrollh) * u - 1, 8, scrollh, 3, 4,
                                           vge.RGBA(220, 220, 220, 255), vge.RGBA(128, 128, 128, 255));
            vge.BeginPath();
            vge.RoundedRect(x + w - 12 + 1, y + 4 + 1 + (h - 8 - scrollh) * u, 8 - 2, scrollh - 2, 2);
            vge.FillPaint(shadowPaint);
            //	nvgFillColor( vge.RGBA(0,0,0,128));
            vge.Fill();

            vge.RestoreState();
        }

        void drawSVGImage(VGElement vge, int imageID, float x, float y, float w, float h, float a)
        {
            NVGpaint imgPaint;

            imgPaint = vge.ImagePattern(x, y, w, h, 0, imageID, a);
            vge.BeginPath();
            vge.RoundedRect(x, y, w, h, 3.0f);
            vge.FillPaint(imgPaint);
            vge.Fill();
        }


        bool isBlack(NVGcolor col)
        {
            if (col.r == 0.0f && col.g == 0.0f && col.b == 0.0f && col.a == 0.0f)
            {
                return true;
            }
            return false;
        }

    }
}