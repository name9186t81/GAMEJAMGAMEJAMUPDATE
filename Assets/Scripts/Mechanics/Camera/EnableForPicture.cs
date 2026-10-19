using UnityEngine;

namespace Mechanics
{
    public sealed class EnableForPicture : MonoBehaviour, IReactToPicture
    {
        public void ReactToPicture(Vector2 cameraPos, GameCamera cam)
        {
            gameObject.SetActive(true);
        }
    }
}
