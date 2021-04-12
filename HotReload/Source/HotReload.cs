// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
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
using System;
using Urho.IO;
using Urho.Resources;

namespace HotReload
{
/**
- This Sample demonstrates the ability to hot-relaod modified components , while the application is running
- There are 2 components in this demo Oscillator.cs and Rotator.cs that are modifiable and hot-reloaded during the runtime of the app.
- Start the app 
- Modify the source code of either or both compnents , save ,and observe compilation and hot-reload of the components , the scene will be updated on the fly.
- The compnents can be modified in any possible way , they will be reloaded only if compilation succeeds.
- Recomended modifications in Rotator.cs are Vector3 RotationSpeed
- Recomended modifications in Oscillator.cs are  : Vector3 movementVector  ,float movementFactor  ,float period;
*/
    public class HotReload : Sample
    {

        DynamicComponentManager dynamicComponentManager = null;
        Camera camera;
        Scene scene;

        [Preserve]
        public HotReload() : base(new ApplicationOptions(assetsFolder: "Data;CoreData;Source"))
        {
            // Currently only supported on Desktop
            if (Platform.IsMobile() == true) Exit();
        }

        protected override void Start()
        {
            base.Start();

            var cache = ResourceCache;
            cache.AutoReloadResources = true;

            scene = new Scene();

            dynamicComponentManager = new DynamicComponentManager();
            dynamicComponentManager.Temporary = true;
            dynamicComponentManager.SetScene(scene);

            CreateScene();
            SimpleCreateInstructionsWithWasd();
            SetupViewport();


        }



        protected override void Stop()
        {
            dynamicComponentManager?.Stop();
            dynamicComponentManager = null;
        }

        void CreateScene()
        {

            scene.CreateComponent<Octree>();

            var planeNode = scene.CreateChild("Plane");
            planeNode.Scale = new Vector3(100, 1, 100);
            var planeObject = planeNode.CreateComponent<StaticModel>();
            planeObject.Model = ResourceCache.GetModel("Models/Plane.mdl");
            planeObject.SetMaterial(ResourceCache.GetMaterial("Materials/StoneTiled.xml"));


            var lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f)); // The direction vector does not need to be normalized
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;



            var skyNode = scene.CreateChild("Sky");
            skyNode.SetScale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>();
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));

            var rand = new Random();
            for (int i = 0; i < 200; i++)
            {
                var mushroom = scene.CreateChild("Mushroom");
                mushroom.Position = new Vector3(rand.Next(90) - 45, 0, rand.Next(90) - 45);
                mushroom.Rotation = new Quaternion(0, rand.Next(360), 0);
                mushroom.SetScale(0.5f + rand.Next(20000) / 10000.0f);
                var mushroomObject = mushroom.CreateComponent<StaticModel>();
                mushroomObject.Model = ResourceCache.GetModel("Models/Mushroom.mdl");
                mushroomObject.SetMaterial(ResourceCache.GetMaterial("Materials/Mushroom.xml"));

                Component comp = dynamicComponentManager.CreateComponent("Rotator.cs");
                if (comp != null)
                {
                    mushroom.AddComponent(comp);
                }

                Component oscillator = dynamicComponentManager.CreateComponent("Oscillator.cs");
                if (oscillator != null)
                {
                    mushroom.AddComponent(oscillator);
                }



            }

            CameraNode = scene.CreateChild("camera");
            camera = CameraNode.CreateComponent<Camera>();


            CameraNode.Position = new Vector3(0, 5, 0);
        }

        void SetupViewport()
        {
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));
        }

        protected override void OnUpdate(float timeStep)
        {
            Input input = Input;

            base.OnUpdate(timeStep);
            SimpleMoveCamera3D(timeStep);

            if (input.GetKeyPress(Key.F5))
            {
                string path = FileSystem.ProgramDir + "Assets/Data/Scenes";
                if (!FileSystem.DirExists(path))
                {
                    FileSystem.CreateDir(path);
                }
                scene.SaveXml(path + "/HotReloadDemo.xml");
            }


            if (input.GetKeyPress(Key.F7))
            {
                 string filePath = FileSystem.ProgramDir + "Assets/Data/Scenes/HotReloadDemo.xml";
                if (FileSystem.FileExists(filePath))
                {
                    scene.LoadXml(filePath);
                    dynamicComponentManager.SetScene(scene);

                    CameraNode = scene.GetChild("camera");
                    camera = CameraNode.GetComponent<Camera>();
                    SetupViewport();
                }

            }
        }
    }
}