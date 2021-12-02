//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using System.IO;
using Urho;
using Urho.IO;

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

            var Input = Application.Current.Input;

            Vector2 direction =  Vector2.Zero;

            if (GameRoot.IsMobile)
            {
                JoystickState joystick;
                if (GameRoot.screenJoystickIndex != -1 && Input.GetJoystick(GameRoot.screenJoystickIndex, out joystick))
                {
                    direction = new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), -joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
                }
            }
            else
            {
                if (Input.GetKeyDown(Key.A))
                    direction.X -= 1;
                if (Input.GetKeyDown(Key.D))
                    direction.X += 1;
                if (Input.GetKeyDown(Key.S))
                    direction.Y -= 1;
                if (Input.GetKeyDown(Key.W))
                    direction.Y += 1;
            }

            
            // Clamp the length of the vector to a maximum of 1.
            if (direction.LengthSquared > 1)
                direction.Normalize();

            return direction;
        }

        public static Vector2 GetAimDirection()
        {
            if (GameRoot.IsMobile)
            {
                JoystickState joystick;
                if (GameRoot.screenJoystickIndex != -1 && Application.Current.Input.GetJoystick(GameRoot.screenJoystickIndex, out joystick))
                {
                    return  Vector2.Normalize(new Vector2(joystick.GetAxisPosition(JoystickState.AxisRight_X), -joystick.GetAxisPosition(JoystickState.AxisRight_Y)));
                }
                else
                return Vector2.Zero;
            }
            else
                return GetMouseAimDirection();
        }

        private static Vector2 GetMouseAimDirection()
        {
            var Input = Application.Current.Input;


            uint numJoySticks = Input.NumJoysticks;

            if (numJoySticks > 0)
            {
                Vector2 dir = new Vector2(0, 0);

                Input.GetJoystickByIndex(0,out var state);

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


            Vector3 screenToWorld = GameRoot.Viewport.ScreenToWorldPoint(Input.MousePosition.X, Input.MousePosition.Y, 0);
            Vector2 direction = new Vector2(screenToWorld.X, GameRoot.ScreenBounds.Height() - screenToWorld.Y);

            Vector2 shipPos = PlayerShip.Instance.Position;

            shipPos.Y = GameRoot.ScreenBounds.Height() - shipPos.Y;

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