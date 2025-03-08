﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace RogueEssence.Content
{
    public class SongSetting
    {
        public LoopedSong Song;
        public float CrossVolume;

        public SongSetting(LoopedSong song, float crossVolume)
        {
            Song = song;
            CrossVolume = crossVolume;
        }
    }
    public static class SoundManager
    {
        static TimeSpan currentTime;

        static float bgmVol;

        static Dictionary<string, SongSetting> songs;

        static float bgmBalance;
        public static float BGMBalance
        {
            get { return bgmBalance; }
            set
            {
                bgmBalance = value;
                updateSongVolume();
            }
        }
        static float seBalance;
        public static float SEBalance
        {
            get { return seBalance; }
            set { seBalance = value; }
        }

        private static Dictionary<string, LoopedSong> loopedSE;
        private static List<DynamicSoundEffectInstance> sounds;

        private static string[] playedSounds = new string[8];
        private static int soundIndex = 0;

        public static void InitStatic()
        {
            bgmBalance = 1f;
            seBalance = 1f;
            songs = new Dictionary<string, SongSetting>();
            loopedSE = new Dictionary<string, LoopedSong>();
            sounds = new List<DynamicSoundEffectInstance>();
        }

        public static void PlayBGM(string baseFile, string[] family)
        {
            foreach(string oldFile in songs.Keys)
            {
                LoopedSong song = songs[oldFile].Song;
                song.Stop();
            }
            songs.Clear();

            foreach (string fileName in family)
            {
                LoopedSong song = new LoopedSong(fileName);
                float volume = (fileName == baseFile) ? 1f : 0f;
                songs.Add(fileName, new SongSetting(song, volume));
            }
            updateSongVolume();

            foreach (SongSetting song in songs.Values)
            {
                song.Song.Play();
            }
        }

        public static void SetBGMVolume(float volume)
        {
            bgmVol = volume;
            updateSongVolume();
        }

        public static void SetBGMCrossVolume(Dictionary<string, float> volumes)
        {
            foreach (string fileName in volumes.Keys)
                songs[fileName].CrossVolume = volumes[fileName];

            updateSongVolume();
        }

        private static void updateSongVolume()
        {
            foreach (SongSetting song in songs.Values)
                song.Song.Volume = bgmVol * BGMBalance * (float)Math.Log10(song.CrossVolume * 9f + 1f);
        }


        public static void PlayLoopedSE(string fileName, float volume = 1.0f)
        {
            if (loopedSE.ContainsKey(fileName))
                return;

            if (!String.IsNullOrWhiteSpace(fileName))
            {
                LoopedSong se = new LoopedSong(fileName);
                se.Play();
                float seVol = volume;
                se.Volume = seVol * seVol;
                loopedSE.Add(fileName, se);
            }
        }

        public static void StopLoopedSE(string fileName, float volume = 1.0f)
        {
            LoopedSong se;
            if (loopedSE.TryGetValue(fileName, out se))
            {
                se.Stop();
                loopedSE.Remove(fileName);
            }
        }

        public static void StopAllLoopedSE()
        {
            foreach (string fileName in loopedSE.Keys)
                loopedSE[fileName].Stop();
            loopedSE.Clear();
        }

        public static void SetLoopedSEVolume(string fileName, float volume)
        {
            LoopedSong se;
            if (loopedSE.TryGetValue(fileName, out se))
                se.Volume = volume * SEBalance;
        }

        public static void NewFrame(GameTime gameTime)
        {
            soundIndex = 0;
            for (int ii = sounds.Count - 1; ii >= 0; ii--)
            {
                if (sounds[ii].PendingBufferCount == 0)
                {
                    sounds[ii].Stop();
                    sounds.RemoveAt(ii);
                }
            }
            currentTime = gameTime.TotalGameTime;
            //if (song != null && song.State == SoundState.Stopped)
            //{
            //    song.Play();
            //    songTime = gameTime.TotalGameTime;
            //}
            //if (crossSong != null && crossSong.State == SoundState.Stopped)
            //{
            //    long gameTimeSamples = song.GetSampleFromTimeSpan(gameTime.TotalGameTime - songTime);
            //    long audioEngineSamples = song.GetSamplesPlayed();
            //    System.Diagnostics.Debug.WriteLine("{0}\n{1}\n---", gameTimeSamples, audioEngineSamples);
            //    crossSong.PlayAt(gameTimeSamples);
            //    songTime = gameTime.TotalGameTime;
            //}
        }



        public static int PlaySound(string fileName, float volume = 1.0f)
        {
            if (volume * seBalance <= 0f)
                return 0;

            //don't play more than X sound effects in one frame
            if (soundIndex == playedSounds.Length)
                return 0;

            //don't play more than one instance of the same sound in one frame
            for (int ii = 0; ii < soundIndex; ii++)
            {
                if (fileName == playedSounds[ii])
                    return 0;
            }
            playedSounds[soundIndex] = fileName;
            soundIndex++;

            IntPtr stbVorbisData = FAudio.stb_vorbis_open_filename(fileName, out int error, IntPtr.Zero);
            FAudio.stb_vorbis_info fileInfo = FAudio.stb_vorbis_get_info(stbVorbisData);


            long total_samples = FAudio.stb_vorbis_stream_length_in_samples(stbVorbisData);
            long total_frames = total_samples * 60 / fileInfo.sample_rate;
            float[] chunk = new float[fileInfo.channels * total_samples];
            int framesRead = FAudio.stb_vorbis_get_samples_float_interleaved(stbVorbisData, fileInfo.channels, chunk, fileInfo.channels * (int)total_samples);
            FAudio.stb_vorbis_close(stbVorbisData);


            DynamicSoundEffectInstance soundStream = new DynamicSoundEffectInstance(
                (int)fileInfo.sample_rate,
                (fileInfo.channels == 1) ? AudioChannels.Mono : AudioChannels.Stereo
            );
            soundStream.Volume = volume * seBalance;
            soundStream.SubmitFloatBufferEXT(chunk, 0, framesRead * fileInfo.channels);
            soundStream.Play();
            sounds.Add(soundStream);

            return (int)total_frames;
        }

    }
}
