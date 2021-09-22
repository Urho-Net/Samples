using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Urho;
using Urho.Avalonia;
using Urho.Urho2D;
using Urho.Gui;
using Urho.IO;
using ControlCatalog.ViewModels;

#pragma warning disable 4014

namespace ControlCatalog.Pages
{
    public class UrhoTexturePage : UserControl
    {

        Camera camera;
        Scene scene;
        protected Node CameraNode { get; set; }
        protected const float TouchSensitivity = 2;
        protected float Yaw { get; set; }
        protected float Pitch { get; set; }
        static readonly Random random = new Random();
        /// Return a random float between 0.0 (inclusive) and 1.0 (exclusive.)
        public static float NextRandom() { return (float)random.NextDouble(); }
        /// Return a random float between 0.0 and range, inclusive from both ends.
        public static float NextRandom(float range) { return (float)random.NextDouble() * range; }
        /// Return a random float between min and max, inclusive from both ends.
        public static float NextRandom(float min, float max) { return (float)((random.NextDouble() * (max - min)) + min); }
        /// Return a random integer between min and max - 1.
        public static int NextRandom(int min, int max) { return random.Next(min, max); }

        private readonly Image _urhoPlaceHolder = null;

        private WriteableBitmap _transparentBitmap;

        UIElement renderPlaceHolder = null;
        BorderImage textureContainer = null;
        Texture2D renderTexture = null;

        Task warmupRunner = null;
        bool isRunnerRunning = false;

        Urho.Timer fpsTimer = null;
        float fpsCounter = 0;

        Urho.Gui.Text fpsText = null;

        private ContextMenuPageViewModel contextMenu = null;

        public UrhoTexturePage()
        {
            this.InitializeComponent();
            _urhoPlaceHolder = this.FindControl<Image>("UrhoPlaceHolder");
            _urhoPlaceHolder.Focusable = true;
            this.Focusable = true;
            _urhoPlaceHolder.Stretch = Stretch.Fill;

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
             fpsTimer = new Urho.Timer();
             fpsCounter = 0;
            CreateScene();
           
        }

        void CreateRenderTexture(int width, int height)
        {

            if (renderTexture != null && renderTexture.Width == width && renderTexture.Height == height)
            {
                return;
            }

            if (renderTexture != null)
            {
                renderTexture.Dispose();
                renderTexture = null;
            }

            if (textureContainer != null)
            {
                textureContainer.Remove();
                textureContainer = null;
            }

            if (renderPlaceHolder != null)
            {
                renderPlaceHolder.Remove();
                renderPlaceHolder = null;
            }


            renderTexture = new Texture2D();
            renderTexture.SetSize(width, height, Graphics.RGBAFormat, TextureUsage.Rendertarget);

            textureContainer = Urho.Application.Current.UI.Root.CreateBorderImage();
            textureContainer.Width = width;
            textureContainer.Height = height;
            textureContainer.Texture = renderTexture;

            var urhoWindow = GetUrhoWindow();
            renderPlaceHolder = urhoWindow.UrhoUIElement.CreateChild<UIElement>();
            renderPlaceHolder.Width = width;
            renderPlaceHolder.Height = height;

            // decrease the priorty below the windows priority (100)
            textureContainer.Priority = 100;
     
            fpsText = textureContainer.CreateText("fps");
            fpsText.SetFont(Urho.Application.Current.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 25);
            fpsText.Visible = true;
        }


        public override void Render(DrawingContext context)
        {

            if (_transparentBitmap != null)
            {
                context.DrawImage(_transparentBitmap, new Avalonia.Rect(0, 0, _transparentBitmap.PixelSize.Width, _transparentBitmap.PixelSize.Height),
                                new Avalonia.Rect(Bounds.Size));
            }

            base.Render(context);
        }

        private void OnUrhoUpdate(UpdateEventArgs evt)
        {
            UpdateUrho3D(evt.TimeStep);

            fpsCounter++;
            if(fpsTimer.GetMSec(false) >= 1000)
            {
                fpsTimer.Reset();
                if (fpsText != null)
                    fpsText.Value = " FPS:" + fpsCounter.ToString();
                fpsCounter = 0;
            }
        }

