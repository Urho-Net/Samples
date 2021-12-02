
using System;
using System.IO;
using Urho.Audio;
using Urho;
using System.Collections.Generic;

namespace ShapeBlaster
{
    public static class SoundManager
    {
        private static Sound mainMusic;
        
        private static SoundSource musicSource;

        private static  List<Sound> explosions = new List<Sound>();

         private static List<Sound> shots = new List<Sound>();

         private static readonly Random rand = new Random();

         private static float explosingGain = 0.3f;
         private static float shotGain = 0.3f;

        public static void Init()
        {
            var cache = Application.Current.ResourceCache;
            mainMusic = cache.GetSound("Music/music.ogg");
            mainMusic.Looped = true;


            Node musicNode = GameRoot.Scene.CreateChild("Music");
            musicSource = musicNode.CreateComponent<SoundSource>();
            musicSource.SetSoundType(SoundType.Music.ToString());


            for (int i = 1; i <= 8; i++)
            {
                 string fileName = String.Format("Sounds/explosion-0{0}.wav" , i);
                 Sound explosion = cache.GetSound(fileName);
                 explosions.Add(explosion);

            }

            for (int i = 1; i <= 4; i++)
            {
                string fileName = String.Format("Sounds/shoot-0{0}.wav", i);
                Sound shot = cache.GetSound(fileName);
                shots.Add(shot);
            }

        }


        public static void PlayMusic()
        {
            if(musicSource.Playing == false)
            {
                musicSource.Play(mainMusic);
            }   
        }

        public static void PlayExplosion()
        {
            Node soundNode = GameRoot.Scene.CreateChild();
            SoundSource soundSource = soundNode.CreateComponent<SoundSource>();
            soundSource.SetSoundType(SoundType.Effect.ToString());
            soundSource.AutoRemoveMode = AutoRemoveMode.Node;
            soundSource.Gain = explosingGain;
            var explosing = explosions[rand.Next(explosions.Count)];
            soundSource.Play(explosing);
        }

        public static void PlayShot()
        {
            Node soundNode = GameRoot.Scene.CreateChild();
            SoundSource soundSource = soundNode.CreateComponent<SoundSource>();
            soundSource.SetSoundType(SoundType.Effect.ToString());
            soundSource.AutoRemoveMode = AutoRemoveMode.Node;
            soundSource.Gain = shotGain;
            var shot = shots[rand.Next(shots.Count)];
            soundSource.Play(shot);
        }

    }

}