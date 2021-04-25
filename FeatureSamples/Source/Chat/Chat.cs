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
using System.Collections.Generic;
using System.Text;
using Urho.Gui;
using Urho.IO;
using Urho.Network;
using Urho.Resources;
using System;

namespace Chat
{
    public class Chat : Sample
    {
        // Identifier for the chat network messages
        const int MsgChat = Protocol.msg_user + 0;
        // UDP port we will use
        const short ChatServerPort = 2345;

        /// Strings printed so far.
        List<string> chatHistory = new List<string>();
        /// Chat text element.
        Text chatHistoryText;
        /// Button container element.
        UIElement buttonContainer;
        /// Server address / chat message line editor element.
        LineEdit textEdit;
        /// Send button.
        Button sendButton;
        /// Connect button.
        Button connectButton;
        /// Disconnect button.
        Button disconnectButton;
        /// Start server button.
        Button startServerButton;

        Button cleanButton;


        public Chat() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }



        protected override void Start()
        {
            base.Start();
            Input.SetMouseVisible(true, false);
            CreateUI();
            SimpleCreateInstructions("This is a demo of a simple chat application\n"+
            "Allows sending/recieving text messages between devices\n"+
            "Enter server IP bellow and press \"Connect\" \n  "+
            "To connect to a Wireless LAN Server \nOr press \"Start Server\" to Start WLAN server\n" +
            "All devices including the server must be on the same Wireless LAN" +
            "\nTo find out server IP running on desktop type ifconfig/ipconfig \n  From a command shell on the same device that runs the server" +
             "\nTo find out server IP running on Android" + "\n    go to Settings->WI-FI->Additional settings" +
            "\nUsually WLAN server IP starts with 192.x.x.x");
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
            chatHistoryText = new Text();
            if (Graphics.Width <= 1440)
            {
                chatHistoryText.SetFont(font, 20);
            }
            else
            {
                chatHistoryText.SetFont(font, 30);
            }
            root.AddChild(chatHistoryText);

            buttonContainer = new UIElement();
            root.AddChild(buttonContainer);
            buttonContainer.SetFixedSize(graphics.Width, 60);
            buttonContainer.SetPosition(0, graphics.Height - 60);
            buttonContainer.LayoutMode = LayoutMode.Horizontal;

            textEdit = new LineEdit();
            textEdit.SetStyleAuto();
            buttonContainer.AddChild(textEdit);

            sendButton = CreateButton("Send", 140);
            connectButton = CreateButton("Connect", 180);
            disconnectButton = CreateButton("Disconnect", 200);
            startServerButton = CreateButton("Start Server", 220);

            cleanButton = CreateButton("Clear", 140);

            UpdateButtons();

            // No viewports or scene is defined. However, the default zone's fog color controls the fill color
            Renderer.DefaultZone.FogColor = new Color(0.0f, 0.0f, 0.1f);

            Input.SetMouseVisible(true);
        }

        void SubscribeToEvents()
        {

            textEdit.TextFinished += HandleTextFinishedSend;
            sendButton.Released += HandleSend;
            connectButton.Released += HandleConnect;
            disconnectButton.Released += HandleDisconnect;
            startServerButton.Released += HandleStartServer;

            cleanButton.Released += HandleClearScreen;

            Log.LogMessage += HandleLogMessage;
            Network.ServerConnected += HandleServerConnected;
            Network.ServerDisconnected += HandleServerDisconnected;
            Network.ConnectFailed += HandleConnectFailed;

            Network.ClientConnected += HandleClientConnected;
            Network.ClientDisconnected += HandleClientDisconnected;
        }

        void UnSubscribeFromEvents()
        {
            textEdit.TextFinished -= HandleTextFinishedSend;
            sendButton.Released -= HandleSend;
            connectButton.Released -= HandleConnect;
            disconnectButton.Released -= HandleDisconnect;
            startServerButton.Released -= HandleStartServer;

            cleanButton.Released -= HandleClearScreen;

            Log.LogMessage -= HandleLogMessage;
            Network.ServerConnected -= HandleServerConnected;
            Network.ServerDisconnected -= HandleServerDisconnected;
            Network.ConnectFailed -= HandleConnectFailed;

            Network.ClientConnected -= HandleClientConnected;
            Network.ClientDisconnected -= HandleClientDisconnected;
        }

