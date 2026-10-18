using Mechanics;
using UnityEngine;

namespace Architecture
{
    public sealed class MiscEvents : MonoBehaviour
    {
        [SerializeField] private GameCamera _cameraPrefab;
        [SerializeField] private Sprite _deathWatchTrace;
        [SerializeField, Range(0, 1f)] private float _deathWatchChance;

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
        }

        private void Death(Health.DamageArgs obj)
        {
            float rnd = Random.value;

            if(rnd < _deathWatchChance)
            {
                InitDeathWatch();
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
    }
}
