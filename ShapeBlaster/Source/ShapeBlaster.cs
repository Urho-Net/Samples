//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using Urho;

namespace ShapeBlaster
{


    public class ShapeBlaster : Sample
    {
        [Preserve]
        public ShapeBlaster() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }


        protected override void Start()
        {
            base.Start();

            Art.Load();

            var graphics = Graphics;
            float width = graphics.Width;
            float height = graphics.Height;

            ScreenSize = new Vector2(width, height);
            ScreenBounds = new IntRect(0, 0, (int)ScreenSize.X, (int)ScreenSize.Y);

            var renderer = Renderer;

            renderer.HDRRendering = true;

            var cache = ResourceCache;

            Scene = new Scene();
            Scene.CreateComponent<Octree>();

            var camera = Scene.CreateChild("Camera").CreateComponent<Camera>();
            camera.Node.Position = new Vector3(width / 2.0f, height / 2.0f, 0.0f);
            camera.Orthographic = true;
            camera.OrthoSize = height;

            var viewport = new Viewport(Scene, camera, null);
            Renderer.SetViewport(0, viewport);


            var renderpath = viewport.RenderPath.Clone();
            renderpath.Append(cache.GetXmlFile("PostProcess/CustomBloomHDR.xml"));
            renderpath.Append(cache.GetXmlFile("PostProcess/BlurLight.xml"));
            viewport.RenderPath = renderpath;

            CustomRenderer.Initialize();

            ParticleManager = new ParticleManager<ParticleState>(1024 * 20, ParticleState.UpdateParticle);

#if _MOBILE_
            const int maxGridPoints = 400
#else
            const int maxGridPoints = 1600;
#endif

            float amt = (float)Math.Sqrt(ScreenBounds.Width() * ScreenBounds.Height() / maxGridPoints);
            Vector2 gridSpacing = new Vector2(amt, amt);

            IntRect expandedBounds = ScreenBounds;
            expandedBounds.Inflate((int)gridSpacing.X, (int)gridSpacing.Y);
            Grid = new Grid(expandedBounds, gridSpacing);

            EntityManager.Add(PlayerShip.Instance);

            Engine.SubscribeToEvent(new StringHash("RenderPathEvent"), (arg) =>
      {
          string name = arg.EventData["Name"].variant;

          if (name != "customrender") return;
          CustomRenderer.Begin();

          Draw();

          CustomRenderer.End();

      });


        }


        float deltaTime = 0.0f;


        protected override void OnUpdate(float time)
        {
            base.OnUpdate(time);


            ElapsedTime += time;

#if !ATOMIC_IOS
            deltaTime += time;

            if (deltaTime < 1.0f / 60.0f)
                return;

            deltaTime = 0.0f;

#endif

            ShipInput.Update();

            if (!paused)
            {
                PlayerStatus.Update();
                EntityManager.Update();
                EnemySpawner.Update();
                ParticleManager.Update();
                Grid.Update();
            }

        }

        void Draw()
        {
            EntityManager.Draw();
            Grid.Draw();
            ParticleManager.Draw();

        }

        // GodMode by default as the game is really hard :)
        public static bool GodMode = true;

        public static Scene Scene { get; private set; }

        public static float ElapsedTime { get; private set; }

        public static Vector2 ScreenSize { get; private set; }
        public static IntRect ScreenBounds { get; private set; }

        public static Grid Grid { get; private set; }
        public static ParticleManager<ParticleState> ParticleManager { get; private set; }

        bool paused = false;


    }

}