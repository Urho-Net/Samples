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

using UrhoNetSamples;
using Urho;


namespace MultipleViewports
{
    public class MultipleViewports : Sample
    {

        bool drawDebug;
        Node rearCameraNode;

        [Preserve]
        public MultipleViewports() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            Graphics.WindowTitle = "MultipleViewports";
            CreateScene();
            if (isMobile)
                SimpleCreateInstructionsWithWasd("Bloom to toggle bloom, FXAA to toggle FXAA\n" +"Debug to toggle debug geometry");
            else
                SimpleCreateInstructionsWithWasd("B to toggle bloom, F to toggle FXAA\n" +"Space to toggle debug geometry");
            SetupViewport();
            SubscribeToEvents();
        }


        void SubscribeToEvents()
        {
            Engine.PostRenderUpdate += (args =>
                {
                    // If draw debug mode is enabled, draw viewport debug geometry, which will show eg. drawable bounding boxes and skeleton
                    // bones. Note that debug geometry has to be separately requested each frame. Disable depth test so that we can see the
                    // bones properly
                    if (drawDebug)
                        Renderer.DrawDebugGeometry(false);
                });
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
            SimpleMoveCamera3D(timeStep);

            var effectRenderPath = Renderer.GetViewport(0).RenderPath;
            if (Input.GetKeyPress(Key.B))
                effectRenderPath.ToggleEnabled("Bloom");
            if (Input.GetKeyPress(Key.F))
                effectRenderPath.ToggleEnabled("FXAA2");

            if (Input.GetKeyPress(Key.Space))
                drawDebug = !drawDebug;
        }

        void SetupViewport()
        {
            var renderer = Renderer;
            var graphics = Graphics;

            renderer.NumViewports = 2;

            // Set up the front camera viewport
            Viewport viewport = new Viewport(Context, scene, CameraNode.GetComponent<Camera>(), null);
            renderer.SetViewport(0, viewport);

            // Clone the default render path so that we do not interfere with the other viewport, then add
            // bloom and FXAA post process effects to the front viewport. Render path commands can be tagged
            // for example with the effect name to allow easy toggling on and off. We start with the effects
            // disabled.
            var cache = ResourceCache;
            RenderPath effectRenderPath = viewport.RenderPath.Clone();
            effectRenderPath.Append(cache.GetXmlFile("PostProcess/Bloom.xml"));
            effectRenderPath.Append(cache.GetXmlFile("PostProcess/FXAA2.xml"));
            // Make the bloom mixing parameter more pronounced
            effectRenderPath.SetShaderParameter("BloomMix", new Vector2(0.9f, 0.6f));

            effectRenderPath.SetEnabled("Bloom", false);
            effectRenderPath.SetEnabled("FXAA2", false);
            viewport.RenderPath = effectRenderPath;

            // Set up the rear camera viewport on top of the front view ("rear view mirror")
            // The viewport index must be greater in that case, otherwise the view would be left behind
            IntRect rect = new IntRect(graphics.Width * 2 / 3, 32, graphics.Width - 32, graphics.Height / 3);
            Viewport rearViewport = new Viewport(Context, scene, rearCameraNode.GetComponent<Camera>(), rect, null);

            renderer.SetViewport(1, rearViewport);
        }

        void CreateScene()
        {
            var cache = ResourceCache;
            scene = new Scene();

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Also create a DebugRenderer component so that we can draw debug geometry
            scene.CreateComponent<Octree>();
            scene.CreateComponent<DebugRenderer>();

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
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            // Create some mushrooms
            const uint numMushrooms = 240;
            for (uint i = 0; i < numMushrooms; ++i)
            {
                var mushroomNode = scene.CreateChild("Mushroom");
                mushroomNode.Position = new Vector3(NextRandom(90.0f) - 45.0f, 0.0f, NextRandom(90.0f) - 45.0f);
                mushroomNode.Rotation = new Quaternion(0.0f, NextRandom(360.0f), 0.0f);
                mushroomNode.SetScale(0.5f + NextRandom(2.0f));
                StaticModel mushroomObject = mushroomNode.CreateComponent<StaticModel>();
                mushroomObject.Model = cache.GetModel("Models/Mushroom.mdl");
                mushroomObject.SetMaterial(cache.GetMaterial("Materials/Mushroom.xml"));
                mushroomObject.CastShadows = true;
            }

            // Create randomly sized boxes. If boxes are big enough, make them occluders
            const uint numBoxes = 20;
            for (uint i = 0; i < numBoxes; ++i)
            {
                var boxNode = scene.CreateChild("Box");
                float size = 1.0f + NextRandom(10.0f);
                boxNode.Position = new Vector3(NextRandom(80.0f) - 40.0f, size * 0.5f, NextRandom(80.0f) - 40.0f);
                boxNode.SetScale(size);
                StaticModel boxObject = boxNode.CreateComponent<StaticModel>();
                boxObject.Model = cache.GetModel("Models/Box.mdl");
                boxObject.SetMaterial(cache.GetMaterial("Materials/Stone.xml"));
                boxObject.CastShadows = true;
                if (size >= 3.0f)
                    boxObject.Occluder = true;
            }

            // Create the cameras. Limit far clip distance to match the fog
            CameraNode = scene.CreateChild("Camera");
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;

            // Parent the rear camera node to the front camera node and turn it 180 degrees to face backward
            // Here, we use the angle-axis constructor for Quaternion instead of the usual Euler angles
            rearCameraNode = CameraNode.CreateChild("RearCamera");
            rearCameraNode.Rotate(Quaternion.FromAxisAngle(Vector3.UnitY, 180.0f), TransformSpace.Local);

            Camera rearCamera = rearCameraNode.CreateComponent<Camera>();
            rearCamera.FarClip = 300.0f;
            // Because the rear viewport is rather small, disable occlusion culling from it. Use the camera's
            // "view override flags" for this. We could also disable eg. shadows or force low material quality
            // if we wanted

            rearCamera.ViewOverrideFlags = ViewOverrideFlags.DisableOcclusion;

            // Set an initial position for the front camera scene node above the plane
            CameraNode.Position = new Vector3(0.0f, 5.0f, 0.0f);
        }

        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <add sel=\"/element\">" +
            "        <element type=\"Button\">" +
            "            <attribute name=\"Name\" value=\"Button3\" />" +
            "            <attribute name=\"Position\" value=\"-190 -120\" />" +
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
            "                <attribute name=\"Text\" value=\"FXAA\" />" +
            "            </element>" +
            "            <element type=\"Text\">" +
            "                <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "                <attribute name=\"Text\" value=\"F\" />" +
            "            </element>" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Bloom</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"B\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Debug</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"SPACE\" />" +
            "        </element>" +
            "    </add>" +
            "</patch>";
    }
}