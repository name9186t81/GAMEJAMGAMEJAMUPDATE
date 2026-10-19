using Audio;
using Effects;
using UnityEngine;

namespace Mechanics
{
    public sealed class CameraProjector : MonoBehaviour, IInteractable
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteAnimation _idleAnimation;
        [SerializeField] private SpriteAnimation _spawnAnimation;
        [SerializeField] private float _spawnDelay;

        [SerializeField] private AudioClip _idleClip;
        [SerializeField] private AudioClip _placeClip;
        [SerializeField] private AudioClip _takingClip;

        private AudioSource _idleSource;
        private float _delay;
        private bool _gettingDestroyed;
        private GameObject _target;

        public Vector2 Position => transform.position;

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnDeath += Death;
            }

            AudioController.Instance.PlayAudio(_placeClip, transform.position, 1f, 1f);
            _delay = _idleClip.length;
        }

        private void Update()
        {
            _spawnDelay -= Time.deltaTime;

            if(_spawnDelay < 0f)
            {
                _spawnAnimation.Stop();
                _idleAnimation.Play();
            }

            _delay -= Time.deltaTime;
            Vector2 player = PlayerEvents.Instance.Player.transform.position;
            float dist = Vector2.Distance(player, transform.position);

            if(_idleSource != null)
            {
                _idleSource.volume = 1 - Mathf.Clamp01(dist / 5);
            }

            if(_delay < 0f)
            {
                _idleSource = null;
                if(dist < 5f)
                {
                    _idleSource = AudioController.Instance.PlayAudio(_idleClip, transform.position, 1f, 1f);
                    _delay = _idleClip.length;
                }
            }
        }

        private void Death(Health.DamageArgs obj)
        {
            if (_gettingDestroyed) return;
            _gettingDestroyed = true;
            Destroy(_target);
            Destroy(gameObject);
        }

        public void Interact(AbstractGameActor target)
        {
            if (_gettingDestroyed) return;
            if (target is Player player)
            {
                player.AddProjector();
            }

            if (_idleSource != null)
            {
                _idleSource.Stop();
            }
            AudioController.Instance.PlayAudio(_takingClip, transform.position, 1f, 1f);
            _gettingDestroyed = true;
            Destroy(_target);
            Destroy(gameObject);
        }

        public void Project(Vector2 worldPos, GameCamera cam, bool needFlip)
        {
            _target = cam.Project(worldPos);
            _renderer.flipX = needFlip;
        }

        private void OnDestroy()
        {
            Destroy(_target);
        }
    }
}
