using Mechanics;
using UnityEngine;

public class CameraPlayerFollower : MonoBehaviour
{
    void Update()
    {
        if (PlayerEvents.Exists && PlayerEvents.Instance.Player != null)
        {
            var lerp = Vector2.Lerp(transform.position, PlayerEvents.Instance.Player.transform.position, 0.1f);

            transform.position = new Vector3(lerp.x, lerp.y, -10f);
        }
    }
}
