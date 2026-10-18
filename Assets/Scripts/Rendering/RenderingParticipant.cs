using UnityEngine;

namespace Rendering
{
    public abstract class RenderingParticipant : MonoBehaviour
    {
        public enum DrawMode
        {
            Normal,
            Height,
            Albedo
        }

        public abstract void SetDrawMode(DrawMode drawMode);
    }
}
