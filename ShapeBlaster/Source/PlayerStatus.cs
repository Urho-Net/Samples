//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using Urho;
using System.IO;

namespace ShapeBlaster
{
    static class PlayerStatus
    {
        // amount of time it takes, in seconds, for a multiplier to expire.
        private const uint  multiplierExpiryTime = 500;
        private const int maxMultiplier = 20;

        public static int Lives { get; private set; }
        public static int Score { get; private set; }
        public static int HighScore { get; private set; }
        public static int Multiplier { get; private set; }
        public static bool IsGameOver { get { return Lives == 0; } }

        private static int scoreForExtraLife;       // score required to gain an extra life

        // Static constructor
        static PlayerStatus()
        {
            Reset();
            HighScore = LoadHighScore();
        }

        public static void Reset()
        {
            if (Score > HighScore)
				SaveHighScore(HighScore = Score);

            Score = 0;
            Multiplier = 1;
            Lives = 4;
            scoreForExtraLife = 2000;
            GameRoot.MultiplierTimer.Reset();
        }

        public static void Update()
        {
            if (Multiplier > 1)
            {
                // update the multiplier timer
   
                if (GameRoot.MultiplierTimer.GetMSec(false) > multiplierExpiryTime)
                {
                    GameRoot.MultiplierTimer.Reset();
                    ResetMultiplier();
                }
            }
        }

        public static void AddPoints(int basePoints)
        {
            if (PlayerShip.Instance.IsDead)
                return;

            Score += basePoints * Multiplier;
            while (Score >= scoreForExtraLife)
            {
                scoreForExtraLife += 2000;
                Lives++;
            }
        }

        public static void IncreaseMultiplier()
        {
            if (PlayerShip.Instance.IsDead)
                return;

            GameRoot.MultiplierTimer.Reset();
            if (Multiplier < maxMultiplier)
                Multiplier++;
        }

        public static void ResetMultiplier()
        {
            Multiplier = 1;
            GameRoot.MultiplierTimer.Reset();
        }

        public static void RemoveLife()
        {
            Lives--;
        }

        private static int LoadHighScore()
        {
            // return the saved high score if possible and return 0 otherwise
           return PlayerPrefs.GetInt("HighScore");
        }

        private static void SaveHighScore(int score)
        {
           PlayerPrefs.SetInt("HighScore",score);
        }
    }
}
