using System;
using UnityEngine;

namespace Mechanics
{
    public sealed class PlayerRespawner : MonoBehaviour
    {
        [SerializeField] private Player _playerPrefab;
        [SerializeField] private float _respawnTime;

        public event Action OnRespawnBegin;
        public event Action OnRespawn;

        private bool _respawning;

        private float _respawnElapsed;

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnDeath += PlayerDeath;
            }
        }

        private void Update()
        {
            if (_respawning)
            {
                _respawnElapsed += Time.deltaTime;
                if (_respawnElapsed > _respawnTime)
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        var player = Instantiate(_playerPrefab);
                        _playerPrefab.transform.position = transform.position;

                        OnRespawn?.Invoke();
                        _respawning = false;
                    }
                }
            }
        }

        private void PlayerDeath(Health.DamageArgs obj)
        {
            OnRespawnBegin?.Invoke();

            _respawnElapsed = 0f;
            _respawning = true;
        }
    }
}
