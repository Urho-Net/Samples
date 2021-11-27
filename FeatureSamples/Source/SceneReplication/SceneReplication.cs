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
using Urho.Physics;
using Urho.Gui;
using Urho.Network;
using Urho.Resources;
using System.Collections.Generic;
using System;

namespace SceneReplication
{
    public class SceneReplication : Sample
    {
        protected Network Network;
        Camera camera;

        const short ServerPort = 2345;
        UIElement buttonContainer;
        /// Server address / chat message line editor element.
        LineEdit textEdit;
        /// Connect button.
        Button connectButton;
        /// Disconnect button.
        Button disconnectButton;
        /// Start server button.
        Button startServerButton;


        uint clientObjectID_;

        StringHash E_CLIENTOBJECTID = new StringHash("ClientObjectID");
        StringHash P_ID = new StringHash("ID");

        const uint CTRL_FORWARD = 1;
        const uint CTRL_BACK = 2;
        const uint CTRL_LEFT = 4;
        const uint CTRL_RIGHT = 8;


        Dictionary<Connection, Node> serverObjects_ = new Dictionary<Connection, Node>();

        [Preserve]
        public SceneReplication() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            Network = Application.Network;
            
            Input.SetMouseVisible(true, false);
            if (IsMobile)
            {
                RemoveScreenJoystick();
            }

            CreateUI();
            CreateScene();
            SimpleCreateInstructionsWithWasd("This is a demo of a simple client/server application\nSynchronizing a scene between connected devices\nEnter server IP bellow and press \"Connect\" \n  To connect to a Wireless LAN Server \nOr press \"Start Server\" to Start WLAN server\n" +
            "All devices including the server must be on the same Wireless LAN" +
            "\nTo find out server IP type ifconfig/ipconfig \n  From a command shell on the same device that runs the server" +
            "\nTo find out server IP running on Android" + "\n    go to Settings->WI-FI->Additional settings" +
            "\nUsually WLAN server IP starts with 192.x.x.x", Color.Black);
            SetupViewport();
            SubscribeToEvents();
        }

        protected override void Stop()
        {
            UnSubscribeFromEvents();

            HandleDisconnect(new ReleasedEventArgs());
            base.Stop();
        }

