using UrhoNetSamples;
using Urho;
using System;
using Urho.Urho2D;
using Urho.Gui;

namespace Urho2DIsometricDemo
{
    public class Urho2DIsometricDemo : Sample
    {

        const float PIXEL_SIZE = 0.01f;

        const float MOVE_SPEED = 23.0f;
        const int LIFES = 3;

        /// The controllable character component.
        Character2D character2D_ = null;
        /// Flag for drawing debug geometry.
        bool drawDebug_ = false;

        /// Sample2D utility object.
        Sample2D sample2D_ = null;

        Node cameraNode_;


        PhysicsWorld2D physicsWorld2D = null;

        public Urho2DIsometricDemo() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();

            // This is an hack to hide the info 
            SimpleCreateInstructions();
            ToggleInfo();
            infoButton.Visible = false;

            sample2D_ = new Sample2D(this);
            sample2D_.demoFilename_ = "Isometric2D";

            // Create the scene content
            CreateScene();

            // Create the UI content
            sample2D_.CreateUIContent("PLATFORMER 2D DEMO", character2D_.remainingLifes_, character2D_.remainingCoins_);

            // Hook up to the frame update events
            SubscribeToEvents();
        }

        private void CreateScene()
        {
            scene = new Scene();
            sample2D_.scene_ = scene;

            // Create the Octree, DebugRenderer and PhysicsWorld2D components to the scene
            scene.CreateComponent<Octree>();
            scene.CreateComponent<DebugRenderer>();
            /*PhysicsWorld2D* physicsWorld =*/
            physicsWorld2D = scene.CreateComponent<PhysicsWorld2D>();
            physicsWorld2D.Gravity = new Vector2(0.0f, 0.0f); // Neutralize gravity as the character will always be grounded

            // Create camera
            cameraNode_ = scene.CreateChild("Camera");
            var camera = cameraNode_.CreateComponent<Camera>();
            camera.Orthographic = true;


            camera.OrthoSize = ((float)Graphics.Height * PIXEL_SIZE);
            camera.Zoom = (2.0f * Math.Min((float)Graphics.Width / 1280.0f, (float)Graphics.Height / 800.0f)); // Set zoom according to user's resolution to ensure full visibility (initial zoom (2.0) is set for full visibility at 1280x800 resolution)

            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));

            // Create tile map from tmx file
            var cache = ResourceCache;

            Node tileMapNode = scene.CreateChild("TileMap");
            var tileMap = tileMapNode.CreateComponent<TileMap2D>();
            tileMap.TmxFile = (cache.GetTmxFile2D("Urho2D/Tilesets/atrium.tmx"));
            TileMapInfo2D info = tileMap.Info;

            // Create Spriter Imp character (from sample 33_SpriterAnimation)
            Node spriteNode = sample2D_.CreateCharacter(info, 0.0f, new Vector3(-5.0f, 11.0f, 0.0f), 0.15f);
            character2D_ = spriteNode.CreateComponent<Character2D>(); // Create a logic component to handle character behavior
                                                                      // Scale character's speed on the Y axis according to tiles' aspect ratio
            character2D_.moveSpeedScale_ = info.TileHeight / info.TileWidth;
            character2D_.zoom_ = camera.Zoom;

            // Generate physics collision shapes from the tmx file's objects located in "Physics" (top) layer
            TileMapLayer2D tileMapLayer = tileMap.GetLayer(tileMap.NumLayers - 1);
            sample2D_.CreateCollisionShapesFromTMXObjects(tileMapNode, tileMapLayer, info);

            // Instantiate enemies and moving platforms at each placeholder of "MovingEntities" layer (placeholders are Poly Line objects defining a path from points)
            sample2D_.PopulateMovingEntities(tileMap.GetLayer(tileMap.NumLayers - 2));

            // Instantiate coins to pick at each placeholder of "Coins" layer (placeholders for coins are Rectangle objects)
            TileMapLayer2D coinsLayer = tileMap.GetLayer(tileMap.NumLayers - 3);
            sample2D_.PopulateCoins(coinsLayer);

            // Init coins counters
            character2D_.remainingCoins_ = coinsLayer.NumObjects;
            character2D_.maxCoins_ = coinsLayer.NumObjects;

        }

        protected override void Stop()
        {
            UnSubscribeFromEvents();
            base.Stop();
        }

        private void SubscribeToEvents()
        {
            Button playButton = (Button)UI.Root.GetChild("PlayButton", true);
            playButton.Released += HandlePlayButton;

            Engine.PostUpdate += HandlePostUpdate;
            Engine.PostRenderUpdate += HandlePostRenderUpdate;
            physicsWorld2D.PhysicsBeginContact2D += HandleCollisionBegin;

        }

        private void UnSubscribeFromEvents()
        {
            Button playButton = (Button)UI.Root.GetChild("PlayButton", true);
            playButton.Released -= HandlePlayButton;

            Engine.PostUpdate -= HandlePostUpdate;
            Engine.PostRenderUpdate -= HandlePostRenderUpdate;
            physicsWorld2D.PhysicsBeginContact2D -= HandleCollisionBegin;
        }


        private void HandleCollisionBegin(PhysicsBeginContact2DEventArgs obj)
        {
            var hitNode = obj.NodeA;
            if (hitNode.Name == "Imp")
                hitNode = obj.NodeB;
            String nodeName = hitNode.Name;
            Node character2DNode = scene.GetChild("Imp", true);

            // Handle coins picking
            if (nodeName == "Coin")
            {
                hitNode.Remove();
                character2D_.remainingCoins_ -= 1;
                if (character2D_.remainingCoins_ == 0)
                {
                    Text instructions = (Text)UI.Root.GetChild("Instructions", true);
                    instructions.Value = "!!! You have all the coins !!!";
                }
                Text coinsText = (Text)UI.Root.GetChild("CoinsText", true);
                coinsText.Value = character2D_.remainingCoins_.ToString(); // Update coins UI counter
                sample2D_.PlaySoundEffect("Powerup.wav");
            }

            // Handle interactions with enemies
            if (nodeName == "Enemy" || nodeName == "Orc")
            {
                var animatedSprite = character2DNode.GetComponent<AnimatedSprite2D>();
                float deltaX = character2DNode.Position.X - hitNode.Position.X;

                // Orc killed if character is fighting in its direction when the contact occurs (flowers are not destroyable)
                if (nodeName == "Orc" && animatedSprite.Animation == "attack" && (deltaX < 0 == animatedSprite.FlipX))
                {
                    (hitNode.GetComponent<Mover>()).emitTime_ = 1;
                    if (hitNode.GetChild("Emitter", true) == null)
                    {
                        hitNode.GetComponent<RigidBody2D>().Remove(); // Remove Orc's body
                        sample2D_.SpawnEffect(hitNode);
                        sample2D_.PlaySoundEffect("BigExplosion.wav");
                    }
                }
                // Player killed if not fighting in the direction of the Orc when the contact occurs, or when colliding with a flower
                else
                {
                    if (character2DNode.GetChild("Emitter", true) == null)
                    {
                        character2D_.wounded_ = true;
                        if (nodeName == "Orc")
                        {
                            var orc = hitNode.GetComponent<Mover>();
                            orc.fightTimer_ = 1;
                        }
                        sample2D_.SpawnEffect(character2DNode);
                        sample2D_.PlaySoundEffect("BigExplosion.wav");
                    }
                }
            }
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);


            // Zoom in/out
            if (cameraNode_ != null)
                sample2D_.Zoom(cameraNode_.GetComponent<Camera>());

            // Toggle debug geometry with 'Z' key
            if (Input.GetKeyPress(Key.Z))
                drawDebug_ = !drawDebug_;


        }

        private void HandlePostUpdate(PostUpdateEventArgs obj)
        {
            if (character2D_ == null)
                return;

            Node character2DNode = character2D_.Node;
            cameraNode_.Position = (new Vector3(character2DNode.Position.X, character2DNode.Position.Y, -10.0f)); // Camera tracks character
        }

        private void HandlePostRenderUpdate(PostRenderUpdateEventArgs obj)
        {
            if (drawDebug_)
            {
                var physicsWorld = scene.GetComponent<PhysicsWorld2D>();
                physicsWorld.DrawDebugGeometry();

                Node tileMapNode = scene.GetChild("TileMap", true);
                var map = tileMapNode.GetComponent<TileMap2D>();
                map.DrawDebugGeometry(scene.GetComponent<DebugRenderer>(), false);
            }
        }


        private void HandlePlayButton(ReleasedEventArgs obj)
        {
            // Hide Instructions and Play/Exit buttons
            Text instructionText = (Text)UI.Root.GetChild("Instructions", true);
            instructionText.Value = "";
            Button exitButton = (Button)UI.Root.GetChild("ExitButton", true);
            exitButton.Visible = false;
            Button playButton = (Button)UI.Root.GetChild("PlayButton", true);
            playButton.Visible = false;

            // Remove fullscreen UI and unfreeze the scene
            if (UI.Root.GetChild("FullUI", true) != null)
            {
                UI.Root.GetChild("FullUI", true).Remove();
            }
            else
            {
                UnSubscribeFromEvents();

                sample2D_ = new Sample2D(this);
                sample2D_.demoFilename_ = "Platformer2D";

                // TBD ELI , IS IT NEEDED ? , HAVE TO CHECK
                scene.Dispose();

                CreateScene();
                sample2D_.scene_ = scene;

                Text lifeText = (Text)UI.Root.GetChild("LifeText", true);
                lifeText.Value = new string(character2D_.remainingLifes_.ToString()); // Update lifes UI counter

                Text coinsText = (Text)UI.Root.GetChild("CoinsText", true);
                coinsText.Value = character2D_.remainingCoins_.ToString(); // Update coins UI counter

                SubscribeToEvents();
            }
            // Hide mouse cursor
            Input.SetMouseVisible(false);
        }






    }

}
