// Copyright (c) 2020-2022 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2022 the Urho3D project.
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

using System.Collections.Generic;
using UrhoNetSamples;
using Urho;
using Urho.Audio;
using Urho.Urho2D;


namespace Racer2D
{
    public class Racer2D : Sample
    {
        private static Scene _scene;
        private static Terrain _terrain;

        private Viewport _viewport;
        private Camera _camera;
        private Vehicle _vehicle;
        private Clouds _clouds;

        public static Node[] vehicleNodes = null;

        PhysicsWorld2D physicsWorld2D = null;
        bool restart = false;

        public Racer2D() : base(new ApplicationOptions(assetsFolder: "Data;CoreData;Data/Racer2D")) { }
        protected override void Start()
        {
            base.Start();
            Graphics.WindowTitle = "Racer2D";

            if (IsMobile)
            {
                SimpleCreateInstructionsWithWasd(
                    "Joystick ,  move right to drive foward\n" +
                    "Joystick ,  move left to stop\n" +
                    "Button up to roll up\n Button down to roll down ", Color.Black);
            }
            else
            {
                SimpleCreateInstructionsWithWasd(
                    "D-drive forward\nA-stop\nW-roll up\nS-roll down", Color.Black);
            }


            CreateScene();

        }

        private void CreateScene()
        {
            // We setup our scene, main camera and viewport
            // This scene contorled by the Sample .
            scene = new Scene();

            _scene = scene;
            _scene.CreateComponent<Octree>().SetSize(new BoundingBox(1, 100), 3);
            _camera = _scene.CreateChild("Camera").CreateComponent<Camera>();
            _camera.Node.Position = new Vector3(50, 10, -1);
            _camera.Orthographic = true;
            _camera.OrthoSize = 26;

            _viewport = new Viewport(Context, _scene, _camera, null);
            Renderer.SetViewport(0, _viewport);

            // We create a sound source for the music and the music
            SoundSource musicSource = _scene.CreateComponent<SoundSource>();
            Sound music = ResourceCache.GetSound("Music/Happy_Bee.ogg");
            music.Looped = (true);
            musicSource.Play(music);
            musicSource.SetSoundType("Music");

            // We don't need a sound listener for the above, but we add one for the sounds and adjust the music gain
            Audio.Listener = (_camera.Node.CreateComponent<SoundListener>());
            Audio.SetMasterGain("Music", 0.3f);

            // We create a background node which is a child of the camera so it won't move relative to it
            Node bg = _camera.Node.CreateChild("Background");
            StaticSprite2D bgspr = bg.CreateComponent<StaticSprite2D>();
            bgspr.Sprite = (ResourceCache.GetSprite2D("Scenarios/grasslands/BG.png"));
            bg.Position = (new Vector3(0, 0, 100));
            bg.SetScale2D(Vector2.One * 6f);

            // We add a physics world so we can simulate physics, and enable CCD
            physicsWorld2D = _scene.CreateComponent<PhysicsWorld2D>();
            physicsWorld2D.ContinuousPhysics = (true);

            // We create a terrain, vehicle and cloud system
            _terrain = new Terrain(_scene);
            _vehicle = CreateVehicle(new Vector2(50, 10));
            _clouds = new Clouds(50, 5, 40, 16, 40);

            SubscribeToEvents();
        }

        private void HandleCollisionEnd(PhysicsEndContact2DEventArgs obj)
        {
            var nodeA = obj.NodeA;
            var nodeB = obj.NodeB;
            if (nodeA.Name == "Head" && nodeB.Name == "Terrain" ||
                nodeB.Name == "Head" && nodeA.Name == "Terrain")
            {
                restart = true;
            }
        }

        private void HandleCollisionBegin(PhysicsBeginContact2DEventArgs obj)
        {
     
        }

        protected override void Stop()
        {
            UnSubscribeFromEvents();
            base.Stop();
        }

        void SubscribeToEvents()
        {
            Engine.PostUpdate += OnPostUpdate;
            physicsWorld2D.PhysicsBeginContact2D += HandleCollisionBegin;
            physicsWorld2D.PhysicsEndContact2D += HandleCollisionEnd;
        }

        void UnSubscribeFromEvents()
        {
            Engine.PostUpdate -= OnPostUpdate;
            physicsWorld2D.PhysicsBeginContact2D += HandleCollisionBegin;
            physicsWorld2D.PhysicsEndContact2D += HandleCollisionEnd;
        }

        private void OnPostUpdate(PostUpdateEventArgs obj)
        {

            if (restart == true)
            {
                restart = false;
                foreach (Node node in vehicleNodes)
                {
                    node.Remove();
                }
                _vehicle = CreateVehicle(new Vector2(50, 10));
                _camera.Node.Position = new Vector3(50, 10, -1);
            }

            var TimeStep = obj.TimeStep;

            _camera.Node.Position =
            new Vector3(LerpVector2(_camera.Node.Position2D, _vehicle.Node.Position2D + Vector2.UnitX * 10, 5 * TimeStep)) + Vector3.Back * 10;
            // We tick the cloud system
            _clouds.Tick(TimeStep, _vehicle.Node.Position.X);
        }

