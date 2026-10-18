using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rendering
{
    public sealed class TilemapRenderingParticipant : RenderingParticipant
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private float _maxHeight;
        [SerializeField] private float _minHeight;
        private Color _originalColor;

        private void Awake()
        {
            _originalColor = _tilemap.color;
        }

        public override void SetDrawMode(DrawMode drawMode)
        {
            if(drawMode == DrawMode.Height)
            {
                _tilemap.color = new Color(_minHeight, _maxHeight, 1);
            }
            else
            {
                _tilemap.color = _originalColor;
            }
        }
    }
}
