using Audio;
using Effects;
using Movement;
using System;
using UnityEngine;
using Utils;

namespace Mechanics
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Player : AbstractGameActor, IReactToPicture
    {
        private class Arm
        {
            public SpriteRenderer ArmRenderer1;
            public SpriteRenderer ArmRenderer2;
            public float MaxDist;

            public void Update(Vector2 start, Vector2 end)
            {
                var dir = start.DirectionNormalized(end, out float dist);

                if(dist > MaxDist)
                {
                    ArmRenderer1.transform.position = Vector2.Lerp(start, end, 0.33f);
                    ArmRenderer2.transform.position = Vector2.Lerp(start, end, 0.66f);

                    ArmRenderer1.transform.up = ArmRenderer1.transform.up = dir;
                    return;
                }
                Vector2 perp1 = new Vector2(-dir.y, dir.x);

                if (Vector2.Dot(perp1, Vector2.down) < 0) perp1 *= -1;

                float diff = MaxDist - dist;
                Vector2 pos = (start + end) * 0.5f + perp1 * diff;

                ArmRenderer1.transform.position = (pos + start) * 0.5f;
                ArmRenderer2.transform.position = (pos + end) * 0.5f;

                ArmRenderer1.transform.up = start.Direction(pos);
                ArmRenderer2.transform.up = pos.Direction(end);
            }

            public void Disable()
            {
                ArmRenderer1.gameObject.SetActive(false);
                ArmRenderer2.gameObject.SetActive(false);
            }
            public void Enable()
            {
                ArmRenderer1.gameObject.SetActive(true);
                ArmRenderer2.gameObject.SetActive(true);
            }
        }

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
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteAnimation _idleAnimation;
        [SerializeField] private SpriteAnimation _walkAnimation;
        [SerializeField] private SpriteAnimation _preJumpAnimation;
        [SerializeField] private SpriteAnimation _ascending;
        [SerializeField] private SpriteAnimation _jumpPeakAnimation;
        [SerializeField] private SpriteAnimation _landingAnimation;
        [SerializeField] private SpriteAnimation _fallAnimation;
        [SerializeField] private SpriteAnimation _cameraAnimation;
        [SerializeField] private SpriteAnimation _placeProjectorAnimation;
        
        [SerializeField] private ProgressBar _bar;
        [SerializeField] private AudioClip _stepClip;
        [SerializeField] private Sprite _handStart;
        [SerializeField] private Transform _shoulder;
        [SerializeField] private Sprite _handEnd;
        [SerializeField] private Material _armMat;
        [SerializeField] private SpriteRenderer _cameraRenderer;

        public event Action<Vector2> OnPictureTaken;

        private Collider2D[] _cached = new Collider2D[64];

        private Rigidbody2D _rig;

        private Vector2 _gravityForce;

        private float _lastNonZeroWalkDirection = 1;
        private float _walkDirection;
        private Vector2 _walkForce;

        private Vector2 _lastStepPos;

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

        private Arm[] _arms;

        protected override void Init()
        {
            _totalProjectors = _initialProjectors;
            _rig = GetComponent<Rigidbody2D>();
            _rig.freezeRotation = true;
            _minJumpTime = _jumpHeightMin / _jumpHeight * _jumpTime * 0.5f;

            float bestMatch = 0f;
            float bestVal = 999;
            for(int i = 0; i < 500; i++)
            {
                float dt = (float)i / 1000;
                float val = _jumpCurve.Evaluate(dt);

                if(Mathf.Abs(val - _jumpHeightMin / _jumpHeight) < bestVal)
                {
                    bestVal = Mathf.Abs(val - _jumpHeightMin / _jumpHeight);
                    bestMatch = dt;
                }
            }

            _minJumpTime = bestMatch * _jumpTime;
            GroundDetector.OnLanding += Landed;
            _camera.OnPictureTaken += (vec) => OnPictureTaken?.Invoke(vec);
            OnDeath += (_) => _isDying = true;

            Arm mainArm = InitArm();
            mainArm.ArmRenderer1.sortingOrder = mainArm.ArmRenderer2.sortingOrder = _renderer.sortingOrder + 1;

            Arm second = InitArm();
            second.ArmRenderer1.sortingOrder = second.ArmRenderer2.sortingOrder = _renderer.sortingOrder - 1;
            second.ArmRenderer1.color = second.ArmRenderer2.color = new Color(0.8f, 0.8f, 0.8f, 1);

            _arms = new Arm[2];
            _arms[0] = mainArm;
            _arms[1] = second;
        }

        private Arm InitArm()
        {
            var renderer1 = new GameObject("ARM_RENDERER1");
            renderer1.transform.SetParent(transform, false);

            var sr = renderer1.AddComponent<SpriteRenderer>();
            sr.sprite = _handStart;
            sr.material = _armMat;

            var renderer2 = new GameObject("ARM_RENDERER2");
            renderer2.transform.SetParent(transform, false);

            var sr2 = renderer2.AddComponent<SpriteRenderer>();
            sr2.sprite = _handEnd;
            sr2.material = _armMat;

            Arm arm = new Arm()
            {
                ArmRenderer1 = sr,
                ArmRenderer2 = sr2,
                MaxDist = 1
            };
            return arm;
        }

        private void Landed()
        {
            if(GroundDetector.TimeSinceGrounded > 0.5f)
            {
                _landingCooldown = 0.6f;
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

            _landingCooldown -= Time.deltaTime;
            bool landed = _landingCooldown > 0f;
            if (landed)
            {
                _walkDirection = 0f;
                _lastStepPos = transform.position;
            }

            if(Vector2.Distance(transform.position, _lastStepPos) > 3f)
            {
                AudioController.Instance.PlayAudio(_stepClip, transform.position, Mathf.Lerp(0.95f, 1.05f, UnityEngine.Random.value), 1f);
                _lastStepPos = transform.position;
            }

            if (_isJumping)
            {
                _lastStepPos = transform.position;
            }

            if(_walkDirection != 0)
            {
                _lastNonZeroWalkDirection = _walkDirection;
            }
            _renderer.flipX = _lastNonZeroWalkDirection < 0;

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
                _arms[0].Enable();
                _arms[1].Enable();
                _cameraRenderer.gameObject.SetActive(true);

                float fact1 = Mathf.Clamp01(_cameraChargeElapsed / 0.2f);
                Vector2 mousePos = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
                float distance = Vector2.Distance(mousePos, transform.position);

                float fact2 = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(distance / 5));

                Vector2 dir = -mousePos.Direction(transform.position) / distance;
                Vector2 endPoint = (Vector2)transform.position + dir * fact1 * fact2;

                _renderer.flipX = Vector2.Dot(dir, Vector2.left) > 0;

                _arms[0].Update(_shoulder.position, endPoint);
                _arms[1].Update(_shoulder.position - Vector3.up * 0.07f, endPoint + new Vector2(-dir.y, dir.x) * 0.08f);

                _cameraRenderer.flipY = Vector2.Dot(dir, Vector2.left) > 0;
                _cameraRenderer.transform.position = endPoint;
                _cameraRenderer.transform.right = dir;

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
            else
            {
                _arms[0].Disable();
                _arms[1].Disable();

                _cameraRenderer.gameObject.SetActive(false);
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
            _wantProjector = Input.GetKey(KeyCode.Mouse1) && !_isJumping && !_wantCameraCharge && _camera.HasPicture && !landed && _totalProjectors > 0;

            if (!wanted && _wantProjector)
            {
                _camera.EnableTrace();
                _camera.EnableTraceProjection();

                _projectorPreview.gameObject.SetActive(true);
                _bar.gameObject.SetActive(true);
            }

            const float PROJECTOR_HEIGHT = 2F;
            if (_wantProjector)
            {
                _walkDirection = 0f;

                _projectorElapsed += Time.deltaTime;
                _bar.Progress = _projectorElapsed / _projectorPlaceTime;

                var ray = Physics2D.Raycast(
                    (Vector2)transform.position + _lastNonZeroWalkDirection * new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _projectorPlaceOffset,
                    GroundDetector.GravityDirection,
                    PROJECTOR_HEIGHT,
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

            if (Input.GetKeyUp(KeyCode.Mouse1) && !landed || _projectorElapsed > _projectorPlaceTime)
            {
                var ray = Physics2D.Raycast(
                    (Vector2)transform.position + _lastNonZeroWalkDirection * new Vector2(-GroundDetector.GravityDirection.y, GroundDetector.GravityDirection.x) * _projectorPlaceOffset, 
                    GroundDetector.GravityDirection,
                    PROJECTOR_HEIGHT, 
                    _validProjectorMask);

                if (_projectorElapsed > _projectorPlaceTime && ray && _totalProjectors > 0)
                {
                    var projector = Instantiate(_projectorPrefab);
                    projector.transform.position = ray.point;
                    projector.Project(_camera.ExpectedPos, _camera, _lastNonZeroWalkDirection < 0);

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
                    _bar.gameObject.SetActive(false);
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
                if (_currentAnimation != null)
                {
                    _currentAnimation.Stop();
                    _currentAnimation.Reset();
                }

                switch (state)
                {
                    case State.Walking: _currentAnimation = _walkAnimation; break;
                    case State.Ascending: _currentAnimation = _ascending; break;
                    case State.JumpPeak: _currentAnimation = _jumpPeakAnimation; break;
                    case State.JumpPrepare: _currentAnimation = _preJumpAnimation; break;
                    case State.TakingPicture: _currentAnimation = _cameraAnimation; break;
                    case State.Landing: _currentAnimation = _landingAnimation; break;
                    case State.Falling: _currentAnimation = _fallAnimation; break;
                    case State.PlacingProjector: _currentAnimation = _placeProjectorAnimation; break;
                    default: _currentAnimation = _idleAnimation; break;
                }

                _currentAnimation.Play();
                _state = state;
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