        #region Static Utils

        // Convenience function to create node with sprite and rigidbody (optional)
        public static Vehicle CreateVehicle(Vector2 position)
        {
            var Cache = Application.Current.ResourceCache;

            Node vehicleNode = CreateSpriteNode(Cache.GetSprite2D("Characters/truck/vehicle.png"), 1.4f);

            // We create the vehicle and the chassis (CreateChassis returns the Vehicle for convenience)
            Vehicle vehicle = vehicleNode.CreateComponent<Vehicle>().CreateChassis(
                new Vector2(-0.1f, 0.4f), 1.4f, 5,
                new Vector3(-2f, -1, 1), Cache.GetParticleEffect2D("Particles/smoke.pex"),
                Cache.GetSound("Sounds/engine_sound_crop.wav"), Cache.GetSound("Sounds/tires_squal_loop.wav"),
                new[] { Cache.GetSound("Sounds/susp_1.wav"), Cache.GetSound("Sounds/susp_3.wav") },
                300, 50, 5, 1000);

            // We create the wheels
            Sprite2D wspr = Cache.GetSprite2D("Characters/truck/wheel.png");
            Node w1 = vehicle.CreateWheel(
                wspr, new Vector2(1.5f, -1.5f), 1.25f, 4, 0.4f, Cache.GetParticleEffect2D("Particles/dust.pex"), 2.6f);
            Node w2 = vehicle.CreateWheel(
                wspr, new Vector2(-1.8f, -1.5f), 1.25f, 4, 0.4f, Cache.GetParticleEffect2D("Particles/dust.pex"), 2.6f);

            // We create the head
            Node head = vehicle.CreateHead(
                Cache.GetSprite2D("Characters/truck/head.png"), new Vector3(-1, 2.7f, -1), 1f, new Vector2(-1, 1.8f));

            // We position the vehicle
            vehicleNodes = new Node[] { vehicleNode, w1, w2, head };
            foreach (Node node in vehicleNodes)
                node.Translate2D(position);

            return vehicle;
        }

        // Create a node with a sprite and optionally set scale and add a RigidBody2D component
        public static Node CreateSpriteNode(Sprite2D sprite, Node parent, float scale = 1f, bool addRigidBody = true)
        {
            Node n = parent.CreateChild();
            n.SetScale2D(Vector2.One * scale);
            n.CreateComponent<StaticSprite2D>().Sprite = (sprite);
            if (addRigidBody) n.CreateComponent<RigidBody2D>().BodyType = (BodyType2D.Dynamic);
            return n;
        }

        // Convenience overload
        public static Node CreateSpriteNode(Sprite2D sprite, float scale = 1f, bool addRigidBody = true)
        {
            return CreateSpriteNode(sprite, _scene, scale, addRigidBody);
        }

        // Convenience function to add a collider to a given node
        public static T AddCollider<T>(Node node, float fric = 1, float dens = 1, float elas = 0) where T : CollisionShape2D
        {
            CollisionShape2D s = node.CreateComponent<T>();
            s.Friction = (fric);
            s.Density = (dens);
            s.Restitution = (elas);
            return (T)s;
        }

        // This returns the surface point closest to wheel's center
        public static Vector3 GetSurfacePointClosestToPoint(Node wheel)
        {
            // We sample various points near the wheel position
            List<Vector2> points = new List<Vector2>();
            for (float xOffset = -1; xOffset < 1; xOffset += 0.02f)
            {
                float y = _terrain.SampleSurface(wheel.Position.X + xOffset);
                points.Add(new Vector2(wheel.Position.X + xOffset, y));
            }

            // We get the closest one
            float lastDist = float.MaxValue;
            Vector2 closestPoint = Vector2.Zero;
            foreach (Vector2 point in points)
            {
                float pointDist = Extensions.Distance(wheel.Position2D, point);
                if (pointDist < lastDist)
                {
                    closestPoint = point;
                    lastDist = pointDist;
                }
            }

            return new Vector3(closestPoint.X, closestPoint.Y, 2);
        }

        public static Vector2 LerpVector2(Vector2 vecA, Vector2 vecB, float t)
        {
            Vector2 ipo;
            ipo.X = vecA.X + t * (vecB.X - vecA.X);
            ipo.Y = vecA.Y + t * (vecB.Y - vecA.Y);
            return ipo;
        }

        #endregion

        const string WithRollUpDown =
           "<patch>" +
           "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
           "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Up</replace>" +
           "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
           "        <element type=\"Text\">" +
           "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
           "            <attribute name=\"Text\" value=\"R\" />" +
           "        </element>" +
           "    </add>" +
           "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
           "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Down</replace>" +
           "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
           "        <element type=\"Text\">" +
           "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
           "            <attribute name=\"Text\" value=\"F\" />" +
           "        </element>" +
           "    </add>" +
           "</patch>";

        protected override string JoystickLayoutPatch => WithRollUpDown;

    }

}
