using Urho;
using System;

// Class name must be equal to file name for dynamic reloadable Component

namespace HotReload
{
    class Rotator : Component
    {
        private Vector3 RotationSpeed;

        public Rotator(IntPtr handle) : base(handle) 
        { 
             ReceiveSceneUpdates = true;
            RotationSpeed = new Vector3(0, 20, 0);
        }
        public Rotator()
        {
            //to receive OnUpdate:
            ReceiveSceneUpdates = true;
            RotationSpeed = new Vector3(0, 20, 0);
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