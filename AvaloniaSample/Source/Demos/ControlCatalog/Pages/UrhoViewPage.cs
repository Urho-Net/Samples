using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering;
using Urho;
using Urho.Avalonia;
using Urho.Gui;
using Urho.IO;
#pragma warning disable 4014

namespace ControlCatalog.Pages
{
    public class UrhoViewPage : UserControl
    {

        static readonly Random random = new Random();
        /// Return a random float between 0.0 (inclusive) and 1.0 (exclusive.)
        public static float NextRandom() { return (float)random.NextDouble(); }
        /// Return a random float between 0.0 and range, inclusive from both ends.
        public static float NextRandom(float range) { return (float)random.NextDouble() * range; }
        /// Return a random float between min and max, inclusive from both ends.
        public static float NextRandom(float min, float max) { return (float)((random.NextDouble() * (max - min)) + min); }
        /// Return a random integer between min and max - 1.
        public static int NextRandom(int min, int max) { return random.Next(min, max); }

        Urho.Gui.View3D urhoView3D = null;
        Camera camera;
        Scene scene;
        protected Node CameraNode { get; set; }

        private readonly Image _urhoPlaceHolder;

        private bool isDirty = false;

        public UrhoViewPage()
        {
            this.InitializeComponent();

            _urhoPlaceHolder = this.FindControl<Image>("UrhoPlaceHolder");
            _urhoPlaceHolder.Stretch = Stretch.Fill;
            _urhoPlaceHolder.LayoutUpdated += OnLayoutUpdated;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnUrhoUpdate(UpdateEventArgs obj)
        {
            UpdateUrho3D();
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            isDirty = true;
        }

        private void UpdateUrho3D()
        {
            if (isDirty == true && _urhoPlaceHolder.TransformedBounds != null)
            {
                isDirty = false;
                var urhoWindow = GetUrhoWindow();
                var transBounds = _urhoPlaceHolder.TransformedBounds.Value;
        
                AvaloniaUrhoContext.EnsureInvokeOnMainThread(() =>
                   {
                       urhoView3D.Position = new IntVector2((int)(transBounds.Clip.Left * urhoWindow.RenderScaling), (int)(transBounds.Clip.Top * urhoWindow.RenderScaling));
                       urhoView3D.Width = (int)(transBounds.Clip.Width * urhoWindow.RenderScaling);
                       urhoView3D.Height = (int)(transBounds.Clip.Height * urhoWindow.RenderScaling);
                   });
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (urhoView3D == null)
            {
                var urhoWindow = GetUrhoWindow();
                if (urhoWindow != null)
                {
                    urhoView3D = urhoWindow.UrhoUIElement.CreateView3D();
                    CreateScene();
                    SetupViewport();
                }
            }

            if (urhoView3D != null)
            {
                urhoView3D.Visible = true;
                urhoView3D.Enabled = true;
            }

            Urho.Application.Current.Update += OnUrhoUpdate;

            isDirty = true;

        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Urho.Application.Current.Update -= OnUrhoUpdate;

            if (urhoView3D != null)
            {
                urhoView3D.Visible = false;
                urhoView3D.Enabled = false;
            }
        }


        Avalonia.Controls.Window GetWindow() => (Avalonia.Controls.Window)this.VisualRoot;
        UrhoWindowImpl GetUrhoWindow()
        {
            if (((Avalonia.Controls.Window)this.VisualRoot).PlatformImpl != null)
                return ((Avalonia.Controls.Window)this.VisualRoot).PlatformImpl as UrhoWindowImpl;
            else
                return null;
        }

        void CreateScene()
        {
            var cache = Urho.Application.Current.ResourceCache;
            scene = new Scene();

            // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
            // (-1000, -1000, -1000) to (1000, 1000, 1000)
            scene.CreateComponent<Octree>();
            scene.CreateComponent<DebugRenderer>();

            // Create scene node & StaticModel component for showing a static plane
            var planeNode = scene.CreateChild("Plane");
            planeNode.Scale = new Vector3(50, 1, 50);
            var planeObject = planeNode.CreateComponent<StaticModel>();
            planeObject.Model = cache.GetModel("Models/Plane.mdl");
            planeObject.SetMaterial(cache.GetMaterial("Materials/StoneTiled.xml"));

            // Create a Zone component for ambient lighting & fog control
            var zoneNode = scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();

            // Set same volume as the Octree, set a close bluish fog and some ambient light
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Urho.Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Urho.Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100;
            zone.FogEnd = 300;

            // Create a directional light to the world. Enable cascaded shadows on it
            var lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);

            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            // Create animated models
            const int numModels = 30;
            const float modelMoveSpeed = 2.0f;
            const float modelRotateSpeed = 100.0f;
            var bounds = new BoundingBox(new Vector3(-20.0f, 0.0f, -20.0f), new Vector3(20.0f, 0.0f, 20.0f));

            for (var i = 0; i < numModels; ++i)
            {
                var modelNode = scene.CreateChild("Jack");
                modelNode.Position = new Vector3(NextRandom(-20, 20), 0.0f, NextRandom(-20, 20));
                modelNode.Rotation = new Quaternion(0, NextRandom(0, 360), 0);
                //var modelObject = modelNode.CreateComponent<AnimatedModel>();
                var modelObject = new AnimatedModel();
                modelNode.AddComponent(modelObject);
                modelObject.Model = cache.GetModel("Models/Kachujin/Kachujin.mdl");
                modelObject.Material = cache.GetMaterial("Models/Kachujin/Materials/Kachujin.xml");
                modelObject.CastShadows = true;

                // Create an AnimationState for a walk animation. Its time position will need to be manually updated to advance the
                // animation, The alternative would be to use an AnimationController component which updates the animation automatically,
                // but we need to update the model's position manually in any case
                var walkAnimation = cache.GetAnimation("Models/Kachujin/Kachujin_Walk.ani");
                var state = modelObject.AddAnimationState(walkAnimation);
                // The state would fail to create (return null) if the animation was not found
                if (state != null)
                {
                    // Enable full blending weight and looping
                    state.Weight = 1;
                    state.Looped = true;
                }

                // Create our custom Mover component that will move & animate the model during each frame's update
                var mover = new Mover(modelMoveSpeed, modelRotateSpeed, bounds);
                modelNode.AddComponent(mover);
            }

            // Create the camera. Limit far clip distance to match the fog
            CameraNode = scene.CreateChild("Camera");
            camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300;

            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = new Vector3(0.0f, 5.0f, 0.0f);
        }
        void SetupViewport()
        {
            urhoView3D.SetView(scene, camera);
        }

        class Mover : Component
        {
            float MoveSpeed { get; }
            float RotationSpeed { get; }
            BoundingBox Bounds { get; }

            public Mover(float moveSpeed, float rotateSpeed, BoundingBox bounds)
            {
                MoveSpeed = moveSpeed;
                RotationSpeed = rotateSpeed;
                Bounds = bounds;
                ReceiveSceneUpdates = true;
            }

            protected override void OnUpdate(float timeStep)
            {
                // This moves the character position
                Node.Translate(Vector3.UnitZ * MoveSpeed * timeStep, TransformSpace.Local);

                // If in risk of going outside the plane, rotate the model right
                var pos = Node.Position;
                if (pos.X < Bounds.Min.X || pos.X > Bounds.Max.X || pos.Z < Bounds.Min.Z || pos.Z > Bounds.Max.Z)
                    Node.Yaw(RotationSpeed * timeStep, TransformSpace.Local);

                // Get the model's first (only) animation
                // state and advance its time. Note the
                // convenience accessor to other components in
                // the same scene node

                var model = GetComponent<AnimatedModel>();
                if (model.NumAnimationStates > 0)
                {
                    var state = model.AnimationStates.First();
                    state.AddTime(timeStep);
                }
            }
        }

    }
}
