using Urho;
using System;
using Urho.Gui;
using Urho.Resources;

namespace NanoVGSample
{
    public partial class NanoVGSample : Sample
    {
        Camera camera;
        Scene scene;

        float time_ = 0.0f;

        class DemoData
        {
            public int fontNormal, fontBold, fontIcons, fontEmoji;
            public int[] images = new int[12];
            public int svgImage;
        };

        DemoData demoData_ = new DemoData();


        [Preserve]
        public NanoVGSample() : base(new ApplicationOptions(assetsFolder: "Data;CoreData") { Width = 1600, Height = 1200 }) { }

        protected override void Start()
        {
            base.Start();
            InitUI();
            InitControls();
            loadDemoData();
            CreateScene();
            // SimpleCreateInstructionsWithWasd();
            SetupViewport();
        }

        void InitUI()
        {
            // Load the style sheet from xml
            UI.Root.SetDefaultStyle(ResourceCache.GetXmlFile("UI/DefaultStyle.xml"));
        }

        Window InitWindow()
        {
            Window window = new Window();

            UI.Root.AddChild(window);
            window.MinWidth = (int)(Graphics.Width / 1.5);
            window.MinHeight = (int)(Graphics.Height / 1.5);
            window.SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Top);
            window.SetLayout(LayoutMode.Vertical, 6, new IntRect(6, 6, 6, 6));
            window.Name = "NanoVG Window";
            window.Movable = true;
            window.Resizable = true;
            window.FocusMode = FocusMode.Focusable;


            // Create Window 'titlebar' container
            var titleBar = new UIElement();
            titleBar.SetMinSize(0, 24);
            titleBar.MaxHeight = 24;
            titleBar.VerticalAlignment = VerticalAlignment.Top;
            titleBar.LayoutMode = LayoutMode.Horizontal;
            titleBar.FocusMode = FocusMode.Focusable;

            // Create the Window title Text
            var windowTitle = new Text();
            windowTitle.Name = "WindowTitle";
            windowTitle.Value = "Hello NanoVG!";

            // Create the Window's close button
            var buttonClose = new Button();
            buttonClose.Name = "CloseButton";

            // Add the controls to the title bar
            titleBar.AddChild(windowTitle);
            titleBar.AddChild(buttonClose);

            // Add the title bar to the Window
            window.AddChild(titleBar);

            // Apply styles
            window.SetStyleAuto();
            windowTitle.SetStyleAuto();
            buttonClose.SetStyle("CloseButton");

            return window;
        }

        void InitControls()
        {
            var window = InitWindow();
            var vgCanvas = window.CreateChild<VGCanvas>("VGCanvas");
            vgCanvas.ClearColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            vgCanvas.OnVGElementRender += OnVGElementRender;
            vgCanvas.FocusMode = FocusMode.Focusable;

            XmlFile style = ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
            Cursor cursor = new Cursor();
            cursor.SetStyleAuto(style);
            int cursorSize = Graphics.Width / 60;
            cursor.Size = new IntVector2(cursorSize, cursorSize);
            UI.Cursor = cursor;
            UI.Cursor.Visible = true;


        }

        private void OnVGElementRender(OnVGElementRenderEventArgs obj)
        {
            VGElement vglement = obj.VGElement;
            IntVector2 size = vglement.Size;
            IntVector2 mouseElementPosition = vglement.ScreenToElement(UI.Cursor.Position);
            renderVGElement(vglement, mouseElementPosition.X, mouseElementPosition.Y, size.X, size.Y, time_, 0, demoData_);
        }


        private void OnVGFrameBufferRender(OnVGFrameBufferRenderEventArgs obj)
        {
            VGFrameBuffer vgFrameBuffer = obj.VGFrameBuffer;
            IntVector2 size = vgFrameBuffer.Size;
            renderVGFrameBuffer(vgFrameBuffer, 0, 0, size.X, size.Y, time_, 0, demoData_);
        }