        void CreateUI()
        {

            var graphics = Graphics;
            UIElement root = UI.Root;
            var cache = ResourceCache;
            XmlFile uiStyle = cache.GetXmlFile("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);

            Font font = cache.GetFont("Fonts/Anonymous Pro.ttf");

            buttonContainer = new UIElement();
            root.AddChild(buttonContainer);
            buttonContainer.SetFixedSize(graphics.Width, 60);
            buttonContainer.SetPosition(0, graphics.Height - 60);
            buttonContainer.LayoutMode = LayoutMode.Horizontal;

            textEdit = new LineEdit();
            textEdit.SetStyleAuto(null);
            textEdit.TextElement.SetFont(font, 24);
            // TBD ELI , debug only
            // textEdit.TextElement.Value = "192.168.1.110";
            buttonContainer.AddChild(textEdit);

            connectButton = CreateButtonLocal("Connect", 180);
            disconnectButton = CreateButtonLocal("Disconnect", 200);
            startServerButton = CreateButtonLocal("Start Server", 220);

            UpdateButtons();

            Input.SetMouseVisible(true);

        }

        void SubscribeToEvents()
        {

            //user event
            SubscribeToEvent(E_CLIENTOBJECTID, HandleClientObjectID);

            // Subscribe HandlePostUpdate() method for processing update events. Subscribe to PostUpdate instead
            // of the usual Update so that physics simulation has already proceeded for the frame, and can
            // accurately follow the object with the camera
            Engine.PostUpdate += HandlePostUpdate;

            scene.GetComponent<PhysicsWorld>().PhysicsPreStep += HandlePhysicsPreStep;

            connectButton.Released += HandleConnect;
            disconnectButton.Released += HandleDisconnect;
            startServerButton.Released += HandleStartServer;

            Network.ServerConnected += HandleServerConnected;
            Network.ServerDisconnected += HandleServerDisconnected;
            Network.ConnectFailed += HandleConnectFailed;

            Network.ClientConnected += HandleClientConnected;
            Network.ClientDisconnected += HandleClientDisconnected;

            Network.RegisterRemoteEvent(E_CLIENTOBJECTID);

        }

        void UnSubscribeFromEvents()
        {
            UnSubscribeFromEvent(E_CLIENTOBJECTID);

            Engine.PostUpdate -= HandlePostUpdate;

            scene.GetComponent<PhysicsWorld>().PhysicsPreStep -= HandlePhysicsPreStep;

            connectButton.Released -= HandleConnect;
            disconnectButton.Released -= HandleDisconnect;
            startServerButton.Released -= HandleStartServer;

            Network.ServerConnected -= HandleServerConnected;
            Network.ServerDisconnected -= HandleServerDisconnected;
            Network.ConnectFailed -= HandleConnectFailed;

            Network.ClientConnected -= HandleClientConnected;
            Network.ClientDisconnected -= HandleClientDisconnected;

            Network.UnregisterRemoteEvent(E_CLIENTOBJECTID);

        }


        private void HandleConnectFailed(ConnectFailedEventArgs obj)
        {
            UpdateButtons();
        }

        private void HandleStartServer(ReleasedEventArgs obj)
        {

            Network.StartServer((ushort)ServerPort);
            UpdateButtons();
        }

        private void HandleDisconnect(ReleasedEventArgs obj)
        {
            var network = Network;
            Connection serverConnection = network.ServerConnection;
            // If we were connected to server, disconnect
            if (serverConnection != null)
            {
                serverConnection.Disconnect();
                scene.Clear(true, false);
                clientObjectID_ = 0;
            }
            // Or if we were running a server, stop it
            else if (network.ServerRunning)
            {
                network.StopServer();
                scene.Clear(true, false);
            }

            UpdateButtons();

        }

        private void HandleConnect(ReleasedEventArgs obj)
        {

            string address = textEdit.Text.Trim();
            if (string.IsNullOrEmpty(address))
                address = "localhost"; // Use localhost to connect if nothing else specified
                                       // Empty the text edit after reading the address to connect to
            textEdit.Text = string.Empty;

            // Connect to server, specify scene to use as a client for replication
            clientObjectID_ = 0; // Reset own object ID from possible previous connection

            Network.Connect(address, ServerPort, scene);

            UpdateButtons();

        }



        void CreateScene()
        {
            scene = new Scene();
            var cache = ResourceCache;


            scene.CreateComponent<Octree>(CreateMode.Local);
            scene.CreateComponent<PhysicsWorld>(CreateMode.Local);


            var lightNode = scene.CreateChild("DirectionalLight", CreateMode.Local);
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f)); // The direction vector does not need to be normalized
            var light = lightNode.CreateComponent<Light>(CreateMode.Local);
            light.LightType = LightType.Directional;


