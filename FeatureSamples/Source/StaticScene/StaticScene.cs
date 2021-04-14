using Urho;
using UrhoNetSamples;
using System;

namespace StaticScene
{
	public class StaticScene : Sample
	{
		Camera camera;

		[Preserve]
		public StaticScene() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

		protected override void Start ()
		{
			base.Start ();
			CreateScene ();
			SimpleCreateInstructionsWithWasd ("",Color.Black);
			SetupViewport ();
		}

		protected override void Stop ()
		{
			base.Stop ();
			camera.Dispose();
		}


		void CreateScene ()
		{
			scene = new Scene ();

			// Create the Octree component to the scene. This is required before adding any drawable components, or else nothing will
			// show up. The default octree volume will be from (-1000, -1000, -1000) to (1000, 1000, 1000) in world coordinates; it
			// is also legal to place objects outside the volume but their visibility can then not be checked in a hierarchically
			// optimizing manner
			scene.CreateComponent<Octree> ();


			// Create a child scene node (at world origin) and a StaticModel component into it. Set the StaticModel to show a simple
			// plane mesh with a "stone" material. Note that naming the scene nodes is optional. Scale the scene node larger
			// (100 x 100 world units)
			var planeNode = scene.CreateChild("Plane");
			planeNode.Scale = new Vector3 (100, 1, 100);
			var planeObject = planeNode.CreateComponent<StaticModel> ();
			planeObject.Model = ResourceCache.GetModel ("Models/Plane.mdl");
			planeObject.SetMaterial (ResourceCache.GetMaterial ("Materials/StoneTiled.xml"));


			// Create a directional light to the world so that we can see something. The light scene node's orientation controls the
			// light direction; we will use the SetDirection() function which calculates the orientation from a forward direction vector.
			// The light will use default settings (white light, no shadows)
			var lightNode = scene.CreateChild("DirectionalLight");
			lightNode.SetDirection (new Vector3(0.6f, -1.0f, 0.8f)); // The direction vector does not need to be normalized
			var light = lightNode.CreateComponent<Light>();
			light.LightType = LightType.Directional;


			// Create skybox. The Skybox component is used like StaticModel, but it will be always located at the camera, giving the
			// illusion of the box planes being far away. Use just the ordinary Box model and a suitable material, whose shader will
			// generate the necessary 3D texture coordinates for cube mapping
			var skyNode = scene.CreateChild("Sky");
			skyNode.SetScale(500.0f); // The scale actually does not matter
			var skybox = skyNode.CreateComponent<Skybox>();
			skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
			skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));


			// Create more StaticModel objects to the scene, randomly positioned, rotated and scaled. For rotation, we construct a
			// quaternion from Euler angles where the Y angle (rotation about the Y axis) is randomized. The mushroom model contains
			// LOD levels, so the StaticModel component will automatically select the LOD level according to the view distance (you'll
			// see the model get simpler as it moves further away). Finally, rendering a large number of the same object with the
			// same material allows instancing to be used, if the GPU supports it. This reduces the amount of CPU work in rendering the
			// scene.
			var rand = new Random();
			for (int i = 0; i < 200; i++)
			{
				var mushroom = scene.CreateChild ("Mushroom");
				mushroom.Position = new Vector3 (rand.Next (90)-45, 0, rand.Next (90)-45);
				mushroom.Rotation = new Quaternion (0, rand.Next (360), 0);
				mushroom.SetScale (0.5f+rand.Next (20000)/10000.0f);
				var mushroomObject = mushroom.CreateComponent<StaticModel>();
				mushroomObject.Model = ResourceCache.GetModel ("Models/Mushroom.mdl");
				mushroomObject.SetMaterial (ResourceCache.GetMaterial ("Materials/Mushroom.xml"));
			}

			// Create a scene node for the camera, which we will move around
    		// The camera will use default settings (1000 far clip distance, 45 degrees FOV, set aspect ratio automatically)
			CameraNode = scene.CreateChild ("camera");
			camera = CameraNode.CreateComponent<Camera>();

			// Set an initial position for the camera scene node above the plane
			CameraNode.Position = new Vector3 (0, 5, 0);
		}
		
		void SetupViewport ()
		{
			// Set up a viewport to the Renderer subsystem so that the 3D scene can be seen. We need to define the scene and the camera
			// at minimum. Additionally we could configure the viewport screen size and the rendering path (eg. forward / deferred) to
			// use, but now we just use full screen and default render path configured in the engine command line options
			Renderer.SetViewport (0, new Viewport (Context, scene, camera, null));
		
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			SimpleMoveCamera3D(timeStep);
		}
	}
}