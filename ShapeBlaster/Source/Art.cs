//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using Urho;
using Urho.Urho2D;
using System;

namespace ShapeBlaster
{
    public class CustomSprite
    {
        public CustomSprite(Sprite2D sprite)
        {
            Texture = sprite.Texture;

            var rect = sprite.GetTextureRectangle();

            TexCoordTL = rect.Min;
            TexCoordBR = rect.Max;

            Height = Math.Max(1,(int)(rect.Height() * 128.0f));
            Width = Math.Max(1,(int)(rect.Width() * 128.0f));

        }

        public int Height;
        public int Width;

        public Vector2 TexCoordTL;
        public Vector2 TexCoordBR;
        public Texture2D Texture;

    }

    static class Art
    {
        public static CustomSprite Player { get; private set; }
        public static CustomSprite Seeker { get; private set; }
        public static CustomSprite Wanderer { get; private set; }
        public static CustomSprite Bullet { get; private set; }
        public static CustomSprite Pointer { get; private set; }
        public static CustomSprite BlackHole { get; private set; }

        public static CustomSprite LineParticle { get; private set; }
        public static CustomSprite Glow { get; private set; }
        public static CustomSprite Pixel { get; private set; }     // a single white pixel

        public static void Load()
        {
            var cache = Application.Current.ResourceCache;

            SpriteSheet2D sheet = cache.GetSpriteSheet2D("Sprites/AtomicBlasterSprites.xml");

            Player = new CustomSprite(sheet.GetSprite("Player"));
            Seeker = new CustomSprite(sheet.GetSprite("Seeker"));
            Wanderer = new CustomSprite(sheet.GetSprite("Wanderer"));
            Bullet = new CustomSprite(sheet.GetSprite("Bullet"));
            Pointer = new CustomSprite(sheet.GetSprite("Pointer"));
            BlackHole = new CustomSprite(sheet.GetSprite("BlackHole"));
            
            LineParticle = new CustomSprite(sheet.GetSprite("Laser"));
            Glow = new CustomSprite(sheet.GetSprite("Glow"));
            Pixel = new CustomSprite(sheet.GetSprite("Pixel"));
        }
    }
}
