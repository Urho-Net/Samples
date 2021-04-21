using Urho;
using Urho.Urho2D;
using Urho.Gui;
using Urho.Resources;
using Urho.Audio;
using UrhoNetSamples;
using System;

namespace Urho2DPlatformer
{

    public class Sample2D : Component
    {
        const float CAMERA_MIN_DIST = 0.1f;
        const float CAMERA_MAX_DIST = 6.0f;

        public string demoFilename_ = "Platformer2D";

        public Scene scene_;

        public Sample2D()
        {

        }
        public Sample2D(IntPtr handle) : base(handle)
        {

        }

        public void CreateCollisionShapesFromTMXObjects(Node tileMapNode, TileMapLayer2D tileMapLayer, TileMapInfo2D info)
        {
            var body = tileMapNode.CreateComponent<RigidBody2D>();
            body.BodyType = (BodyType2D.Static);

            // Generate physics collision shapes and rigid bodies from the tmx file's objects located in "Physics" layer
            for (uint i = 0; i < tileMapLayer.NumObjects; ++i)
            {
                TileMapObject2D tileMapObject = tileMapLayer.GetObject(i); // Get physics objects

                // Create collision shape from tmx obj
                switch (tileMapObject.ObjectType)
                {
                    case TileMapObjectType2D.Rectangle:
                        {
                            CreateRectangleShape(tileMapNode, tileMapObject, tileMapObject.Size, info);
                        }
                        break;

                    case TileMapObjectType2D.Ellipse:
                        {
                            CreateCircleShape(tileMapNode, tileMapObject, tileMapObject.Size.X / 2, info); // Ellipse is built as a Circle shape as it doesn't exist in Box2D
                        }
                        break;

                    case TileMapObjectType2D.Polygon:
                        {
                            CreatePolygonShape(tileMapNode, tileMapObject);
                        }
                        break;

                    case TileMapObjectType2D.Polyline:
                        {
                            CreatePolyLineShape(tileMapNode, tileMapObject);
                        }
                        break;
                }
            }
        }

        public CollisionBox2D CreateRectangleShape(Node node, TileMapObject2D obj, Vector2 size, TileMapInfo2D info)
        {
            var shape = node.CreateComponent<CollisionBox2D>();
            shape.Size = size;
            if (info.Orientation == Orientation2D.Orthogonal)
                shape.Center = obj.Position + size / 2;
            else
            {
                shape.Center = obj.Position + new Vector2(info.TileWidth / 2, 0.0f);
                shape.Angle = 45.0f; // If our tile map is isometric then shape is losange
            }

            shape.Friction = 0.8f;
            if (obj.HasProperty("Friction"))
                shape.Friction = float.Parse((obj.GetProperty("Friction")));
            return shape;
        }

        public CollisionCircle2D CreateCircleShape(Node node, TileMapObject2D obj, float radius, TileMapInfo2D info)
        {
            var shape = node.CreateComponent<CollisionCircle2D>();
            Vector2 size = obj.Size;
            if (info.Orientation == Orientation2D.Orthogonal)
                shape.Center = obj.Position + size / 2;
            else
            {
                shape.Center = obj.Position + new Vector2(info.TileWidth / 2, 0.0f);
            }

            shape.Radius = radius;
            shape.Friction = 0.8f;
            if (obj.HasProperty("Friction"))
                shape.Friction = float.Parse(obj.GetProperty("Friction"));
            return shape;

        }

        public CollisionPolygon2D CreatePolygonShape(Node node, TileMapObject2D obj)
        {
            var shape = node.CreateComponent<CollisionPolygon2D>();
            uint numVertices = obj.NumPoints;
            shape.VertexCount = (numVertices);
            for (uint i = 0; i < numVertices; ++i)
                shape.SetVertex(i, obj.GetPoint(i));
            shape.Friction = 0.8f;
            if (obj.HasProperty("Friction"))
                shape.Friction = float.Parse(obj.GetProperty("Friction"));
            return shape;
        }
        /// Build collision shape from Tiled 'Poly Line' objects.
        public CollisionChain2D CreatePolyLineShape(Node node, TileMapObject2D obj)
        {
            var shape = node.CreateComponent<CollisionChain2D>();
            uint numVertices = obj.NumPoints;
            shape.VertexCount = (numVertices);
            for (uint i = 0; i < numVertices; ++i)
                shape.SetVertex(i, obj.GetPoint(i));
            shape.Friction = 0.8f;
            if (obj.HasProperty("Friction"))
                shape.Friction = float.Parse(obj.GetProperty("Friction"));
            return shape;
        }