            var skyNode = scene.CreateChild("Sky", CreateMode.Local);
            skyNode.SetScale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>(CreateMode.Local);
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));


            // Create a "floor" consisting of several tiles. Make the tiles physical but leave small cracks between them
            for (int y = -20; y <= 20; ++y)
            {
                for (int x = -20; x <= 20; ++x)
                {
                    var floorNode = scene.CreateChild("FloorTile", CreateMode.Local);
                    floorNode.Position = new Vector3(x * 20.2f, -0.5f, y * 20.2f);
                    floorNode.Scale = new Vector3(20.0f, 1.0f, 20.0f);
                    var floorObject = floorNode.CreateComponent<StaticModel>(CreateMode.Local);
                    floorObject.Model = cache.GetModel("Models/Box.mdl");
                    floorObject.Material = cache.GetMaterial("Materials/Stone.xml");

                    var body = floorNode.CreateComponent<RigidBody>(CreateMode.Local);
                    body.Friction = 1.0f;
                    var shape = floorNode.CreateComponent<CollisionShape>(CreateMode.Local);
                    shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
                }
            }

            CameraNode = scene.CreateChild("camera", CreateMode.Local);
            camera = CameraNode.CreateComponent<Camera>(CreateMode.Local);
            camera.FarClip = 300.0f;

            CameraNode.Position = new Vector3(0, 5, 0);
        }

        void SetupViewport()
        {
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));
        }


        void HandlePostUpdate(PostUpdateEventArgs arg)
        {
            MoveCamera(arg.TimeStep);
        }

        protected void MoveCamera(float timeStep, float moveSpeed = 10.0f)
        {
            const float mouseSensitivity = .1f;

            if (UI.FocusElement != null)
                return;

            var mouseMove = Input.MouseMove;
            if (Network.ServerRunning || Network.ServerConnection != null)
            {
                Yaw += mouseSensitivity * mouseMove.X;
                Pitch += mouseSensitivity * mouseMove.Y;
                Pitch = MathHelper.Clamp(Pitch, -90, 90);

                CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
            }

            if (clientObjectID_ != 0)
            {
                var ballNode = scene.GetNode(clientObjectID_);
                if (ballNode != null)
                {
                    const float CAMERA_DISTANCE = 5.0f;

                    // Move camera some distance away from the ball
                    CameraNode.Position = (ballNode.Position + CameraNode.Rotation * Vector3.Back * CAMERA_DISTANCE);

                }
            }

        }

        Button CreateButtonLocal(string text, int width)
        {
            var cache = ResourceCache;
            Font font = cache.GetFont("Fonts/Anonymous Pro.ttf");

            Button button = new Button();
            buttonContainer.AddChild(button);
            button.SetStyleAuto(null);
            button.SetFixedHeight(60);
            button.SetFixedWidth(width);

            var buttonText = new Text();
            button.AddChild(buttonText);
            buttonText.SetFont(font, 24);
            buttonText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);

            buttonText.Value = text;

            return button;
        }

        Node CreateControllableObject()
        {
            var cache = ResourceCache;

            // Create the scene node & visual representation. This will be a replicated object
            var ballNode = scene.CreateChild("Ball");
            ballNode.Position = new Vector3(NextRandom(40.0f) - 20.0f, 5.0f, NextRandom(40.0f) - 20.0f);
            ballNode.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            var ballObject = ballNode.CreateComponent<StaticModel>();
            ballObject.Model = cache.GetModel("Models/Sphere.mdl");
            ballObject.Material = cache.GetMaterial("Materials/StoneSmall.xml");

            // Create the physics components
            var body = ballNode.CreateComponent<RigidBody>();
            body.Mass = 1.0f;
            body.Friction = 1.0f;
            // In addition to friction, use motion damping so that the ball can not accelerate limitlessly
            body.LinearDamping = 0.5f;
            body.AngularDamping = 0.5f;
            var shape = ballNode.CreateComponent<CollisionShape>();
            shape.SetSphere(1.0f, Vector3.Zero, Quaternion.Identity);

            // Create a random colored point light at the ball so that can see better where is going
            var light = ballNode.CreateComponent<Light>();
            light.Range = 3.0f;
            light.Color =
                new Color(0.5f + ((uint)NextRandom() & 1u) * 0.5f, 0.5f + ((uint)NextRandom() & 1u) * 0.5f, 0.5f + ((uint)NextRandom() & 1u) * 0.5f);

            return ballNode;
        }


        void UpdateButtons()
        {
            var network = Network;
            Connection serverConnection = network.ServerConnection;
            bool serverRunning = network.ServerRunning;

            if (IsMobile)
            {
                if (serverConnection != null)
                {
                    CreateScreenJoystick();
                }
                else
                {
                    RemoveScreenJoystick();
                }
            }

            if (connectButton != null)
            {
                connectButton.Visible = serverConnection == null && !serverRunning;
            }

            if (disconnectButton != null)
            {
                disconnectButton.Visible = serverConnection != null || serverRunning;
            }

            if (startServerButton != null)
            {
                startServerButton.Visible = serverConnection == null && !serverRunning;
            }
        }

        void HandleClientDisconnected(ClientDisconnectedEventArgs args)
        {
            // When a client disconnects, remove the controlled object
            var connection = args.Connection;
            Node obj;
            if (serverObjects_.TryGetValue(connection, out obj))
            {
                if (obj != null)
                    obj.Remove();
            }

            serverObjects_.Remove(connection);
            UpdateButtons();
        }

        void HandleClientConnected(ClientConnectedEventArgs args)
        {
            var newConnection = args.Connection;
            newConnection.Scene = scene;


            // Then create a controllable object for that client
            var newObject = CreateControllableObject();
            serverObjects_[newConnection] = newObject;

            // Finally send the object's node ID using a remote event
            DynamicMap remoteEventData = new DynamicMap();
            remoteEventData[P_ID] = newObject.ID;
            newConnection.SendRemoteEvent(E_CLIENTOBJECTID, true, remoteEventData);

            UpdateButtons();

        }

        void HandleServerDisconnected(ServerDisconnectedEventArgs arg)
        {
            UpdateButtons();
        }

        void HandleServerConnected(ServerConnectedEventArgs args)
        {
            UpdateButtons();
        }

        void HandleClientObjectID(UrhoEventArgs args)
        {
            clientObjectID_ = args.EventData[P_ID];
        }

        /* This function is different on the client and server. The client collects controls (WASD controls + yaw angle)
         and sets them to its server connection object, so that they will be sent to the server automatically at a
         fixed rate, by default 30 FPS. The server will actually apply the controls (authoritative simulation.)*/
        void HandlePhysicsPreStep(PhysicsPreStepEventArgs args)
        {
            var ui = UI;
            var input = Input;
            Controls controls = new Controls();
            var network = Network;
            Connection serverConnection = network.ServerConnection;
            bool serverRunning = network.ServerRunning;

            controls.Yaw = Yaw;

            if (serverConnection != null) // Client: collect controls
            {
                if (IsMobile)
                {
                    Vector2 axis_0 = GetJoystickAxisInput();

                    controls.Set(CTRL_FORWARD, axis_0.Y < -0.5);
                    controls.Set(CTRL_BACK, axis_0.Y > 0.5);
                    controls.Set(CTRL_LEFT, axis_0.X < -0.5);
                    controls.Set(CTRL_RIGHT, axis_0.X > 0.5);

                }

                if (!IsMobile && UI.FocusElement == null)
                {

                    controls.Set(CTRL_FORWARD, input.GetKeyDown(Key.W));
                    controls.Set(CTRL_BACK, input.GetKeyDown(Key.S));
                    controls.Set(CTRL_LEFT, input.GetKeyDown(Key.A));
                    controls.Set(CTRL_RIGHT, input.GetKeyDown(Key.D));

                }

                serverConnection.Controls = controls;

                // In case the server wants to do position-based interest management using the NetworkPriority components, we should also
                // tell it our observer (camera) position. In this sample it is not in use, but eg. the NinjaSnowWar game uses it
                serverConnection.Position = (CameraNode.Position);

            }
            else if (serverRunning) // Server: apply controls to client objects
            {
                var connections = network.GetClientConnections();

                foreach (var connection in connections)
                {
                    Node ballNode = serverObjects_[connection];
                    if (ballNode == null)
                        continue;

                    var body = ballNode.GetComponent<RigidBody>();

                    // Get the last controls sent by the client
                    controls = connection.Controls;
                    // Torque is relative to the forward vector
                    Quaternion rotation = new Quaternion(0.0f, controls.Yaw, 0.0f);

                    const float MOVE_TORQUE = 3.0f;
                    if ((controls.Buttons & CTRL_FORWARD) != 0)
                        body.ApplyTorque(rotation * Vector3.Right * MOVE_TORQUE);
                    if ((controls.Buttons & CTRL_BACK) != 0)
                        body.ApplyTorque(rotation * Vector3.Left * MOVE_TORQUE);
                    if ((controls.Buttons & CTRL_LEFT) != 0)
                        body.ApplyTorque(rotation * Vector3.Forward * MOVE_TORQUE);
                    if ((controls.Buttons & CTRL_RIGHT) != 0)
                        body.ApplyTorque(rotation * Vector3.Back * MOVE_TORQUE);

                }
            }
        }

        public Vector2 GetJoystickAxisInput()
        {
            Vector2 axis_0 = new Vector2();
            JoystickState joystick;
            if (screenJoystickIndex != -1 && Input.GetJoystick(screenJoystickIndex, out joystick))
            {
                axis_0 = new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
            }

            return axis_0;
        }

    }
}