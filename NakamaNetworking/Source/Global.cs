using System.Threading.Tasks;
using Nakama;

namespace NakamaNetworking
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

        public static NakamaClient NakamaConnection = null;
        public static IMatch currentMatch = null;

        public const float matchStateThresholdTime = 0.05f;

        /// <summary>
        /// Sends a match state message across the network.
        /// </summary>
        /// <param name="opCode">The operation code.</param>
        /// <param name="state">The stringified JSON state data.</param>
        public static async Task SendMatchState(long opCode, string state)
        {
            if (NakamaConnection != null && NakamaConnection.Socket != null && NakamaConnection.Socket.IsConnected == true && currentMatch != null )
                await NakamaConnection.Socket.SendMatchStateAsync(currentMatch.Id, opCode, state);
        }

    }

}