        public Node CreateCharacter(TileMapInfo2D info, float friction, Vector3 position, float scale)
        {
            var cache = Application.ResourceCache;
            Node spriteNode = scene_.CreateChild("Imp");
            spriteNode.Position = position;
            spriteNode.Scale = new Vector3(scale, scale, scale);
            var animatedSprite = spriteNode.CreateComponent<AnimatedSprite2D>();
            // Get scml file and Play "idle" anim
            var animationSet = cache.GetAnimationSet2D("Urho2D/imp/imp.scml");
            animatedSprite.AnimationSet = animationSet;
            animatedSprite.SetAnimation("idle");
            animatedSprite.Layer = 3; // Put character over tile map (which is on layer 0) and over Orcs (which are on layer 2)
            var impBody = spriteNode.CreateComponent<RigidBody2D>();
            impBody.BodyType = (BodyType2D.Dynamic);
            impBody.AllowSleep = false;
            var shape = spriteNode.CreateComponent<CollisionCircle2D>();
            shape.Radius = 1.1f; // Set shape size
            shape.Friction = friction; // Set friction
            shape.Restitution = 0.1f; // Bounce

            return spriteNode;
        }

        public Node CreateTrigger()
        {
            Node node = scene_.CreateChild(); // Clones will be renamed according to object type
            var body = node.CreateComponent<RigidBody2D>();
            body.BodyType = BodyType2D.Static;
            var shape = node.CreateComponent<CollisionBox2D>(); // Create box shape
            shape.Trigger = true;
            return node;
        }

        public Node CreateEnemy()
        {
            var cache = Application.ResourceCache;
            Node node = scene_.CreateChild("Enemy");
            var staticSprite = node.CreateComponent<StaticSprite2D>();
            staticSprite.Sprite = cache.GetSprite2D("Urho2D/Aster.png");
            var body = node.CreateComponent<RigidBody2D>();
            body.BodyType = BodyType2D.Static;
            var shape = node.CreateComponent<CollisionCircle2D>(); // Create circle shape
            shape.Radius = 0.25f; // Set radius
            return node;
        }

        public Node CreateOrc()
        {
            var cache = Application.ResourceCache;
            Node node = scene_.CreateChild("Orc");
            node.Scale = scene_.GetChild("Imp", true).Scale;
            var animatedSprite = node.CreateComponent<AnimatedSprite2D>();
            var animationSet = cache.GetAnimationSet2D("Urho2D/Orc/Orc.scml");
            animatedSprite.AnimationSet = animationSet;
            animatedSprite.SetAnimation("run"); // Get scml file and Play "run" anim
            animatedSprite.Layer = 2; // Make orc always visible
            var body = node.CreateComponent<RigidBody2D>();
            var shape = node.CreateComponent<CollisionCircle2D>();
            shape.Radius = 1.3f; // Set shape size
            shape.Trigger = true;
            return node;
        }

        public Node CreateCoin()
        {
            var cache = Application.ResourceCache;
            Node node = scene_.CreateChild("Coin");
            node.SetScale(0.5f);
            var animatedSprite = node.CreateComponent<AnimatedSprite2D>();
            var animationSet = cache.GetAnimationSet2D("Urho2D/GoldIcon.scml");
            animatedSprite.AnimationSet = animationSet;// Get scml file and Play "idle" anim
            animatedSprite.SetAnimation("idle");
            animatedSprite.Layer = 4;
            var body = node.CreateComponent<RigidBody2D>();
            body.BodyType = BodyType2D.Static;
            var shape = node.CreateComponent<CollisionCircle2D>(); // Create circle shape
            shape.Radius = 0.32f; // Set radius
            shape.Trigger = true;
            return node;
        }

