using Effects;
using System;
using UnityEngine;

namespace IK
{
    public sealed class Chain : MonoBehaviour
    {
        [SerializeField] private Transform _startTransform;
        [SerializeField] private Transform _endTransform;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _mat;
        [SerializeField] private float _distanceFactor = 1;
        [SerializeField] private float _forcedDistance = -1;
        [SerializeField] private float _damping = 0.95f;
        [SerializeField] private float _gravityStrength =1;
        [SerializeField] private int _order = 1;
        [SerializeField] private bool _autoInit;

        private float _accumulated;
        private float _length;
        private float _targetLength;
        private BodyPoint[] _points;
        private SpriteRenderer[] _renderers;
        private BodyConnection[] _connections;
        private Vector2? _start;
        private Vector2? _end;

        public event Action OnStep;
        public event Action OnInit;

        private void Awake()
        {
            if (_autoInit)
            {
                Init(_sprite, _forcedDistance > -1 ? _forcedDistance : Vector2.Distance(_startTransform.position, _endTransform.position) * _distanceFactor, _order, _startTransform == null ? null : _startTransform.position);
            }
        }
        private void Update()
        {
            _accumulated += Time.deltaTime;

            while(_accumulated > PointsUtils.TICK_RATE)
            {
                _accumulated -= PointsUtils.TICK_RATE;
                Step();
            }

            if(_startTransform != null)
            {
                _start = _startTransform.position;
            }

            if(_endTransform != null)
            {
                _end = _endTransform.position;
            }

            for(int i = 0; i < _renderers.Length; i++)
            {
                if(i == 0)
                {
                    _renderers[i].transform.right = _points[i + 1].GetDirection(_points[i]);
                    _renderers[i].transform.position = _points[i].Center(_points[i + 1]);
                    _renderers[i].size = new Vector2(_points[i].Distance(_points[i + 1]), _renderers[i].size.y);
                }
                else
                {
                    _renderers[i].transform.right = _points[i].GetDirection(_points[i - 1]);
                    _renderers[i].transform.position = _points[i].Center(_points[i - 1]);
                    _renderers[i].size = new Vector2(_points[i].Distance(_points[i - 1]), _renderers[i].size.y);
                }
            }
        }

        private void Step()
        {
            if(_start != null)
            {
                _points[0].HardReset(_start.Value);
            }

            if(_end != null)
            {
                _points[_points.Length - 1].HardReset(_end.Value);
            }

            float newDelta = _targetLength / _points.Length;

            for(int i = 0; i < _connections.Length; i++)
            {
                _connections[i].MaxDistance = newDelta;
                _connections[i].Update();
            }

            for(int i = 0; i < _points.Length; i++)
            {
                _points[i].Velocity += Vector2.down * 0.016f * _gravityStrength;
                _points[i].Position += _points[i].Velocity;
                _points[i].Velocity *= _damping;
            }

            if (_start != null)
            {
                _points[0].HardReset(_start.Value);
            }

            if (_end != null)
            {
                _points[_points.Length - 1].HardReset(_end.Value);
            }

            OnStep?.Invoke();
        }
            
        public void SetStartPoint(Vector2 start)
        {
            _start = start;
        }

        public void SetEndPoint(Vector2 end)
        {
            _end = end;
        }

        /// <summary>
        /// Changes the length without reseting the whole chain.
        /// </summary>
        /// <param name="length"></param>
        public void SetLength(float length)
        {
            _targetLength = length;
        }

        public void Init(Sprite sprite, float length, int order, Vector2? start = null)
        {
            _sprite = sprite;
            _length = length;

            _length = _targetLength = length;
            int count = (int)Mathf.Ceil(_length / _sprite.bounds.size.x);

            float singleLength = _length / count;

            _points = new BodyPoint[count];
            _connections = new BodyConnection[count - 1];

            for(int i = 0; i < count; i++)
            {
                _points[i] = new BodyPoint(1f, singleLength);
                if (start.HasValue)
                {
                    _points[i].PrevPosition = _points[i].Position = start.Value + singleLength * i * Vector2.down;
                }

                if(i > 0)
                {
                    _connections[i - 1] = new BodyConnection(_points[i - 1], _points[i], singleLength, -1f, BodyConnection.ConnectionType.Pull);
                }
            }
            _points[0].Mass = _points[count - 1].Mass = 100;

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    Destroy(_renderers[i].gameObject);
                }
            }

            _renderers = new SpriteRenderer[count];
            for(int i = 0; i < _renderers.Length; i++)
            {
                var go = new GameObject();
                go.name = "CHAIN_RENDERER_" + i.ToString();
                go.transform.SetParent(transform);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = order;
                renderer.drawMode = SpriteDrawMode.Tiled;

                if (_mat) renderer.material = _mat;

                _renderers[i] = renderer;
            }

            OnInit?.Invoke();
        }

        public BodyPoint[] Points => _points;
    }
}
