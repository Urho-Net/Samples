//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using System.IO;
using Urho;

namespace ShapeBlaster
{
    static class ShipInput
    {
        private static bool isAimingWithMouse = false;
        static IntVector2 lastTouchPos = IntVector2.Zero;

        public static void Update()
        {
            isAimingWithMouse = true;
        }
    
        public static Vector2 GetMovementDirection()
        {

            var input = Application.Current.Input;

            Vector2 direction = new Vector2(0, 0);

#if !_MOBILE_
            if (input.GetKeyDown(Key.A))
                direction.X -= 1;
            if (input.GetKeyDown(Key.D))
                direction.X += 1;
            if (input.GetKeyDown(Key.S))
                direction.Y -= 1;
            if (input.GetKeyDown(Key.W))
                direction.Y += 1;
#endif      

        
#if _MOBILE_

            var touchPos = lastTouchPos;

            if (input.NumTouches == 1)
            {
                touchPos = input.GetTouch(0).LastPosition;
                lastTouchPos = touchPos;

            }

            if (touchPos != IntVector2.Zero)
            {
                direction = new Vector2((float)touchPos.X, (float)touchPos.Y);
            }


            Vector2 shipPos = PlayerShip.Instance.Position;

            shipPos.Y = ShapeBlaster.ScreenBounds.Height() - shipPos.Y;

            direction -= shipPos;

            direction.Y = -direction.Y;

            if (touchPos == IntVector2.Zero || direction.Length < 4.0f)
                direction = Vector2.Zero;

#endif

#if ATOMIC_IOS
            uint numJoySticks = 0;
#else
            uint numJoySticks = input.NumJoysticks;
#endif

            if (numJoySticks > 0)
            {
                input.GetJoystickByIndex(0, out var state);

                float x = state.GetAxisPosition(0);
                float y = state.GetAxisPosition(1);

                if (x < -0.15f)
                    direction.X = x;
                if (x > 0.15f)
                    direction.X = x;

                if (y < -0.15f)
                    direction.Y = -y;
                if (y > 0.15f)
                    direction.Y = -y;

            }

            // Clamp the length of the vector to a maximum of 1.
            if (direction.LengthSquared > 1)
                direction.Normalize();

            return direction;
        }

        public static Vector2 GetAimDirection()
        {
            return GetMouseAimDirection();
        }

        private static Vector2 GetMouseAimDirection()
        {
            var input = Application.Current.Input;


            uint numJoySticks = input.NumJoysticks;

            if (numJoySticks > 0)
            {
                Vector2 dir = new Vector2(0, 0);

                input.GetJoystickByIndex(0,out var state);

                float x = state.GetAxisPosition(0);
                float y = state.GetAxisPosition(1);

                if (x < -0.15f)
                    dir.X = x;
                if (x > 0.15f)
                    dir.X = x;

                if (y < -0.15f)
                    dir.Y = -y;
                if (y > 0.15f)
                    dir.Y = -y;

                // Clamp the length of the vector to a maximum of 1.
                if (dir.LengthSquared > 1)
                    dir.Normalize();

                return dir;

            }


#if !_MOBILE_
            Vector2 direction = new Vector2((float)input.MousePosition.X, (float)input.MousePosition.Y);
#else
            
            Vector2 direction = PlayerShip.Instance.Velocity;
            return Vector2.Normalize(direction);  
#endif

            Vector2 shipPos = PlayerShip.Instance.Position;

            shipPos.Y = ShapeBlaster.ScreenBounds.Height() - shipPos.Y;          

            direction -= shipPos;

            direction.Y = -direction.Y;
           
            if (direction == Vector2.Zero)
                return Vector2.Zero;
            else
                return Vector2.Normalize(direction);
        }

        public static bool WasBombButtonPressed()
        {
            var input =  Application.Current.Input;
           
            return input.GetKeyPress(Key.Space);
        }
    }

}