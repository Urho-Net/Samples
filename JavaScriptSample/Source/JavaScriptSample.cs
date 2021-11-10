// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)

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
using Jint;
using Urho.IO;
using System.Reflection;
using System.Text;

namespace JavaScriptSample
{
	public class JavaScriptSample : Sample
	{
        private static Assembly UrhoDotNet = Assembly.Load(new AssemblyName("UrhoDotNet"));

		private Jint.Engine jintEngine = null;

		[Preserve]
		public JavaScriptSample() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

		protected override void Start ()
		{
			base.Start ();

			Log.LogLevel = LogLevel.Info;

            jintEngine = new Jint.Engine(cfg => cfg.AllowClr(UrhoDotNet));
			jintEngine.SetValue("Application", Application.Current);
			jintEngine.SetValue("Platform", Application.Platform);

			using (var javaScriptCode = ResourceCache.GetFile("JavaScript/JavaScriptSample.js"))
			{
				if(javaScriptCode.IsOpen())
				{
					var buf  = new byte [javaScriptCode.Size];
					javaScriptCode.Read(buf);
					string code = Encoding.UTF8.GetString(buf);
					jintEngine.Execute(code);
				}
			}

			// call JavaScript function CreateScene() located in JavaScriptSample.js
			jintEngine.Invoke("CreateScene");
			
        }

		

		protected override void OnUpdate(float timeStep)
		{
			// call JavaScript function OnUpdate(timeStep) located in JavaScriptSample.js
			jintEngine.Invoke("OnUpdate", timeStep);
		}
	}
}