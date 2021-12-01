//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using Urho;

namespace ShapeBlaster
{
    static class MathUtil
    {
        public static Vector2 FromPolar(float angle, float magnitude)
        {
            return magnitude * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
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
            var rot4 = Vector3.Multiply(rot1, rot2);
            var rot5 = Vector3.Multiply(rot1, rot3);

            var v = new Vector2();
            v.X = (float)((double)value.X * (1.0 - (double)rot5.Y - (double)rot5.Z) + (double)value.Y * ((double)rot4.Y - (double)rot4.Z));
            v.Y = (float)((double)value.X * ((double)rot4.Y + (double)rot4.Z) + (double)value.Y * (1.0 - (double)rot4.X - (double)rot5.Z));
            result.X = v.X;
            result.Y = v.Y;
        }
    }
}
