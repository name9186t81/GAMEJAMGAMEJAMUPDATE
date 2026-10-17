using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TMPro;

namespace Dialogues
{
    public sealed class TextEFfectsProcessor : ITextPreprocessor
    {
        private readonly IList<TagInfo> _tags;
        private readonly IEnumerable<string> _rawTags;

        public IReadOnlyList<TagInfo> Tags => (IReadOnlyList<TagInfo>)_tags;
        public TextEFfectsProcessor(IEnumerable<string> rawTags)
        {
            _rawTags = rawTags;
            _tags = new List<TagInfo>();
        }

        public string PreprocessText(string text)
        {
            _tags.Clear();
            StringBuilder builder = new StringBuilder();
            int ind = 0;

            for (int i = 0, length = text.Length; i < length; i++)
            {
                if (text[i] == '<')
                {
                    if (CheckTag(text, i, ind, out int end) || CheckTagTMP(text, i, builder, out end))
                    {
                        i = end;
                    }
                }
                else
                {
                    builder.Append(text[i]);
                    ind++;
                }
            }

            return builder.ToString();
        }

        private bool CheckTag(in string text, int start, int buildInd, out int end)
        {
            if (!IsTag(text, start, out end)) return false;
            string tag = text.Substring(start + 1, end - start - 1);

            if (tag[0] != '/')
            {
                if (!_rawTags.Contains(tag)) return false;
                _tags.Add(new TagInfo(tag, buildInd));
            }
            else
            {
                tag = tag.Substring(1);
                bool hit = false;
                foreach (var tagInfo in _tags)
                {
                    if (!tagInfo.OpenAndEqual(tag)) continue;

                    tagInfo.Close(buildInd - 1);
                    hit = true;
                    break;
                }

                if (!hit) return false;
            }

            return true;
        }

        private bool CheckTagTMP(in string text, int start, StringBuilder builder, out int end)
        {
            if (!IsTag(text, start, out end)) return false;

            string tag = text.Substring(start + 1, end - start - 1);
            if (tag[0] == '/') tag = tag.Substring(1);

            if (_rawTags.Contains(tag)) return false;

            builder.Append(text.Substring(start, end - start + 1));
            return true;
        }

        private bool IsTag(in string text, int start, out int end)
        {
            end = text.IndexOf('>', start);
            if (end == -1) return false;
            if (end - start - 1 <= 0) return false;

            return true;
        }
    }
}