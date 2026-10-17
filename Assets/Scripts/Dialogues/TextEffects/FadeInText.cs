using TMPro;

using UnityEngine;

namespace Dialogues
{
    public sealed class FadeInText : MonoBehaviour
    {
        [SerializeField] private float _timePerCharacter;
        [SerializeField] private float _scale;
        [SerializeField] private TextMeshPro _text;
        private Rect[] _scales;
        private float _totalTime;
        private float _pausedTime;
        private bool _wasPaused;
        private int _index;
        private float _elapsed;
        private float _speedModifier = 1f;

        public float EstimatedTime => _timePerCharacter * _text.text.Length;
        public bool Paused => _pausedTime > 0;
        public float SpeedModififer { get => _speedModifier; set => _speedModifier = value; }
        public int Index => _index;
        public bool Finished => _index >= _scales.Length;

        private bool _needReset;

        public void Pause(float time)
        {
            _pausedTime = time;
        }
        public void UnPause()
        {
            _pausedTime = 0f;
        }

        private void Start()
        {
            _text.ForceMeshUpdate();

            var info = _text.textInfo;
            _scales = new Rect[info.characterCount];//todo use sprite hash map to store scales

            for (int i = 0, length = info.characterCount; i < length; i++)
            {
                var charInfo = info.characterInfo[i];
                var verts = info.meshInfo[charInfo.materialReferenceIndex].vertices;

                Vector2 diff = (verts[charInfo.vertexIndex + 2] - verts[charInfo.vertexIndex]);
                Vector2 pos = (Vector2)verts[charInfo.vertexIndex] + diff / 2;
                _scales[i] = new Rect(pos.x, pos.y, diff.x, diff.y);
            }
        }

        public void ForceUpdate()
        {
            var info = _text.textInfo;

            if (_index >= _scales.Length || _index < 0)
            {
                //State = EffectState.Dead; //there is no need to keep this component after its done its job
                return;
            }

            while (!info.characterInfo[_index].isVisible) _index++;

            _pausedTime -= Time.deltaTime;
            _elapsed += Time.deltaTime * _speedModifier * (Paused ? 0f : _wasPaused ? 1000f : 1f);
            _wasPaused = Paused;
            float delta = _elapsed / _timePerCharacter;
            delta = Mathf.Clamp01(delta);

            float scale = 0f;
            if (delta < 0.5f)
            {
                scale = Mathf.Lerp(0, _scale, delta * 2f);
            }
            else
            {
                float t = (delta - 0.5f) * 2;
                scale = Mathf.Lerp(_scale, 1f, t * t * t);
            }
            Vector2 originalSize = _scales[_index].size;
            Vector2 halfSize = (originalSize / 2) * scale;

            var charInfo = info.characterInfo[_index];
            var verts = info.meshInfo[charInfo.materialReferenceIndex].vertices;

            verts[charInfo.vertexIndex] = _scales[_index].position - halfSize;
            verts[charInfo.vertexIndex + 1] = _scales[_index].position + new Vector2(-halfSize.x, halfSize.y);
            verts[charInfo.vertexIndex + 2] = _scales[_index].position + halfSize;
            verts[charInfo.vertexIndex + 3] = _scales[_index].position + new Vector2(halfSize.x, -halfSize.y);

            for (int i = _index + 1; i < info.characterCount; ++i)
            {
                var cInfo = info.characterInfo[i];

                if (!cInfo.isVisible) continue;

                var vert = info.meshInfo[cInfo.materialReferenceIndex].vertices;

                for (int j = 0; j < 4; ++j)
                {
                    vert[cInfo.vertexIndex + j] = _scales[i].position;
                }
            }

            if (delta == 1f)
            {
                _index++;
                _elapsed = 0f;
            }
        }

        private void ResetDelayed()
        {
            _elapsed = 0f;
            _index = 0;
            _totalTime = 0f;

            var info = _text.textInfo;

            _text.ForceMeshUpdate();

            _scales = new Rect[info.characterCount];//todo use sprite hash map to store scales

            for (int i = 0, length = info.characterCount; i < length; i++)
            {
                var charInfo = info.characterInfo[i];
                var verts = info.meshInfo[charInfo.materialReferenceIndex].vertices;

                _totalTime += _timePerCharacter;
                if (!charInfo.isVisible) continue;

                Vector2 diff = (verts[charInfo.vertexIndex + 2] - verts[charInfo.vertexIndex]);
                Vector2 pos = (Vector2)verts[charInfo.vertexIndex] + diff / 2;
                _scales[i] = new Rect(pos.x, pos.y, diff.x, diff.y);

                for (int j = 0; j < 4; ++j)
                {
                    verts[charInfo.vertexIndex + j] = Vector3.zero;
                }
            }

            _text.UpdateVertexData();
        }

        private void LateUpdate()
        {
            if (_needReset)
            {
                ResetDelayed();
                _needReset = false;
            }
        }

        public void Reset()
        {
            _needReset = true;
        }

        public float TotalTime => _totalTime;
    }
}