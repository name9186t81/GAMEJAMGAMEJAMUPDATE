using Movement;
using UnityEngine;
using Utils;

namespace Mechanics
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : IDamagable
    {
        [SerializeField, Range(0, 1f)] private float _acceleration;

        [SerializeField] private float _dashDistance;
        [SerializeField] private float _dashTime;
        [SerializeField] private AnimationCurve _dashCurve;

        [SerializeField] private float _jumpTime;
        [SerializeField] private float _jumpHeight;
        [SerializeField] private float _jumpHeightMin;
        [SerializeField] private AnimationCurve _jumpCurve;

        [SerializeField] private float _maxGravityStrength;
        [SerializeField] private float _gravityGainPerSecond;

        [SerializeField] private GameCamera _camera;

        private Rigidbody2D _rig;

        private Vector2 _gravityForce;

        private float _walkDirection;
        private Vector2 _walkForce;

        private float _jumpElapsed;
        private float _minJumpTime;
        private bool _isJumping;
        private bool _needJumping;

        private bool _isDashing;
        private Vector2 _initialDashDirection;
        private float _dashElapsed;
        private float _dashIntegral;

        private Vector2 _instantVelocity;
        private Vector2 _velocity;

        protected override void Init()
        {
            _rig = GetComponent<Rigidbody2D>();
            _dashIntegral = _dashCurve.Area();
            _rig.freezeRotation = true;
            _minJumpTime = _jumpHeightMin / _jumpHeight * _jumpTime * 0.5f;

            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.RegisterPlayer(this);
            }
        }

        private void Update()
        {
            _walkDirection = 0;
            if (Input.GetKey(KeyCode.D))
            {
                _walkDirection += 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                _walkDirection -= 1;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && !_isDashing)
            {
                _isDashing = true;
                _initialDashDirection = _movementDirection.sqrMagnitude < 0.05f ? new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) : _movementDirection;
                _dashElapsed = 0f;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !_isJumping)
            {
                const float CAYOTE_TIME = 0.1f;
                _isJumping = GroundDetector.IsGrounded || GroundDetector.TimeSinceGrounded < CAYOTE_TIME;
                _jumpElapsed = 0f;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                _needJumping = true;
            }
            else
            {
                _needJumping = false;
            }
        }

        private void FixedUpdate()
        {
            _instantVelocity += _walkForce;

            _walkForce = Vector2.Lerp(_walkForce, new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _walkDirection * BaseSpeed, _acceleration);
            if (_isJumping)
            {
                DoJump(Time.deltaTime);
            }
            if (_isDashing)
            {
                DoDash(Time.deltaTime);
            }
            DoGravity();

            _velocity *= 0.95f;

            _rig.linearVelocity = _velocity + _instantVelocity + _gravityForce;
            _instantVelocity = Vector2.zero;
        }

        private void DoGravity()
        {
            if (_isJumping || GroundDetector.IsGrounded)
            {
                _gravityForce = Vector2.zero;
                return;
            }

            _gravityForce += _gravityGainPerSecond * Time.deltaTime * GroundDetector.GravityDirection;
            if(_gravityForce.sqrMagnitude > _maxGravityStrength * _maxGravityStrength)
            {
                _gravityForce = _gravityForce.normalized * _maxGravityStrength;
            }
        }

        private void DoJump(float timeDelta)
        {
            float dt = _jumpElapsed / _jumpTime;
            _jumpElapsed += timeDelta;

            if (GroundDetector.IsGrounded && _jumpElapsed > 0.3f) //grounded - break jump
            {
                _isJumping = false;
                _jumpElapsed = 0f;
                return;
            }

            if (dt > 1)
            {
                _isJumping = false;
                _jumpElapsed = 0f;

                float moment1 = _jumpCurve.SafeEvaluate(dt - timeDelta * 2); //assume timeDelta is constant
                float moment2 = _jumpCurve.SafeEvaluate(dt - timeDelta);
                Vector2 vel = GroundDetector.GravityDirection * (_jumpHeight * (moment1 - moment2) / timeDelta);

                _gravityForce += vel;
                return;
            }

            if (!_needJumping && _jumpElapsed > _minJumpTime && dt < 0.5f)
            {
                _jumpElapsed = 0.5f * _jumpTime + timeDelta; //todo find jump apex instead of assuming its on 0.5 position
                dt = 0.5f;
            }

            float moment = _jumpCurve.SafeEvaluate(dt);
            float nextMoment = _jumpCurve.SafeEvaluate(dt + timeDelta / _jumpTime);
            _instantVelocity -= GroundDetector.GravityDirection * (_jumpHeight * (nextMoment - moment) / timeDelta);
        }

        private void DoDash(float timeDelta)
        {
            float dt = _dashElapsed / _dashTime;
            _dashElapsed += timeDelta;

            if (dt > 1)
            {
                _isDashing = false;
                return;
            }

            _instantVelocity += _initialDashDirection * (_dashDistance * _dashCurve.SafeEvaluate(dt) / (_dashTime * _dashIntegral));
        }

        protected override Vector2 GetVelocity()
        {
            return _velocity + _instantVelocity;
        }

        protected override ActorCollider[] GetAllColliders()
        {
            return null;
        }

        public GameCamera Camera => _camera;
    }
}
