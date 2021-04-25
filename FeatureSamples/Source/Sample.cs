// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2020 the Urho3D project.
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

using System;
using System.Diagnostics;
using System.Globalization;
using Urho;
using Urho.Resources;
using Urho.Gui;
using Urho.Audio;
using Urho.IO;
using Urho.Network;
using Urho.Urho2D;


namespace UrhoNetSamples
{
    public class Sample : Component
    {

        protected Scene scene = null;
        UrhoConsole console;
        DebugHud debugHud;
        ResourceCache cache;
        Sprite logoSprite;

        Text infoText;

        protected int screenJoystickIndex = -1;
        public static bool isMobile;
        public event Action RequestToExit;

        protected Renderer Renderer;
        protected ResourceCache ResourceCache;
        protected Input Input;
        protected Time Time;
        protected Audio Audio;
        protected UI UI;
        protected Graphics Graphics;
        protected FileSystem FileSystem;

        protected Engine Engine;

        protected Network Network;

        protected Log Log;
        public Button backButton;
        public Button infoButton;

        protected const float TouchSensitivity = 2;
        protected float Yaw { get; set; }
        protected float Pitch { get; set; }
        protected bool TouchEnabled { get; set; }
        protected Node CameraNode { get; set; }
        protected MonoDebugHud MonoDebugHud { get; set; }
        bool isMonoDebugHudVisible;

        protected enum E_JoystickType
        {
            OneJoyStick_NoButtons = 1,
            OneJoyStick_OneButton,
            OneJoyStick_TwoButtons
        }

        protected E_JoystickType JoystickType = E_JoystickType.OneJoyStick_NoButtons;

        [Preserve]
        protected Sample(ApplicationOptions options = null)
        {
            // This one is not used because Sample is inherited from Component and not Application .
            // if you want to add a resource path , add it in UrhoNetSamples.cs
        }

        static string DefaultJoystickLayout = "";

        static Sample()
        {

            Urho.Application.UnhandledException += Application_UnhandledException1;
        }
                
        public void ExitSample()
        {
            RequestToExit?.Invoke();
        }
        static void Application_UnhandledException1(object sender, Urho.UnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached && !e.Exception.Message.Contains("BlueHighway.ttf"))
                Debugger.Break();
            e.Handled = true;
        }

        protected bool IsLogoVisible
        {
            get { return logoSprite.Visible; }
            set { logoSprite.Visible = value; }
        }

        static readonly Random random = new Random();
        /// Return a random float between 0.0 (inclusive) and 1.0 (exclusive.)
        public static float NextRandom() { return (float)random.NextDouble(); }
        /// Return a random float between 0.0 and range, inclusive from both ends.
        public static float NextRandom(float range) { return (float)random.NextDouble() * range; }
        /// Return a random float between min and max, inclusive from both ends.
        public static float NextRandom(float min, float max) { return (float)((random.NextDouble() * (max - min)) + min); }
        /// Return a random integer between min and max - 1.
        public static int NextRandom(int min, int max) { return random.Next(min, max); }

        /// <summary>
        /// Joystick XML layout for mobile platforms
        /// </summary>
        protected virtual string JoystickLayoutPatch => string.Empty;


        public string GetJoystickLayoutPatch()
        {
            return JoystickLayoutPatch;
        }
        public void Run()
        {
            LogSharp.LogLevel = LogSharpLevel.Debug;

            if (DefaultJoystickLayout == "")
            {
                using (var layout = Application.ResourceCache.GetXmlFile("UI/ScreenJoystick_Samples.xml"))
                {
                    DefaultJoystickLayout = layout.ToDebugString();
                }
            }

            Start();
        }

        public void Exit()
        {
            Stop();
        }
        protected virtual void Start()
        {
            Renderer = Application.Renderer;
            ResourceCache = Application.ResourceCache;
            Input = Application.Input;
            Time = Application.Time;
            Audio = Application.Audio;
            UI = Application.UI;
            Graphics = Application.Graphics;
            FileSystem = Application.FileSystem;
            Engine = Application.Engine;
            Network = Application.Network;
            Log = Application.Log;

            this.ReceiveSceneUpdates = true;

            isMobile = (Application.Platform == Platforms.iOS || Application.Platform == Platforms.Android);

            if (Application.Platform == Platforms.Android ||
                Application.Platform == Platforms.iOS ||
                Application.Options.TouchEmulation)
            {
                InitTouchInput();
            }
            Application.Input.Enabled = true;
            MonoDebugHud = new MonoDebugHud(Application);
            MonoDebugHud.Show();
            isMonoDebugHudVisible = true;

            CreateLogo();
            SetWindowAndTitleIcon();
            CreateConsoleAndDebugHud();
            Application.Input.KeyDown += HandleKeyDown;

            backButton = CreateButton("<<<<", 130);
            backButton.Name = "BackButton";
            backButton.Opacity = 0.6f;
            backButton.Position = new IntVector2(10, 10);


            infoButton = CreateButton("Info", 130);
            infoButton.Name = "InfoButton";
            infoButton.Opacity = 0.6f;
            infoButton.Position = new IntVector2(10, 80);
        }

