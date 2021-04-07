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
using UrhoNetSamples;
using Urho;
using Urho.Physics;

namespace PhysicsStressTest
{
    public class PhysicsStressTest : Sample
    {

        bool drawDebug;

        [Preserve]
        public PhysicsStressTest() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            Graphics.WindowTitle = "PhysicsStressTest";
            CreateScene();
            if (isMobile)
            {
                SimpleCreateInstructionsWithWasd(
                    "Button to spawn physics objects\n" +
                    "Button to toggle physics debug geometry");
            }
            else
            {
                SimpleCreateInstructionsWithWasd(
                    "LMB to spawn physics objects\n" +
                    "Space to toggle physics debug geometry");
            }
            SetupViewport();
            SubscribeToEvents();
        }


        protected override void Stop()
        {
            UnSubscribeFromEvents();
            base.Stop();
        }

        void SubscribeToEvents()
        {
            Engine.PostRenderUpdate += OnPostRenderUpdate;
        }

        void UnSubscribeFromEvents()
        {
            Engine.PostRenderUpdate -= OnPostRenderUpdate;
        }

        private void OnPostRenderUpdate(PostRenderUpdateEventArgs obj)
        {
            // If draw debug mode is enabled, draw viewport debug geometry, which will show eg. drawable bounding boxes and skeleton
            // bones. Note that debug geometry has to be separately requested each frame. Disable depth test so that we can see the
            // bones properly
            if (drawDebug)
                Renderer.DrawDebugGeometry(false);
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
            SimpleMoveCamera3D(timeStep);
            var input = Input;
            // "Shoot" a physics object with left mousebutton
            if (input.GetMouseButtonPress(MouseButton.Left))
                SpawnObject();


            if (Input.GetKeyPress(Key.Space))
                drawDebug = !drawDebug;
        }

        void SetupViewport()
        {
            var renderer = Renderer;
            renderer.SetViewport(0, new Viewport(Context, scene, CameraNode.GetComponent<Camera>(), null));
        }

        void SpawnObject()
        {
            var cache = ResourceCache;

            // Create a smaller box at camera position
            Node boxNode = scene.CreateChild("SmallBox");
            boxNode.Position = CameraNode.Position;
            boxNode.Rotation = CameraNode.Rotation;
            boxNode.SetScale(0.25f);
            StaticModel boxObject = boxNode.CreateComponent<StaticModel>();
            boxObject.Model = (cache.GetModel("Models/Box.mdl"));
            boxObject.SetMaterial(cache.GetMaterial("Materials/StoneSmall.xml"));
            boxObject.CastShadows = true;

            // Create physics components, use a smaller mass also
            RigidBody body = boxNode.CreateComponent<RigidBody>();
            body.Mass = 0.25f;
            body.Friction = 0.75f;
            CollisionShape shape = boxNode.CreateComponent<CollisionShape>();
            shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);

            const float objectVelocity = 10.0f;

            // Set initial velocity for the RigidBody based on camera forward vector. Add also a slight up component
            // to overcome gravity better
            body.SetLinearVelocity(CameraNode.Rotation * new Vector3(0.0f, 0.25f, 1.0f) * objectVelocity);
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Create a physics simulation world with default parameters, which will update at 60fps. Like the Octree must
            // exist before creating drawable components, the PhysicsWorld must exist before creating physics components.
            // Finally, create a DebugRenderer component so that we can draw physics debug geometry
            scene.CreateComponent<Octree>();
            scene.CreateComponent<PhysicsWorld>();
            scene.CreateComponent<DebugRenderer>();

            // Create a Zone component for ambient lighting & fog control
            Node zoneNode = scene.CreateChild("Zone");
            Zone zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;

            // Create a directional light to the world. Enable cascaded shadows on it
            Node lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            Light light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            {
                // Create a floor object, 500 x 500 world units. Adjust position so that the ground is at zero Y
                Node floorNode = scene.CreateChild("Floor");
                floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
                floorNode.Scale = new Vector3(500.0f, 1.0f, 500.0f);
                StaticModel floorObject = floorNode.CreateComponent<StaticModel>();
                floorObject.Model = cache.GetModel("Models/Box.mdl");
                floorObject.SetMaterial(cache.GetMaterial("Materials/StoneTiled.xml"));

                // Make the floor physical by adding RigidBody and CollisionShape components
                /*RigidBody* body = */
                floorNode.CreateComponent<RigidBody>();
                CollisionShape shape = floorNode.CreateComponent<CollisionShape>();
                shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
            }

            {
                // Create static mushrooms with triangle mesh collision
                const uint numMushrooms = 50;
                for (uint i = 0; i < numMushrooms; ++i)
                {
                    Node mushroomNode = scene.CreateChild("Mushroom");
                    mushroomNode.Position = new Vector3(NextRandom(400.0f) - 200.0f, 0.0f, NextRandom(400.0f) - 200.0f);
                    mushroomNode.Rotation = new Quaternion(0.0f, NextRandom(360.0f), 0.0f);
                    mushroomNode.SetScale(5.0f + NextRandom(5.0f));
                    StaticModel mushroomObject = mushroomNode.CreateComponent<StaticModel>();
                    mushroomObject.Model = (cache.GetModel("Models/Mushroom.mdl"));
                    mushroomObject.SetMaterial(cache.GetMaterial("Materials/Mushroom.xml"));
                    mushroomObject.CastShadows = true;

                    mushroomNode.CreateComponent<RigidBody>();
                    CollisionShape shape = mushroomNode.CreateComponent<CollisionShape>();
                    // By default the highest LOD level will be used, the LOD level can be passed as an optional parameter
                    shape.SetTriangleMesh(mushroomObject.Model, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);
                }
            }

            {
                // Create a large amount of falling physics objects
                const uint numObjects = 1000;
                for (uint i = 0; i < numObjects; ++i)
                {
                    Node boxNode = scene.CreateChild("Box");
                    boxNode.Position = new Vector3(0.0f, i * 2.0f + 100.0f, 0.0f);
                    StaticModel boxObject = boxNode.CreateComponent<StaticModel>();
                    boxObject.Model = cache.GetModel("Models/Box.mdl");
                    boxObject.SetMaterial(cache.GetMaterial("Materials/StoneSmall.xml"));
                    boxObject.CastShadows = true;

                    // Give the RigidBody mass to make it movable and also adjust friction
                    RigidBody body = boxNode.CreateComponent<RigidBody>();
                    body.Mass = 1.0f;
                    body.Friction = 1.0f;
                    // Disable collision event signaling to reduce CPU load of the physics simulation
                    body.CollisionEventMode = CollisionEventMode.Never;
                    CollisionShape shape = boxNode.CreateComponent<CollisionShape>();
                    shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
                }
            }

            // Create the camera. Limit far clip distance to match the fog. Note: now we actually create the camera node outside
            // the scene, because we want it to be unaffected by scene load / save
            CameraNode = new Node();
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;

            // Set an initial position for the camera scene node above the floor
            CameraNode.Position = new Vector3(0.0f, 3.0f, -20.0f);

        }

        protected override string JoystickLayoutPatch => JoystickLayoutPatches.WithFireAndDebugButtons;
    }
}