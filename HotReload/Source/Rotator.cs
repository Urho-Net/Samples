using Urho;
using System;
using Urho.Resources;

// Class name must be equal to file name for dynamic reloadable Component

namespace HotReload
{
    class Rotator : Component
    {

        private Vector3 RotationSpeed;

        private void InitiaizeVariables()
        {
            ReceiveSceneUpdates = true;
            if (Node != null)
            {
                Node.Rotation = Quaternion.Identity;
            }
            RotationSpeed = new Vector3(0, 20, 0);
        }


    	public override void OnDeserialize(IComponentDeserializer d)
		{
			RotationSpeed = d.Deserialize<Vector3>(nameof(RotationSpeed));
            Node.Position = d.Deserialize<Vector3>(nameof(Node.Position));
            Node.Rotation = d.Deserialize<Quaternion>(nameof(Node.Rotation));
		}

		public override void OnSerialize(IComponentSerializer s)
		{
			s.Serialize(nameof(RotationSpeed), RotationSpeed);
            s.Serialize(nameof(Node.Position),  Node.Position);
            s.Serialize(nameof(Node.Rotation),  Node.Rotation);
           
		}

        public Rotator(IntPtr handle) : base(handle)
        {
            InitiaizeVariables();
        }
        public Rotator()
        {
            InitiaizeVariables();
        }

        public override void OnSceneSet(Scene scene)
        {
            InitiaizeVariables();
        }

        protected override void OnUpdate(float timeStep)
        {
            Node.Rotate(new Quaternion(
                RotationSpeed.X * timeStep,
                RotationSpeed.Y * timeStep,
                RotationSpeed.Z * timeStep),
                TransformSpace.Local);
        }


    }
}