        public Node CreateMovingPlatform()
        {
            var cache = Application.ResourceCache;
            Node node = scene_.CreateChild("MovingPlatform");
            node.Scale = new Vector3(3.0f, 1.0f, 0.0f);
            var staticSprite = node.CreateComponent<StaticSprite2D>();
            staticSprite.Sprite = cache.GetSprite2D("Urho2D/Box.png");
            var body = node.CreateComponent<RigidBody2D>();
            body.BodyType = BodyType2D.Static;
            var shape = node.CreateComponent<CollisionBox2D>(); // Create box shape
            shape.Size = new Vector2(0.32f, 0.32f); // Set box size
            shape.Friction = 0.8f; // Set friction
            return node;
        }

        public void PopulateMovingEntities(TileMapLayer2D movingEntitiesLayer)
        {
            // Create enemy (will be cloned at each placeholder)
            Node enemyNode = CreateEnemy();
            Node orcNode = CreateOrc();
            Node platformNode = CreateMovingPlatform();

            // Instantiate enemies and moving platforms at each placeholder (placeholders are Poly Line objects defining a path from points)
            for (uint i = 0; i < movingEntitiesLayer.NumObjects; ++i)
            {
                // Get placeholder object
                TileMapObject2D movingObject = movingEntitiesLayer.GetObject(i); // Get placeholder object
                if (movingObject.ObjectType == TileMapObjectType2D.Polyline)
                {
                    // Clone the enemy and position it at placeholder point
                    Node movingClone;
                    Vector2 offset = new Vector2(0.0f, 0.0f);
                    if (movingObject.Type == "Enemy")
                    {
                        movingClone = enemyNode.Clone();
                        offset = new Vector2(0.0f, -0.32f);
                    }
                    else if (movingObject.Type == "Orc")
                        movingClone = orcNode.Clone();
                    else if (movingObject.Type == "MovingPlatform")
                        movingClone = platformNode.Clone();
                    else
                        continue;
                    movingClone.SetPosition2D(movingObject.GetPoint(0) + offset);

                    // Create script object that handles entity translation along its path
                    var mover = movingClone.CreateComponent<Mover>();

                    // Set path from points
                    Vector2[] path = CreatePathFromPoints(movingObject, offset);
                    mover.SetPath(path);

                    // Override default speed
                    if (movingObject.HasProperty("Speed"))
                        mover.speed_ = float.Parse(movingObject.GetProperty("Speed"));
                }
            }

            // Remove nodes used for cloning purpose
            enemyNode.Remove();
            orcNode.Remove();
            platformNode.Remove();
        }

        public void PopulateCoins(TileMapLayer2D coinsLayer)
        {
            // Create coin (will be cloned at each placeholder)
            Node coinNode = CreateCoin();

            // Instantiate coins to pick at each placeholder
            for (uint i = 0; i < coinsLayer.NumObjects; ++i)
            {
                TileMapObject2D coinObject = coinsLayer.GetObject(i); // Get placeholder object
                Node coinClone = coinNode.Clone();
                coinClone.SetPosition2D(coinObject.Position + coinObject.Size / 2 + new Vector2(0.0f, 0.16f));

            }
            // Remove node used for cloning purpose
            coinNode.Remove();
        }

        public void PopulateTriggers(TileMapLayer2D triggersLayer)
        {
            // Create trigger node (will be cloned at each placeholder)
            Node triggerNode = CreateTrigger();

            // Instantiate triggers at each placeholder (Rectangle objects)
            for (uint i = 0; i < triggersLayer.NumObjects; ++i)
            {
                TileMapObject2D triggerObject = triggersLayer.GetObject(i); // Get placeholder object
                if (triggerObject.ObjectType == TileMapObjectType2D.Rectangle)
                {
                    Node triggerClone = triggerNode.Clone();
                    triggerClone.Name = triggerObject.Type;
                    var shape = triggerClone.GetComponent<CollisionBox2D>();
                    shape.Size = (triggerObject.Size);
                    triggerClone.SetPosition2D(triggerObject.Position + triggerObject.Size / 2);
                }
            }
        }