        protected virtual void Stop()
        {
            UnSubscribeFromAllEvents();

            MonoDebugHud?.Hide();
            MonoDebugHud = null;
            if (isMobile)
            {
                Application.Input.RemoveScreenJoystick(screenJoystickIndex);
            }
            this.ReceiveSceneUpdates = false;

            for (uint i = 0; i < Renderer.NumViewports; i++)
            {
                Renderer.SetViewport(i, null);
            }

            scene?.Dispose();
        }

        protected override void OnUpdate(float timeStep)
        {
            MoveCameraByTouches(timeStep);
            base.OnUpdate(timeStep);
        }

        /// <summary>
        /// Move camera for 2D samples
        /// </summary>
        protected void SimpleMoveCamera2D(float timeStep)
        {
            // Do not move if the UI has a focused element (the console)
            if (Application.UI.FocusElement != null)
                return;

            // Movement speed as world units per second
            const float moveSpeed = 4.0f;

            // Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
            if (Application.Input.GetKeyDown(Key.W)) CameraNode.Translate(Vector3.UnitY * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.S)) CameraNode.Translate(-Vector3.UnitY * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.A)) CameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.D)) CameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);

            if (Application.Input.GetKeyDown(Key.PageUp))
            {
                Camera camera = CameraNode.GetComponent<Camera>();
                camera.Zoom = camera.Zoom * 1.01f;
            }

            if (Application.Input.GetKeyDown(Key.PageDown))
            {
                Camera camera = CameraNode.GetComponent<Camera>();
                camera.Zoom = camera.Zoom * 0.99f;
            }
        }

        /// <summary>
        /// Move camera for 3D samples
        /// </summary>
        protected void SimpleMoveCamera3D(float timeStep, float moveSpeed = 10.0f)
        {
            const float mouseSensitivity = .1f;

            if (Application.UI.FocusElement != null)
                return;

            var mouseMove = Application.Input.MouseMove;
            Yaw += mouseSensitivity * mouseMove.X;
            Pitch += mouseSensitivity * mouseMove.Y;
            Pitch = MathHelper.Clamp(Pitch, -90, 90);

            CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);

            if (Application.Input.GetKeyDown(Key.W)) CameraNode.Translate(Vector3.UnitZ * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.S)) CameraNode.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.A)) CameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
            if (Application.Input.GetKeyDown(Key.D)) CameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);
        }

        protected void MoveCameraByTouches(float timeStep)
        {
            if (!TouchEnabled || CameraNode == null)
                return;

            var input = Application.Input;
            for (uint i = 0, num = input.NumTouches; i < num; ++i)
            {
                TouchState state = input.GetTouch(i);
                if (state.TouchedElement != null)
                    continue;

                if (state.Delta.X != 0 || state.Delta.Y != 0)
                {
                    var camera = CameraNode.GetComponent<Camera>();
                    if (camera == null)
                        return;

                    var graphics = Application.Graphics;
                    Yaw += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.X;
                    Pitch += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.Y;
                    CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
                }
                else
                {
                    var cursor = Application.UI.Cursor;
                    if (cursor != null && cursor.Visible)
                        cursor.Position = state.Position;
                }
            }
        }

        protected void SimpleCreateInstructionsWithWasd(string extra = "", Color textColor = new Color())
        {
            string text = "";

            if (isMobile)
            {
                text = "Use virtual joystick to move";
            }
            else
            {
                text = "Use WASD keys and mouse/touch to move";
            }

            if (extra != "")
            {
                extra += "\n";
            }

            SimpleCreateInstructions(extra + text, textColor);
        }

        protected void SimpleCreateInstructions(string text = "", Color textColor = new Color())
        {
            text += "\nPress Info to toggle this textual information";

            if (isMobile)
            {
                text += "\nPress <<<< to go back to the main list";
            }
            else
            {
                text += "\nPress Esacpe to go back to the main list";
            }

            infoText = new Text()
            {
                Value = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            if (Graphics.Width <= 1024)
            {
                infoText.SetFont(Application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 16);
            }
            else if (Graphics.Width <= 1440)
            {
                infoText.SetFont(Application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 20);
            }
            else
            {
                infoText.SetFont(Application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 25);
            }

            if (textColor != Color.Transparent)
                infoText.SetColor(textColor);

            Application.UI.Root.AddChild(infoText);

            infoText.Visible = true;
        }

        public void ToggleInfo()
        {
            infoText.Visible = !infoText.Visible;
            logoSprite.Visible = !logoSprite.Visible;
            if (isMonoDebugHudVisible)
            {
                MonoDebugHud.Hide();
                isMonoDebugHudVisible = false;
            }
            else
            {
                MonoDebugHud.Show();
                isMonoDebugHudVisible = true;
            }
        }

        void CreateLogo()
        {
            cache = Application.ResourceCache;
            var logoTexture = cache.GetTexture2D("Textures/LogoLarge.png");

            if (logoTexture == null)
                return;

            logoSprite = UI.Root.CreateSprite();
            logoSprite.Texture = logoTexture;
            int w = logoTexture.Width;
            int h = logoTexture.Height;
            logoSprite.SetScale(256.0f / w);
            logoSprite.SetSize(w, h);
            logoSprite.SetHotSpot(0, h);
            // logoSprite.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Bottom);
            logoSprite.Position = new IntVector2(Graphics.Width / 2 - 200, Graphics.Height - 80);
            logoSprite.Opacity = 0.75f;
            logoSprite.Priority = -100;

            logoSprite.Visible = true;
        }

        void SetWindowAndTitleIcon()
        {
            var icon = cache.GetImage("Textures/UrhoIcon.png");
            Application.Graphics.SetWindowIcon(icon);
            Application.Graphics.WindowTitle = "UrhoNetSamples";
        }

        void CreateConsoleAndDebugHud()
        {
            var xml = cache.GetXmlFile("UI/DefaultStyle.xml");
            console = Application.Engine.CreateConsole();
            console.DefaultStyle = xml;
            console.Background.Opacity = 0.8f;

            debugHud = Application.Engine.CreateDebugHud();
            debugHud.DefaultStyle = xml;
        }

        void HandleKeyDown(KeyDownEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    console.Toggle();
                    return;
                case Key.F2:
                    debugHud.ToggleAll();
                    return;
            }

            var renderer = Application.Renderer;
            switch (e.Key)
            {
                case Key.N1:
                    var quality = (int)renderer.TextureQuality;
                    ++quality;
                    if (quality > 2)
                        quality = 0;
                    renderer.TextureQuality = (MaterialQuality)quality;
                    break;

                case Key.N2:
                    var mquality = (int)renderer.MaterialQuality;
                    ++mquality;
                    if (mquality > 2)
                        mquality = 0;
                    renderer.MaterialQuality = (MaterialQuality)mquality;
                    break;
                case Key.N3:
                    renderer.SpecularLighting = !renderer.SpecularLighting;
                    break;

                case Key.N4:
                    renderer.DrawShadows = !renderer.DrawShadows;
                    break;

                case Key.N5:
                    var shadowMapSize = renderer.ShadowMapSize;
                    shadowMapSize *= 2;
                    if (shadowMapSize > 2048)
                        shadowMapSize = 512;
                    renderer.ShadowMapSize = shadowMapSize;
                    break;

                // shadow depth and filtering quality
                case Key.N6:
                    var q = (int)renderer.ShadowQuality++;
                    if (q > 3)
                        q = 0;
                    renderer.ShadowQuality = (ShadowQuality)q;
                    break;

                // occlusion culling
                case Key.N7:
                    var o = !(renderer.MaxOccluderTriangles > 0);
                    renderer.MaxOccluderTriangles = o ? 5000 : 0;
                    break;

                // instancing
                case Key.N8:
                    renderer.DynamicInstancing = !renderer.DynamicInstancing;
                    break;

                case Key.N9:
                    Image screenshot = new Image();
                    Application.Graphics.TakeScreenShot(screenshot);
                    screenshot.SavePNG(Application.FileSystem.ProgramDir + $"Data/Screenshot_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}.png");
                    break;
            }
        }

        void InitTouchInput()
        {
            TouchEnabled = true;

            RemoveScreenJoystick();

            using (var layout = new XmlFile())
            {
                layout.FromString(DefaultJoystickLayout);

                if (!string.IsNullOrEmpty(JoystickLayoutPatch))
                {
                    using (XmlFile patchXmlFile = new XmlFile())
                    {
                        patchXmlFile.FromString(JoystickLayoutPatch);
                        layout.Patch(patchXmlFile);
                    }
                }
                screenJoystickIndex = Application.Input.AddScreenJoystick(layout, ResourceCache.GetXmlFile("UI/DefaultStyle.xml"));
                Application.Input.SetScreenJoystickVisible(screenJoystickIndex, true);
            }

        }

        protected void AdjuistJoystickSize(XmlFile layout)
        {

            int multiplier = (Graphics.Width + Graphics.Height) / 100;

            IntVector2 sizeButtonA = new IntVector2();
            IntVector2 positionButtonA = new IntVector2();
            IntVector2 sizeButtonB = new IntVector2();
            IntVector2 positionButtonB = new IntVector2();

            if (JoystickType == E_JoystickType.OneJoyStick_OneButton)
            {
                sizeButtonA = new IntVector2(multiplier * 5, multiplier * 5);
                positionButtonA = new IntVector2(multiplier * -5, multiplier * -4);
            }
            else if (JoystickType == E_JoystickType.OneJoyStick_TwoButtons)
            {
                sizeButtonA = new IntVector2(multiplier * 5, multiplier * 5);
                positionButtonA = new IntVector2(multiplier * -5, multiplier * -8);
                sizeButtonB = new IntVector2(multiplier * 5, multiplier * 5);
                positionButtonB = new IntVector2(multiplier * -5, multiplier * -2);
            }

            IntVector2 sizeLStick = new IntVector2(multiplier * 10 + 5, multiplier * 10 + 5);
            IntVector2 positionLStick = new IntVector2(multiplier * 2, multiplier * -2);
            IntVector2 sizeInnerButton = new IntVector2(multiplier * 8, multiplier * 8);
            IntVector2 positionInnerButton = new IntVector2(positionLStick.X / 2 + 3, Math.Abs(positionLStick.Y / 2) + 4);

            string patch = "<patch>";

            patch +=  string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Axis0']]/attribute[@name='Size']/@value\">{0} {1}</replace>", sizeLStick.X, sizeLStick.Y);
            patch +=  string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Axis0']]/attribute[@name='Position']/@value\">{0} {1}</replace>", positionLStick.X, positionLStick.Y);

            patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Axis0']]/element[./attribute[@name='Name' and @value='InnerButton']]/attribute[@name='Size']/@value\">{0} {1}</replace>", sizeInnerButton.X, sizeInnerButton.Y);
            patch +=  string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Axis0']]/element[./attribute[@name='Name' and @value='InnerButton']]/attribute[@name='Position']/@value\">{0} {1}</replace>", positionInnerButton.X, positionInnerButton.Y);

            if (JoystickType == E_JoystickType.OneJoyStick_OneButton)
            {
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Size']/@value\">{0} {1}</replace>", sizeButtonA.X, sizeButtonA.Y);
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Position']/@value\">{0} {1}</replace>", positionButtonA.X, positionButtonA.Y);
            }
            else if (JoystickType == E_JoystickType.OneJoyStick_TwoButtons)
            {
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Size']/@value\">{0} {1}</replace>", sizeButtonA.X, sizeButtonA.Y);
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Position']/@value\">{0} {1}</replace>", positionButtonA.X, positionButtonA.Y);
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Size']/@value\">{0} {1}</replace>", sizeButtonB.X, sizeButtonB.Y);
                patch += string.Format("<replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Position']/@value\">{0} {1}</replace>", positionButtonB.X, positionButtonB.Y);

            }

            patch += "</patch>";

            using (XmlFile patchXmlFile = new XmlFile())
            {
                patchXmlFile.FromString(patch);
                layout.Patch(patchXmlFile);
            }

        }

        protected void CreateScreenJoystick(E_JoystickType type = E_JoystickType.OneJoyStick_NoButtons)
        {
            RemoveScreenJoystick();

            JoystickType = type;

            string path = "ScreenJoystick/ScreenOneJoystick.xml";

            switch (type)
            {

                case E_JoystickType.OneJoyStick_OneButton:
                    path = "ScreenJoystick/ScreenOneJoystickOneButton.xml";
                    break;

                case E_JoystickType.OneJoyStick_TwoButtons:
                    path = "ScreenJoystick/ScreenOneJoystickTwoButtons.xml";
                    break;
                default:
                    path = "ScreenJoystick/ScreenOneJoystick.xml";
                    break;
            }

            XmlFile layout = ResourceCache.GetXmlFile(path);

            AdjuistJoystickSize(layout);

            screenJoystickIndex = Application.Input.AddScreenJoystick(layout, ResourceCache.GetXmlFile("UI/DefaultStyle.xml"));
            Application.Input.SetScreenJoystickVisible(screenJoystickIndex, true);

        }

        protected void RemoveScreenJoystick()
        {
            if (screenJoystickIndex != -1)
            {
                Application.Input.SetScreenJoystickVisible(screenJoystickIndex, false);
                Application.Input.RemoveScreenJoystick(screenJoystickIndex);
                screenJoystickIndex = -1;
            }
        }


        private Button CreateButton(string text, int width)
        {
            var cache = ResourceCache;
            Font font = cache.GetFont("Fonts/Anonymous Pro.ttf");

            Button button = new Button();
            UI.Root.AddChild(button);
            button.SetStyleAuto();
            button.SetFixedHeight(60);
            button.SetFixedWidth(width);

            var buttonText = new Text();
            button.AddChild(buttonText);
            buttonText.SetFont(font, 32);
            buttonText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);

            buttonText.Value = text;

            return button;
        }

    }
}
