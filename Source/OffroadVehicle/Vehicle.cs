// Copyright (c) 2020-2021 Eli Aloni a.k.a elix22.
// Copyright (c) 2008-2021 the Urho3D project.
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
using System.Collections.Generic;
using Urho;
using Urho.Physics;
using Urho.Audio;

namespace OffroadVehicle
{
    /// <summary>
    /// Vehicle component, responsible for physical movement according to controls.
    /// </summary>
    public class Vehicle : Component
    {

        #region Constants
        public const float CUBE_HALF_EXTENTS = 1.15f;
        public const float MIN_SLOW_DOWN_VEL = 15.0f;
        public const float MIN_STICTION_VEL = 5.0f;
        public const float MIN_BRAKE_FORCE = 2.0f;
        public const float MIN_IDLE_RPM = 1000.0f;
        public const float MIN_DOWN_FORCE = 10.0f;
        public const float MAX_DOWN_FORCE = 1e4f;
        public const float MAX_ANGULAR_VEL_LIMIT = 10.0f;
        public const float LINEAR_VEL_LIMIT_MPH = 140.0f;
        public const float VEL_TO_MPH = (3.6f / 1.60934f);
        public const float MAX_LINEAR_VEL_LIMIT = (LINEAR_VEL_LIMIT_MPH / VEL_TO_MPH);

        public const float AUDIO_FIXED_FREQ_44K = 44100.0f;
        public const float MIN_SHOCK_IMPACT_VEL = 3.0f;
        public const float MAX_SKID_TRACK_SPEED = 70.0f;
        public const float MIN_SIDE_SLIP_VEL = 4.0f;

        public const float MIN_WHEEL_RPM = 0.60f;
        public const float MAX_WHEEL_RPM = 0.75f;
        public const float MIN_WHEEL_RPM_AIR = 0.89f;
        public const float MAX_WHEEL_RPM_AIR = 0.90f;

        public const float MIN_PEELOUT_VAL_AT_ZER0 = 0.8f;
        public const float MAX_REAR_SLIP = 0.6f;


        public const int CtrlForward = 1;
        public const int CtrlBack = 2;
        public const int CtrlLeft = 4;
        public const int CtrlRight = 8;

        public const int CtrlSpace = 16;
        public const float YawSensitivity = 0.1f;
        #endregion


        #region members
        CustomRaycastVehicle raycastVehicle_;
        /// Current left/right steering amount (-1 to 1.)
        float steering_;

        // IDs of the wheel scene nodes for serialization.
        List<Node> m_vpNodeWheel = new List<Node>();

        float m_fVehicleMass;
        float m_fEngineForce;
        float m_fBreakingForce;

        float m_fmaxEngineForce;
        float m_fmaxBreakingForce;

        float m_fVehicleSteering;
        float m_fsteeringIncrement;
        float m_fsteeringClamp;
        float m_fwheelRadius;
        float m_fwheelWidth;
        float m_fwheelFriction;
        float m_fsuspensionStiffness;
        float m_fsuspensionDamping;
        float m_fsuspensionCompression;
        float m_frollInfluence;
        float m_fsuspensionRestLength;

        
        float m_fRearSlip;

        Vector3 centerOfMassOffset_;

        // acceleration
        float currentAcceleration_;

        // ang velocity limiter
        float m_fYAngularVelocity;

        // wheel contacts
        int numWheels_;
        int numWheelContacts_;
        int prevWheelContacts_;
        bool isBraking_;
        List<float> gearShiftSpeed_ = new List<float>();
        bool[] prevWheelInContact_ = null;

        // gears
        float downShiftRPM_;
        float upShiftRPM_;
        int numGears_;
        int curGearIdx_;
        float curRPM_;
        float minIdleRPM_;

        Sound engineSnd_;
        Sound skidSnd_;
        Sound shockSnd_;
        SoundSource3D engineSoundSrc_;
        SoundSource3D skidSoundSrc_;
        SoundSource3D shockSoundSrc_;
        bool playAccelerationSoundInAir_;


        List<Node> particleEmitterNodeList_ = new List<Node>();

        bool dbgRender_ = false;

        const float KMH_TO_MPH = (1.0f / 1.60934f);

        public Controls Controls { get; set; } = new Controls();