        void CreateScene()
        {
            scene = new Scene();

            // Create the Octree component to the scene. This is required before adding any drawable components, or else nothing will
            // show up. The default octree volume will be from (-1000, -1000, -1000) to (1000, 1000, 1000) in world coordinates; it
            // is also legal to place objects outside the volume but their visibility can then not be checked in a hierarchically
            // optimizing manner
            scene.CreateComponent<Octree>();


            // Create a child scene node (at world origin) and a StaticModel component into it. Set the StaticModel to show a simple
            // plane mesh with a "stone" material. Note that naming the scene nodes is optional. Scale the scene node larger
            // (100 x 100 world units)
            var planeNode = scene.CreateChild("Plane");
            planeNode.Scale = new Vector3(200, 1, 200);
            var planeObject = planeNode.CreateComponent<StaticModel>();
            planeObject.Model = ResourceCache.GetModel("Models/Plane.mdl");
            planeObject.SetMaterial(ResourceCache.GetMaterial("Materials/StoneTiled.xml"));


            // Create a directional light to the world so that we can see something. The light scene node's orientation controls the
            // light direction; we will use the SetDirection() function which calculates the orientation from a forward direction vector.
            // The light will use default settings (white light, no shadows)
            var lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f)); // The direction vector does not need to be normalized
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;


            // Create skybox. The Skybox component is used like StaticModel, but it will be always located at the camera, giving the
            // illusion of the box planes being far away. Use just the ordinary Box model and a suitable material, whose shader will
            // generate the necessary 3D texture coordinates for cube mapping
            var skyNode = scene.CreateChild("Sky");
            skyNode.SetScale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>();
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));


            // Create more StaticModel objects to the scene, randomly positioned, rotated and scaled. For rotation, we construct a
            // quaternion from Euler angles where the Y angle (rotation about the Y axis) is randomized. The mushroom model contains
            // LOD levels, so the StaticModel component will automatically select the LOD level according to the view distance (you'll
            // see the model get simpler as it moves further away). Finally, rendering a large number of the same object with the
            // same material allows instancing to be used, if the GPU supports it. This reduces the amount of CPU work in rendering the
            // scene.
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
            }

            // Create a "screen" like object for viewing the second scene. Construct it from two StaticModels, a box for the
            // frame and a plane for the actual view
            {
                Node boxNode = scene.CreateChild("ScreenBox");
                boxNode.Position = new Vector3(0.0f, 10.0f, 0.0f);
                boxNode.Scale = new Vector3(21.0f, 16.0f, 0.5f);
                StaticModel boxObject = boxNode.CreateComponent<StaticModel>();
                boxObject.Model = ResourceCache.GetModel("Models/Box.mdl");
                boxObject.Material = ResourceCache.GetMaterial("Materials/Stone.xml");

                Node screenNode = scene.CreateChild("Screen");
                screenNode.Position = new Vector3(0.0f, 10.0f, -0.27f);
                screenNode.Rotation = new Quaternion(-90.0f, 0.0f, 0.0f);
                screenNode.Scale = new Vector3(20.0f, 0.0f, 15.0f);
                StaticModel screenObject = screenNode.CreateComponent<StaticModel>();
                screenObject.Model = ResourceCache.GetModel("Models/Plane.mdl");

                VGFrameBuffer vgFrameBuffer = scene.CreateComponent<VGFrameBuffer>();
                vgFrameBuffer.OnVGFrameBufferRender += OnVGFrameBufferRender;
                vgFrameBuffer.CreateFrameBuffer(1024, 1024);
                vgFrameBuffer.ClearColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
                vgFrameBuffer.EnableRenderEvents();
                // Create a new material from scratch, use the diffuse unlit technique, assign the render texture
                // as its diffuse texture, then assign the material to the screen plane object
                var renderMaterial = new Material();
                renderMaterial.SetTechnique(0, ResourceCache.GetTechnique("Techniques/DiffUnlit.xml"));
                renderMaterial.SetTexture(TextureUnit.Diffuse, vgFrameBuffer.RenderTarget);
                // Since the screen material is on top of the box model and may Z-fight, use negative depth bias
                // to push it forward (particularly necessary on mobiles with possibly less Z resolution)
                renderMaterial.DepthBias = new BiasParameters(-0.001f, 0.0f);
                screenObject.SetMaterial(renderMaterial);
            }

            // Create a scene node for the camera, which we will move around
            // The camera will use default settings (1000 far clip distance, 45 degrees FOV, set aspect ratio automatically)
            CameraNode = scene.CreateChild("camera");
            camera = CameraNode.CreateComponent<Camera>();

            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = new Vector3(0.0f, 7.0f, -70.0f);
            Yaw = -20;
            //CameraNode.Rotate(new Quaternion(-60,new Vector3(0,1,0)));
        }


        void SetupViewport()
        {
            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen. We need to define the scene and the camera
            // at minimum. Additionally we could configure the viewport screen size and the rendering path (eg. forward / deferred) to
            // use, but now we just use full screen and default render path configured in the engine command line options
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
            time_ += timeStep;
            if (UI.FocusElement == null)
                SimpleMoveCamera3D(timeStep);
        }
    }
}