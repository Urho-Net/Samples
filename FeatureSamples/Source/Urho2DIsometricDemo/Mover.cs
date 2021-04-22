
using Urho;
using Urho.Urho2D;
using Urho.Gui;
using Urho.Resources;
using System;

namespace Urho2DIsometricDemo
{

    public class Mover : LogicComponent
    {
        public Mover()
        {
            speed_ = (0.8f);
            currentPathID_ = (1);
            emitTime_ = (0.0f);
            fightTimer_ = (0.0f);
            flip_ = (0.0f);
        }
        public Mover(IntPtr handle) : base(handle)
        {
            speed_ = (0.8f);
            currentPathID_ = (1);
            emitTime_ = (0.0f);
            fightTimer_ = (0.0f);
            flip_ = (0.0f);
        }

        public override void OnDeserialize(IComponentDeserializer d)
        {
            int path_length = d.Deserialize<int>("path_Length");
            if (path_length > 0)
            {
                path_ = new Vector2[path_length];
                for (int i = 0; i < path_.Length; i++)
                {
                    path_[i] = d.Deserialize<Vector2>("item"+i.ToString());
                }
            }
        }

        public override void OnSerialize(IComponentSerializer s)
        {
            if (path_ != null && path_.Length > 0)
            {
                s.Serialize("path_Length", path_.Length);
                for (int i = 0; i < path_.Length; i++)
                {
                    s.Serialize("item"+i.ToString(), path_[i]);
                }
            }
        }

        protected override void OnUpdate(float timeStep)
        {

            if (path_ == null || path_.Length < 2)
                return;

            // Handle Orc states (idle/wounded/fighting)
            if (Node.Name == "Orc")
            {
                var animatedSprite = Node.GetComponent<AnimatedSprite2D>();
                String anim = "run";

                // Handle wounded state
                if (emitTime_ > 0.0f)
                {
                    emitTime_ += timeStep;
                    anim = "dead";

                    // Handle dead
                    if (emitTime_ >= 3.0f)
                    {
                        Node.Remove();
                        return;
                    }
                }
                else
                {
                    // Handle fighting state
                    if (fightTimer_ > 0.0f)
                    {
                        anim = "attack";
                        flip_ = Scene.GetChild("Imp", true).Position.X - Node.Position.X;
                        fightTimer_ += timeStep;
                        if (fightTimer_ >= 3.0f)
                            fightTimer_ = 0.0f; // Reset
                    }
                    // Flip Orc animation according to speed, or player position when fighting
                    animatedSprite.FlipX = flip_ >= 0.0f;
                }
                // Animate
                if (animatedSprite.Animation != anim)
                    animatedSprite.SetAnimation(anim);
            }

            // Don't move if fighting or wounded
            if (fightTimer_ > 0.0f || emitTime_ > 0.0f)
                return;

            // Set direction and move to target
            Vector2 dir = path_[currentPathID_] - Node.Position2D;
            Vector2 dirNormal = Vector2.Normalize(dir);
            Node.Translate(new Vector3(dirNormal.X, dirNormal.Y, 0.0f) * Math.Abs(speed_) * timeStep);
            flip_ = dir.X;

            // Check for new target to reach
            if (Math.Abs(dir.Length) < 0.1f)
            {
                if (speed_ > 0.0f)
                {
                    if (currentPathID_ + 1 < path_.Length)
                        currentPathID_ = currentPathID_ + 1;
                    else
                    {
                        // If loop, go to first waypoint, which equates to last one (and never reverse)
                        if (path_[currentPathID_] == path_[0])
                        {
                            currentPathID_ = 1;
                            return;
                        }
                        // Reverse path if not looping
                        currentPathID_ = currentPathID_ - 1;
                        speed_ = -speed_;
                    }
                }
                else
                {
                    if (currentPathID_ - 1 >= 0)
                        currentPathID_ = currentPathID_ - 1;
                    else
                    {
                        currentPathID_ = 1;
                        speed_ = -speed_;
                    }
                }
            }

            base.OnUpdate(timeStep);
        }

        public void SetPath(Vector2[] path)
        {
            path_ = path;
        }

        Vector2[] path_;
        /// Movement speed.
        public float speed_;
        /// ID of the current path point.
        public int currentPathID_;
        /// Timer for particle emitter duration.
        public float emitTime_;
        /// Timer used for handling "attack" animation.
        public float fightTimer_;
        /// Flip animation based on direction, or player position when fighting.
        public float flip_;

    }

}