        private void UpdateUrho3D(float timeStep)
        {

            var urhoWindow = GetUrhoWindow();
            var Input = Urho.Application.Current.Input;

         

            if ( _urhoPlaceHolder.TransformedBounds != null)
            {
                
                var renderScaling = urhoWindow.RenderScaling;
                var transformedBounds = _urhoPlaceHolder.TransformedBounds.Value;
                int transformedBoundsWidth = (int)(transformedBounds.Clip.Width * renderScaling);
                int transformedBoundsHeight = (int)(transformedBounds.Clip.Height * renderScaling);

                if (renderTexture == null)
                {
                    int width = Urho.Application.Current.Graphics.Width;
                    int height = Urho.Application.Current.Graphics.Height;

                    CreateRenderTexture(width, height);
                    SetViewPort();
                    _transparentBitmap?.Dispose();
                    _transparentBitmap = new WriteableBitmap(new PixelSize(100, 100), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Opaque);
                }

                if (renderPlaceHolder.Width != transformedBoundsWidth || renderPlaceHolder.Height != transformedBoundsHeight)
                {
                    renderPlaceHolder.Position = new IntVector2((int)(transformedBounds.Clip.Left * renderScaling), (int)(transformedBounds.Clip.Top * renderScaling));
                    renderPlaceHolder.Width = transformedBoundsWidth;
                    renderPlaceHolder.Height = transformedBoundsHeight;
                    textureContainer.Width = renderPlaceHolder.Width;
                    textureContainer.Height = renderPlaceHolder.Height;
                }
            }

            if (textureContainer != null)
            {
                if (urhoWindow.UrhoUIElement.HasFocus())
                {
                    textureContainer.Priority = 101;
                    urhoWindow.UrhoUIElement.Priority = 102;
                }
                else
                {
                    textureContainer.Priority = 100;
                    urhoWindow.UrhoUIElement.Priority = 100;
                }
            }
            
            if (textureContainer != null)
            {
                textureContainer.Position = renderPlaceHolder.ScreenPosition;
            }

            var renderPlaceHolderScreenPosition = renderPlaceHolder.ScreenPosition;
            var mousePosition = Input.MousePosition;

            if (urhoWindow.UrhoUIElement.HasFocus() &&
                mousePosition.X > renderPlaceHolderScreenPosition.X && mousePosition.X < renderPlaceHolderScreenPosition.X + renderPlaceHolder.Width &&
                mousePosition.Y > renderPlaceHolderScreenPosition.Y && mousePosition.Y <  renderPlaceHolderScreenPosition.Y + renderPlaceHolder.Height )
            {
                SimpleMoveCamera3D(timeStep);
            }

            
        }

        private unsafe void WarmupRunner()
        {
            int runningCounter = 0;
            while (isRunnerRunning)
            {
                AvaloniaUrhoContext.EnsureInvokeOnMainThread(() =>
                {
                     this.InvalidateVisual();
                });

                Thread.Sleep(50);

                runningCounter++;

                if(runningCounter > 20)isRunnerRunning = false;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Urho.Application.Current.Update += OnUrhoUpdate;
            isRunnerRunning = true;
            warmupRunner = System.Threading.Tasks.Task.Run(WarmupRunner);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Urho.Application.Current.Update -= OnUrhoUpdate;


            isRunnerRunning = false;
            warmupRunner.Wait();

            if (renderTexture != null)
            {
                renderTexture.Dispose();
                renderTexture = null;
            }

            if (textureContainer != null)
            {
                textureContainer.Remove();
                textureContainer = null;
            }

            if (_transparentBitmap != null)
            {
                _transparentBitmap?.Dispose();
                _transparentBitmap = null;
            }

            fpsText = null;

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
        void SetViewPort()
        {
            RenderSurface surface = renderTexture.RenderSurface;
            Viewport viewport = new Viewport(scene, camera, null);
            surface.SetViewport(0, viewport);
            surface.UpdateMode = RenderSurfaceUpdateMode.Updatealways;
        }

        protected void SimpleMoveCamera3D(float timeStep, float moveSpeed = 10.0f)
        {
            const float mouseSensitivity = .1f;

            var Input = Urho.Application.Current.Input;

            if (Input.GetMouseButtonDown(MouseButton.Left))
            {
                var mouseMove = Input.MouseMove;
                Yaw += mouseSensitivity * mouseMove.X;
                Pitch += mouseSensitivity * mouseMove.Y;
                Pitch = MathHelper.Clamp(Pitch, -90, 90);

                CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
            }

            if (Input.GetKeyDown(Key.W)) CameraNode.Translate(Vector3.UnitZ * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.S)) CameraNode.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.A)) CameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
            if (Input.GetKeyDown(Key.D)) CameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);
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



        protected override void OnDataContextChanged(EventArgs e)
        {

            if (contextMenu == null)
            {
                contextMenu = new ContextMenuPageViewModel();
                DataContext = contextMenu;
                contextMenu.View = this;
            }

            base.OnDataContextChanged(e);

        }
        Avalonia.Controls.Window GetWindow() => (Avalonia.Controls.Window)this.VisualRoot;
        UrhoWindowImpl GetUrhoWindow()
        {
            if (((Avalonia.Controls.Window)this.VisualRoot).PlatformImpl != null)
                return ((Avalonia.Controls.Window)this.VisualRoot).PlatformImpl as UrhoWindowImpl;
            else
                return null;
        }

    }
}
