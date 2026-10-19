using UnityEngine;

namespace Effects
{
    public sealed class ProgressBar : MonoBehaviour
    {
        [SerializeField] private int _baseOrder;
        [SerializeField] private float _baseLength;
        [SerializeField] private Sprite _filler;
        [SerializeField] private Sprite _base;
        [SerializeField] private Material _mat;

        private float _progress;
        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _fillerRenderer;

        private void Awake()
        {
            var go1 = new GameObject("BaseRenderer");
            var go2 = new GameObject("FillerRenderer");

            go1.transform.SetParent(transform, false);
            go2.transform.SetParent(go1.transform, false);

            _baseRenderer = go1.AddComponent<SpriteRenderer>();
            _baseRenderer.sprite = _base;
            _baseRenderer.sortingOrder = _baseOrder;
            _baseRenderer.drawMode = SpriteDrawMode.Sliced;
            if (_mat) _baseRenderer.material = _mat;

            _fillerRenderer = go2.AddComponent<SpriteRenderer>();
            _fillerRenderer.sprite = _filler;
            _fillerRenderer.sortingOrder = _baseOrder - 1;
            _fillerRenderer.drawMode = SpriteDrawMode.Sliced;
            if(_mat) _fillerRenderer.material = _mat;

            UpdateProgress(0);
            SetLength(_baseLength);
        }

        private void UpdateProgress(float val)
        {
            val = Mathf.Clamp01(val);

            float size = val * _baseRenderer.size.x;
            _fillerRenderer.size = new Vector2(size, _baseRenderer.size.y);
            _fillerRenderer.transform.localPosition = new Vector2((1 - val) * -_baseRenderer.size.x / 2, 0);

            _progress = val;
        }

        public void SetLength(float length)
        {
            _baseRenderer.size = new Vector2(length, 1f);
        }

        public float Progress
        {
            get { return _progress; }
            set { UpdateProgress(value); }
        }
    }
}