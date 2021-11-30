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

        public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
        {
            // Using formula from http://www.mvps.org/directx/articles/catmull/
            // Internally using doubles not to lose precission
            double amountSquared = amount * amount;
            double amountCubed = amountSquared * amount;
            return (float)(0.5 * (2.0 * value2 +
                (value3 - value1) * amount +
                (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * amountSquared +
                (3.0 * value2 - value1 - 3.0 * value3 + value4) * amountCubed));
        }

        public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount)
        {
            return new Vector2(
                CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
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

		public static void Inflate(this IntRect rect , int horizontalAmount, int verticalAmount)
        {
            rect.Left -= horizontalAmount;
            rect.Right += horizontalAmount;

            rect.Top -= verticalAmount;
            rect.Bottom += verticalAmount;

        }

        public static bool Contains(this IntRect rect, Vector2 vector)
        {
            int x = (int)vector.X;
            int y = (int)vector.Y;

            if (x < rect.Left || y < rect.Top || x >= rect.Right || y >= rect.Bottom)
                return false;

            return true;
        }

        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            float halfRoll = roll * 0.5f;
            float halfPitch = pitch * 0.5f;
            float halfYaw = yaw * 0.5f;

            float sinRoll = (float)Math.Sin(halfRoll);
            float cosRoll = (float)Math.Cos(halfRoll);
            float sinPitch = (float)Math.Sin(halfPitch);
            float cosPitch = (float)Math.Cos(halfPitch);
            float sinYaw = (float)Math.Sin(halfYaw);
            float cosYaw = (float)Math.Cos(halfYaw);

            return new Quaternion((cosYaw * sinPitch * cosRoll) + (sinYaw * cosPitch * sinRoll),
                                  (sinYaw * cosPitch * cosRoll) - (cosYaw * sinPitch * sinRoll),
                                  (cosYaw * cosPitch * sinRoll) - (sinYaw * sinPitch * cosRoll),
                                  (cosYaw * cosPitch * cosRoll) + (sinYaw * sinPitch * sinRoll));
        }


        public static Color Lerp(Color value1, Color value2, float amount)
        {
            amount = MathHelper.Clamp(amount, 0, 1);
            return new Color(
                MathHelper.Lerp(value1.R, value2.R, amount),
                MathHelper.Lerp(value1.G, value2.G, amount),
                MathHelper.Lerp(value1.B, value2.B, amount),
                MathHelper.Lerp(value1.A, value2.A, amount));
        }

        public static Vector2 Transform(Vector2 value, Quaternion rotation)
        {
            Transform(ref value, ref rotation, out value);
            return value;
        }

        public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector2 result)
        {
            var rot1 = new Vector3(rotation.X + rotation.X, rotation.Y + rotation.Y, rotation.Z + rotation.Z);
            var rot2 = new Vector3(rotation.X, rotation.X, rotation.W);
            var rot3 = new Vector3(1, rotation.Y, rotation.Z);
            var rot4 = rot1.Multiply(rot2);
            var rot5 = rot1.Multiply(rot3);

            var v = new Vector2();
            v.X = (float)((double)value.X * (1.0 - (double)rot5.Y - (double)rot5.Z) + (double)value.Y * ((double)rot4.Y - (double)rot4.Z));
            v.Y = (float)((double)value.X * ((double)rot4.Y + (double)rot4.Z) + (double)value.Y * (1.0 - (double)rot4.X - (double)rot5.Z));
            result.X = v.X;
            result.Y = v.Y;
        }

        public static float WrapAngle(float angle)
        {
            angle = (float)Math.IEEERemainder((double)angle, 6.2831854820251465);
            if (angle <= -3.14159274f)
            {
                angle += 6.28318548f;
            }
            else
            {
                if (angle > 3.14159274f)
                {
                    angle -= 6.28318548f;
                }
            }
            return angle;
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
