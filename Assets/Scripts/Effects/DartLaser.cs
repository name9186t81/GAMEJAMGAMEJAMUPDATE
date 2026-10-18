using Mechanics;
using UnityEngine;
using UnityEngine.UIElements;
using Utils;

namespace Effects
{
    public class DartLaser : MonoBehaviour
    {
        [SerializeField] private AnimationCurve _laserFalloff;
        [SerializeField] private float _laserLifeTime;
        [SerializeField] private LineRenderer _renderer;
        [SerializeField] private Texture2D _beam;
        [SerializeField] private DartsTrap _trap;

        private bool _isFiring;
        private float _firingElapsed;

        private void Awake()
        {
            _renderer.positionCount = 0;
            _renderer.material.SetTexture("_MainTex", _beam);

            _trap.OnFire += Fire;
        }

        private void Update()
        {
            if (!_isFiring) return;

            _firingElapsed += Time.deltaTime;
            if(_firingElapsed > _laserLifeTime)
            {
                _isFiring = false;
                _renderer.widthMultiplier = 0;
                _renderer.positionCount = 0;
                return;
            }

            float delta = _firingElapsed / _laserLifeTime;
            _renderer.widthMultiplier = _laserFalloff.SafeEvaluate(delta);
        }
        private void Fire(RaycastHit2D obj)
        {
            Vector2 endPoint;
            if (obj)
            {
                endPoint = obj.point;
            }
            else
            {
                endPoint = transform.right * 100;
            }

            _renderer.positionCount = 2;
            _renderer.SetPosition(0, transform.position);
            _renderer.SetPosition(1, endPoint);
            _isFiring = true;
            _firingElapsed = 0f;
        }
    }
}
