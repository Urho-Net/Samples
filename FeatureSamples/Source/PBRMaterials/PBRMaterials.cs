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
using System;
using Urho.Resources;
using Urho;


namespace PBRMaterials
{
	public class PBRMaterials : Sample
	{

		[Preserve]
		public PBRMaterials() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

		protected override void Start()
		{

			base.Start();

			// Create the scene content
			CreateScene();

			// Setup the viewport for displaying the scene
			SetupViewport();
		}

		void CreateScene()
		{
			var cache = ResourceCache;
			scene = new Scene();

			scene.LoadXmlFromCache(cache, "Scenes/PBRExample.xml");

			// Create the camera (not included in the scene file)
			CameraNode = scene.CreateChild("Camera");
			CameraNode.CreateComponent<Camera>();

			// Set an initial position for the camera scene node above the plane
			CameraNode.Position = new Vector3(0.0f, 4.0f, -20.0f);
		}

		protected override void OnUpdate(float timeStep)
		{
			SimpleMoveCamera3D(timeStep);
		}

		void SetupViewport()
		{
			Viewport viewport = new Viewport(scene, CameraNode.GetComponent<Camera>(), null);
			Renderer.SetViewport(0, viewport);

			var effectRenderPath = viewport.RenderPath.Clone();
			effectRenderPath.Append(ResourceCache.GetXmlFile("PostProcess/BloomHDR.xml"));
			effectRenderPath.Append(ResourceCache.GetXmlFile("PostProcess/FXAA2.xml"));
			effectRenderPath.Append(ResourceCache.GetXmlFile("PostProcess/GammaCorrection.xml"));

			viewport.RenderPath = effectRenderPath;
		}
	}
}
