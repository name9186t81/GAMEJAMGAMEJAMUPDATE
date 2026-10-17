namespace Dialogues
{
    public sealed class TagInfo
    {
        private readonly string _tag;
        private readonly int _hash;
        private readonly int _start;
        private int _end;

        public string Tag => _tag;
        public int Start => _start;
        public int End => _end;

        public TagInfo(string tag, int start)
        {
            _tag = tag;
            _start = start;

            _end = -1;
            _hash = _tag.GetHashCode();
        }

        public void Close(int end)
        {
            _end = end;
        }

        public bool OpenAndEqual(string otherTag)
        {
            return _end == -1 && CompareTags(otherTag);
        }

        public bool CompareTags(string other)
        {
            return other.GetHashCode() == _hash;
        }
    }
}