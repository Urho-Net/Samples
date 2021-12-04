// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2020 the Urho3D project.
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
using Urho;
using Urho.Gui;
using Urho.Resources;
using Urho.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

using StaticScene;
using System.Collections;

namespace UrhoNetSamples
{
    public class UrhoNetSamples : SampleApplication
    {

        ListView listView;

        public static bool IsMobile
        {
            get{

                if (Application.Platform == Platforms.iOS || Application.Platform == Platforms.Android)
                    return true;

                if (Application.Platform == Platforms.Web)
                {
                    string UserAgent = Environment.GetEnvironmentVariable("UserAgent");
                    if (UserAgent != null)
                    {
                        var mobileStrings = new List<string> { "Android", "iPhone", "iPad","Mobile","webOS","iPod","BlackBerry","Windows Phone"};
                        if(mobileStrings.Any(x => UserAgent.Contains(x,StringComparison.CurrentCultureIgnoreCase)))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        Type[] samples;

        Dictionary<string, Type> samplesList = new Dictionary<string, Type>();

        public class TypeComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                return (new CaseInsensitiveComparer()).Compare(x.ToString(), y.ToString());
            }
        }



        public UrhoNetSamples() : base(new ApplicationOptions(assetsFolder: "Data;CoreData;Data/FlappyUrho;Data/MaterialEffects;;Data/Parallax;Data/Racer2D") { ResizableWindow = true }) { }



        protected override void Start()
        {
            base.Start();

            Log.LogLevel = LogLevel.Info;
            
            Input.KeyDown += HandleKeyDown;

            if (IsMobile)
            {
                Input.SetScreenJoystickVisible(screenJoystickIndex, false);
                if (Application.Platform == Platforms.Web)
                    Input.TouchEmulation = true;
            }

            CreateUI();

            FindAvailableSamples();

            PopulateSamplesList();
        }


        void CreateUI()
        {
            XmlFile uiStyle = ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            UI.Root.SetDefaultStyle(uiStyle);


            listView = UI.Root.CreateChild<ListView>(new StringHash("ListView"));
            listView.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            listView.Size = new IntVector2(Graphics.Width, Graphics.Height);
            listView.SetStyleAuto();
            listView.SetFocus(true);
            Input.SetMouseVisible(true);

            UI.Root.Resized += OnUIResized;

        }

        private void OnUIResized(ResizedEventArgs obj)
        {
            listView.Size = new IntVector2(Graphics.Width, Graphics.Height);
        }

        string ExtractSampleName(Type sample)
        {
            return sample.ToString().Split('.')[1];
        }
        void FindAvailableSamples()
        {
            samples = typeof(Sample).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Sample)) && t != typeof(Sample)).ToArray();

            Array.Sort(samples, new TypeComparer());

            foreach (var sample in samples)
            {
                string SampleName = ExtractSampleName(sample);

                // PBRMaterials doesn't work well on mobiles
                if ((Application.Platform == Platforms.Web || IsMobile) && SampleName == "PBRMaterials") continue;

                if(Application.Platform == Platforms.Web)
                {
                    // TBD ELI , Networking is not supported on Browser
                    if(SampleName == "Chat" || SampleName == "SceneReplication")continue;
                }

                samplesList[SampleName] = sample;
            }

        }

        void PopulateSamplesList()
        {
            foreach (var sample in samples)
            {
                string SampleName = ExtractSampleName(sample);

                // PBRMaterials doesn't work well on mobiles
                if ((Application.Platform == Platforms.Web || IsMobile ) && SampleName == "PBRMaterials") continue;

                if (Application.Platform == Platforms.Web)
                {
                    // TBD ELI , Networking is not supported on Browser
                    if (SampleName == "Chat" || SampleName == "SceneReplication") continue;
                }

                ListAddSampleEntry(SampleName);
            }
        }

        void ListAddSampleEntry(string name)
        {
            Button button = new Button();
            button.MinHeight = 80;
            button.SetStyleAuto();
            button.Name = name;

            button.Released += OnEntrySelected;

            var title = button.CreateChild<Text>(new StringHash("Text"));
            title.SetAlignment(HorizontalAlignment.Center, VerticalAlignment.Center);
            title.Value = name;
            title.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 40);

            listView.AddItem(button);
        }

        private void OnEntrySelected(ReleasedEventArgs obj)
        {
            Button button = obj.Element as Button;
            string name = button?.Name;

            currentSample = (Sample)Activator.CreateInstance(samplesList[name]);

            if (currentSample != null)
            {
                UI.Root.RemoveChild(listView);
                listView = null;
                UI.Root.Resized -= OnUIResized;
                Input.SetMouseVisible(false);
                Input.SetMouseMode(MouseMode.Relative);
                if (Application.Platform != Platforms.Web)
                    currentSample.RequestToExit += SampleRequetedToExit;
                currentSample.Run();
                currentSample.backButton.Released += OnBackButtonReleased;
                currentSample.infoButton.Released += OnInfoButttonReleased;
                Graphics.WindowTitle = name;
            }

        }

        private void SampleRequetedToExit()
        {
            ExitSample();
        }

        private void OnInfoButttonReleased(ReleasedEventArgs obj)
        {
            currentSample.ToggleInfo();
        }

        private void OnBackButtonReleased(ReleasedEventArgs obj)
        {
            ExitSample();
        }

        void ExitSample()
        {
            if (currentSample != null)
            {
                currentSample.backButton.Released -= OnBackButtonReleased;
                currentSample.infoButton.Released -= OnInfoButttonReleased;
                if (Application.Platform != Platforms.Web)
                    currentSample.RequestToExit -= SampleRequetedToExit;
                currentSample.Exit();
                currentSample.UnSubscribeFromAllEvents();
                currentSample.Dispose();
                currentSample = null;
                UI.Root.RemoveAllChildren();
                CreateUI();
                PopulateSamplesList();
                Graphics.WindowTitle = "Urho.Net Samples";
                Input.SetMouseVisible(true);
            }
        }
        void HandleKeyDown(KeyDownEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Esc:
                    if (currentSample != null)
                    {
                        ExitSample();
                    }
                    else
                    {
                        if (Application.Platform != Platforms.Web)
                            Exit();
                    }
                    return;
            }
        }
    }
}