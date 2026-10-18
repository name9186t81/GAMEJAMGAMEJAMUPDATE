using UnityEngine;

namespace Effects
{
    public class SpriteAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _sprites;
        [SerializeField] private float _timePerSprite;
        [SerializeField] private bool _playAtTheStart;
        [SerializeField] private bool _repeat;
        [SerializeField] private bool _startFromRandomIndex;

        private bool _isPlaying;
        private float _elapsed;
        private int _index;

        private void Awake()
        {
            if (_playAtTheStart)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _elapsed += Time.deltaTime;

            if(_elapsed > _timePerSprite)
            {
                _elapsed -= _timePerSprite;

                _index++;
                if(_index == _sprites.Length)
                {
                    if (_repeat) Play();
                    else _isPlaying = false;
                    return;
                }

                _renderer.sprite = _sprites[_index];
            }
        }

        public void Play()
        {
            _isPlaying = true;
            _elapsed = 0f;
            _index = _startFromRandomIndex ? Random.Range(0, _sprites.Length - 1) : 0;
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        public void Reset()
        {
            _isPlaying = false;
            _index = 0;
            _elapsed = 0;
        }
    }
}