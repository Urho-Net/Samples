// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2021 the Urho3D project.
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
using Urho.Audio;
using Urho.Resources;
using Urho.Physics;
using Urho.Gui;
using System;
using System.Collections.Generic;
using Urho.IO;

namespace FlappyUrho
{
    public class FlappyUrho : Sample
    {


        [Preserve]
        public FlappyUrho() : base(new ApplicationOptions(assetsFolder: "Data/FlappyUrho;Data;CoreData")) { }

        bool IsDesktop()
        {
            return Application.Platform == Platforms.MacOSX || Application.Platform == Platforms.Windows || Application.Platform == Platforms.Linux;
        }
        protected override void Start()
        {
            base.Start();

            Global.gameState = GameState.GS_INTRO;

            Global.neededGameState = GameState.GS_INTRO;


            var cache = ResourceCache;

            CreateScene();
            CreateUI();

            Time.FrameStarted += HandleBeginFrame;

            SoundSource musicSource = scene.GetOrCreateComponent<SoundSource>();
            musicSource.SetSoundType(SoundType.Music.ToString());
            Sound music = cache.GetSound("Music/Urho - Disciples of Urho_LOOP.ogg");
            music.Looped = true;
            musicSource.Play(music);

            Audio.SetMasterGain(SoundType.Music.ToString(), 0.33f);

            LoadHighScore();
        }

        protected override void Stop()
        {
            base.Stop();
            Time.FrameStarted -= HandleBeginFrame;
            Global.Highscore = 0;

        }


        void CreateScene()
        {
            scene = new Scene();


            scene.CreateComponent<Octree>();
            scene.CreateComponent<PhysicsWorld>();


            // Create a scene node for the camera, which we will move around
            // The camera will use default settings (1000 far clip distance, 45 degrees FOV, set aspect ratio automatically)
            Node cameraNode = scene.CreateChild("camera");
            cameraNode.CreateComponent<FlappyCam>();


            Zone zone = cameraNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-100.0f * Vector3.One, 100.0f * Vector3.One));
            zone.FogStart = 34.0f;
            zone.FogEnd = 62.0f;
            zone.FogHeight = -19.0f;
            zone.HeightFog = true;
            zone.FogHeightScale = 0.1f;
            zone.FogColor = new Color(0.05f, 0.23f, 0.23f);
            zone.AmbientColor = new Color(0.05f, 0.13f, 0.13f);


            var lightNode = scene.CreateChild("DirectionalLight");
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowIntensity = 0.23f;
            light.Brightness = 1.23f;
            light.Color = new Color(0.8f, 1.0f, 1.0f);
            lightNode.SetDirection(new Vector3(-0.5f, -1.0f, 1.0f));




            var envNode = scene.CreateChild("Environment");

            var skybox = envNode.CreateComponent<Skybox>();
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Env.xml"));
            skybox.SetZone(zone);
            envNode.CreateComponent<Environment>();


