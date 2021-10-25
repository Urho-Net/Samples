// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
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
using System;
using Urho.Gui;
using Urho.IO;
using Urho.Resources;
using System.IO;
using System.Reflection;

namespace UIBuilder
{
	public class UIBuilder : Sample
	{
#if !_MOBILE_
		DynamicComponentManager dynamicComponentManager = null;
#endif
		Scene scene;

        bool isInitialized = false;

        bool  isMobile = (Application.Platform == Platforms.iOS || Application.Platform == Platforms.Android);

        IntVector2 screenSize = IntVector2.Zero;

		[Preserve]
        public UIBuilder() : base(new ApplicationOptions(assetsFolder: "Data;CoreData"){ Height=800,Width=400,ResizableWindow=true , Orientation = ApplicationOptions.OrientationType.Portrait}) { }

        // Desktop setting
        // public UIBuilder() : base(new ApplicationOptions(assetsFolder: "Data;CoreData") { Height = 400, Width = 800, ResizableWindow = true }) { }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
			string cwd = Directory.GetCurrentDirectory();

            string folderPath = cwd+"/Temp";
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name);
            if (!System.IO.File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

		protected override void Start ()
		{
			base.Start ();

            XmlFile uiStyle = ResourceCache.GetXmlFile("UI/DefaultStyle.xml");
            UI.Root.SetDefaultStyle(uiStyle);

            Log.LogLevel = LogLevel.Info;
            ResourceCache.AutoReloadResources = true;

            screenSize = Graphics.Size;

            Input.SetMouseVisible(true);

            VGRendering.LoadResources();

            // This is needed for Hot-Reload usecase on desktop only.
            if (!isMobile)
            {
                ResourceCache.AddResourceDir("Assets/Data", 0);
                ResourceCache.AddResourceDir("Assets/CoreData", 1);
                ResourceCache.AddResourceDir("Source/HotReload/Builder", 2);
                ResourceCache.AddResourceDir("Source/HotReload/UI", 3);

            }

            scene = new Scene();
		}

        void Init()
        {

#if _MOBILE_
            var uiDynamicBuilder = new UIDynamicBuilder();
            scene.AddComponent(uiDynamicBuilder);  
#else
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
            dynamicComponentManager = new DynamicComponentManager();
            dynamicComponentManager.Temporary = true;
            dynamicComponentManager.SetScene(scene);
#endif

        }

        
        protected override void Stop()
        {
			
#if !_MOBILE_
            dynamicComponentManager?.Stop();
            dynamicComponentManager = null;
#endif
        }

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
            if(isInitialized == false)
            {
                isInitialized = true;
                Init();
            }
            
#if !_MOBILE_
            if(screenSize != Graphics.Size)
            {
                screenSize = Graphics.Size;
                dynamicComponentManager.Recompile();
            }
#endif
		}
	}
}