using Urho;
using UrhoNetSamples;
using System;
using Urho.Physics;

namespace InverseKinematics
{
    public class InverseKinematics : Sample
    {
        Camera camera;

        float floorPitch_ = 0.0f;
        float floorRoll_ = 0.0f ;

        Node floorNode_;
        Node leftFoot_;
        Node rightFoot_;
        Node jackNode_;

        IKEffector leftEffector_;
        /// Inverse kinematic right effector.
        IKEffector rightEffector_;
        /// Inverse kinematic solver.
        IKSolver solver_;

        AnimationController jackAnimCtrl_;

        bool drawDebug_ = false;

        bool IsButtonBPressed = false;

        [Preserve]
        public InverseKinematics() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            CreateScene();

            if (IsMobile)
            {
                CreateScreenJoystick(E_JoystickType.OneJoyStick_TwoButtons);
            }

            if (IsMobile)
            {
                SimpleCreateInstructions("Use touch to look around\nUse Joystick to change incline\nPress A to reset floor\nPress B to draw debug geometry", Color.Black);
            }
            else
            {
                SimpleCreateInstructions("Left-Click and drag to look around\nRight-Click and drag to change incline\nPress space to reset floor\nPress D to draw debug geometry", Color.Black);
            }
            SetupViewport();
            SubscribeToEvents();
         
     
        }

        protected override void Stop()
        {
            base.Stop();
            UnsubscribeFromEvents();
            camera.Dispose();
        }

        private void SubscribeToEvents()
        {
            Engine.PostRenderUpdate += OnPostRenderUpdate;
        }

        private void UnsubscribeFromEvents()
        {
            Engine.PostRenderUpdate -= OnPostRenderUpdate;
        }

        void CreateScene()
        {
            scene = new Scene();
			
			// Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Create a physics simulation world with default parameters, which will update at 60fps. Like the Octree must
            // exist before creating drawable components, the PhysicsWorld must exist before creating physics components.
            // Finally, create a DebugRenderer component so that we can draw physics debug geometry
            scene.CreateComponent<Octree>();
            scene.CreateComponent<PhysicsWorld>();
            scene.CreateComponent<DebugRenderer>();


            // Create a child scene node (at world origin) and a StaticModel component into it. Set the StaticModel to show a simple
            // plane mesh with a "stone" material. Note that naming the scene nodes is optional. Scale the scene node larger
            // (100 x 100 world units)
            floorNode_ = scene.CreateChild("Plane");
            floorNode_.Scale = new Vector3(50, 1, 50);
            var planeObject = floorNode_.CreateComponent<StaticModel>();
            planeObject.Model = ResourceCache.GetModel("Models/Plane.mdl");
            planeObject.SetMaterial(ResourceCache.GetMaterial("Materials/StoneTiled.xml"));

            // Set up collision, we need to raycast to determine foot height
            floorNode_.CreateComponent<RigidBody>();
            var col = floorNode_.CreateComponent<CollisionShape>();
            col.SetBox(new Vector3(1, 0, 1), Vector3.Zero, Quaternion.Identity);


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



            jackNode_ = scene.CreateChild("Jack");
            jackNode_.Rotation = new Quaternion(0.0f, 270.0f, 0.0f);
            var jack = jackNode_.CreateComponent<AnimatedModel>();
            jack.SetModel(ResourceCache.GetModel("Models/Jack.mdl"));
            jack.SetMaterial(ResourceCache.GetMaterial("Materials/Jack.xml"));
            jack.CastShadows = (true);
	

            // Create animation controller and play walk animation
            jackAnimCtrl_ = jackNode_.CreateComponent<AnimationController>();
           jackAnimCtrl_.PlayExclusive("Models/Jack_Walk.ani", 0, true, 0.0f);

            // We need to attach two inverse kinematic effectors to Jack's feet to
            // control the grounding.
            leftFoot_ = jackNode_.GetChild("Bip01_L_Foot", true);
            rightFoot_ = jackNode_.GetChild("Bip01_R_Foot", true);
            leftEffector_ = leftFoot_.CreateComponent<IKEffector>();
            rightEffector_ = rightFoot_.CreateComponent<IKEffector>();
            // Control 2 segments up to the hips
            leftEffector_.ChainLength = 2;
            rightEffector_.ChainLength = 2;

            // For the effectors to work, an IKSolver needs to be attached to one of
            // the parent nodes. Typically, you want to place the solver as close as
            // possible to the effectors for optimal performance. Since in this case
            // we're solving the legs only, we can place the solver at the spine.
            Node spine = jackNode_.GetChild("Bip01_Spine", true);
            solver_ = spine.CreateComponent<IKSolver>();

            // Two-bone solver is more efficient and more stable than FABRIK (but only
            // works for two bones, obviously).
            solver_.Algorithm = (IKSolverAlgorithm.TwoBone);

            // Disable auto-solving, which means we need to call Solve() manually
            solver_.SetFeature(IKSolverFeature.AutoSolve, false);

            // Only enable this so the debug draw shows us the pose before solving.
            // This should NOT be enabled for any other reason (it does nothing and is
            // a waste of performance).
            solver_.SetFeature(IKSolverFeature.UpdateOriginalPose, true);


            // Create a scene node for the camera, which we will move around
            // The camera will use default settings (1000 far clip distance, 45 degrees FOV, set aspect ratio automatically)
            CameraNode = scene.CreateChild("Camera");
            camera = CameraNode.CreateComponent<Camera>();

            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = new Vector3(-7, 3f, -4);

            Pitch = 20;
            Yaw = 50;
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
            const float mouseSensitivity = .1f;

            if (IsMobile || Input.GetMouseButtonDown(MouseButton.Left) == true)
            {
                var mouseMove = Application.Input.MouseMove;
                Yaw += mouseSensitivity * mouseMove.X;
                Pitch += mouseSensitivity * mouseMove.Y;
                Pitch = MathHelper.Clamp(Pitch, -90, 90);
            }


            if (Input.GetMouseButtonDown(MouseButton.Right) == true)
            {
                
                var mouseMoveInt = Application.Input.MouseMove;
                float radYaw = MathHelper.DegreesToRadians(Yaw);

                Vector2 mouseMove = new Matrix2(-MathF.Cos(radYaw), MathF.Sin(radYaw),
                                                  MathF.Sin(radYaw), MathF.Cos(radYaw)) * new Vector2(mouseMoveInt.Y, -mouseMoveInt.X);

                floorPitch_ += mouseSensitivity * mouseMove.X;
                floorPitch_ = MathHelper.Clamp(floorPitch_, -90, 90);
                floorRoll_ += mouseSensitivity * mouseMove.Y;

            }

            if(Application.Input.GetKeyPress(Key.Space))
            {
                floorPitch_ = 0;
                floorRoll_ = 0;
            }

            if(Application.Input.GetKeyPress(Key.D))
            {
                drawDebug_ = !drawDebug_;
            }

            if (IsMobile)
            {
                JoystickState joystick;
                if (screenJoystickIndex != -1 && Input.GetJoystick(screenJoystickIndex, out joystick))
                {
                    float radYaw = MathHelper.DegreesToRadians(Yaw);
                    var JoystickMoveInput = new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
                    Vector2 JoystickMove = new Matrix2(-MathF.Cos(radYaw), MathF.Sin(radYaw),
                                      MathF.Sin(radYaw), MathF.Cos(radYaw)) * new Vector2(JoystickMoveInput.Y, -JoystickMoveInput.X);

                    floorPitch_ += mouseSensitivity * JoystickMove.X;
                    floorPitch_ = MathHelper.Clamp(floorPitch_, -90, 90);
                    floorRoll_ += mouseSensitivity * JoystickMove.Y;


                    if (joystick.GetButtonDown(JoystickState.Button_A))
                    {
                        floorPitch_ = 0;
                        floorRoll_ = 0;
                    }

                    if (joystick.GetButtonDown(JoystickState.Button_B) && IsButtonBPressed == false)
                    {
                        IsButtonBPressed = true;
                        drawDebug_ = !drawDebug_;
                    }

                    if(!joystick.GetButtonDown(JoystickState.Button_B))
                    {
                        IsButtonBPressed = false;
                    }
                }
            }

            floorNode_.Rotation = new Quaternion(floorPitch_, 0, floorRoll_);
            CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
           
        }


