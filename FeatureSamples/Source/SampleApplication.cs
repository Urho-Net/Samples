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

using System;
using System.Diagnostics;
using System.Globalization;
using Urho.Resources;
using Urho.Gui;
using Urho;

namespace UrhoNetSamples
{
	public class SampleApplication : Application
	{

		ResourceCache cache;

	
		protected int screenJoystickIndex;


		protected bool TouchEnabled { get; set; }



		protected Sample currentSample = null;


		[Preserve]
		protected SampleApplication(ApplicationOptions options = null) : base(options) {}

		static SampleApplication()
		{
			Urho.Application.UnhandledException += Application_UnhandledException1;
		}

		static void Application_UnhandledException1(object sender, Urho.UnhandledExceptionEventArgs e)
		{
			if (Debugger.IsAttached && !e.Exception.Message.Contains("BlueHighway.ttf"))
				Debugger.Break();
			e.Handled = true;
		}


		/// <summary>
		/// Joystick XML layout for mobile platforms
		/// </summary>
		protected virtual string JoystickLayoutPatch => string.Empty;

		protected override void Start ()
		{
	
			base.Start();
			if (Platform == Platforms.Android || 
				Platform == Platforms.iOS || 
				Options.TouchEmulation)
			{
				InitTouchInput();
			}
			Input.Enabled = true;

			SetWindowAndTitleIcon ();	
		}





		protected void SetWindowAndTitleIcon()
		{
			cache = ResourceCache;
			var icon = cache.GetImage("Textures/UrhoIcon.png");
			Graphics.SetWindowIcon(icon);
			Graphics.WindowTitle = "Urho.Net Samples";
		}


		protected void InitTouchInput()
		{
			TouchEnabled = true;
			var layout = ResourceCache.GetXmlFile("UI/ScreenJoystick_Samples.xml");
			if (!string.IsNullOrEmpty(JoystickLayoutPatch))
			{
				XmlFile patchXmlFile = new XmlFile();
				patchXmlFile.FromString(JoystickLayoutPatch);
				layout.Patch(patchXmlFile);
			}
			screenJoystickIndex = Input.AddScreenJoystick(layout, ResourceCache.GetXmlFile("UI/DefaultStyle.xml"));
			Input.SetScreenJoystickVisible(screenJoystickIndex, true);

			
		}
	}
}
