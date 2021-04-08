
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
using Urho.Actions;
using Urho.Gui;

namespace BasicTechniques
{
    public class BasicTechniques : Sample
    {
        float yaw;
        float pitch;
        Node cameraNode;


        [Preserve]
        public BasicTechniques() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }


        protected override void Start()
        {
            base.Start();

            SimpleCreateInstructionsWithWasd();
            // Create the scene content
            CreateScene();

            // Setup the viewport for displaying the scene
            SetupViewport();
        }

        void CreateScene()
        {
            Renderer.HDRRendering = true;
            scene = new Scene();
            scene.CreateComponent<Octree>();
            var zone = scene.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.3f, 0.3f, 0.3f);

            const float stepX = 0.23f;
            const float stepY = 0.3f;

            //by enabling this flag, we are able to edit assets via external editors (e.g. VS Code) and see changes immediately.
            ResourceCache.AutoReloadResources = true;

            cameraNode = scene.CreateChild();
            cameraNode.CreateComponent<Camera>();
            cameraNode.Position = new Vector3(stepX, -stepY, -1);

            Node lightNode = scene.CreateChild();
            lightNode.SetDirection(new Vector3(-1, -1, 1));
            Light light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.Brightness = 1.1f;
            lightNode.Position = new Vector3(0, 0, 0);

            var materials = new string[,]
            {
                { "NoTexture", "NoTextureUnlit", "NoTextureNormal", "NoTextureAdd", "NoTextureMultiply" },
                { "Diff", "DiffUnlit", "DiffNormal", "DiffAlpha", "DiffAdd" },
                { "DiffEmissive", "DiffSpec", "DiffNormalSpec", "DiffAO", "DiffEnvCube" },
                { "Water", "Terrain", "NoTextureVCol", "Default", "CustomShader" },
            };

            for (int i = 0; i < materials.GetLength(1); i++)
            {
                for (int j = 0; j < materials.GetLength(0); j++)
                {
                    var sphereNode = scene.CreateChild();
                    var earthNode = sphereNode.CreateChild();
                    var textNode = sphereNode.CreateChild();
                    var material = materials[j, i];

                    Text3D text = textNode.CreateComponent<Text3D>();
                    text.Text = material;
                    text.SetFont(CoreAssets.Fonts.AnonymousPro, 13);
                    text.TextAlignment = HorizontalAlignment.Center;
                    text.VerticalAlignment = VerticalAlignment.Bottom;
                    text.HorizontalAlignment = HorizontalAlignment.Center;
                    textNode.Position = new Vector3(0, -0.75f, 0);

                    sphereNode.Position = new Vector3(i * stepX, -j * stepY, 1);
                    sphereNode.SetScale(0.2f);

                    var earthModel = earthNode.CreateComponent<StaticModel>();
                    //for VCol we have a special model:
                    if (material.Contains("VCol"))
                        earthModel.Model = ResourceCache.GetModel("Sample43/SphereVCol.mdl");
                    else
                        //built-in sphere model (.mdl):
                        earthModel.Model = CoreAssets.Models.Sphere;

                    earthModel.SetMaterial(ResourceCache.GetMaterial($"Sample43/Mat{material}.xml", sendEventOnFailure: false));
                    var backgroundNode = sphereNode.CreateChild();
                    backgroundNode.Scale = new Vector3(1, 1, 0.001f) * 1.1f;
                    backgroundNode.Position = new Vector3(0, 0, 0.55f);
                    var backgroundModel = backgroundNode.CreateComponent<StaticModel>();
                    backgroundModel.Model = CoreAssets.Models.Box;
                    backgroundModel.SetMaterial(Material.FromImage("Sample43/Background.png"));

                    earthNode.RunActions(new RepeatForever(new RotateBy(1f, 0, 5, 0)));
                }
            }
        }

        protected override void OnUpdate(float timeStep)
        {
            // rotate & move camera by mouse and WASD:

            const float mouseSensitivity = .1f;
            const float moveSpeed = 2;

            var mouseMove = Input.MouseMove;
            yaw += mouseSensitivity * mouseMove.X;
            pitch += mouseSensitivity * mouseMove.Y;
            pitch = MathHelper.Clamp(pitch, -90, 90);

            cameraNode.Rotation = new Quaternion(pitch, yaw, 0);

            if (Input.GetKeyDown(Key.W)) cameraNode.Translate(Vector3.UnitZ * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.S)) cameraNode.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.A)) cameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.D)) cameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);

            if (Input.GetKeyDown(Key.U)) cameraNode.Translate(Vector3.UnitY * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.J)) cameraNode.Translate(-Vector3.UnitY * moveSpeed * timeStep);
        }

        void SetupViewport()
        {
            Viewport viewport = new Viewport(scene, cameraNode.GetComponent<Camera>(), null);
            viewport.SetClearColor(Color.Black);
            Renderer.SetViewport(0, viewport);

            var rp = viewport.RenderPath.Clone();
            rp.Append(ResourceCache.GetXmlFile("PostProcess/BloomHDR.xml"));
            rp.Append(ResourceCache.GetXmlFile("PostProcess/FXAA2.xml"));
            //rp.Append(ResourceCache.GetXmlFile("PostProcess/GammaCorrection.xml"));

            viewport.RenderPath = rp;
        }


        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Up</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"U\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Down</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"J\" />" +
            "        </element>" +
            "    </add>" +
            "</patch>";
    }
}