        #endregion



        public Vehicle(IntPtr handle) : base(handle) { InitVars(); }

        public Vehicle() { InitVars(); }

        void ResetForces()
        {
            raycastVehicle_.ResetForces();
            raycastVehicle_.SetAngularVelocity(Vector3.Zero);
        }

        public float GetSpeedKmH() { return raycastVehicle_.CurrentSpeedKmHour; }
        float GetSpeedMPH() { return raycastVehicle_.CurrentSpeedKmHour * KMH_TO_MPH; }
        void SetDbgRender(bool enable) { dbgRender_ = enable; }
        public int GetCurrentGear() { return curGearIdx_; }
        public float GetCurrentRPM() { return curRPM_; }

        public void FixedUpdate(float timeStep)
        {
            float newSteering = 0.0f;
            float accelerator = 0.0f;
            bool braking = false;


            if (Controls.IsDown(CtrlLeft))
                newSteering = -1.0f;
            if (Controls.IsDown(CtrlRight))
                newSteering = 1.0f;
            if (Controls.IsDown(CtrlForward))
                accelerator = 1.0f;
            if (Controls.IsDown(CtrlBack))
                accelerator = -0.4f;

            if (Controls.IsDown(CtrlSpace))
            {
                braking = true;
                accelerator = 0.0f;
            }

            if (newSteering != 0.0f || accelerator != 0.0f)
            {
                raycastVehicle_.Activate();
            }

            UpdateGear();

            UpdateSteering(newSteering);

            ApplyEngineForces(accelerator, braking);

            // do this right after applyEngineForce and before applying other forces
            if (ApplyStiction(newSteering, accelerator, braking))
            {
                return;
            }

            ApplyDownwardForce();

            LimitLinearAndAngularVelocity();

            AutoCorrectPitchRoll();

            UpdateDrift();
        }

        public void FixedPostUpdate(float timeStep)
        {
            float curSpdMph = GetSpeedMPH();

            // clear contact states
            prevWheelContacts_ = numWheelContacts_;
            numWheelContacts_ = 0;
            float wheelVelocity = 0.0f;
            Vector3 linVel = raycastVehicle_.LinearVelocity;

            for (int i = 0; i < raycastVehicle_.NumWheels; i++)
            {

                float m_skidInfoCumulative = raycastVehicle_.GetSkidInfoCumulative(i);
                float m_wheelsRadius = raycastVehicle_.GetWheelsRadius(i);
                float m_deltaRotation = raycastVehicle_.GetDeltaRotation(i);
                float m_rotation = raycastVehicle_.GetRotation(i);

                // adjust wheel rotation based on acceleration
                if ((curGearIdx_ == 0 || !raycastVehicle_.IsWheelInContact(i)) && currentAcceleration_ > 0.0f)
                {
                    // peel out on 1st gear
                    if (curGearIdx_ == 0 && m_skidInfoCumulative > MIN_PEELOUT_VAL_AT_ZER0)
                    {
                        m_skidInfoCumulative = MIN_PEELOUT_VAL_AT_ZER0;
                    }

                    if (m_skidInfoCumulative > 0.05f)
                    {
                        m_skidInfoCumulative -= 0.002f;
                    }

                    float deltaRotation = (gearShiftSpeed_[curGearIdx_] * (1.0f - m_skidInfoCumulative) * timeStep) / (m_wheelsRadius);

                    if (deltaRotation > m_deltaRotation)
                    {
                        m_rotation += deltaRotation - m_deltaRotation;
                        m_deltaRotation = deltaRotation;
                    }
                }
                else
                {
                    m_skidInfoCumulative = raycastVehicle_.GetSkidInfo(i);

                    if (!raycastVehicle_.IsWheelInContact(i) && currentAcceleration_ < float.Epsilon)
                    {
                        m_rotation *= 0.95f;
                        m_deltaRotation *= 0.95f;
                    }
                }

                // ground contact
                float whSlipVel = 0.0f;
                if (raycastVehicle_.IsWheelInContact(i))
                {
                    numWheelContacts_++;

                    // check side velocity slip
                    whSlipVel = Math.Abs(Vector3.Dot(raycastVehicle_.GetWheelAxleWS(i), linVel));

                    if (whSlipVel > MIN_SIDE_SLIP_VEL)
                    {
                        m_skidInfoCumulative = (m_skidInfoCumulative > 0.9f) ? 0.89f : m_skidInfoCumulative;
                    }
                }

                // wheel velocity from rotation
                // note (correct eqn): raycastVehicle_.GetLinearVelocity().Length() ~= m_deltaRotation * whInfo.m_wheelsRadius)/timeStep
                wheelVelocity += (m_deltaRotation * m_wheelsRadius) / timeStep;

                raycastVehicle_.SetSkidInfoCumulative(i, m_skidInfoCumulative);
                raycastVehicle_.SetDeltaRotation(i, m_deltaRotation);
                raycastVehicle_.SetRotation(i, m_rotation);
            }

            // set cur rpm based on wheel rpm
            int numPoweredWheels = raycastVehicle_.NumWheels;

            // adjust rpm based on wheel speed
            if (curGearIdx_ == 0 || numWheelContacts_ == 0)
            {
                // average wheel velocity
                wheelVelocity /= (float)numPoweredWheels;

                // physics velocity to kmh to mph (based on Bullet's calculation for KmH)
                wheelVelocity = wheelVelocity * 3.6f * KMH_TO_MPH;
                float wheelRPM = upShiftRPM_ * wheelVelocity / gearShiftSpeed_[curGearIdx_];

                if (curGearIdx_ == 0)
                {
                    if (wheelRPM > upShiftRPM_ * MAX_WHEEL_RPM)
                    {
                        wheelRPM = upShiftRPM_ * Sample.NextRandom(MIN_WHEEL_RPM, MAX_WHEEL_RPM);
                    }
                }
                else
                {
                    if (playAccelerationSoundInAir_)
                    {
                        if (wheelRPM > upShiftRPM_ * MAX_WHEEL_RPM_AIR)
                        {
                            wheelRPM = upShiftRPM_ * Sample.NextRandom(MIN_WHEEL_RPM_AIR, MAX_WHEEL_RPM_AIR);
                        }
                    }
                    else
                    {
                        wheelRPM = 0.0f;
                    }
                }

                if (wheelRPM > curRPM_)
                    curRPM_ = wheelRPM;

                if (curRPM_ < MIN_IDLE_RPM)
                    curRPM_ += minIdleRPM_;
            }

        }

