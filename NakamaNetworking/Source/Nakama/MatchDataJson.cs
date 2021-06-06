/*
Copyright 2021 Heroic Labs

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections.Generic;
using Nakama.TinyJson;
using System.Text;
using Urho;

namespace NakamaNetworking
{
    /// <summary>
    /// A static class that creates JSON string network messages.
    /// </summary>
    public static class MatchDataJson
    {
        /// <summary>
        /// Creates a network message containing velocity and position.
        /// </summary>
        /// <param name="velocity">The velocity to send.</param>
        /// <param name="position">The position to send.</param>
        /// <returns>A JSONified string containing velocity and position data.</returns>
        public static string VelocityPositionRotation(Vector3 velocity, Vector3 position,Quaternion rotation)
        {
            var values = new Dictionary<string, string>
            {
                { "velocity.x", velocity.X.ToString() },
                { "velocity.y", velocity.Y.ToString() },
                { "velocity.z", velocity.Z.ToString() },
                { "position.x", position.X.ToString() },
                { "position.y", position.Y.ToString() },
                { "position.z", position.Z.ToString() },
                { "rotation.x", rotation.X.ToString() },
                { "rotation.y", rotation.Y.ToString() },
                { "rotation.z", rotation.Z.ToString() },
                { "rotation.w", rotation.W.ToString() },
            };

            return values.ToJson();
        }

        public static string ControlsInput(Controls controls)
        {
            Vector2 axisInput = controls.ExtraData["axis_0"];

            var values = new Dictionary<string, string>
            {
                { "pitch", controls.Pitch.ToString() },
                { "yaw", controls.Yaw.ToString() },
                { "forward", controls.IsDown(Global.CtrlForward).ToString()},
                { "back", controls.IsDown(Global.CtrlBack).ToString()},
                { "left", controls.IsDown(Global.CtrlLeft).ToString()},
                { "right", controls.IsDown(Global.CtrlRight).ToString()},
                { "jump", controls.IsDown(Global.CtrlJump).ToString()},
                { "axis.x", axisInput.X.ToString()} ,
                { "axis.y", axisInput.Y.ToString()} ,
            };

            return values.ToJson();
        }

        
        public static string CharacterName(string name)
        {

            var values = new Dictionary<string, string>
            {
                { "name", name}
            };

            return values.ToJson();
        }

        public static IDictionary<string, string> GetStateAsDictionary(byte[] state)
        {
            return Encoding.UTF8.GetString(state).FromJson<Dictionary<string, string>>();
        }

    }


}