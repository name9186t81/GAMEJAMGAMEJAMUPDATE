using Architecture;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mechanics
{
    public sealed class ExitTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _hintRange;
        [SerializeField] private SpriteRenderer _hint;
        [SerializeField] private string _nextSceneName;

        public string NextSceneName => _nextSceneName;

        public Vector2 Position => transform.position;

        private void Update()
        {
            if(PlayerEvents.Exists && PlayerEvents.Instance.Player != null)
            {
                Vector2 player = PlayerEvents.Instance.Player.transform.position;
                float dist = Vector2.Distance(player, Position);

                _hint.color = new Color(_hint.color.r, _hint.color.g, _hint.color.b, Mathf.Clamp01(1 - dist / _hintRange));
            }
        }
        public void Interact(AbstractGameActor target)
        {
            MiscEvents.Instance.LoadLevel(_nextSceneName, 1f);
        }
    }
}