        public void OnPostUpdate(PostUpdateEventArgs args)
        {
            float timeStep = args.TimeStep;
            float curSpdMph = GetSpeedMPH();

            for (int i = 0; i < raycastVehicle_.NumWheels; i++)
            {
                // update wheel transform - performed after whInfo.m_rotation is adjusted from above
                raycastVehicle_.UpdateWheelTransform(i, true);

                Vector3 v3Origin = raycastVehicle_.GetWheelPositionWS(i);
                Quaternion qRot = raycastVehicle_.GetWheelRotation(i);

                Node pWheel = m_vpNodeWheel[i];
                pWheel.Position = (v3Origin);

                Vector3 v3PosLS = raycastVehicle_.GetChassisConnectionPointCS(i);
                Quaternion qRotator = (v3PosLS.X >= 0.0 ? new Quaternion(0.0f, 0.0f, -90.0f) : new Quaternion(0.0f, 0.0f, 90.0f));
                pWheel.Rotation = (qRot * qRotator);
            }

            // update sound and wheel effects
            PostUpdateSound(timeStep);

            PostUpdateWheelEffects();
        }

        public void InitVars()
        {
            steering_ = 0.0f;
            m_fVehicleMass = 100.0f;
            m_fEngineForce = 0.0f;
            m_fBreakingForce = 20.0f;

            m_fmaxEngineForce = 950f;
            m_fmaxBreakingForce = 800f;

            m_fVehicleSteering = 0.0f;
            m_fsteeringIncrement = 0.030f;
            m_fsteeringClamp = 0.5f;
            m_fwheelRadius = 0.4f;
            m_fwheelWidth = 0.4f;
            m_fwheelFriction = 2.2f;

            m_fsuspensionStiffness = 20.0f;
            m_fsuspensionDamping = 2.0f;
            m_fsuspensionCompression = 5.0f;
            m_frollInfluence = 0.1f;
            m_fsuspensionRestLength = 0.6f;
  

            // skid
            m_fYAngularVelocity = 1.0f;
       
            numWheelContacts_ = 0;

            currentAcceleration_ = 0.0f;

            // gear
            downShiftRPM_ = 4500.0f;
            upShiftRPM_ = 7500.0f;
            curGearIdx_ = 0;
            curRPM_ = MIN_IDLE_RPM;

            // most vehicle dynamics have complicated gear ratio equations, gear shifting formulas, etc.
            // but it all comes down to at speeds to shift gear
            // speed shown is in mph - has no effect on whether the speed is displayed in KmH or MPH
            gearShiftSpeed_.Add(50.0f);
            gearShiftSpeed_.Add(70.0f);
            gearShiftSpeed_.Add(90.0f);
            gearShiftSpeed_.Add(110.0f);
            gearShiftSpeed_.Add(130.0f);
            numGears_ = gearShiftSpeed_.Count;

            // wheel nodes
            m_vpNodeWheel.Clear();

            // sound
            //playAccelerationSoundInAir_ = true;
        }

