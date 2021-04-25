// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2021 the Urho3D project.
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
using System;
using Urho.Urho2D;
using Urho.Gui;

namespace Urho2DPlatformer
{
    public class Urho2DPlatformer : Sample
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

        [Preserve]
        public Urho2DPlatformer() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();

            // This is an hack to hide the info 
            SimpleCreateInstructions();
            ToggleInfo();
            infoButton.Visible = false;

            sample2D_ = new Sample2D(this);
            sample2D_.demoFilename_ = "Platformer2D";

            Input.SetMouseVisible(true);
            
            // Create the scene content
            CreateScene();

            // Create the UI content
            sample2D_.CreateUIContent("PLATFORMER 2D DEMO", character2D_.remainingLifes_, character2D_.remainingCoins_);

            // Hook up to the frame update events
            SubscribeToEvents();
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
            physicsWorld2D.PhysicsEndContact2D += HandleCollisionEnd;

        }

        private void UnSubscribeFromEvents()
        {
            Button playButton = (Button)UI.Root.GetChild("PlayButton", true);
            playButton.Released -= HandlePlayButton;

            Engine.PostUpdate -= HandlePostUpdate;
            Engine.PostRenderUpdate -= HandlePostRenderUpdate;
            physicsWorld2D.PhysicsBeginContact2D -= HandleCollisionBegin;
            physicsWorld2D.PhysicsEndContact2D -= HandleCollisionEnd;
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

            // Create camera
            cameraNode_ = scene.CreateChild("Camera");
            var camera = cameraNode_.CreateComponent<Camera>();
            camera.Orthographic = true;


            camera.OrthoSize = ((float)Graphics.Height * PIXEL_SIZE);
            camera.Zoom = (2.0f * Math.Min((float)Graphics.Width / 1280.0f, (float)Graphics.Height / 800.0f)); // Set zoom according to user's resolution to ensure full visibility (initial zoom (2.0) is set for full visibility at 1280x800 resolution)

            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));


            // Set background color for the scene
            Zone zone = Renderer.DefaultZone;
            zone.FogColor = new Color(0.2f, 0.2f, 0.2f);

            // Create tile map from tmx file
            var cache = ResourceCache;

            Node tileMapNode = scene.CreateChild("TileMap");
            var tileMap = tileMapNode.CreateComponent<TileMap2D>();
            tileMap.TmxFile = (cache.GetTmxFile2D("Urho2D/Tilesets/Ortho.tmx"));
            TileMapInfo2D info = tileMap.Info;

            // Create Spriter Imp character (from sample 33_SpriterAnimation)
            Node spriteNode = sample2D_.CreateCharacter(info, 0.8f, new Vector3(1.0f, 8.0f, 0.0f), 0.2f);
            character2D_ = spriteNode.CreateComponent<Character2D>(); // Create a logic component to handle character behavior

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

            //Instantiate triggers (for ropes, ladders, lava, slopes...) at each placeholder of "Triggers" layer (placeholders for triggers are Rectangle objects)
            sample2D_.PopulateTriggers(tileMap.GetLayer(tileMap.NumLayers - 4));

            // Create background
            sample2D_.CreateBackgroundSprite(info, 3.5f, "Textures/HeightMap.png", true);

        }

        private void HandleCollisionBegin(PhysicsBeginContact2DEventArgs obj)
        {
            var hitNode = obj.NodeA;
            if (hitNode.Name == "Imp")
                hitNode = obj.NodeB;
            String nodeName = hitNode.Name;
            Node character2DNode = scene.GetChild("Imp", true);

            // Handle ropes and ladders climbing
            if (nodeName == "Climb")
            {
                if (character2D_.isClimbing_) // If transition between rope and top of rope (as we are using split triggers)
                    character2D_.climb2_ = true;
                else
                {
                    character2D_.isClimbing_ = true;
                    var body = character2DNode.GetComponent<RigidBody2D>();
                    body.GravityScale = 0.0f; // Override gravity so that the character doesn't fall
                                              // Clear forces so that the character stops (should be performed by setting linear velocity to zero, but currently doesn't work)
                    body.SetLinearVelocity(new Vector2(0.0f, 0.0f));
                    body.Awake = false;
                    body.Awake = true;
                }
            }

            if (nodeName == "CanJump")
                character2D_.aboveClimbable_ = true;

            // Handle coins picking
            if (nodeName == "Coin")
            {
                hitNode.Remove();
                character2D_.remainingCoins_ -= 1;
                if (character2D_.remainingCoins_ == 0)
                {
                    Text instructions = (Text)UI.Root.GetChild("Instructions", true);
                    instructions.Value = "!!! Go to the Exit !!!";
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

            // Handle exiting the level when all coins have been gathered
            if (nodeName == "Exit" && character2D_.remainingCoins_ == 0)
            {
                // Update UI
                Text instructions = (Text)UI.Root.GetChild("Instructions", true);
                instructions.Value = ("!!! WELL DONE !!!");
                instructions.Position = new IntVector2(0, 0);
                // Put the character outside of the scene and magnify him
                character2DNode.Position = new Vector3(-20.0f, 0.0f, 0.0f);
                character2DNode.SetScale(1.5f);
            }

            // Handle falling into lava
            if (nodeName == "Lava")
            {
                var body = character2DNode.GetComponent<RigidBody2D>();
                body.ApplyForceToCenter(new Vector2(0.0f, 1000.0f), true);
                if (character2DNode.GetChild("Emitter", true) == null)
                {
                    character2D_.wounded_ = true;
                    sample2D_.SpawnEffect(character2DNode);
                    sample2D_.PlaySoundEffect("BigExplosion.wav");
                }
            }

            // Handle climbing a slope
            if (nodeName == "Slope")
                character2D_.onSlope_ = true;
        }




        private void HandleCollisionEnd(PhysicsEndContact2DEventArgs obj)
        {
            // Get colliding node
            var hitNode = obj.NodeA;
            if (hitNode.Name == "Imp")
                hitNode = obj.NodeB;
            String nodeName = hitNode.Name;
            Node character2DNode = scene.GetChild("Imp", true);

            // Handle leaving a rope or ladder
            if (nodeName == "Climb")
            {
                if (character2D_.climb2_)
                    character2D_.climb2_ = false;
                else
                {
                    character2D_.isClimbing_ = false;
                    var body = character2DNode.GetComponent<RigidBody2D>();
                    body.GravityScale = 1.0f; // Restore gravity
                }
            }

            if (nodeName == "CanJump")
                character2D_.aboveClimbable_ = false;

            // Handle leaving a slope
            if (nodeName == "Slope")
            {
                character2D_.onSlope_ = false;
                // Clear forces (should be performed by setting linear velocity to zero, but currently doesn't work)
                var body = character2DNode.GetComponent<RigidBody2D>();
                body.SetLinearVelocity(Vector2.Zero);
                body.Awake = false;
                body.Awake = true;
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
