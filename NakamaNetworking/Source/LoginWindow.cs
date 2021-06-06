
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
using System;
using System.Linq;
using System.Net;
using Urho;
using Urho.Gui;
using Urho.Resources;

namespace NakamaNetworking
{
    public class LoginWindow
    {
        private Sample application;
        private LineEdit[] LineEditIPEntries = new LineEdit[4];
        private byte[] ByteIPEntries = new byte[4];


        private Window window = null;
        private string PlayerName = "";
        private int PlayerCount = 2 ;


        public LoginWindow(Sample sample)
        {
            this.application = sample;
            CreateUI();
        }

        public void Show()
        {
            // Invoke on Main UI thread
            Application.InvokeOnMain(() => {window.Visible = true;application.Input.SetMouseVisible(true);});
        }

        public void Hide()
        {
            // Invoke on Main UI thread
            Application.InvokeOnMain(() => {window.Visible = false;application.Input.SetMouseVisible(false);});
        }
        public void CreateUI()
        {
            var graphics = application.Graphics;
            UIElement root = application.UI.Root;
            var cache = application.ResourceCache;
            XmlFile uiStyle = cache.GetXmlFile("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);

            window = root.CreateWindow();
            window.SetStyleAuto();
            window.Resizable = false;
            window.Movable = false;
            window.Size = new IntVector2(graphics.Width / 2, graphics.Height / 2);
            window.Position = new IntVector2(graphics.Width / 4, graphics.Height / 4);
            window.LayoutMode = LayoutMode.Vertical;
            application.Input.SetMouseVisible(true);

            LineEdit PlayerNameLineEdit = CreatePlayerNameEntry(window, "Player name", "Player" + new Random().Next(100));
            PlayerNameLineEdit.TextChanged += OnPlayerNameChanged;

            DropDownList PlayerCountDropDownList = CreatePlayerCountDropDownListEntry(window, "Players count");
            PlayerCountDropDownList.ItemSelected += OnPlayCountSelected;

            CreateIPEditEntry(window, "Server IP");
            Button ConnectToServer = CreateButton(window, "Connect", 140);
            ConnectToServer.Released += OnConnectToServer;

        }

        private void OnPlayCountSelected(ItemSelectedEventArgs obj)
        {
           PlayerCount =  obj.Selection + 2;
        }

        private void OnPlayerNameChanged(TextChangedEventArgs obj)
        {
            PlayerName = obj.Text;
        }

        private async void OnConnectToServer(ReleasedEventArgs obj)
        {
            try
            {
                IPAddress ipAddress = new IPAddress(ByteIPEntries);
                application.UpdateInfoText("CONNECTING TO SERVER PLEASE WAIT...");
                await Global.NakamaConnection.Connect(ipAddress.ToString());
            }
            catch (Exception ex)
            {
                LogSharp.Error(ex.ToString());
                try
                {
                    application.UpdateInfoText("INVALID IP !!");
                    application.UpdateInfoText("CONNECTING TO LOCAL HOST PLEASE WAIT...");
                    await Global.NakamaConnection.Connect("127.0.0.1");
                }
                catch (Exception ex2)
                {
                    LogSharp.Error(ex2.ToString());
                }
            }
        }

        private static string GetNumbers(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }

        private int GetIPEntryIndex(LineEdit lineEdit)
        {
            for (int i = 0; i < 4; i++)
            {
                if (LineEditIPEntries[i] == lineEdit)
                {
                    return i;
                }
            }

            return -1;
        }
        private void OnIPEntryChanged(TextChangedEventArgs obj)
        {
            LineEdit lineEdit = obj.Element as LineEdit;


            // Make sure that this LineEdit is part of the IP Entries.
            int indexEntry = GetIPEntryIndex(lineEdit);
            if (indexEntry == -1) return;

            // Make sure that text contains only numbers
            lineEdit.Text = GetNumbers(obj.Text);

            // Make sure that text can be parsed to byte , otherwise erase it
            if (byte.TryParse(lineEdit.Text, out ByteIPEntries[indexEntry]) == false)
            {
                lineEdit.Text = "";
                ByteIPEntries[indexEntry] = 0;
            }

            // If entry contains 3 digits , move to the next entry
            if (lineEdit.Text.Length == 3)
            {

                if (indexEntry >= 0 && indexEntry < 3)
                {
                    indexEntry++;
                    LineEditIPEntries[indexEntry].SetFocus(true);
                }
            }

        }

        UIElement NewWindowEntry(Window window)
        {
            var container = new UIElement();
            container.LayoutMode = LayoutMode.Horizontal;
            window.AddChild(container);
            return container;
        }
        LineEdit CreatePlayerNameEntry(Window window, string text, string lineEditText)
        {
            var container = NewWindowEntry(window);
            Font font = application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf");
            var entryText = new Text();
            container.AddChild(entryText);
            entryText.SetFont(font, 24);
            entryText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            entryText.Value = text;

            LineEdit lineEdit = container.CreateLineEdit();
            lineEdit.SetStyleAuto();
            lineEdit.Text = lineEditText;
            PlayerName = lineEditText;
            return lineEdit;
        }

        void CreateIPEditEntry(Window window, string text)
        {
            var container = NewWindowEntry(window);
            Font font = application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf");
            var entryText = new Text();
            container.AddChild(entryText);
            entryText.SetFont(font, 24);
            entryText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            entryText.Value = text;

            for (int i = 0; i < 4; i++)
            {
                LineEdit lineEdit = container.CreateLineEdit();
                LineEditIPEntries[i] = lineEdit;
                lineEdit.SetStyleAuto();
                lineEdit.TextChanged += OnIPEntryChanged;
                if (i < 3)
                {
                    var dot = new Text();
                    container.AddChild(dot);
                    dot.SetFixedWidth(5);
                    dot.SetFont(font, 12);
                    dot.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Bottom);
                    dot.Value = ".";
                }
            }

            string SavedHostIP = PlayerPrefs.GetString("HostIP");

            if (SavedHostIP != "")
            {
                IPAddress ipAddress = IPAddress.Parse(SavedHostIP);
                Byte[] BytesIPAddress = ipAddress.GetAddressBytes();

                for (int i = 0; i < 4 && i < BytesIPAddress.Length ; i++)
                {
                    ByteIPEntries[i] = BytesIPAddress[i];
                    LineEditIPEntries[i].Text = ByteIPEntries[i].ToString();
                }
            }
            
           
        }

