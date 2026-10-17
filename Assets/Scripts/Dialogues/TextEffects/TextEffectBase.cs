using UnityEngine;

namespace Dialogues
{
    public abstract class TextEffectBase : ScriptableObject
    {
        [Header("Base settings")]
        [SerializeField] private string _tag;
        [Space]
        private bool _inited;
        private int _hash;
        private FadeInText _fadeInText;

        protected FadeInText FadeEffect => _fadeInText;

        public void SetFade(FadeInText fadeInText)
        {
            _fadeInText = fadeInText;
        }

        public virtual void Init()
        {
            _hash = _tag.GetHashCode();
            _inited = true;
        }

        public abstract void Use(TMPro.TMP_Text text, int start, int end);

        public bool CompareTags(string otherTag)
        {
            return _hash == otherTag.GetHashCode();
        }

        public bool Inited => _inited;
        public string Tag => _tag;
    }
}