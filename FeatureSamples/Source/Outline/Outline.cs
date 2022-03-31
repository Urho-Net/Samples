// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2015 the Urho3D project.

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

// Based upon the original work done by 1vanK
//https://github.com/1vanK/Urho3DOutline

using UrhoNetSamples;
using Urho;


namespace OutlineGlow
{
    public class OutlineGlow : Sample
    {

        protected Scene outlineScene = null;
        protected Node outlineCameraNode { get; set; }

        [Preserve]
        public OutlineGlow() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            CreateScene();
            SetupViewports();
            SimpleCreateInstructionsWithWasd("", Color.Black);
        }

        protected override void Stop()
        {
            if (outlineScene != null)
            {
                outlineScene.Dispose();
                outlineScene = null;
            }
            base.Stop();
        }


        protected override void OnUpdate(float timeStep)
        {

            base.OnUpdate(timeStep);
            SimpleMoveCamera3D(timeStep);
            outlineCameraNode.Position = CameraNode.Position;
            outlineCameraNode.Rotation = CameraNode.Rotation;
            outlineCameraNode.Scale = CameraNode.Scale;
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();
            outlineScene = new Scene();

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Also create a DebugRenderer component so that we can draw debug geometry
            scene.CreateComponent<Octree>();
            outlineScene.CreateComponent<Octree>();

            // Create scene node & StaticModel component for showing a static plane
            var planeNode = scene.CreateChild("Plane");
            planeNode.Scale = new Vector3(100.0f, 1.0f, 100.0f);
            var planeObject = planeNode.CreateComponent<StaticModel>();
            planeObject.Model = cache.GetModel("Models/Plane.mdl");
            planeObject.SetMaterial(cache.GetMaterial("Materials/StoneTiled.xml"));

            // Create a Zone component for ambient lighting & fog control
            var zoneNode = scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;

            // Create a directional light to the world. Enable cascaded shadows on it
            var lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            var light = lightNode.CreateComponent<Light>();
            // light.Color=new Color(0.6f,0.5f,0.2f);
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);


            // Create some mushrooms
            for (uint i = 0; i < 200; ++i)
            {
                var mushroomNode = scene.CreateChild("Mushroom");
                mushroomNode.Position = new Vector3(NextRandom(90.0f) - 45.0f, 0.0f, NextRandom(90.0f) - 45.0f);
                mushroomNode.Rotation = new Quaternion(0.0f, NextRandom(360.0f), 0.0f);
                mushroomNode.SetScale(0.5f + NextRandom(2.0f));
                var mushroomObject = mushroomNode.CreateComponent<StaticModel>();
                mushroomObject.Model = cache.GetModel("Models/Mushroom.mdl");
                mushroomObject.SetMaterial(cache.GetMaterial("Materials/Mushroom.xml"));
                mushroomObject.CastShadows = true;


                var outlineNode = outlineScene.CreateChild();
                outlineNode.Position = mushroomNode.Position;
                outlineNode.Rotation = mushroomNode.Rotation;
                outlineNode.Scale = mushroomNode.Scale;
                var mushroomOutlineObject = outlineNode.CreateComponent<StaticModel>();
                mushroomOutlineObject.Model = cache.GetModel("Models/Mushroom.mdl");
                mushroomOutlineObject.SetMaterial(cache.GetMaterial("Materials/White.xml"));
            }


            // Create the camera. Limit far clip distance to match the fog
            CameraNode = scene.CreateChild("Camera");
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = new Vector3(0.0f, 5.0f, 0.0f);


            outlineCameraNode = outlineScene.CreateChild("Camera");
            var outlinecamera = outlineCameraNode.CreateComponent<Camera>();
            outlinecamera.FarClip = 300.0f;
            // Set an initial position for the camera scene node above the plane
            outlineCameraNode.Position = new Vector3(0.0f, 5.0f, 0.0f);

        }


        void SetupViewports()
        {
            var cache = ResourceCache;


            var viewport = new Viewport(Context, scene, CameraNode.GetComponent<Camera>());
            Renderer.SetViewport(0, viewport);
            RenderPath effectRenderPath = viewport.RenderPath.Clone();
            effectRenderPath.Append(cache.GetXmlFile("PostProcess/Outline.xml"));
            viewport.RenderPath = effectRenderPath;


            var renderTexture = new Urho.Urho2D.Texture2D(Context);
            renderTexture.SetSize(Graphics.Width, Graphics.Height, Graphics.RGBFormat, TextureUsage.Rendertarget);
            renderTexture.FilterMode = TextureFilterMode.Nearest;
            renderTexture.Name = "OutlineMask";
            cache.AddManualResource(renderTexture);

            var surface = renderTexture.RenderSurface;
            surface.UpdateMode = RenderSurfaceUpdateMode.Updatealways;
            var outlineViewport = new Viewport(Context, outlineScene, outlineCameraNode.GetComponent<Camera>());
            surface.SetViewport(0, outlineViewport);

        }
    }
}