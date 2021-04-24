namespace MaterialEffects
{
    public static class Global
    {

        public const float CameraMinDist = 1.0f;
        public const float CameraInitialDist = 5.0f;
        public const float CameraMaxDist = 20.0f;

        public const float GyroscopeThreshold = 0.1f;

        public const int CtrlForward = 1;
        public const int CtrlBack = 2;
        public const int CtrlLeft = 4;
        public const int CtrlRight = 8;
        public const int CtrlJump = 16;

        public const float MoveForce = 0.8f;
        public const float InairMoveForce = 0.02f;
        public const float BrakeForce = 0.2f;
        public const float JumpForce = 7.0f;
        public const float YawSensitivity = 0.1f;
        public const float InairThresholdTime = 0.1f;

  
        public enum CollisionLayerType
        {
            ColLayer_None = (0),

            ColLayer_Static = (1 << 0), // 1
            ColLayer_Water = (1 << 1), // 2 -- previously thought Bullet used this as kinematic layer, turns out Bullet has a kinematic collision flag=2

            ColLayer_Character = (1 << 2), // 4

            ColLayer_Projectile = (1 << 3), // 8

            ColLayer_Platform = (1 << 4), // 16
            ColLayer_Trigger = (1 << 5), // 32

            ColLayer_Ragdoll = (1 << 6), // 64
            ColLayer_Kinematic = (1 << 7), // 128

            ColLayer_All = (0xffff)
        };


        public enum CollisionMaskType
        {
            ColMask_Static = 0xFFFF - (CollisionLayerType.ColLayer_Platform | CollisionLayerType.ColLayer_Trigger),       // ~(16|32) = 65487
            ColMask_Character = 0xFFFF - (CollisionLayerType.ColLayer_Ragdoll | CollisionLayerType.ColLayer_Kinematic),                           // ~(64)    = 65471
            ColMask_Kinematic = 0xFFFF - (CollisionLayerType.ColLayer_Ragdoll | CollisionLayerType.ColLayer_Character),                           // ~(64)    = 65471
            ColMask_Projectile = 0xFFFF - (CollisionLayerType.ColLayer_Trigger),                           // ~(32)    = 65503
            ColMask_Platform = 0xFFFF - (CollisionLayerType.ColLayer_Static | CollisionLayerType.ColLayer_Trigger),         // ~(1|32)  = 65502
            ColMask_Trigger = 0xFFFF - (CollisionLayerType.ColLayer_Projectile | CollisionLayerType.ColLayer_Platform),    // ~(8|16)  = 65511
            ColMask_Ragdoll = 0xFFFF - (CollisionLayerType.ColLayer_Character),                         // ~(4)     = 65531

            ColMask_Camera = 0xFFFF - (CollisionLayerType.ColLayer_Character | CollisionLayerType.ColLayer_Projectile | CollisionLayerType.ColLayer_Trigger) // ~(4|8|32) = 65491
        };


    }

}