        private void HandleClearScreen(ReleasedEventArgs obj)
        {
            chatHistory.Clear();
            chatHistoryText.Value = "";
        }

        private void HandleStartServer(ReleasedEventArgs obj)
        {
            Network.StartServer((ushort)ChatServerPort);

            UpdateButtons();
        }

        private void HandleDisconnect(ReleasedEventArgs obj)
        {
            var network = Network;
            Connection serverConnection = network.ServerConnection;
            // If we were connected to server, disconnect
            if (serverConnection != null)
                serverConnection.Disconnect(0);
            // Or if we were running a server, stop it
            else if (network.ServerRunning)
                network.StopServer();

            UpdateButtons();

        }

        private void HandleConnect(ReleasedEventArgs obj)
        {
            string address = textEdit.Text.Trim();
            if (string.IsNullOrEmpty(address))
                address = "localhost"; // Use localhost to connect if nothing else specified
                                       // Empty the text edit after reading the address to connect to
            textEdit.Text = string.Empty;

            // Connect to server, do not specify a client scene as we are not using scene replication, just messages.
            // At connect time we could also send identity parameters (such as username) in a VariantMap, but in this
            // case we skip it for simplicity
            Network.Connect(address, ChatServerPort, null);

            UpdateButtons();

        }

        private void HandleSend(ReleasedEventArgs obj)
        {
            SendMessage();
        }

        private void HandleTextFinishedSend(TextFinishedEventArgs obj)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            string text = textEdit.Text;
            if (string.IsNullOrEmpty(text))
                return; // Do not send an empty message

            Connection serverConnection = Network.ServerConnection;

            if (serverConnection != null)
            {
                // Send the chat message as in-order and reliable
                serverConnection.SendMessage(MsgChat, true, true, Encoding.UTF8.GetBytes(text));
                // Empty the text edit after sending
                textEdit.Text = string.Empty;
            }
        }

        private void HandleConnectFailed(ConnectFailedEventArgs obj)
        {
            UpdateButtons();
        }

        Button CreateButton(string text, int width)
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

        void ShowChatText(string row)
        {
            chatHistory.Add(row);
            string outText = string.Join("\n", chatHistory) + "\n";
            chatHistoryText.Value = outText;
        }

        void UpdateButtons()
        {
            var network = Network;
            Connection serverConnection = network.ServerConnection;
            bool serverRunning = network.ServerRunning;

            // Show and hide buttons so that eg. Connect and Disconnect are never shown at the same time
            sendButton.Visible = serverConnection != null;
            connectButton.Visible = serverConnection == null && !serverRunning;
            disconnectButton.Visible = serverConnection != null || serverRunning;
            startServerButton.Visible = serverConnection == null && !serverRunning;

            cleanButton.Visible = serverConnection != null || serverRunning;
        }

        void HandleLogMessage(LogMessageEventArgs args)
        {
            ShowChatText(args.Message);
        }

        void HandleClientDisconnected(ClientDisconnectedEventArgs args)
        {
            if (args.Connection != null)
            {
                args.Connection.NetworkMessage -= HandleNetworkMessage;
            }
        }
        void HandleClientConnected(ClientConnectedEventArgs args)
        {
            if (args.Connection != null)
            {
                args.Connection.NetworkMessage += HandleNetworkMessage;
            }
        }

        void HandleServerDisconnected(ServerDisconnectedEventArgs arg)
        {
            UpdateButtons();
            Connection connection = Network.ServerConnection;
            if (connection != null)
            {
                connection.NetworkMessage -= HandleNetworkMessage;
            }
        }
        void HandleServerConnected(ServerConnectedEventArgs args)
        {
            UpdateButtons();
            Connection connection = Network.ServerConnection;
            if (connection != null)
            {
                connection.NetworkMessage += HandleNetworkMessage;
            }
        }

        void HandleNetworkMessage(NetworkMessageEventArgs args)
        {
            int msgID = args.MessageID;
            if (msgID == MsgChat)
            {
                Connection connection = args.Connection;
                string address = connection.Address;
                ushort port = connection.Port;

                MemoryBuffer mb = new MemoryBuffer(args.Data);
                ShowChatText(address + ":" + port.ToString() + " => " + mb.GetString());

                if (Network.ServerRunning)
                {
                    Network.Broadcast(MsgChat, true, true, args.Data, 0);
                }
            }
        }

        protected override string JoystickLayoutPatch => JoystickLayoutPatches.Hidden;
    }
}

