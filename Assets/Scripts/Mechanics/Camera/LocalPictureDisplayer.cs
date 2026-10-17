using UnityEngine;

namespace Mechanics
{
    public class LocalPictureDisplayer : MonoBehaviour
    {
        private RenderTexture _texture;

        public void Init(RenderTexture texture, SpriteRenderer renderer)
        {
            _texture = new RenderTexture(texture.width, texture.height, 0, UnityEngine.Experimental.Rendering.DefaultFormat.HDR);
            Graphics.CopyTexture(texture, _texture);
            renderer.material.SetTexture("_Projection", _texture);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
            }
        }
    }
}
