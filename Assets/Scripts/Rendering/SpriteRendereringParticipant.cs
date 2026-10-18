using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rendering
{
    internal class SpriteRendereringParticipant : RenderingParticipant
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _maxHeight;
        [SerializeField] private float _minHeight;
        private Color _originalColor;

        private void Awake()
        {
            _originalColor = _renderer.color;
        }

        public override void SetDrawMode(DrawMode drawMode)
        {
            if (drawMode == DrawMode.Height)
            {
                _renderer.color = new Color(_minHeight, _maxHeight, 1);
            }
            else
            {
                _renderer.color = _originalColor;
            }
        }
    }
}
