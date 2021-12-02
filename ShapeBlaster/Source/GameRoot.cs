//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using Urho;

namespace ShapeBlaster
{


    public class GameRoot : Sample
    {
        [Preserve]
        public GameRoot() : base(new ApplicationOptions(assetsFolder: "Data;CoreData") {Orientation = ApplicationOptions.OrientationType.Landscape }) { }


        public static Timer MultiplierTimer = null;
        protected override void Start()
        {
            base.Start();

            Log.LogLevel = LogLevel.Info;

            PlayerPrefs.Init(this);

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

            Viewport = new Viewport(Scene, camera, null);
            Renderer.SetViewport(0, Viewport);

          

            var renderpath = Viewport.RenderPath.Clone();
            renderpath.Append(cache.GetXmlFile("PostProcess/CustomRender.xml"));
            Viewport.RenderPath = renderpath;

            CustomRenderer.Initialize();

            ParticleManager = new ParticleManager<ParticleState>(1024 * 20, ParticleState.UpdateParticle);

            int maxGridPoints = 1600;
            if (IsMobile && Platform != Platforms.Web)
            {
                maxGridPoints = 400;
            }
            else if (Platform == Platforms.Web)
            {
                maxGridPoints = 100;
            }

            float amt = (float)Math.Sqrt(ScreenBounds.Width() * ScreenBounds.Height() / (float)maxGridPoints);
            Vector2 gridSpacing = new Vector2(amt, amt);

            IntRect expandedBounds = ScreenBounds;
            MathUtil.Inflate(ref expandedBounds,(int)gridSpacing.X, (int)gridSpacing.Y);
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

            Input.SetMouseVisible(true);

            

            SoundManager.Init();

            SoundManager.PlayMusic();

            if (IsMobile)
            {
                CreateScreenJoystick();
            }

            CreatePlayerStatusUI();

            MultiplierTimer = new Timer();
            MultiplierTimer.Reset();

        }


        float accDeltaTime = 0.0f;


        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);


            ElapsedTime += deltaTime;
            accDeltaTime += deltaTime;

            if (!IsMobile && Platform != Platforms.Web)
            {
                if (accDeltaTime < 1.0f / 60.0f)
                    return;
            }

            ShipInput.Update();

            if (!paused)
            {
                PlayerStatus.Update();
                EntityManager.Update();
                EnemySpawner.Update();
                ParticleManager.Update();
                Grid.Update(accDeltaTime);
            }

            accDeltaTime = 0.0f;

            playerStatusText.Value = "Lives: " + PlayerStatus.Lives + "\n";
            playerStatusText.Value += "Score: " + PlayerStatus.Score + "\n";
            playerStatusText.Value += "Multiplier: " + PlayerStatus.Multiplier;

            if (PlayerStatus.IsGameOver)
            {
                string text = "Game Over\n" +
                    "Your Score: " + PlayerStatus.Score + "\n" +
                    "High Score: " + ((PlayerStatus.HighScore > PlayerStatus.Score) ? PlayerStatus.HighScore : PlayerStatus.Score);
                playerStatusText.Value = text;
            }

        }

        void Draw()
        {
            EntityManager.Draw();
            Grid.Draw();
            ParticleManager.Draw();

        }

        // GodMode by default as the game is really hard :)
        public static bool GodMode = false;

        public static Scene Scene { get; private set; }

        public static float ElapsedTime { get; private set; }

        public static Vector2 ScreenSize { get; private set; }
        public static IntRect ScreenBounds { get; private set; }

        public static Grid Grid { get; private set; }
        public static ParticleManager<ParticleState> ParticleManager { get; private set; }

        bool paused = false;

        public static Viewport Viewport { get; private set; }


    }

}