        private void OnPostRenderUpdate(PostRenderUpdateEventArgs obj)
        {

            var phyWorld = scene.GetComponent<PhysicsWorld>();
            Vector3 leftFootPosition = leftFoot_.WorldPosition;
            Vector3 rightFootPosition = rightFoot_.WorldPosition;

            PhysicsRaycastResult result = new PhysicsRaycastResult();
            scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, new Ray(leftFootPosition + new Vector3(0, 1, 0), new Vector3(0, -1, 0)), 2);
            if (result.Body != null)
            {
                // Cast again, but this time along the normal. Set the target position
                // to the ray intersection
                phyWorld.RaycastSingle(ref result, new Ray(leftFootPosition + result.Normal, -result.Normal), 2);
                // The foot node has an offset relative to the root node
                float footOffset = leftFoot_.WorldPosition.Y - jackNode_.WorldPosition.Y;
                leftEffector_.TargetPosition = (result.Position + result.Normal * footOffset);
                // Rotate foot according to normal
                leftFoot_.Rotate(Quaternion.FromRotationTo(new Vector3(0, 1, 0), result.Normal), TransformSpace.World);
            }

            // Same deal with the right foot
            phyWorld.RaycastSingle(ref result, new Ray(rightFootPosition + new Vector3(0, 1, 0), new Vector3(0, -1, 0)), 2);
            if (result.Body != null)
            {
                phyWorld.RaycastSingle(ref result, new Ray(rightFootPosition + result.Normal, -result.Normal), 2);
                float footOffset = rightFoot_.WorldPosition.Y - jackNode_.WorldPosition.Y;
                rightEffector_.TargetPosition = (result.Position + result.Normal * footOffset);
                rightFoot_.Rotate(Quaternion.FromRotationTo(new Vector3(0, 1, 0), result.Normal), TransformSpace.World);
            }

            solver_.Solve();

            if (drawDebug_)
                solver_.DrawDebugGeometry(false);

        }
    }
}