            CreateUrho();
            CreateNets();
            CreateWeeds();
            CreateCrown();
        }

        void CreateUrho()
        {
            Node urhoNode = scene.CreateChild("Urho");
            urhoNode.CreateComponent<Fish>();
        }

        void CreateNets()
        {
            for (int i = 0; i < Global.NUM_BARRIERS; ++i)
            {
                Node barrierNode = scene.CreateChild("Barrier");
                barrierNode.CreateComponent<Barrier>();
                barrierNode.Position = new Vector3(Global.BAR_OUTSIDE_X * 1.23f + i * Global.BAR_INTERVAL, Global.BAR_RANDOM_Y, 0.0f);
            }
        }

        void CreateWeeds()
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int i = 0; i < Global.NUM_WEEDS; ++i)
                {
                    Node weedNode = scene.CreateChild("Weed");
                    weedNode.CreateComponent<Weed>();
                    weedNode.Position = new Vector3(i * Global.BAR_INTERVAL * Randoms.Next(0.1f, 0.23f) - 23.0f,
                                                  Global.WEED_RANDOM_Y,
                                                  Randoms.Next(-27.0f + r * 34.0f, -13.0f + r * 42.0f));

                    weedNode.Rotation = new Quaternion(0.0f, Randoms.Next(360.0f), 0.0f);
                    weedNode.Scale = new Vector3(Randoms.Next(0.5f, 1.23f), Randoms.Next(0.8f, 2.3f), Randoms.Next(0.5f, 1.23f));
                }
            }
        }

        void CreateCrown()
        {
            Node crownNode = scene.CreateChild("Crown");
            crownNode.CreateComponent<Crown>();
        }

        void CreateUI()
        {

            var ui = UI;
            var cache = ResourceCache;

            Font font = cache.GetFont("Fonts/Ubuntu-BI.ttf");

            Text helpText = ui.Root.CreateText("help text");
            helpText.SetFont(font, 20);
            helpText.TextEffect = (TextEffect.Shadow);
            helpText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            helpText.SetPosition(0, ui.Root.Height / 4);
            helpText.AddTag("Intro");
            helpText.AddTag("Dead");

            if (IsDesktop())
            {
                helpText.Value = "Left click to swim";
            }
            else
            {
                helpText.Value = "Touch screen to swim";
            }

            Node scoreNode = scene.CreateChild("Score");
            Node highscoreNode = scene.CreateChild("Highscore");
            Global.SetScores3D(scoreNode.CreateComponent<Score3D>(),
                                highscoreNode.CreateComponent<Score3D>());
        }


        void HandleBeginFrame(FrameStartedEventArgs arg)
        {
            if (Global.gameState == Global.neededGameState)
                return;

            var cache = ResourceCache;

            if (Global.neededGameState == GameState.GS_DEAD)
            {

                Node urhoNode = scene.GetChild("Urho");
                SoundSource soundSource = urhoNode.GetOrCreateComponent<SoundSource>();
                soundSource.Play(cache.GetSound("Samples/Hit.ogg"));

            }
            else if (Global.neededGameState == GameState.GS_INTRO)
            {

                Node urhoNode = scene.GetChild("Urho");
                Fish fish = urhoNode.GetComponent<Fish>();
                fish.Reset();

                Node crownNode = scene.GetChild("Crown");
                Crown crown = crownNode.GetComponent<Crown>();
                crown.Reset();

                if (Global.Score > Global.Highscore)
                {
                    Global.Highscore = Global.Score;
                    SaveHighScore();
                }
                Global.Score = 0;
                Global.sinceLastReset = 0.0f;

                Node[] barriers = scene.GetChildrenWithComponent<Barrier>();
                foreach (Node b in barriers)
                {

                    Vector3 pos = b.Position;
                    pos.Y = Global.BAR_RANDOM_Y;

                    if (pos.X < Global.BAR_OUTSIDE_X)
                        pos.X += Global.NUM_BARRIERS * Global.BAR_INTERVAL;

                    b.Position = pos;
                }


                Node[] weeds = scene.GetChildrenWithComponent<Weed>();
                foreach (Node w in weeds)
                {

                    w.Remove();
                }
                CreateWeeds();
            }

            Global.gameState = Global.neededGameState;

            UpdateUIVisibility();
        }

        void UpdateUIVisibility()
        {

            var ui_root = UI.Root;

            string tag;
            if (Global.gameState == GameState.GS_PLAY) tag = "Gameplay";
            else if (Global.gameState == GameState.GS_DEAD) tag = "Dead";
            else tag = "Intro";

            IReadOnlyList<UIElement> uiElements = ui_root.Children;

            foreach (UIElement e in uiElements)
            {
                e.Visible = e.HasTag(tag);
                if (e.Name == "BackButton") e.Visible = true;
            }

        }
        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            var input = Input;

            if (Global.gameState == GameState.GS_PLAY)
            {

                Global.sinceLastReset += timeStep;
            }

            scene.TimeScale = (float)Math.Pow(1.0f + Math.Clamp(Global.sinceLastReset * 0.0023f, 0.0f, 1.0f), 2.3f);
            SoundSource musicSource = scene.GetComponent<SoundSource>();
            musicSource.Frequency = (float)(0.5f * (musicSource.Frequency + 44000.0f * Math.Sqrt(scene.TimeScale)));

            if (input.GetMouseButtonPress(MouseButton.Left) || (input.NumTouches > 0))
            {

                if (Global.gameState == GameState.GS_INTRO)
                    Global.neededGameState = GameState.GS_PLAY;
                else if (Global.gameState == GameState.GS_DEAD)
                    Global.neededGameState = GameState.GS_INTRO;
            }


            if (IsDesktop() && input.GetKeyPress(Key.N9))
            {
                Image screenshot = new Image();
                Graphics.TakeScreenShot(screenshot);
                screenshot.SavePNG(FileSystem.ProgramDir + $"Assets/Data/Screenshot_{GetType().Name}_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.png");
            }

            Global.OnUpdate();

        }

        void SaveHighScore()
        {
            FileSystem.CreateDir(FileSystem.UserDocumentsDir + "/FlappyUrho");

            string filePath = FileSystem.UserDocumentsDir + "/FlappyUrho/configs.xml";
            using (var file = new File(Context, filePath, FileMode.Write))
            {
                var xmlConfig = new XmlFile();
                var configElem = xmlConfig.CreateRoot("root");
                XmlElement highscoreElem = configElem.CreateChild("HighScore");
                highscoreElem.SetUInt("value", Global.Highscore);
                xmlConfig.Save(file);

            }
        }

        void LoadHighScore()
        {

            string filePath = FileSystem.UserDocumentsDir + "/FlappyUrho/configs.xml";
            if (!FileSystem.FileExists(filePath)) return;


            using (var file = new File(Context, filePath, FileMode.Read))
            {
                var xmlConfig = new XmlFile();
                xmlConfig.Load(file);

                XmlElement configElem = xmlConfig.GetRoot();

                if (configElem.Null == true) return;

                XmlElement highscoreElem = configElem.GetChild("HighScore");

                if (highscoreElem.Null == false && highscoreElem.HasAttribute("value"))
                {
                    Global.Highscore = highscoreElem.GetUInt("value");
                }
            }

        }

        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch => JoystickLayoutPatches.Hidden;
    }
}