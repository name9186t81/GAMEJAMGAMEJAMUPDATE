using UnityEngine;

namespace Assets.Scripts.Audio
{
    public sealed class MusicController : MonoSingletone<MusicController>
    {
        [SerializeField] private AudioClip[] _tracks;
        [SerializeField] private float _minDelay;
        [SerializeField] private float _maxDelay;
        [SerializeField] private float _fadeInTime;
        [SerializeField] private float _baseVolume;
        [SerializeField] private float _initialDelay;
        [SerializeField] private AudioSource _source;

        private AudioClip _selected;
        private float _delay;
        private float _elapsed;

        protected override MusicController GetInstance()
        {
            return this;
        }

        private void Start()
        {
            _delay = _initialDelay;
        }

        private void Update()
        {
            if(_delay > 0f)
            {
                _delay -= Time.deltaTime;
                return;
            }

            if(_selected == null)
            {
                _selected = _tracks[Random.Range(0, _tracks.Length - 1)];
                _elapsed = 0f;

                _source.clip = _selected;
                _source.Play();
            }

            _elapsed += Time.deltaTime;
            _source.volume = Mathf.Clamp01(_elapsed / _fadeInTime) * _baseVolume;
            _source.transform.position = Camera.main.transform.position;

            if (!_source.isPlaying)
            {
                _delay = Random.Range(_minDelay, _maxDelay);
            }
        }

        public float Volume
        {
            get => _baseVolume;
            set => _baseVolume = value;
        }
    }
}