        DropDownList CreatePlayerCountDropDownListEntry(Window window, string text)
        {
            var container = NewWindowEntry(window);
            Font font = application.ResourceCache.GetFont("Fonts/Anonymous Pro.ttf");
            var entryText = new Text();
            container.AddChild(entryText);
            entryText.SetFont(font, 24);
            entryText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            entryText.Value = text;

            var dropDownList = container.CreateDropDownList();
            dropDownList.SetStyleAuto();

            for (int i = 2; i <= 8; i++)
            {
                var entry = new Text();
                dropDownList.AddItem(entry);
                entry.SetStyleAuto();
                entry.SetFont(font, 24);

                entry.Value = i.ToString() + " players";
            }
            
            PlayerCount = 2;


            int SavedPlayeCount = PlayerPrefs.GetInt("PlayerCount");
            if(SavedPlayeCount >= 2)
            {
                PlayerCount = SavedPlayeCount;
                dropDownList.Selection = (uint)(PlayerCount-2);
            }
           
            return dropDownList;
        }

        public int GetPlayerCount()
        {
            return PlayerCount;
        }

        public string GetPlayerName()
        {
            return PlayerName;
        }

        public string GetHostIP()
        {
            String HostIP = "";

            try
            {
                IPAddress ipAddress = new IPAddress(ByteIPEntries);
                HostIP = ipAddress.ToString();
            }
            catch (Exception ex)
            {
                LogSharp.Error(ex.ToString());
            }

            return HostIP;
        }

        Button CreateButton(Window window, string text, int width)
        {
            var cache = application.ResourceCache;
            Font font = cache.GetFont("Fonts/Anonymous Pro.ttf");

            var parent = NewWindowEntry(window);

            Button button = new Button();
            parent.AddChild(button);
            button.SetStyleAuto();
            button.SetFixedHeight(40);
            button.SetFixedWidth(width);

            var buttonText = new Text();
            button.AddChild(buttonText);
            buttonText.SetFont(font, 24);
            buttonText.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);

            buttonText.Value = text;
            return button;
        }

    }

}