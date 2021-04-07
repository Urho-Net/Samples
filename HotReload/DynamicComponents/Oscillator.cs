using Urho;
using Urho.IO;
using System;

// Class name must be equal to file name for dynamic reloadable Component

namespace HotReload
{
    class Oscillator : Component
    {

        private Vector3 movementVector = new Vector3(1f, 0f, 1f);
        private float movementFactor = 0f;
        private float period = 2f;

        Vector3 startingPosition;

        float totalTime = 0f;

        public Oscillator(IntPtr handle) : base(handle) 
        { 
            ReceiveSceneUpdates = true;
            if(Node != null)
            {
                startingPosition = Node.Position;
              //  Log.Write(LogLevel.Warning," Constructor Oscillator(IntPtr handle)" + startingPosition.ToString());
            }
        }
        public Oscillator()
        {
            //to receive OnUpdate:
            ReceiveSceneUpdates = true;
        }

        public override void OnSceneSet(Scene scene)
        {
            if (scene != null)
            {
                startingPosition = Node.Position;
              //  Log.Write(LogLevel.Warning,"OnSceneSet " + startingPosition.ToString());
            }
        }
        protected override void OnUpdate(float timeStep)
        {
            totalTime += timeStep;
            period = MathHelper.Clamp(period, 0.1f, period);

            float cycles = (float)totalTime / period;

            const float tau = MathF.PI * 2;
            float rawSineWave = MathF.Sin(cycles * tau);

            movementFactor = rawSineWave / 2f + 0.5f;

            Vector3 offset = movementFactor * movementVector;
            Node.Position = startingPosition + offset;

        }


    }
}