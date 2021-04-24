using UrhoNetSamples;
using Urho;
using Urho.Physics;
using Urho.Gui;
using System;

namespace MaterialEffects
{
    public class Timer
    {
        public Timer()
        {
            Reset();
        }

        public uint GetMSec(bool reset)
        {
            uint currentTime = Tick();
            uint elapsedTime = currentTime - startTime_;
            if (reset)
                startTime_ = currentTime;

            return elapsedTime;
        }
        /// Reset the timer.
        public void Reset()
        {
            startTime_ = Tick();
        }

        static uint Tick()
        {
            return (uint)DateTime.Now.TimeOfDay.Milliseconds;

        }

        /// Starting clock value in milliseconds.
        uint startTime_;

    }

}
