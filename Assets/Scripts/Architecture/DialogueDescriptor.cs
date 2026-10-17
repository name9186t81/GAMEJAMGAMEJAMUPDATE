using UnityEngine;

namespace Mechanics
{
    [CreateAssetMenu(fileName = "DialogueDescriptor", menuName = "Dialogues")]
    public sealed class DialogueDescriptor : ScriptableObject
    {
        [SerializeField] private bool _loadOnInit;
        [SerializeField] private string _identifier;
        [SerializeField] private string[] _lines;
        private int _hash;

        private void Awake()
        {
            if(_identifier != null)
            {
                _hash = _identifier.GetHashCode();
            }
        }

        private void OnValidate()
        {
            if (_identifier != null)
            {
                _hash = _identifier.GetHashCode();
            }
        }

        public bool LoadOnInit => _loadOnInit;
        public int Hash => _hash;
        public string Identifier => _identifier;
        public string[] Lines => _lines;
    }
}