        public void Init()
        {
            var cache = Application.ResourceCache;
            var node_ = Node;

            raycastVehicle_ = node_.CreateComponent<CustomRaycastVehicle>();
            CollisionShape hullColShape = node_.CreateComponent<CollisionShape>();
            StaticModel hullObject = node_.CreateComponent<StaticModel>();


            raycastVehicle_.Mass = m_fVehicleMass;
            raycastVehicle_.LinearDamping = 0.2f;
            raycastVehicle_.AngularDamping = 0.1f;
            raycastVehicle_.CollisionLayer = 1;

            Model vehModel = cache.GetModel("Offroad/Models/offroadVehicle.mdl");
            hullObject.Model = vehModel;
            hullObject.Material = cache.GetMaterial("Offroad/Models/Materials/offroadVehicle.xml");
            hullObject.CastShadows = true;

            // set convex hull and resize local AABB.Y size
            Model vehColModel = cache.GetModel("Offroad/Models/vehCollision.mdl");
            hullColShape.SetConvexHull(vehColModel, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);
            raycastVehicle_.CompoundScaleLocalAabbMin(new Vector3(0.7f, 0.5f, 1.0f));
            raycastVehicle_.CompoundScaleLocalAabbMax(new Vector3(0.7f, 0.5f, 1.0f));

            bool isFrontWheel = true;
            Vector3 wheelDirectionCS0 = new Vector3(0, -1, 0);
            Vector3 wheelAxleCS = new Vector3(-1, 0, 0);

            //******************
            // center of mass
            centerOfMassOffset_ = new Vector3(0, -0.07f, 0.6f);

            // change center of mass
            raycastVehicle_.SetVehicleCenterOfMass(centerOfMassOffset_);

            // add wheels
            Vector3 connectionPointCS0 = new Vector3(CUBE_HALF_EXTENTS - (0.6f * m_fwheelWidth), centerOfMassOffset_.Y + 0.05f, 2 * CUBE_HALF_EXTENTS - m_fwheelRadius - 0.4f - centerOfMassOffset_.Z);
            raycastVehicle_.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, m_fsuspensionRestLength, m_fwheelRadius, isFrontWheel);

            connectionPointCS0 = new Vector3(-CUBE_HALF_EXTENTS + (0.6f * m_fwheelWidth), centerOfMassOffset_.Y + 0.05f, 2 * CUBE_HALF_EXTENTS - m_fwheelRadius - 0.4f - centerOfMassOffset_.Z);
            raycastVehicle_.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, m_fsuspensionRestLength, m_fwheelRadius, isFrontWheel);

