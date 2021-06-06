// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2021 the Urho3D project.
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
using Urho;
using Urho.Physics;
using Urho.Gui;
using System;
using Nakama;
using Nakama.TinyJson;
using System.Threading.Tasks;
using System.Collections.Generic;
using Urho.Resources;
using System.Net;

namespace NakamaNetworking
{
    public class NakamaNetworking : Sample
    {

        /// Touch utility object.
        Touch touch;
        /// The controllable character component.
        LocalKinematicCharacter LocalCharacter;

        /// First person camera flag.
        bool firstPerson;
        PhysicsWorld physicsWorld;

        private IUserPresence localUser;


        Node localPlayer = null;
        private IDictionary<string, Node> players;

        LoginWindow loginWindow = null;
        private static NakamaNetworking _instance = null;
        [Preserve]
        public NakamaNetworking() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        public static NakamaNetworking Instance()
        {
            return _instance;
        }

        protected override void Start()
        {

            _instance = this;

            base.Start();

            PlayerPrefs.Init(this);

            if (TouchEnabled)
                touch = new Touch(TouchSensitivity, Input);
            CreateScene();

            loginWindow = new LoginWindow(this);
       
            localPlayer = CreateCharacter();

            if (isMobile)
            {
                CreateScreenJoystick(E_JoystickType.OneJoyStick_OneButton);
            }


            if (isMobile)
            {
                SimpleCreateInstructionsWithWasd("Button A to jump", Color.Black);
            }
            else
            {
                SimpleCreateInstructionsWithWasd("Space to jump, F to toggle 1st/3rd person", Color.Black);
            }
            SubscribeToEvents();

           

            players = new Dictionary<string, Node>();

            try
            {
                ClearInfoText();
                Global.NakamaConnection = new NakamaClient(OnSocketConnected, OnSocketClosed);

            }
            catch (Exception ex)
            {
                LogSharp.Error("Exception:" + ex.ToString());
            }

        }

 
        private async void OnSocketConnected()
        {
            loginWindow.Hide();

            string hostIP = loginWindow.GetHostIP();
            PlayerPrefs.SetString("HostIP",hostIP);

            UpdateInfoText("CONNECTED TO SERVER \nFINDING MATCH PLEASE WAIT...");
            Global.NakamaConnection.Socket.ReceivedMatchmakerMatched += m => InvokeOnMain(() => OnReceivedMatchmakerMatched(m));
            Global.NakamaConnection.Socket.ReceivedMatchPresence += m => InvokeOnMain(() => OnReceivedMatchPresence(m));
            
            int playerCount = loginWindow.GetPlayerCount();
            PlayerPrefs.SetInt("PlayerCount",playerCount);
            await Global.NakamaConnection.FindMatch(playerCount, playerCount);
        }

        private void OnSocketClosed()
        {
             UpdateInfoText("CONNECTION TO SERVER CLOSED");
        }

        protected override void Stop()
        {
            base.Stop();
            UnSubscribeFromEvents();

            foreach (var player in players)
            {
                player.Value.Remove();
            }

            players.Clear();

        }



        void SubscribeToEvents()
        {
            Engine.PostUpdate += (HandlePostUpdate);
        }

        void UnSubscribeFromEvents()
        {
            Engine.PostUpdate -= (HandlePostUpdate);
        }

        void HandlePostUpdate(PostUpdateEventArgs args)
        {

            if (LocalCharacter == null)
                return;

            Node characterNode = LocalCharacter.Node;

            // Get camera lookat dir from character yaw + pitch
            Quaternion rot = characterNode.Rotation;
            Quaternion dir = rot * Quaternion.FromAxisAngle(Vector3.UnitX, LocalCharacter.Controls.Pitch);

            // Turn head to camera pitch, but limit to avoid unnatural animation
            Node headNode = characterNode.GetChild("Mutant:Head", true);
            float limitPitch = MathHelper.Clamp(LocalCharacter.Controls.Pitch, -45.0f, 45.0f);
            Quaternion headDir = rot * Quaternion.FromAxisAngle(new Vector3(1.0f, 0.0f, 0.0f), limitPitch);
            // This could be expanded to look at an arbitrary target, now just look at a point in front
            Vector3 headWorldTarget = headNode.WorldPosition + headDir * new Vector3(0.0f, 0.0f, 1.0f);
            headNode.LookAt(headWorldTarget, new Vector3(0.0f, 1.0f, 0.0f), TransformSpace.World);
            // Correct head orientation because LookAt assumes Z = forward, but the bone has been authored differently (Y = forward)
            headNode.Rotate(new Quaternion(0.0f, 90.0f, 90.0f), TransformSpace.Local);

            if (firstPerson)
            {
                CameraNode.Position = headNode.WorldPosition + rot * new Vector3(0.0f, 0.15f, 0.2f);
                CameraNode.Rotation = dir;
            }
            else
            {
                // Third person camera: position behind the character
                Vector3 aimPoint = characterNode.Position + rot * new Vector3(0.0f, 1.7f, 0.0f);

                // Collide camera ray with static physics objects (layer bitmask 2) to ensure we see the character properly
                Vector3 rayDir = dir * new Vector3(0f, 0f, -1f);
                float rayDistance = touch != null ? touch.CameraDistance : Global.CameraInitialDist;

                PhysicsRaycastResult result = new PhysicsRaycastResult();
                scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, new Ray(aimPoint, rayDir), rayDistance, 2);
                if (result.Body != null)
                    rayDistance = Math.Min(rayDistance, result.Distance);
                rayDistance = MathHelper.Clamp(rayDistance, Global.CameraMinDist, Global.CameraMaxDist);

                CameraNode.Position = aimPoint + rayDir * rayDistance;
                CameraNode.Rotation = dir;
            }
        }

