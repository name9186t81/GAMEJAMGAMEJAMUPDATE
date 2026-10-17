using System.Collections.Generic;
using UnityEngine;

namespace Mechanics
{
    public class DialoguesProvider : MonoSingletone<DialoguesProvider>
    {
        [SerializeField] private DialogueDescriptor[] _descriptors;
        private Dictionary<int, string[]> _loadedDialogues = new Dictionary<int, string[]>();

        private void Awake()
        {
            for (int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].LoadOnInit)
                {
                    int hash = _descriptors[i].Hash;

                    if (_loadedDialogues.ContainsKey(hash))
                    {
                        Debug.LogError("DIALOGUES CONFLICT ERROR KEY - " + _descriptors[i].Identifier);
                        continue;
                    }

                    _loadedDialogues.Add(hash, _descriptors[i].Lines);
                }
            }
        }

        public bool TryGetDialogue(string identifier, out string[] dialogue)
        {
            int hash = identifier.GetHashCode();

            if (_loadedDialogues.TryGetValue(hash, out dialogue) || TryFindDescriptor(hash, out dialogue)) return true;

            var asset = Resources.Load<TextAsset>("Dialogues/" + identifier + ".dlg");
            if(asset == null)
            {
                return false;
            }

            string[] lines = asset.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
            dialogue = lines;
            return true;
        }

        private bool TryFindDescriptor(int hash, out string[] dialogue)
        {
            for(int i = 0; i < _descriptors.Length; i++)
            {
                if (_descriptors[i].Hash == hash)
                {
                    dialogue = _descriptors[i].Lines;
                    return true;
                }
            }

            dialogue = default;
            return false;
        }

        protected override DialoguesProvider GetInstance()
        {
            return this;
        }
    }
}