            isFrontWheel = false;
            connectionPointCS0 = new Vector3(-CUBE_HALF_EXTENTS + (0.6f * m_fwheelWidth), centerOfMassOffset_.Y, -2 * CUBE_HALF_EXTENTS + m_fwheelRadius + 0.4f - centerOfMassOffset_.Z);
            raycastVehicle_.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, m_fsuspensionRestLength, m_fwheelRadius, isFrontWheel);

            connectionPointCS0 = new Vector3(CUBE_HALF_EXTENTS - (0.6f * m_fwheelWidth), centerOfMassOffset_.Y, -2 * CUBE_HALF_EXTENTS + m_fwheelRadius + 0.4f - centerOfMassOffset_.Z);
            raycastVehicle_.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, m_fsuspensionRestLength, m_fwheelRadius, isFrontWheel);

            numWheels_ = raycastVehicle_.NumWheels;
            prevWheelInContact_ = new bool[numWheels_];

            for (int i = 0; i < numWheels_; i++)
            {
                raycastVehicle_.SetWheelSuspensionStiffness(i, m_fsuspensionStiffness);
                raycastVehicle_.SetWheelDampingRelaxation(i, m_fsuspensionDamping);
                raycastVehicle_.SetWheelDampingCompression(i, m_fsuspensionCompression);
                raycastVehicle_.SetWheelFrictionSlip(i, m_fwheelFriction);
                raycastVehicle_.SetRollInfluence(i, m_frollInfluence);

                prevWheelInContact_[i] = false;

                // side friction stiffness is different for front and rear wheels
                if (i < 2)
                {
                    raycastVehicle_.SetSideFrictionStiffness(i, 0.9f);
                }
                else
                {
                    m_fRearSlip = MAX_REAR_SLIP;
                    raycastVehicle_.SetSideFrictionStiffness(i, MAX_REAR_SLIP);

                }

            }

            if (raycastVehicle_ != null)
            {
                raycastVehicle_.ResetSuspension();

                float wheelDim = m_fwheelRadius * 2.0f;
                float wheelThickness = 1.0f;
                Model tireModel = cache.GetModel("Offroad/Models/tire.mdl");
                BoundingBox tirebbox = tireModel.BoundingBox;
                float tireScaleXZ = wheelDim / tirebbox.Size.X;

                Color LtBrown = new Color(0.972f, 0.780f, 0.412f);
                Model trackModel = cache.GetModel("Offroad/Models/wheelTrack.mdl");


                for (int i = 0; i < raycastVehicle_.NumWheels; i++)
                {
                    //synchronize the wheels with the chassis worldtransform
                    raycastVehicle_.UpdateWheelTransform(i, true);

                    Vector3 v3Origin = raycastVehicle_.GetWheelPositionWS(i);
                    Quaternion qRot = raycastVehicle_.GetWheelRotation(i);

                    // wheel node
                    Node wheelNode = Scene.CreateChild();
                    m_vpNodeWheel.Add(wheelNode);

                    wheelNode.Position = v3Origin;
                    Vector3 v3PosLS = raycastVehicle_.GetChassisConnectionPointCS(i);

                    wheelNode.Rotation = (v3PosLS.X >= 0.0 ? new Quaternion(0.0f, 0.0f, -90.0f) : new Quaternion(0.0f, 0.0f, 90.0f));
                    wheelNode.Scale = new Vector3(tireScaleXZ, wheelThickness, tireScaleXZ);

                    // tire model
                    StaticModel pWheel = wheelNode.CreateComponent<StaticModel>();
                    pWheel.Model = tireModel;
                    pWheel.Material = cache.GetMaterial("Offroad/Models/Materials/Tire.xml");
                    pWheel.CastShadows = true;

                    // particle emitter
                    Node pNodeEmitter = Scene.CreateChild();
                    Vector3 emitPos = v3Origin + new Vector3(0, -m_fwheelRadius, 0);
                    pNodeEmitter.Position = emitPos;
                    ParticleEmitter particleEmitter = pNodeEmitter.CreateComponent<ParticleEmitter>();
                    particleEmitter.Effect = cache.GetParticleEffect("Offroad/Particles/Dust.xml");
                    particleEmitter.Emitting = false;

                    particleEmitterNodeList_.Add(pNodeEmitter);
                }

            }

            // init sound
            engineSoundSrc_ = node_.CreateComponent<SoundSource3D>();
            engineSnd_ = cache.GetSound("Offroad/Sounds/engine-prototype.ogg");
            engineSnd_.Looped = true;

            engineSoundSrc_.SetDistanceAttenuation(1.0f, 30.0f, 0.1f);
            string tst = SoundType.Effect.ToString();
            engineSoundSrc_.SetSoundType(SoundType.Effect.ToString());
            engineSoundSrc_.Gain = (0.7f);
            engineSoundSrc_.Play(engineSnd_);
            engineSoundSrc_.Frequency = (AUDIO_FIXED_FREQ_44K * 0.05f);

            skidSoundSrc_ = node_.CreateComponent<SoundSource3D>();
            skidSnd_ = cache.GetSound("Offroad/Sounds/skid-gravel.ogg");
            skidSoundSrc_.SetSoundType(SoundType.Effect.ToString());
            skidSoundSrc_.Gain = (0.4f);
            skidSoundSrc_.SetDistanceAttenuation(1.0f, 30.0f, 0.1f);
            skidSoundSrc_.Frequency = (AUDIO_FIXED_FREQ_44K * 1.4f);

            shockSoundSrc_ = node_.CreateComponent<SoundSource3D>();
            shockSnd_ = cache.GetSound("Offroad/Sounds/shocks-impact.ogg");
            shockSoundSrc_.SetSoundType(SoundType.Effect.ToString());
            shockSoundSrc_.Gain = (0.7f);
            shockSoundSrc_.SetDistanceAttenuation(1.0f, 30.0f, 0.1f);

            // acceleration sound while in air - most probably want this on
            playAccelerationSoundInAir_ = false;

        }

        void UpdateSteering(float newSteering)
        {
            // gradual change
            if (newSteering != 0.0f)
            {
                steering_ += newSteering * m_fsteeringIncrement;
            }
            else
            {
                steering_ *= 0.90f;
            }

            steering_ = Math.Clamp(steering_, -m_fsteeringClamp, m_fsteeringClamp);

            // angular velocity
            if (Math.Abs(steering_) > m_fsteeringClamp * 0.75f)
            {
                m_fYAngularVelocity += 0.2f;
            }
            else
            {
                m_fYAngularVelocity *= 0.98f;
            }

            m_fYAngularVelocity = Math.Clamp(m_fYAngularVelocity, 2.0f, 4.0f);

            // apply value
            m_fVehicleSteering = steering_;

            for (int i = 0; i < 2; ++i)
            {
                raycastVehicle_.SetSteeringValue(m_fVehicleSteering, i);
            }
        }

        void ApplyEngineForces(float accelerator, bool braking)
        {
            // 4x wheel drive
            const float numDriveTrains = 2.0f;
            const float invNumDriveTrains = 1.0f / numDriveTrains;

            isBraking_ = braking;
            currentAcceleration_ = accelerator;
            m_fBreakingForce = braking ? m_fmaxBreakingForce * 0.5f : 0.0f;
            m_fEngineForce = m_fmaxEngineForce * accelerator * invNumDriveTrains;

            for (int i = 0; i < numWheels_; ++i)
            {
                raycastVehicle_.ApplyEngineForce(m_fEngineForce, i);

                // apply brake to rear wheels only
                if (i > 1)
                {
                    raycastVehicle_.SetBrake(m_fBreakingForce, i);
                }
            }
        }

        bool ApplyStiction(float steering, float acceleration, bool braking)
        {
            float vel = raycastVehicle_.LinearVelocity.Length;
            float absAccel = Math.Abs(acceleration);
            float absSteer = Math.Abs(steering);
            bool setStiction = false;

            if (absSteer < float.Epsilon && absAccel < float.Epsilon &&
                numWheelContacts_ > 0 && vel < MIN_STICTION_VEL)
            {
                setStiction = true;
            }

            // slow down and change rolling friction on stiction
            for (int i = 0; i < numWheels_; ++i)
            {

                if (absAccel < float.Epsilon && !braking && vel < MIN_SLOW_DOWN_VEL)
                {
                    raycastVehicle_.SetBrake(MIN_BRAKE_FORCE, i);
                }

                if (setStiction)
                {
                    float rollInfluence = MathHelper.Lerp(m_frollInfluence, 1.0f, 1.0f - vel / MIN_STICTION_VEL);
                    raycastVehicle_.SetRollInfluence(i, rollInfluence);
                }
                else
                {
                    raycastVehicle_.SetRollInfluence(i, m_frollInfluence);
                }
            }

            return setStiction;
        }

        void ApplyDownwardForce()
        {
            // apply downward force when some wheels are grounded
            if (numWheelContacts_ > 0 && numWheelContacts_ != numWheels_)
            {
                // small arbitrary multiplier
                const float velocityMultiplyer = 0.5f;
                Vector3 downNormal = Node.Up * -1.0f;
                float velocityMag = raycastVehicle_.LinearVelocity.LengthSquared * velocityMultiplyer;
                velocityMag = Math.Clamp(velocityMag, MIN_DOWN_FORCE, MAX_DOWN_FORCE);
                raycastVehicle_.ApplyForce(velocityMag * downNormal);
            }
        }

        // TBD ELI
        void AutoCorrectPitchRoll()
        {
            // auto correct pitch and roll while air borne
            if (numWheelContacts_ == 0)
            {
                //predictedUp eqn. from https://discourse.urho3d.io/t/constraint-class-working-on-derived-class/4081/6
                const float stability = 0.3f;
                const float speed = 1.5f;
                var t = MathHelper.DegreesToRadians(raycastVehicle_.AngularVelocity.Length) * stability / speed;
                Vector3 predictedUp = Quaternion.FromAxisAngle(Node.Up, MathHelper.DegreesToRadians(raycastVehicle_.AngularVelocity.Length) * stability / speed).ToEulerAngles();
                Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.Up);
                torqueVector *= speed * speed * m_fVehicleMass;
                raycastVehicle_.ApplyTorque(torqueVector);
            }
        }

        void LimitLinearAndAngularVelocity()
        {
            // velocity limit
            Vector3 linVel = raycastVehicle_.LinearVelocity;
            if (linVel.Length > MAX_LINEAR_VEL_LIMIT)
            {
                raycastVehicle_.SetLinearVelocity(Vector3.Normalize(linVel) * MAX_LINEAR_VEL_LIMIT);
            }

            // angular velocity limiters
            Vector3 v3AngVel = raycastVehicle_.AngularVelocity;
            v3AngVel.X = Math.Clamp(v3AngVel.X, -MAX_ANGULAR_VEL_LIMIT, MAX_ANGULAR_VEL_LIMIT);
            v3AngVel.Y = Math.Clamp(v3AngVel.Y, -m_fYAngularVelocity, m_fYAngularVelocity);
            v3AngVel.Z = Math.Clamp(v3AngVel.Z, -MAX_ANGULAR_VEL_LIMIT, MAX_ANGULAR_VEL_LIMIT);
            raycastVehicle_.SetAngularVelocity(v3AngVel);
        }

        void UpdateGear()
        {
            float curSpdMph = GetSpeedMPH();
            int gearIdx = 0;

            // no negative speed value
            if (curSpdMph < 0.0f) curSpdMph *= -1.0f;

            for (int i = 0; i < (int)gearShiftSpeed_.Count - 1; ++i)
            {
                if (curSpdMph > gearShiftSpeed_[i])
                {
                    gearIdx = i + 1;
                }
            }

            // up or down shift when a wheel is in contact with the ground
            if (gearIdx != curGearIdx_ && numWheelContacts_ > 0)
            {
                curRPM_ = upShiftRPM_ * curSpdMph / gearShiftSpeed_[curGearIdx_];

                if (curRPM_ < downShiftRPM_)
                {
                    if (curGearIdx_ > 0)
                    {
                        curGearIdx_--;
                    }
                }
                else if (gearIdx > curGearIdx_)
                {
                    curGearIdx_++;
                }
            }

            // final rpm
            curRPM_ = upShiftRPM_ * curSpdMph / gearShiftSpeed_[curGearIdx_];

            if (curGearIdx_ == 0)
            {
                minIdleRPM_ = Sample.NextRandom(1000.0f, 1025.0f);
                if (curRPM_ < MIN_IDLE_RPM) curRPM_ += minIdleRPM_;
            }
        }

        void UpdateDrift()
        {

            const float slipConditon3 = 0.04f; // dirt


            // set slip
            const float slipConditionValue = slipConditon3;
            const float slipMax = MAX_REAR_SLIP;

            // for demo purpose, limit the drift speed to provide high speed steering experience w/o any drifting
            const float maxDriftSpeed = 70.0f;
            float absSteeringVal = Math.Abs(raycastVehicle_.GetSteeringValue(0));
            float curSpdMph = GetSpeedMPH();

            // set rear wheel slip values
            for (int i = 2; i < numWheels_; ++i)
            {
                // re-calc the slip value only once
                if (i == 2)
                {
                    if (currentAcceleration_ > 0.0f)
                    {
                        float slipMin = (curSpdMph < maxDriftSpeed) ? slipConditionValue : slipMax;
                        float slipAdj = MathHelper.Lerp(slipMax, slipMin, absSteeringVal / m_fsteeringClamp);
                        float deltaSlip = slipAdj - m_fRearSlip;

                        m_fRearSlip += deltaSlip * 0.05f;
                        m_fRearSlip = Math.Clamp(m_fRearSlip, slipConditionValue, slipMax);
                    }
                    else
                    {
                        m_fRearSlip = slipMax;
                    }
                }

                // set value
                raycastVehicle_.SetSideFrictionStiffness(i, m_fRearSlip);
            }
        }

        void PostUpdateSound(float timeStep)
        {
            int playSkidSound = 0;
            bool playShockImpactSound = false;

            for (int i = 0; i < numWheels_; ++i)
            {
                // skid sound
                if (raycastVehicle_.IsWheelInContact(i))
                {
                    if (raycastVehicle_.GetSkidInfoCumulative(i) < 0.9f)
                    {
                        playSkidSound++;
                    }

                    // shock impact
                    if (!prevWheelInContact_[i])
                    {
                        Vector3 velAtWheel = raycastVehicle_.GetVelocityAtPoint(raycastVehicle_.GetWheelPositionLS(i));
                        float downLinVel = Vector3.Dot(velAtWheel, Vector3.Down);

                        if (downLinVel > MIN_SHOCK_IMPACT_VEL)
                        {
                            playShockImpactSound = true;
                        }
                    }
                }

                // update prev wheel in contact
                prevWheelInContact_[i] = raycastVehicle_.IsWheelInContact(i);
            }

            // -ideally, you want the engine sound to sound like it's at 10k rpm w/o any pitch adjustment, and 
            // we nomralize x to be from 0.1f to 1.0f by dividing by 10k in SetFrequency(AUDIO_FIXED_FREQ_44K * x)
            // -if shifting rmps sounds off then change the normalization value. for the engine prototype sound, 
            // the pitch sound is low, so it's normalized by diving by 8k instead of 10k
            const float rpmNormalizedForEnginePrototype = 8000.0f;
            engineSoundSrc_.Frequency = (AUDIO_FIXED_FREQ_44K * curRPM_ / rpmNormalizedForEnginePrototype);

            // shock impact when transitioning from partially off ground (or air borne) to landing
            if (prevWheelContacts_ <= 2 && playShockImpactSound)
            {
                if (!shockSoundSrc_.Playing)
                {
                    shockSoundSrc_.Play(shockSnd_);
                }
            }

            // skid sound
            if (playSkidSound > 1)
            {
                if (!skidSoundSrc_.Playing)
                {
                    skidSoundSrc_.Play(skidSnd_);
                }
            }
            else
            {
                skidSoundSrc_.Stop();
            }
        }

        //TBD ELI
        void PostUpdateWheelEffects()
        {
            float curSpdMph = GetSpeedMPH();
            Vector3 linVel = raycastVehicle_.LinearVelocity;

            for (int i = 0; i < raycastVehicle_.NumWheels; ++i)
            {
    
                ParticleEmitter particleEmitter = particleEmitterNodeList_[i].GetComponent<ParticleEmitter>();

                if (raycastVehicle_.IsWheelInContact(i) && raycastVehicle_.GetSkidInfoCumulative(i) < 0.9f)
                {
                    Vector3 pos2 = raycastVehicle_.GetContactPointWS(i);
                    particleEmitterNodeList_[i].Position = (pos2);

                    // emit dust if moving
                    if (particleEmitter != null && ! particleEmitter.Emitting  && curSpdMph > 2.0f)
                    {
                        particleEmitter.Emitting = (true);
                    }
                }
                else
                {

                    if (particleEmitter != null && particleEmitter.Emitting)
                    {
                        particleEmitter.Emitting = (false);
                    }
                }
            }
        }

    }

}
