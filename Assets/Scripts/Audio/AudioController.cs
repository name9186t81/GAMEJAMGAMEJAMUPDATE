using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Audio
{
    public sealed class AudioController : MonoSingletone<AudioController>
    {
        private class Timed
        {
            public float Delay;
            public AudioSource Source;

            public Timed(float delay, AudioSource source)
            {
                Delay = delay;
                Source = source;
            }
        }
        [SerializeField] private float _volumeBase = 1;
        [SerializeField] private int _maxSources = 32;

        private List<Timed> _delays = new List<Timed>();
        private Stack<AudioSource> _freeSources = new Stack<AudioSource>();

        protected override AudioController GetInstance()
        {
            return this;
        }

        private void Update()
        {
            List<Timed> newList = new List<Timed>();
            for (int i = 0, length = _delays.Count; i < length; i++)
            {
                _delays[i].Delay -= Time.deltaTime;

                if (_delays[i].Delay < 0)
                {
                    _freeSources.Push(_delays[i].Source);
                }
                else
                {
                    newList.Add(_delays[i]);
                }
            }
        }
        public AudioSource PlayAudio(AudioClip clip, Vector2 position, float pitchMod, float volumeMod)
        {
            AudioSource source;

            if (!_freeSources.TryPop(out source))
            {
                var go = new GameObject();
                go.name = "AUDIO_SOURCE";
                go.transform.SetParent(Camera.main.transform, false);

                source = go.AddComponent<AudioSource>();
            }

            var dir = position.DirectionNormalized(Camera.main.transform.position);
            source.clip = clip;
            source.panStereo = -dir.x;
            source.volume = _volumeBase * volumeMod;
            source.pitch = pitchMod;
            source.Play();

            _delays.Add(new Timed(clip.length, source));
            return source;
        }

        public float AudioVolume
        {
            get => _volumeBase;
            set => _volumeBase = value;
        }
    }
}
