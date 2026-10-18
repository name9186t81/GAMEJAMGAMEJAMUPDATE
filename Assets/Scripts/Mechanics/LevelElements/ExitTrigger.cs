using UnityEngine;

namespace Mechanics
{
    public sealed class ExitTrigger : MonoBehaviour
    {
        [SerializeField] private string _nextSceneName;

        public string NextSceneName => _nextSceneName;
    }
}
