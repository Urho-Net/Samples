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
using System;
using Urho.Urho2D;

namespace Urho2DParticles
{
	public class Urho2DParticle : Sample
	{

		Node particleNode;

		[Preserve]
		public Urho2DParticle() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

		protected override void Start()
		{
			base.Start();
			CreateScene();
			Input.SetMouseVisible(true, false);
			SimpleCreateInstructions("Use mouse/touch to move the particle.");
			SetupViewport();
			SubscribeToEvents();
		}

        protected override void Stop()
        {
			UnSubscribeFromEvents();
            base.Stop();
        }

        

		protected override void OnUpdate(float timeStep) {}

		void SubscribeToEvents()
		{
			Input.MouseMoved += OnMouseMoved;

			if (TouchEnabled)
				Input.TouchMove += OnTouchMove;
		}

		void UnSubscribeFromEvents()
		{
			Input.MouseMoved -= OnMouseMoved;

			if (TouchEnabled)
				Input.TouchMove -= OnTouchMove;
		}

        private void OnTouchMove(TouchMoveEventArgs obj)
        {
            HandleMouseMove(obj.X,obj.Y);
        }

        private void OnMouseMoved(MouseMovedEventArgs obj)
        {
            HandleMouseMove(obj.X,obj.Y);
        }

        void HandleMouseMove(int x, int y)
		{
			if (particleNode != null)
			{
				Camera camera = CameraNode.GetComponent<Camera>();
				particleNode.Position=(camera.ScreenToWorldPoint(new Vector3((float)x / Graphics.Width, (float)y / Graphics.Height, 10.0f)));
			}
		}

		void SetupViewport()
		{
			Renderer.SetViewport(0, new Viewport(Context, scene, CameraNode.GetComponent<Camera>(), null));
		}

		void CreateScene()
		{
			scene = new Scene();
			scene.CreateComponent<Octree>();

			// Create camera node
			CameraNode = scene.CreateChild("Camera");
			// Set camera's position
			CameraNode.Position = (new Vector3(0.0f, 0.0f, -10.0f));

			Camera camera = CameraNode.CreateComponent<Camera>();
			camera.Orthographic = true;

			var graphics = Graphics;
			camera.OrthoSize = (float)graphics.Height * Application.PixelSize;
			camera.Zoom=1.2f * Math.Min((float)graphics.Width / 1280.0f, (float)graphics.Height / 800.0f); // Set zoom according to user's resolution to ensure full visibility (initial zoom (1.2) is set for full visibility at 1280x800 resolution)

			var cache = ResourceCache;
			ParticleEffect2D particleEffect = cache.GetParticleEffect2D("Urho2D/sun.pex");
			if (particleEffect == null)
				return;

			particleNode = scene.CreateChild("ParticleEmitter2D");
			ParticleEmitter2D particleEmitter = particleNode.CreateComponent<ParticleEmitter2D>();
			particleEmitter.Effect=particleEffect;

			ParticleEffect2D greenSpiralEffect = cache.GetParticleEffect2D("Urho2D/greenspiral.pex");
			if (greenSpiralEffect == null)
				return;

			Node greenSpiralNode = scene.CreateChild("GreenSpiral");
			ParticleEmitter2D greenSpiralEmitter = greenSpiralNode.CreateComponent<ParticleEmitter2D>();
			greenSpiralEmitter.Effect=greenSpiralEffect;
		}

		protected override string JoystickLayoutPatch => JoystickLayoutPatches.Hidden;
	}
}
