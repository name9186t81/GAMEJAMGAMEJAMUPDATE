using Architecture;
using Audio;
using Mechanics;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public sealed class CamCenti : MonoBehaviour, IReactToPicture
{
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _bodySize;
    [SerializeField] private float _legHeight;
    [SerializeField] private float _cameraChargeTime;
    [SerializeField] private LayerMask _walkMask;
    private State _state = State.Sleeping;

    [SerializeField] private float _awakeness;
    [SerializeField] private float _acceleration;

    [SerializeField] private LineRenderer _legPrefab;
    [SerializeField] private SpriteRenderer _headSprite;
    [SerializeField] private Sprite _bodySegment;
    [SerializeField] private LineRenderer _bodyConnector;

    [SerializeField] private GameObject _damageZone;

    [SerializeField] private GameCamera _cameraPrefab;

    [SerializeField] private AudioClip _stepSound;

    private float _walkSpeed;
    private float _cameraChargeElapsed;

    public event Action OnAwake;
    public event Action OnWalkBreak;
    public event Action<Vector2> OnStep;

    private GameCamera _gameCamera;

    private Vector2 _picturePos;
    private Vector2 _originalpos;
    private Vector2 _originalDirection;
    private Transform[] _segments;
    private Vector2[] _legPositions;
    private LineRenderer[] _legRenderers;

    private CircleCollider2D _collider;

    private enum State
    {
        Sleeping,
        Charging,
        Walking,
        Retreating
    }

    private void Awake()
    {
        Vector2 targetRight = transform.right;
        transform.right = Vector2.right;
        _gameCamera = Instantiate(_cameraPrefab);
        _gameCamera.OverrideControl = true;
        _gameCamera.transform.SetParent(transform, false);
        _damageZone.SetActive(false);

        _originalpos = transform.position;
        _segments = new Transform[44];
        _bodyConnector.positionCount = _segments.Length;
        List<Vector2> pos = new List<Vector2>();

        for (int i = 0; i < _segments.Length; i++)
        {
            float dt = (float)i / (_segments.Length - 1);
            var go = new GameObject("SEGMENT" + i);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Mathf.Lerp(_bodySize / 2, -_bodySize / 2, dt), 0, 0);
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, 1 - 2 * Mathf.Abs(dt - 0.5f));

            _segments[i] = go.transform;

            if(i >= _segments.Length / 4)
            {
                pos.Add(RandomizedLegPosition(_segments[i].position));
                pos.Add(RandomizedLegPosition(_segments[i].position));
            }
        }

        _collider = gameObject.AddComponent<CircleCollider2D>();
        _collider.radius = 0.1f;
        _legPositions = pos.ToArray();

        _legRenderers = new LineRenderer[_legPositions.Length];
        for(int i = 0; i < _legRenderers.Length; i++)
        {
            var go = Instantiate(_legPrefab);
            go.transform.SetParent(transform, false);
            go.name = "LEG_RENDERER";

            go.positionCount = 3;
            _legRenderers[i] = go;
        }

        transform.right = targetRight;
    }

    private void Start()
    {
        if (PlayerEvents.Exists)
        {
            PlayerEvents.Instance.OnPlayerRegistry += Respawn;
        }

        _gameCamera.TraceRenderer.color = Color.red;
        _gameCamera.LocalOffset = 0;
    }

    private void Respawn(Player obj)
    {
        transform.position = _originalpos;
        transform.right = _originalDirection;

        _state = State.Sleeping;
        _gameCamera.DisableTrace();
        _cameraChargeElapsed = 0f;
    }

    private Vector2 RandomizedLegPosition(Vector2 upperPoint)
    {
        Vector2 dir = Vector2.down.Rotate(Mathf.Lerp(-Mathf.PI / 4, Mathf.PI / 4, UnityEngine.Random.value));
        var ray = Physics2D.Raycast(upperPoint, dir, _legHeight, _walkMask);

        if (ray)
        {
            return ray.point;
        }
        else
        {
            return dir * _legHeight + upperPoint;
        }
    }

    private void Update()
    {
        bool rightDir = Vector2.Dot(transform.right, Vector2.right) > 0;

        _headSprite.flipY = !rightDir;

        for (int i = 0; i < _segments.Length; i++)
        {
            float dt = (float)i / (_segments.Length - 1);
            if(dt < 0.25f)
            {
                float localized = dt * 4;
                float fac = Mathf.Sqrt(2) * (localized - 1);
                _segments[i].localPosition = new Vector3(
                    Mathf.Lerp(_bodySize / 2, -_bodySize / 2, dt),
                    2 * _awakeness * (localized < 0.5f ? 1 - 2 * localized * localized : fac * fac),
                    0);
            }

            if(i > 0)
                Debug.DrawLine(_segments[i].position, _segments[i - 1].position, Color.mediumVioletRed);

            if(i >= _segments.Length / 4)
            {
                Vector2 pos = _segments[i].position;

                DoLeg((i - _segments.Length / 4) * 2, pos);
                DoLeg((i - _segments.Length / 4) * 2 + 1, pos);
            }

            _bodyConnector.SetPosition(i, _segments[i].position);
        }
        _headSprite.transform.position = _segments[0].position;
        _collider.offset = _headSprite.transform.position - transform.position;
        if (!rightDir) _collider.offset = new Vector2(-_collider.offset.x, _collider.offset.y);

        AiStuff();
    }

    private void DoLeg(int ind, Vector2 upperPos)
    {
        Vector2 legPos = _legPositions[ind];

        float dist = Vector2.Distance(legPos, upperPos);
        _legRenderers[ind].SetPosition(0, upperPos);

        if (dist > _legHeight)
        {
            _legPositions[ind] = RandomizedLegPosition(upperPos);
            OnStep?.Invoke(legPos);

            _legRenderers[ind].SetPosition(1, (upperPos + legPos) * 0.5f);
            _legRenderers[ind].SetPosition(2, legPos);

            float pDist = 9999999;
            if(PlayerEvents.Exists && PlayerEvents.Instance.Player != null)
            {
                pDist = Vector2.Distance(PlayerEvents.Instance.Player.transform.position, transform.position);
            }
            if(UnityEngine.Random.value < 0.01f)
                AudioController.Instance.PlayAudio(_stepSound, legPos, Mathf.Lerp(0.9f, 1.1f, UnityEngine.Random.value), 1 - Mathf.Clamp01(pDist / 25f));
        }
        else
        {
            float diff = _legHeight - dist;
            Vector2 dir = upperPos.Direction(legPos) / dist;

            Vector2 kneePos;
            if(Vector2.Dot(transform.right, dir) > 0)
            {
                kneePos = new Vector2(-dir.y, dir.x);
            }
            else
            {
                kneePos = new Vector2(dir.y, -dir.x);
            }

            kneePos = kneePos * diff + (legPos + upperPos) * 0.5f;

            Debug.DrawLine(upperPos, kneePos, Color.green);
            Debug.DrawLine(kneePos, legPos, Color.green);

            _legRenderers[ind].SetPosition(1, kneePos);
            _legRenderers[ind].SetPosition(2, legPos);
        }
    }

    private void AiStuff()
    {
        if (PlayerEvents.Instance.Player == null || PlayerEvents.Instance.Player.IsDying)
        {
            transform.position -= (transform.position - (Vector3)_originalpos).normalized * _maxSpeed * 0.2f * Time.deltaTime;
            _gameCamera.DisableTrace();

            return;
        }

        if (PlayerEvents.Instance.Player == null) return;

        Vector2 playerPos = PlayerEvents.Instance.Player.transform.position;
        Vector2 playerHeadDir = -playerPos.DirectionNormalized(_segments[0].position, out float playerDist);

        switch (_state)
        {
            case State.Sleeping:
                {
                    _walkSpeed = 0f;
                    _awakeness = Mathf.Lerp(_awakeness, 0f, 0.1f);
                    _headSprite.transform.right = transform.right;

                    break;
                }
            case State.Charging:
                {
                    _walkSpeed = Mathf.Lerp(_walkSpeed, _maxSpeed, _acceleration);
                    transform.position += transform.right * _walkSpeed * Time.deltaTime;

                    _headSprite.transform.right = transform.right;

                    _awakeness = Mathf.Lerp(_awakeness, 0.5f, 0.1f);
                    _cameraChargeElapsed = 0f;
                    _damageZone.SetActive(true);

                    if (playerDist > 25f)
                    {
                        _damageZone.SetActive(false);
                        if (Vector2.Dot(playerHeadDir, transform.right) < 0) transform.right *= -1;

                        float rnd = UnityEngine.Random.value;

                        if (rnd < 0.5f)
                        {
                            MiscEvents.Instance.Earthquake();
                            _state = State.Walking;
                        }
                        else
                        {
                            MiscEvents.Instance.EarthquakeBig();
                            _state = State.Charging;
                        }
                    }
                    break;
                }
            case State.Walking:
                {
                    _walkSpeed = Mathf.Lerp(_walkSpeed, _maxSpeed * 0.5f, _acceleration);

                    if (Vector2.Dot(playerHeadDir, transform.right) < 0) transform.right *= -1;

                    _headSprite.transform.right = transform.right;
                    if (playerDist < 5f)
                    {
                        _headSprite.transform.right = playerHeadDir;
                        _cameraChargeElapsed += Time.deltaTime;
                        _gameCamera.transform.position = playerPos;
                        _gameCamera.EnableTrace();
                        _walkSpeed = Mathf.Lerp(_walkSpeed, 0f, Mathf.InverseLerp(5f, 3f, playerDist));

                        if(playerDist < 3f)
                        {
                            _walkSpeed *= -1f;
                        }
                        if(_cameraChargeElapsed > _cameraChargeTime)
                        {
                            _gameCamera.TakePicture();
                            _cameraChargeElapsed = 0f;
                            _gameCamera.DisableTrace();
                        }
                    }
                    else
                    {
                        _cameraChargeElapsed = 0;
                        _gameCamera.DisableTrace();
                    }

                    transform.position += transform.right * _walkSpeed * Time.deltaTime;
                    _awakeness = Mathf.Lerp(_awakeness, 1f, 0.1f);
                    break;
                }
            case State.Retreating:
                {
                    if (Vector2.Dot(playerPos - (Vector2)transform.position, transform.right) < 0) transform.right *= -1;

                    _walkSpeed = Mathf.Lerp(_walkSpeed, _maxSpeed * 0.8f, _acceleration);
                    transform.position += -transform.right * _walkSpeed * Time.deltaTime;

                    float dist = Vector2.Distance(transform.position, _picturePos);
                    _headSprite.transform.right = playerHeadDir;

                    if (dist > 20f)
                    {
                        if (Vector2.Dot(PlayerEvents.Instance.Player.WalkForce, _picturePos.Direction(transform.position)) > 0f)
                        {
                            _state = State.Charging;
                        }
                        else
                        {
                            float rnd = UnityEngine.Random.value;

                            if(rnd < 0.5f)
                            {
                                MiscEvents.Instance.Earthquake();
                                _state = State.Walking;
                            }
                            else
                            {
                                MiscEvents.Instance.EarthquakeBig();
                                _state = State.Charging;
                            }
                        }
                    }
                    _awakeness = Mathf.Lerp(_awakeness, 0.5f, 0.1f);
                    break;
                }
        }
    }

    public void ReactToPicture(Vector2 cameraPos, GameCamera cam)
    {
        if (cam == _gameCamera) return;

        _cameraChargeElapsed = 0f;
        _gameCamera.DisableTrace();

        switch (_state)
        {
            case State.Sleeping:
                {
                    OnAwake?.Invoke();
                    MiscEvents.Instance.EarthquakeBig();
                    _state = State.Retreating;
                    _picturePos = cameraPos;
                    break;
                }
            case State.Walking:
                {
                    OnWalkBreak?.Invoke();
                    _picturePos = cameraPos;
                    _state = State.Retreating;
                    break;
                }
            case State.Retreating:
                {
                    _picturePos = cameraPos;
                    _state = State.Retreating;
                    break;
                }
        }
    }
}
