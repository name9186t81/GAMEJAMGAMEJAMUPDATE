using Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace Effects
{
    public sealed class UIPictureShowcase : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private RectTransform _transform;
        [SerializeField] private float _speed;
        [SerializeField] private float _moveTime;
        [SerializeField] private float _stayTime;

        private float _elapsed;
        private Vector2 _original;
        private Vector2 _originalMax;
        private RenderTexture _texture;

        private void Start()
        {
            _original = _transform.offsetMin;
            _originalMax = _transform.offsetMax;

            if (PlayerEvents.Exists)
            {
                PlayerEvents.Instance.OnPictureTaken += PictureTaken;
            }
        }

        private void PictureTaken(Vector2 obj)
        {
            if(_texture != null)
            {
                _texture.Release();
            }

            var texture = PlayerEvents.Instance.Player.Camera.CurrentTexture;
            _texture = new RenderTexture(texture.width, texture.height, 0, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            Graphics.CopyTexture(texture, _texture);
            _image.texture = _texture;
            _transform.gameObject.SetActive(true);
            _image.color = Color.black;

            _elapsed = 0f;
        }

        private void Update()
        {
            if (_elapsed > 2 * _moveTime + _stayTime) return;

            _elapsed += Time.deltaTime;

            if(_elapsed < _moveTime)
            {
                float delta = _elapsed / _moveTime;
                delta *= delta;
                _transform.offsetMin = Vector2.Lerp(new Vector2(_original.x, -100), _original, delta);
                _transform.offsetMax = Vector2.Lerp(new Vector2(_originalMax.x, -100 + (_originalMax.y - _original.y)), _originalMax, delta);
            }
            else if(_elapsed > _moveTime && _elapsed < _moveTime + _stayTime)
            {
                _transform.offsetMin = _original;
                _image.color = Color.Lerp(Color.black, Color.white, (_elapsed - _moveTime) / (_moveTime + _stayTime));
            }
            else
            {
                float delta = (_elapsed - _stayTime - _moveTime) / _moveTime;
                delta *= delta;
                _transform.offsetMin = Vector2.Lerp(_original, new Vector2(_original.x, -100), delta);
                _transform.offsetMax = Vector2.Lerp(_originalMax, new Vector2(_originalMax.x, -100 + (_originalMax.y - _original.y)), delta);
            }

            if (_elapsed > 2 * _moveTime + _stayTime)
            {
                _transform.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if(_texture != null)
            {
                _texture.Release();
            }
        }
    }
}