        public float Zoom(Camera camera)
        {
            var input = Application.Input;
            float zoom_ = camera.Zoom;

            if (input.MouseMoveWheel != 0)
            {
                zoom_ = Math.Clamp(zoom_ + input.MouseMoveWheel * 0.1f, CAMERA_MIN_DIST, CAMERA_MAX_DIST);
                camera.Zoom = zoom_;
            }

            if (input.GetKeyDown(Key.PageUp))
            {
                zoom_ = Math.Clamp(zoom_ * 1.01f, CAMERA_MIN_DIST, CAMERA_MAX_DIST);
                camera.Zoom = zoom_;
            }

            if (input.GetKeyDown(Key.PageDown))
            {
                zoom_ = Math.Clamp(zoom_ * 0.99f, CAMERA_MIN_DIST, CAMERA_MAX_DIST);
                camera.Zoom = zoom_;
            }

            return zoom_;
        }

        public Vector2[] CreatePathFromPoints(TileMapObject2D obj, Vector2 offset)
        {
            Vector2[] path = new Vector2[obj.NumPoints];
            for (uint i = 0; i < obj.NumPoints; ++i)
                path[i] = (obj.GetPoint(i) + offset);
            return path;
        }

        public void CreateUIContent(String demoTitle, uint remainingLifes, uint remainingCoins)
        {
            var cache = Application.ResourceCache;
            var ui = Application.UI;

            
            // Set the default UI style and font
            ui.Root.SetDefaultStyle(cache.GetXmlFile("UI/DefaultStyle.xml"));
            var font = cache.GetFont("Fonts/Anonymous Pro.ttf");

            // We create in-game UIs (coins and lifes) first so that they are hidden by the fullscreen UI (we could also temporary hide them using SetVisible)

            // Create the UI for displaying the remaining coins
            var coinsUI = ui.Root.CreateChild<BorderImage>("Coins");
            coinsUI.Texture = cache.GetTexture2D("Urho2D/GoldIcon.png");
            coinsUI.SetSize(50, 50);
            coinsUI.ImageRect = new IntRect(0, 64, 60, 128);
            coinsUI.SetAlignment(HorizontalAlignment.Left, VerticalAlignment.Top);
            coinsUI.SetPosition(5, 5);
            var coinsText = coinsUI.CreateChild<Text>("CoinsText");
            coinsText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            coinsText.SetFont(font, 30);
            coinsText.TextEffect = TextEffect.Shadow;
            coinsText.Value = remainingCoins.ToString();

            // Create the UI for displaying the remaining lifes
            var lifeUI = ui.Root.CreateChild<BorderImage>("Life");
            lifeUI.Texture = cache.GetTexture2D("Urho2D/imp/imp_all.png");
            lifeUI.SetSize(70, 80);
            lifeUI.SetAlignment(HorizontalAlignment.Right, VerticalAlignment.Top);
            lifeUI.SetPosition(-5, 5);
            var lifeText = lifeUI.CreateChild<Text>("LifeText");
            lifeText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            lifeText.SetFont(font, 30);
            lifeText.TextEffect = TextEffect.Shadow;
            lifeText.Value = remainingLifes.ToString();

            // Create the fullscreen UI for start/end
            var fullUI = ui.Root.CreateChild<Window>("FullUI");
            fullUI.SetStyleAuto();
            fullUI.SetSize(ui.Root.Width, ui.Root.Height);
            fullUI.Enabled = false; // Do not react to input, only the 'Exit' and 'Play' buttons will
            fullUI.Priority = 10;
            

            // Create the title
            var title = fullUI.CreateChild<BorderImage>("Title");
            title.SetMinSize(fullUI.Width, 50);
            title.Texture = cache.GetTexture2D("Textures/HeightMap.png");
            title.SetFullImageRect();
            title.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Top);
            var titleText = title.CreateChild<Text>("TitleText");
            titleText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            titleText.SetFont(font, 24);
            titleText.Value = demoTitle.ToString();

            // Create the image
            var spriteUI = fullUI.CreateChild<BorderImage>("Sprite");
            spriteUI.Texture = cache.GetTexture2D("Urho2D/imp/imp_all.png");
            spriteUI.SetSize(238, 271);
            spriteUI.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            spriteUI.SetPosition(0, -ui.Root.Height / 4);

