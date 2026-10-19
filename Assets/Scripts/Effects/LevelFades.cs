using Architecture;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Effects
{
    public sealed class LevelFades : MonoBehaviour
    {
        [SerializeField] private float _fadeInTime = 1f;
        [SerializeField] private RawImage _target;

        private float _elapsed;
        private float _delay;
        private bool _reverse;

        private void Start()
        {
            MiscEvents.Instance.OnLevelLoad += Load;

            _delay = _fadeInTime;
        }

        private void Update()
        {
            if (_elapsed > _delay) return;

            _elapsed += Time.deltaTime;

            float dt = _elapsed / _delay;
            if (_reverse) dt = 1 - dt;
            _target.color = Color.Lerp(Color.black, Color.clear, dt);
        }

        private void Load(float obj)
        {
            _reverse = true;
            _delay = obj;
            _elapsed = 0f;
        }
    }
}
