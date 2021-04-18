// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2015 the Urho3D project.
// Copyright (c) 2015 Xamarin Inc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using Urho;
using UrhoNetSamples;
using System;
using Urho.Actions;
using Urho.Shapes;

namespace Actions
{
    public class Actions : Sample
    {
        Node boxNode;

        [Preserve]
        public Actions() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }


        protected override void Start()
        {
            base.Start();
            CreateScene();
            SimpleCreateInstructions("W - move forward\nS - move backward\nQ - Fade out\nE - Fade in\nR - increase scale\nG - tint to random color");
        }

        protected override void Stop()
        {
			boxNode.RemoveAllActions();
			boxNode.Dispose();
			base.Stop();
        }

        protected override void OnUpdate(float timeStep)
        {
            var input = Input;

            const float duration = 1f; //2s
            FiniteTimeAction action = null;

            if (input.GetKeyPress(Key.W))
            {
                action = new MoveBy(duration, new Vector3(0, 0, 5));
            }

            if (input.GetKeyPress(Key.S))
            {
                action = new MoveBy(duration, new Vector3(0, 0, -5));
            }

            if (input.GetKeyPress(Key.E))
            {
                action = new FadeIn(duration);
            }

            if (input.GetKeyPress(Key.Q))
            {
                action = new FadeOut(duration);
            }

            if (input.GetKeyPress(Key.R))
            {
                action = new EaseElasticInOut(new ScaleBy(duration, 1.3f));
            }

            if (input.GetKeyPress(Key.G))
            {
                action = new TintTo(duration, NextRandom(1), NextRandom(1), NextRandom(1));
            }

            if (action != null)
            {
                //can be awaited
                boxNode.RunActionsAsync(action);
            }
            base.OnUpdate(timeStep);
        }

        async void CreateScene()
        {
            // 3D scene with Octree
            var scene = new Scene(Context);
            scene.CreateComponent<Octree>();

            // Box
            boxNode = scene.CreateChild();
            boxNode.Position = new Vector3(0, 0, 5);
            boxNode.SetScale(0f);
            boxNode.Rotation = new Quaternion(60, 0, 30);
            var box = boxNode.CreateComponent<Box>();
            box.Color = Color.Magenta;

            // Light
            Node lightNode = scene.CreateChild(name: "light");
            //lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Point;
            light.Range = 50;

            // Camera
            Node cameraNode = scene.CreateChild(name: "camera");
            Camera camera = cameraNode.CreateComponent<Camera>();

            // Viewport
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));

            try
            {
                // Do actions
                await boxNode.RunActionsAsync(new EaseBounceOut(new ScaleTo(duration: 1f, scale: 1)));
                await boxNode.RunActionsAsync(new RepeatForever(
                new RotateBy(duration: 1, deltaAngleX: 90, deltaAngleY: 0, deltaAngleZ: 0)));
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <add sel=\"/element\">" +
            "        <element type=\"Button\">" +
            "            <attribute name=\"Name\" value=\"Button3\" />" +
            "            <attribute name=\"Position\" value=\"-340 -180\" />" +
            "            <attribute name=\"Size\" value=\"144 144\" />" +
            "            <attribute name=\"Horiz Alignment\" value=\"Right\" />" +
            "            <attribute name=\"Vert Alignment\" value=\"Bottom\" />" +
            "            <attribute name=\"Texture\" value=\"Texture2D;Textures/TouchInput.png\" />" +
            "            <attribute name=\"Image Rect\" value=\"96 0 192 96\" />" +
            "            <attribute name=\"Hover Image Offset\" value=\"0 0\" />" +
            "            <attribute name=\"Pressed Image Offset\" value=\"0 0\" />" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"Label\" />" +
            "                <attribute name=\"Horiz Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Vert Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Color\" value=\"0 0 0 1\" />" +
            "                <attribute name=\"Text\" value=\"W\" />" +
            "            </element>" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "                <attribute name=\"Text\" value=\"W\" />" +
            "            </element>" +
            "        </element>" +   
            "        <element type=\"Button\">" +
            "            <attribute name=\"Name\" value=\"Button4\" />" +
            "            <attribute name=\"Position\" value=\"-340 -34\" />" +
            "            <attribute name=\"Size\" value=\"144 144\" />" +
            "            <attribute name=\"Horiz Alignment\" value=\"Right\" />" +
            "            <attribute name=\"Vert Alignment\" value=\"Bottom\" />" +
            "            <attribute name=\"Texture\" value=\"Texture2D;Textures/TouchInput.png\" />" +
            "            <attribute name=\"Image Rect\" value=\"96 0 192 96\" />" +
            "            <attribute name=\"Hover Image Offset\" value=\"0 0\" />" +
            "            <attribute name=\"Pressed Image Offset\" value=\"0 0\" />" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"Label\" />" +
            "                <attribute name=\"Horiz Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Vert Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Color\" value=\"0 0 0 1\" />" +
            "                <attribute name=\"Text\" value=\"S\" />" +
            "            </element>" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "                <attribute name=\"Text\" value=\"S\" />" +
            "            </element>" +
            "        </element>" +               
            "        <element type=\"Button\">" +
            "            <attribute name=\"Name\" value=\"Button5\" />" +
            "            <attribute name=\"Position\" value=\"-190 -180\" />" +
            "            <attribute name=\"Size\" value=\"144 144\" />" +
            "            <attribute name=\"Horiz Alignment\" value=\"Right\" />" +
            "            <attribute name=\"Vert Alignment\" value=\"Bottom\" />" +
            "            <attribute name=\"Texture\" value=\"Texture2D;Textures/TouchInput.png\" />" +
            "            <attribute name=\"Image Rect\" value=\"96 0 192 96\" />" +
            "            <attribute name=\"Hover Image Offset\" value=\"0 0\" />" +
            "            <attribute name=\"Pressed Image Offset\" value=\"0 0\" />" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"Label\" />" +
            "                <attribute name=\"Horiz Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Vert Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Color\" value=\"0 0 0 1\" />" +
            "                <attribute name=\"Text\" value=\"Q\" />" +
            "            </element>" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "                <attribute name=\"Text\" value=\"Q\" />" +
            "            </element>" +
            "        </element>" +
            "        <element type=\"Button\">" +
            "            <attribute name=\"Name\" value=\"Button6\" />" +
            "            <attribute name=\"Position\" value=\"-190 -34\" />" +
            "            <attribute name=\"Size\" value=\"144 144\" />" +
            "            <attribute name=\"Horiz Alignment\" value=\"Right\" />" +
            "            <attribute name=\"Vert Alignment\" value=\"Bottom\" />" +
            "            <attribute name=\"Texture\" value=\"Texture2D;Textures/TouchInput.png\" />" +
            "            <attribute name=\"Image Rect\" value=\"96 0 192 96\" />" +
            "            <attribute name=\"Hover Image Offset\" value=\"0 0\" />" +
            "            <attribute name=\"Pressed Image Offset\" value=\"0 0\" />" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"Label\" />" +
            "                <attribute name=\"Horiz Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Vert Alignment\" value=\"Center\" />" +
            "                <attribute name=\"Color\" value=\"0 0 0 1\" />" +
            "                <attribute name=\"Text\" value=\"E\" />" +
            "            </element>" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "                <attribute name=\"Text\" value=\"E\" />" +
            "            </element>" +
            "        </element>" +                  
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">R</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"R\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">G</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"G\" />" +
            "        </element>" +
            "    </add>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Hat0']]\">" +
			"        <attribute name=\"Is Visible\" value=\"false\" />" +
			"    </add>" +
            "</patch>";

    }
}
