using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;

namespace Dialogues
{
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class TextEffectsParser : MonoBehaviour
    {
        [SerializeField] private FadeInText _fadeIn;
        [SerializeField] private TextEffectBase[] _effects;
        [SerializeField] private bool _preview;
        private TextMeshPro _text;
        private bool _enabled = true;
        private List<(Vector2Int indexes, TextEffectBase effect)> _effectsList;

        private TextEFfectsProcessor _processor;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _processor = new TextEFfectsProcessor(_effects.Select(x => x.Tag));
            _text.textPreprocessor = _processor;

            _text.ForceMeshUpdate();
            for (int i = 0; i < _effects.Length; i++)
            {
                _effects[i].Init();
                _effects[i].SetFade(_fadeIn);
            }

            _effectsList = new List<(Vector2Int indexes, TextEffectBase effect)>();
            foreach (var tag in _processor.Tags)
            {
                foreach (var p in _effects)
                {
                    if (!p.CompareTags(tag.Tag)) continue;
                    _effectsList.Add((new Vector2Int(tag.Start, tag.End), p));
                }
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        public void Enable()
        {
            _enabled = true;
        }

        public void Disable()
        {
            _enabled = false;
        }

        public void ResetEffects()
        {
            if (_fadeIn != null)
            {
                _fadeIn.Reset();
                for (int i = 0; i < _effects.Length; i++)
                {
                    _effects[i].SetFade(_fadeIn);
                }
            }

            _effectsList = new List<(Vector2Int indexes, TextEffectBase effect)>();
            foreach (var tag in _processor.Tags)
            {
                foreach (var p in _effects)
                {
                    if (!p.CompareTags(tag.Tag)) continue;
                    _effectsList.Add((new Vector2Int(tag.Start, tag.End), p));
                }
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }

        private void Update()
        {
            //#if UNITY_EDITOR
            //			if (_preview)
            //			{
            //				if(_processor == null)
            //				{
            //					_text = GetComponent<TextMeshProUGUI>();
            //					_processor = new TextEFfectsProcessor(_effects.Select(x => x.Tag));
            //					_text.textPreprocessor = _processor;
            //				}

            //				for (int i = 0; i < _effects.Length; ++i)
            //				{
            //					if (!_effects[i].Inited)
            //					{
            //						_effects[i].Init();
            //					}
            //				}

            //				foreach (var tag in _processor.Tags)
            //				{
            //					foreach (var p in _effects)
            //					{
            //						if (!p.CompareTags(tag.Tag)) continue;
            //						p.Use(_text, tag.Start, tag.End);
            //					}
            //				}
            //				_text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            //			}
            //#endif
            if (!_enabled) return;
            _text.ForceMeshUpdate();
            var info = _text.textInfo;

            if (_fadeIn != null)
            {
                _fadeIn.ForceUpdate();
            }

            foreach (var effect in _effectsList)
            {
                effect.effect.Use(_text, effect.indexes[0], effect.indexes[1]);
            }

            //todo: generate list of meshes that actually need to be updated instead of updating every char
            for (int i = 0, length = info.meshInfo.Length; i < length; ++i)
            {
                var meshInfo = info.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                _text.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        public int Displayed => _fadeIn == null ? _text.text.Length - 1 : _fadeIn.Index;
        public float AppearanceTime => _fadeIn == null ? 0f : _fadeIn.TotalTime;
    }
}