            // Create the 'EXIT' button
            var exitButton = ui.Root.CreateChild<Button>("ExitButton");
            exitButton.SetStyleAuto();
            exitButton.FocusMode = FocusMode.ResetFocus;
            exitButton.SetSize(100, 50);
            exitButton.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            exitButton.SetPosition(-100, 0);
            exitButton.Priority = 11;
            var exitText = exitButton.CreateChild<Text>("ExitText");
            exitText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            exitText.SetFont(font, 24);
            exitText.Value = "EXIT";
            exitButton.Released += HandleExitButton;


            // Create the 'PLAY' button
            var playButton = ui.Root.CreateChild<Button>("PlayButton");
            playButton.SetStyleAuto();
            playButton.FocusMode = FocusMode.ResetFocus;
            playButton.SetSize(100, 50);
            playButton.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            playButton.SetPosition(100, 0);
            playButton.Priority = 11;
            var playText = playButton.CreateChild<Text>("PlayText");
            playText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            playText.SetFont(font, 24);
            playText.Value = "PLAY";

            // Create the instructions
            var instructionText = ui.Root.CreateChild<Text>("Instructions");
            instructionText.Value = ("Use WASD keys or Arrows to move\nPageUp/PageDown/MouseWheel to zoom\nF5/F7 to save/reload scene\n'Z' to toggle debug geometry\nSpace to fight");
            instructionText.SetFont(cache.GetFont("Fonts/Anonymous Pro.ttf"), 24);
            instructionText.TextAlignment = (HorizontalAlignment.Center); // Center rows in relation to each other
            instructionText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            instructionText.SetPosition(0, ui.Root.Height / 4);
            instructionText.Priority = 10;

            // Show mouse cursor
            Application.Input.SetMouseVisible(true);
        }

        

        private void HandleExitButton(ReleasedEventArgs obj)
        {
            // TBD ELI
        }

        public void SaveScene(bool initial)
        {
            String filename = demoFilename_;
            if (!initial)
                filename += "InGame";

            
            string path = "";

            if(Sample.isMobile)
            {
                path = Application.FileSystem.ProgramDir + "Data/Scenes/";
            }
            else
            {
                path = Application.FileSystem.ProgramDir + "Assets/Data/Scenes/";
            }
             
            if (!Application.FileSystem.DirExists(path))
            {
                Application.FileSystem.CreateDir(path);
            }

            scene_.SaveXml(path + filename + ".xml");
        }

        public void CreateBackgroundSprite(TileMapInfo2D info, float scale, String texture, bool animate)
        {
            var cache = Application.ResourceCache;

            Node node = scene_.CreateChild("Background");
            node.Position = (new Vector3(info.MapWidth, info.MapHeight, 0) / 2);
            node.SetScale(scale);
            var sprite = node.CreateComponent<StaticSprite2D>();
            sprite.Sprite = cache.GetSprite2D(texture);
            sprite.Color = (new Color(Sample.NextRandom(0.0f, 1.0f), Sample.NextRandom(0.0f, 1.0f), Sample.NextRandom(0.0f, 1.0f), 1.0f));
            sprite.Layer = -99;

            // Create rotation animation
            if (animate)
            {
                ValueAnimation animation = new ValueAnimation();
                animation.SetKeyFrame(0, new Quaternion(0.0f, 0.0f, 0.0f));
                animation.SetKeyFrame(1, new Quaternion(0.0f, 0.0f, 180.0f));
                animation.SetKeyFrame(2, new Quaternion(0.0f, 0.0f, 0.0f));
                node.SetAttributeAnimation("Rotation", animation, WrapMode.Loop, 0.05f);
            }
        }


        public void SpawnEffect(Node node)
        {
            var cache = Application.ResourceCache;
            Node particleNode = node.CreateChild("Emitter");
            particleNode.SetScale(0.5f / node.Scale.X);
            var particleEmitter = particleNode.CreateComponent<ParticleEmitter2D>();
            particleEmitter.Layer = 2;
            particleEmitter.Effect = cache.GetParticleEffect2D("Urho2D/sun.pex");
        }

        public void PlaySoundEffect(String soundName)
        {
            var cache = Application.ResourceCache;
            var source = scene_.CreateComponent<SoundSource>();
            var sound = cache.GetSound("Sounds/" + soundName);
            if (sound != null)
            {
                source.AutoRemoveMode = AutoRemoveMode.Component;
                source.Play(sound);
            }
        }


    }
}