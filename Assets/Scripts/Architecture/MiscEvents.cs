using IK;
using Mechanics;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Architecture
{
    public sealed class MiscEvents : MonoSingletone<MiscEvents>
    {
        [SerializeField] private Chain[] _shakeables;
        [SerializeField] private GameCamera _cameraPrefab;
        [SerializeField] private Sprite _deathWatchTrace;
        [SerializeField, Range(0, 1f)] private float _deathWatchChance;

        private string _nextLevelName;
        private float _loadTime;
        private bool _loadingLevel;
        private float _loadingElapsed;

        public event Action OnEarthQuake;
        public event Action<float> OnLevelLoad;
        private float _elapsedShake;

        private GameCamera _deathWatchCamera;
        private float _deathWatchElapsed;
        private float _deathWatchTime;
        private bool _deathWatch;

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnDeath += Death;
            }
        }

        private void Update()
        {
            if (_deathWatch)
            {
                _deathWatchElapsed += Time.deltaTime;
                _deathWatchCamera.EnableTrace();
                _deathWatchCamera.TraceRenderer.sprite = _deathWatchTrace;
                _deathWatchCamera.LocalOffset = 0f;

                if(_deathWatchElapsed > _deathWatchTime)
                {
                    Destroy(_deathWatchCamera.gameObject);
                    _deathWatch = false;
                }
            }

            if (_loadingLevel)
            {
                _loadingElapsed += Time.deltaTime;

                if(_loadingElapsed > _loadTime)
                {
                    _loadingLevel = false;
                    SceneManager.LoadScene(_nextLevelName);
                }
            }
        }

        private void Death(Health.DamageArgs obj)
        {
            float rnd = UnityEngine.Random.value;

            if(rnd < _deathWatchChance)
            {
                InitDeathWatch();
            }
        }

        public void LoadLevel(string name, float delay)
        {
            _loadTime = delay;
            _nextLevelName = name;
            _loadingElapsed = 0f;

            OnLevelLoad?.Invoke(delay);
        }

        public void Earthquake()
        {
            OnEarthQuake?.Invoke();
        }

        public void EarthquakeBig()
        {
            Earthquake();

            if (_shakeables == null) return;
            _elapsedShake = 5.2f;
            StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            while (_elapsedShake > 0f)
            {
                for (int i = 0; i < _shakeables.Length; i++)
                {
                    for (int j = 0; j < _shakeables[i].Points.Length; j++)
                    {
                        float rnd = UnityEngine.Random.value;
                        float angle = rnd * Mathf.PI * 2;
                        _shakeables[i].Points[j].Velocity += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.05f;
                    }
                }

                _elapsedShake -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void InitDeathWatch()
        {
            _deathWatchCamera = Instantiate(_cameraPrefab);
            _deathWatchCamera.transform.position = PlayerEvents.Instance.Player.transform.position;
            _deathWatchElapsed = 0f;
            _deathWatchTime = PlayerEvents.Instance.Player.DyingTime;
            _deathWatch = true;
        }

        protected override MiscEvents GetInstance()
        {
            return this;
        }
    }
}
