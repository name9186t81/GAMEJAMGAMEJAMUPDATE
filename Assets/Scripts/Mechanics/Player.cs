using Architecture;
using Effects;
using Movement;
using NUnit.Framework.Constraints;
using System;
using UnityEngine;
using Utils;

namespace Mechanics
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : AbstractGameActor, IReactToPicture
    {
        private enum State
        {
            Walking,
            Jumping,
            PlacingProjector,
            Falling,
            TakingPicture,
            Dying,
            Idle,
            JumpPeak,
            JumpPrepare,
            Ascending,
            Landing
        } 

        [Header("Walking")]
        [SerializeField, Range(0, 1f)] private float _acceleration;

        [Space]
        [Header("Projectors")]
        [SerializeField] private int _initialProjectors;
        [SerializeField] private float _projectorPlaceTime;
        [SerializeField] private float _projectorPlaceOffset;
        [SerializeField] private LayerMask _validProjectorMask;
        [SerializeField] private CameraProjector _projectorPrefab;
        [SerializeField] private SpriteRenderer _projectorPreview;

        [Space]
        [Header("Jumping")]
        [SerializeField] private float _jumpTime;
        [SerializeField] private float _jumpHeight;
        [SerializeField] private float _jumpHeightMin;
        [SerializeField] private AnimationCurve _jumpCurve;

        [Space]
        [Header("Gravity")]
        [SerializeField] private float _maxGravityStrength = 50;
        [SerializeField] private float _gravityGainPerSecond = 25;

        [Space]
        [Header("Camera")]
        [SerializeField] private float _minCameraChargeTime;
        [SerializeField] private GameCamera _camera;

        [Space]
        [Header("Interactables")]
        [SerializeField] private LayerMask _interactablesMask;
        [SerializeField] private float _interactablesFindRadius;

        [Space]
        [Header("Death")]
        [SerializeField] private float _deathTime;

        [Space]
        [Header("Misc")]
        [SerializeField] private SpriteAnimation _idleAnimation;
        [SerializeField] private SpriteAnimation _walkAnimation;
        [SerializeField] private SpriteAnimation _jumpAnimation;
        [SerializeField] private SpriteAnimation _fallAnimation;
        [SerializeField] private SpriteAnimation _placeProjectorAnimation;
        
        [SerializeField] private ProgressBar _bar;

        public event Action<Vector2> OnPictureTaken;

        private Collider2D[] _cached = new Collider2D[64];

        private Rigidbody2D _rig;

        private Vector2 _gravityForce;

        private float _lastNonZeroWalkDirection = 1;
        private float _walkDirection;
        private Vector2 _walkForce;

        private float _jumpElapsed;
        private float _minJumpTime;
        private float _landingCooldown;
        private bool _isJumping;
        private bool _needJumping;

        private Vector2 _instantVelocity;
        private Vector2 _velocity;

        private bool _wantCameraCharge;
        private float _cameraChargeElapsed;

        private bool _wantProjector;
        private float _projectorElapsed;

        private int _totalProjectors;

        private IInteractable _nearestInteractable;

        private bool _isDying;
        private float _deathElapsed;

        private State _state;
        private SpriteAnimation _currentAnimation;

        protected override void Init()
        {
            _totalProjectors = _initialProjectors;
            _rig = GetComponent<Rigidbody2D>();
            _rig.freezeRotation = true;
            _minJumpTime = _jumpHeightMin / _jumpHeight * _jumpTime * 0.5f;

            GroundDetector.OnLanding += Landed;
            _camera.OnPictureTaken += (vec) => OnPictureTaken?.Invoke(vec);
            OnDeath += (_) => _isDying = true;
        }

        private void Landed()
        {
            if(GroundDetector.TimeSinceGrounded > 0.5f)
            {
                _landingCooldown = 0.1f;
            }
        }

        private void Start()
        {
            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.RegisterPlayer(this);
            }
        }

        private void Update()
        {
            AnimationLogic();

            if (_isDying) return;

            _walkDirection = 0;
            if (Input.GetKey(KeyCode.D))
            {
                _walkDirection += 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                _walkDirection -= 1;
            }

            bool landed = _landingCooldown > 0f;
            if (landed)
            {
                _walkDirection = 0f;
            }

            if(_walkDirection != 0)
            {
                _lastNonZeroWalkDirection = _walkDirection;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !_isJumping && !_wantCameraCharge && !landed)
            {
                const float CAYOTE_TIME = 0.1f;
                _isJumping = GroundDetector.IsGrounded || GroundDetector.TimeSinceGrounded < CAYOTE_TIME;
                _jumpElapsed = 0f;
            }

            if (Input.GetKey(KeyCode.Space) && !_wantCameraCharge && !landed)
            {
                _needJumping = true;
            }
            else
            {
                _needJumping = false;
            }

            bool wanted = _wantCameraCharge;
            _wantCameraCharge = Input.GetKey(KeyCode.Mouse0) && !_isJumping && !landed;

            if (!wanted && _wantCameraCharge)
            {
                _camera.EnableTrace();
            }

            if (_wantCameraCharge)
            {
                if (_isJumping)
                {
                    _wantCameraCharge = false;
                    _cameraChargeElapsed = 0f;
                    _camera.DisableTrace();
                }

                _walkDirection = 0f;
                _cameraChargeElapsed += Time.deltaTime;

                if(_cameraChargeElapsed > _minCameraChargeTime)
                {
                    _camera.TraceRenderer.color = Color.green;
                }
                else
                {
                    _camera.TraceRenderer.color = Color.white;
                }
            }

            if (Input.GetKeyUp(KeyCode.Mouse0) && !landed)
            {
                if (_cameraChargeElapsed > _minCameraChargeTime)
                {
                    _camera.TakePicture();

                    _wantCameraCharge = false;
                    _cameraChargeElapsed = 0f;
                    _camera.DisableTrace();
                }
                else
                {
                    _wantCameraCharge = false;
                    _cameraChargeElapsed = 0f;
                    _camera.DisableTrace();
                }
            }

            wanted = _wantProjector;
            _wantProjector = Input.GetKey(KeyCode.Mouse1) && !_isJumping && !_wantCameraCharge && _camera.HasPicture && !landed;

            if (!wanted && _wantProjector)
            {
                _camera.EnableTrace();
                _camera.EnableTraceProjection();

                _projectorPreview.gameObject.SetActive(true);
                _bar.gameObject.SetActive(true);
            }

            if (_wantProjector)
            {
                _walkDirection = 0f;

                _projectorElapsed += Time.deltaTime;
                _bar.Progress = _projectorElapsed / _projectorPlaceTime;

                var ray = Physics2D.Raycast(
                    (Vector2)transform.position + _lastNonZeroWalkDirection * new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _projectorPlaceOffset,
                    GroundDetector.GravityDirection,
                    1f,
                    _validProjectorMask);

                if (ray && _totalProjectors > 0)
                {
                    _projectorPreview.transform.position = ray.point;
                    _projectorPreview.color = Color.green;
                }
                else
                {
                    _projectorPreview.transform.position = (Vector2)transform.position + _lastNonZeroWalkDirection * new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _projectorPlaceOffset + GroundDetector.GravityDirection * 0.5f;
                    _projectorPreview.color = Color.red;
                }
            }

            if(Input.GetKey(KeyCode.Mouse1) && GroundDetector.TimeSinceGrounded > 0.05f)
            {
                _projectorPreview.color = Color.red;
            }

            if (Input.GetKeyUp(KeyCode.Mouse1) && !landed)
            {
                var ray = Physics2D.Raycast(
                    (Vector2)transform.position + _lastNonZeroWalkDirection * new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _projectorPlaceOffset, 
                    GroundDetector.GravityDirection, 
                    1f, 
                    _validProjectorMask);

                if (_projectorElapsed > _projectorPlaceTime && ray && _totalProjectors > 0)
                {
                    var projector = Instantiate(_projectorPrefab);
                    projector.transform.position = ray.point;
                    projector.Project(_camera.ExpectedPos, _camera);

                    _wantProjector = false;
                    _projectorElapsed = 0f;
                    _camera.DisableTrace();
                    _camera.DisableTraceProjection();
                    _projectorPreview.gameObject.SetActive(false);
                    _bar.gameObject.SetActive(false);
                    _totalProjectors--;
                }
                else
                {
                    _wantProjector = false;
                    _projectorElapsed = 0f;

                    _camera.DisableTrace();
                    _camera.DisableTraceProjection();
                    _projectorPreview.gameObject.SetActive(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.E) && _nearestInteractable != null)
            {
                _nearestInteractable.Interact(this);
            }
        }

        private void AnimationLogic()
        {
            if (_isDying)
            {
                SwitchState(State.Dying);
                return;
            }
            if (_wantProjector)
            {
                SwitchState(State.PlacingProjector);
                return;
            }
            if (_wantCameraCharge)
            {
                SwitchState(State.TakingPicture);
                return;
            }
            if (_isJumping)
            {
                float dt = _jumpElapsed / _jumpTime;

                if(dt < 0.15f)
                {
                    SwitchState(State.JumpPrepare);
                }
                else if(dt < 0.4f)
                {
                    SwitchState(State.Ascending);
                }
                else if(dt < 0.6f)
                {
                    SwitchState(State.JumpPeak);
                }
                if (dt > 0.6f)
                {
                    SwitchState(State.Falling);
                }

                SwitchState(State.Jumping);
                return;
            }
            if(GroundDetector.TimeSinceGrounded > 0.1f)
            {
                SwitchState(State.Falling);
                return;
            }
            if(_landingCooldown > 0f)
            {
                SwitchState(State.Landing);
                return;
            }
            if(Mathf.Abs(_walkDirection) > 0.1f)
            {
                SwitchState(State.Walking);
                return;
            }
            SwitchState(State.Idle);
        }

        private void SwitchState(State state)
        {
            if(_state != state)
            {
                _currentAnimation.Stop();
                _currentAnimation.Reset();

                switch (state)
                {
                    case State.Walking: _currentAnimation = _walkAnimation; break;
                    case State.Jumping: _currentAnimation = _jumpAnimation; break;
                    case State.Falling: _currentAnimation = _fallAnimation; break;
                    case State.PlacingProjector: _currentAnimation = _placeProjectorAnimation; break;
                    default: _currentAnimation = _idleAnimation; break;
                }

                _currentAnimation.Play();
            }
        }

        private void FixedUpdate()
        {
            if (_isDying)
            {
                _rig.linearVelocity = Vector2.zero;
                _deathElapsed += Time.deltaTime;

                if(_deathElapsed > _deathTime)
                {
                    Destroy(gameObject);
                }

                return;
            }

            _instantVelocity += _walkForce;

            _walkForce = Vector2.Lerp(_walkForce, new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _walkDirection * BaseSpeed, _acceleration);
            if (_isJumping)
            {
                DoJump(Time.deltaTime);
            }
            DoGravity();

            _velocity *= 0.95f;

            _rig.linearVelocity = _velocity + _instantVelocity + _gravityForce;
            _instantVelocity = Vector2.zero;

            ScanForInteractables();
        }

        private void ScanForInteractables()
        {
            var overlap = Physics2D.OverlapCircle(transform.position, _interactablesFindRadius, new ContactFilter2D() { layerMask = _interactablesMask }, _cached);
            float dist = 999f;
            _nearestInteractable = null;

            for (int i = 0; i < overlap; i++)
            {
                if (_cached[i].transform.TryGetComponent<IInteractable>(out var interactable))
                {
                    float localDist = Vector2.Distance(transform.position, interactable.Position);
                    if (localDist < dist)
                    {
                        _nearestInteractable = interactable;
                        dist = localDist;
                    }
                }
            }
        }

        private void DoGravity()
        {
            if (_isJumping || GroundDetector.IsGrounded)
            {
                _gravityForce = Vector2.zero;
                return;
            }

            _gravityForce += _gravityGainPerSecond * Time.deltaTime * GroundDetector.GravityDirection;
            if (_gravityForce.sqrMagnitude > _maxGravityStrength * _maxGravityStrength)
            {
                _gravityForce = _gravityForce.normalized * _maxGravityStrength;
            }
        }

        private void DoJump(float timeDelta)
        {
            float dt = _jumpElapsed / _jumpTime;
            _jumpElapsed += timeDelta;

            if ((GroundDetector.IsGrounded || (GroundDetector.Details & GroundDetector.CollisionDetails.TouchingCeiling) != 0) && _jumpElapsed > 0.3f) //grounded - break jump
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

        protected override Vector2 GetVelocity()
        {
            return _velocity + _instantVelocity;
        }

        protected override ActorCollider[] GetAllColliders()
        {
            return null;
        }

        public void AddProjector() => _totalProjectors++;

        public void ReactToPicture(Vector2 cameraPos, GameCamera cam)
        {
            if(cam != _camera)
            {
                TakeDamage(new Health.DamageArgs() { Damage = 5 });
            }
        }

        public GameCamera Camera => _camera;

        public bool FocusingCamera => _wantCameraCharge;
        public bool ChargingProjector => _wantProjector;

        public bool IsDying => _isDying;
        public float DyingTime => _deathTime;
        public float DyingTimeElapsed => _deathElapsed;

        public Vector2 WalkForce => _walkForce;
    }
}
