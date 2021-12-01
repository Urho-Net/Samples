using Urho.Urho2D;
using System;

namespace Urho
{
	public static class Extensions
	{
		public static Vector3 Multiply(this Vector3 a, Vector3 b)
		{
			return Vector3.Multiply(a, b);
		}

		public static int Width(this IntRect rect)
		{
			return Math.Abs(rect.Right - rect.Left);
		}

		public static int Height(this IntRect rect)
		{
			return Math.Abs(rect.Bottom - rect.Top);
		}

        public static float Width(this Rect rect)
		{
			return Math.Abs(rect.Max.X - rect.Min.X);
		}

		public static float Height(this Rect rect)
		{
			return Math.Abs(rect.Max.Y - rect.Min.Y);
		}

		public static float Distance(this Vector2 v1, Vector2 v2)
		{
			return (float)Math.Sqrt((v2.X - v1.X) * (v2.X - v1.X) + (v2.Y - v1.Y) * (v2.Y - v1.Y));
		}


        public static float ToAngle(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }

		public static Vector2 ScaleTo(this Vector2 vector, float length)
		{
			return vector * (length / vector.Length);
		}



		public static float NextFloat(this Random rand, float minValue, float maxValue)
		{
			return (float)rand.NextDouble() * (maxValue - minValue) + minValue;
		}

		public static Vector2 NextVector2(this Random rand, float minLength, float maxLength)
		{
			double theta = rand.NextDouble() * 2 * Math.PI;
			float length = rand.NextFloat(minLength, maxLength);
			return new Vector2(length * (float)Math.Cos(theta), length * (float)Math.Sin(theta));
		}

	

        public static bool Contains(this IntRect rect, Vector2 vector)
        {
            int x = (int)vector.X;
            int y = (int)vector.Y;

            if (x < rect.Left || y < rect.Top || x >= rect.Right || y >= rect.Bottom)
                return false;

            return true;
        }
     

        public static Rect GetTextureRectangle(this Sprite2D sprite)
        {
            Rect rect = new Rect();

            var rectangle_ = sprite.Rectangle;



            var texture_ = sprite.Texture;
            if (texture_ == null) return rect;

            float invWidth = 1.0f / (float)texture_.Width;
            float invHeight = 1.0f / (float)texture_.Height;

            var edgeOffset_ = sprite.TextureEdgeOffset;

            rect.Min.X = ((float)rectangle_.Left + edgeOffset_) * invWidth;
            rect.Max.X = ((float)rectangle_.Right - edgeOffset_) * invWidth;

            rect.Min.Y = ((float)rectangle_.Bottom - edgeOffset_) * invHeight;
            rect.Max.Y = ((float)rectangle_.Top + edgeOffset_) * invHeight;

            return rect;
        }

	}

	
}
