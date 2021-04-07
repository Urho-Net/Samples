using Urho;
using Urho.Resources;
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

        public override void OnDeserialize(IComponentDeserializer d)
        {
            movementVector = d.Deserialize<Vector3>(nameof(movementVector));
            movementFactor = d.Deserialize<float>(nameof(movementFactor));
            period = d.Deserialize<float>(nameof(period));
            Node.Position = d.Deserialize<Vector3>(nameof(Node.Position));
            Node.Rotation = d.Deserialize<Quaternion>(nameof(Node.Rotation));
        }

        public override void OnSerialize(IComponentSerializer s)
        {
            s.Serialize(nameof(movementVector), movementVector);
            s.Serialize(nameof(movementFactor), movementFactor);
            s.Serialize(nameof(period), period);
            s.Serialize(nameof(Node.Position), Node.Position);
            s.Serialize(nameof(Node.Rotation), Node.Rotation);

        }

        private void InitiaizeVariables()
        {
            ReceiveSceneUpdates = true;
            if (Scene != null && Node != null)
            {
                startingPosition = Node.Position;
            }
        }

        public Oscillator(IntPtr handle) : base(handle)
        {
            InitiaizeVariables();
        }
        public Oscillator()
        {
            InitiaizeVariables();
        }

        public override void OnSceneSet(Scene scene)
        {
            InitiaizeVariables();
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
            if (Node != null)
                Node.Position = startingPosition + offset;
        }


    }
}