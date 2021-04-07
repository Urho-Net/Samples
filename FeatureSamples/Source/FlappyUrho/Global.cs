using Urho;
using System;


namespace FlappyUrho
{
    public enum GameState
    {
        GS_INTRO = 0,

        GS_PLAY,

        GS_DEAD
    };

    public static class Global
    {
        public const int NUM_BARRIERS = 5;
        public const int NUM_WEEDS = 34;

        public const float BAR_GAP = 10.0f;

        public const float BAR_INTERVAL = 20.0f;

        public static GameState gameState = GameState.GS_INTRO;
        public static GameState neededGameState = GameState.GS_INTRO;
        public static float sinceLastReset = 0.0f;

        public static float BAR_SPEED {get{return 5.0f + Math.Clamp(sinceLastReset * 0.05f, 0.0f, 2.3f);}}

        public static float BAR_RANDOM_Y {get{return Randoms.Next(-6.0f, 6.0f);}}
        public static float WEED_RANDOM_Y {get{return Randoms.Next(-23.0f, -21.0f);}}

        public static float BAR_OUTSIDE_X = 50.0f;

        public static float GRAV_ACC = 9.8f;

        public static float UP_SPEED = 10.0f;

        public static Vector3 CAMERA_DEFAULT_POS = new Vector3(0.0f, 0.0f, -30.0f);

        public static Quaternion URHO_DEFAULT_ROTATION = new Quaternion(0.0f, -90.0f, 0.0f);

        private static uint score = 0;
        private static uint highscore = 0;
        private static bool scoreTextDirty = true;
        private static bool highscoreTextDirty = true;

        private static Score3D score3d;
        private static Score3D highscore3d;

        public static uint Score
        {
            get
            {
                return score;
            }

            set
            {
                if (score == value)
                    return;

                score = value;
                scoreTextDirty = true;
            }

        }

        public static uint Highscore
        {
            get
            {
                return highscore;
            }

            set
            {
                if (highscore == value)
                    return;

                highscore = value;
                highscoreTextDirty = true;
            }
        }

        public static void SetScores3D(Score3D _score3d, Score3D _highscore3d)
        {
            score3d = _score3d;
            highscore3d = _highscore3d;
            highscore3d.SetAlignRight(true);
        }

        public static void OnUpdate()
        {
            if (scoreTextDirty)
            {

                score3d.SetScore(score);
                scoreTextDirty = false;
            }
            if (highscoreTextDirty)
            {
                highscore3d.SetScore(highscore);
                highscoreTextDirty = false;
            }
        }

    }

}