        protected override void OnUpdate(float timeStep)
        {
            Input input = Input;

            if (LocalCharacter != null)
            {
                // Clear previous controls
                LocalCharacter.Controls.Set(Global.CtrlForward | Global.CtrlBack | Global.CtrlLeft | Global.CtrlRight | Global.CtrlJump, false);

                UpdateJoystickInputs(LocalCharacter.Controls);

                // Update controls using keys
                if (UI.FocusElement == null)
                {
                    if (touch == null || !touch.UseGyroscope)
                    {
                        LocalCharacter.Controls.Set(Global.CtrlForward, input.GetKeyDown(Key.W));
                        LocalCharacter.Controls.Set(Global.CtrlBack, input.GetKeyDown(Key.S));
                        LocalCharacter.Controls.Set(Global.CtrlLeft, input.GetKeyDown(Key.A));
                        LocalCharacter.Controls.Set(Global.CtrlRight, input.GetKeyDown(Key.D));
                    }

                    if (isMobile == false)
                    {
                        LocalCharacter.Controls.Set(Global.CtrlJump, input.GetKeyDown(Key.Space));
                    }

                    // Add character yaw & pitch from the mouse motion or touch input
                    if (TouchEnabled)
                    {
                        for (uint i = 0; i < input.NumTouches; ++i)
                        {
                            TouchState state = input.GetTouch(i);
                            if (state.TouchedElement == null)    // Touch on empty space
                            {
                                Camera camera = CameraNode.GetComponent<Camera>();
                                if (camera == null)
                                    return;

                                var graphics = Graphics;
                                LocalCharacter.Controls.Yaw += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.X;
                                LocalCharacter.Controls.Pitch += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.Y;
                            }
                        }
                    }
                    else
                    {
                        LocalCharacter.Controls.Yaw += (float)input.MouseMove.X * Global.YawSensitivity;
                        LocalCharacter.Controls.Pitch += (float)input.MouseMove.Y * Global.YawSensitivity;
                    }
                    // Limit pitch
                    LocalCharacter.Controls.Pitch = MathHelper.Clamp(LocalCharacter.Controls.Pitch, -80.0f, 80.0f);

                    // Switch between 1st and 3rd person
                    if (input.GetKeyPress(Key.F))
                        firstPerson = !firstPerson;

                    // Turn on/off gyroscope on mobile platform
                    if (touch != null && input.GetKeyPress(Key.G))
                        touch.UseGyroscope = !touch.UseGyroscope;
                }

                // Set rotation already here so that it's updated every rendering frame instead of every physics frame
                if (LocalCharacter != null)
                    LocalCharacter.Node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, LocalCharacter.Controls.Yaw);
            }
        }


        public void UpdateJoystickInputs(Controls controls)
        {
            JoystickState joystick;
            if (screenJoystickIndex != -1 && Input.GetJoystick(screenJoystickIndex, out joystick))
            {
                controls.Set(Global.CtrlJump, joystick.GetButtonDown(JoystickState.Button_A));
                controls.ExtraData["axis_0"] = new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
            }
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();

            // Create scene subsystem components
            scene.CreateComponent<Octree>();
            physicsWorld = scene.CreateComponent<PhysicsWorld>();

            // Create camera and define viewport. We will be doing load / save, so it's convenient to create the camera outside the scene,
            // so that it won't be destroyed and recreated, and we don't have to redefine the viewport on load
            CameraNode = new Node();
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));

            // Create static scene content. First create a zone for ambient lighting and fog control
            Node zoneNode = scene.CreateChild("Zone");
            Zone zone = zoneNode.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));

            // Create a directional light with cascaded shadow mapping
            Node lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.3f, -0.5f, 0.425f));
            Light light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);
            light.SpecularIntensity = 0.5f;

            // Create the floor object
            Node floorNode = scene.CreateChild("Floor");
            floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
            floorNode.Scale = new Vector3(200.0f, 1.0f, 200.0f);
            StaticModel sm = floorNode.CreateComponent<StaticModel>();
            sm.Model = cache.GetModel("Models/Box.mdl");
            sm.SetMaterial(cache.GetMaterial("Materials/Stone.xml"));

            RigidBody body = floorNode.CreateComponent<RigidBody>();
            // Use collision layer bit 2 to mark world scenery. This is what we will raycast against to prevent camera from going
            // inside geometry
            body.CollisionLayer = 2;
            CollisionShape shape = floorNode.CreateComponent<CollisionShape>();
            shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
        }

        Node CreateCharacter(bool isRemote = false)
        {
            var cache = ResourceCache;

            Node objectNode = scene.CreateChild("Jack");
   
            objectNode.Position = new Vector3(NextRandom(-10.0f, 10.0f), 5.0f, NextRandom(-10.0f, 10.0f));

            // spin node
            Node adjustNode = objectNode.CreateChild("AdjNode");
            adjustNode.Rotation = (new Quaternion(0, 180, 0));

            // Create the rendering component + animation controller
            AnimatedModel obj = adjustNode.CreateComponent<AnimatedModel>();
            obj.Model = cache.GetModel("Models/Mutant/Mutant.mdl");
            obj.SetMaterial(cache.GetMaterial("Models/Mutant/Materials/mutant_M.xml"));
            obj.CastShadows = true;
            adjustNode.CreateComponent<AnimationController>();

            // Set the head bone for manual control
            obj.Skeleton.GetBoneSafe("Mutant:Head").Animated = false;

            // Create rigidbody, and set non-zero mass so that the body becomes dynamic
            RigidBody body = objectNode.CreateComponent<RigidBody>();
            body.CollisionLayer = 1;
            body.Kinematic = true;
            body.Trigger = true;

            // Set zero angular factor so that physics doesn't turn the character on its own.
            // Instead we will control the character yaw manually
            body.SetAngularFactor(Vector3.Zero);

            // Set the rigidbody to signal collision also when in rest, so that we get ground collisions properly
            body.CollisionEventMode = CollisionEventMode.Always;

            // Set a capsule shape for collision
            CollisionShape shape = objectNode.CreateComponent<CollisionShape>();
            shape.SetCapsule(0.8f, 1.8f, new Vector3(0.0f, 0.9f, 0.0f), Quaternion.Identity);

            // Create the character logic component, which takes care of steering the rigidbody
            // Remember it so that we can set the controls. Use a WeakPtr because the scene hierarchy already owns it
            // and keeps it alive as long as it's not removed from the hierarchy
            if (isRemote == false)
            {
                // creating local character
                LocalCharacter = objectNode.CreateComponent<LocalKinematicCharacter>();
            }
            else
            {
                // creating remote network character
                objectNode.CreateComponent<RemoteKinematicCharacter>();
            }

            return objectNode;
        }


        /// <summary>
        /// Called when a MatchmakerMatched event is received from the Nakama server.
        /// </summary>
        /// <param name="matched">The MatchmakerMatched data.</param>
        private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matched)
        {
            // Cache a reference to the local user.
            localUser = matched.Self.Presence;

            UpdateInfoText("RECEIVED MATCH PRESENSE");

            // Join the match.
            var match = await Global.NakamaConnection.Socket.JoinMatchAsync(matched);
            
            // Cache a reference to the current match.
            Global.currentMatch = match;

            // Spawn a player instance for each connected user , should be done on the Main Urho Thread.
            foreach (var user in match.Presences)
            {
                InvokeOnMain(() => SpawnPlayer(match.Id,user));
            }



        }

        /// <summary>
        /// Called when a player/s joins or leaves the match.
        /// </summary>
        /// <param name="matchPresenceEvent">The MatchPresenceEvent data.</param>
        private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
        {
            
            // For each new user that joins, spawn a player for them.
            foreach (var user in matchPresenceEvent.Joins)
            {
                LogSharp.Debug("OnReceivedMatchPresence  MatchId:" + matchPresenceEvent.MatchId);
                SpawnPlayer(matchPresenceEvent.MatchId, user);
            }

            // For each player that leaves, despawn their player.
            foreach (var user in matchPresenceEvent.Leaves)
            {
                if (players.ContainsKey(user.SessionId))
                {
                    players[user.SessionId].Remove();
                    players.Remove(user.SessionId);
                    UpdateInfoText("REMOTE USER LEFT : " + user.Username);
                }
            }
        }


        private void SpawnPlayer(string matchId, IUserPresence user)
        {
             var isLocal = user.SessionId == localUser.SessionId;

            // If the player has already been spawned, return early.
            if (players.ContainsKey(user.SessionId))
            {
                return;
            }

            if (!isLocal)
            {
                UpdateInfoText("REMOTE USER JOINED  : " + user.Username);

                var player = CreateCharacter(true);
                player.GetComponent<RemoteKinematicCharacter>().SetNetWorkData (new RemotePlayerNetworkData
                {
                    MatchId = matchId,
                    User = user
                });
                    
                // Add the player to the players array.
                 players.Add(user.SessionId, player);
            }
            else
            {
                 players.Add(user.SessionId, localPlayer);
            }


            // TBD ELI 
            //Send local player name to remote players , this is an hack  
            // For some reason the the  Nakama.Client doesn't forward the actual UserName to remote clients (doesn;t work on some platforms)
            this.LocalCharacter.SendPlayerName();
       

        }


        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">1st/3rd</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"F\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Jump</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"SPACE\" />" +
            "        </element>" +
            "    </add>" +
            "</patch>";
    }

}