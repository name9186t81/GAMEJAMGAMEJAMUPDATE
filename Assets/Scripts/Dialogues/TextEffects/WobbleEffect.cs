using TMPro;

using UnityEngine;

namespace Dialogues
{
    [CreateAssetMenu(fileName = "Wobble Effect", menuName = "YaEm/Wobble effect")]
    public sealed class WobbleEffect : TextEffectBase
    {
        [Header("Wave settings")]
        [SerializeField] private float _frequancy;
        [SerializeField] private float _length;
        [SerializeField] private float _amplitude;

        public override void Use(TMP_Text text, int start, int end)
        {
            var textInfo = text.textInfo;

            for (int i = start; i <= end; ++i)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                for (int j = 0; j < 4; ++j)
                {
                    int ind = charInfo.vertexIndex + j;
                    var orig = verts[ind];

                    verts[ind] = orig + Vector3.up * (Mathf.Sin(Time.time * _frequancy + orig.x * 0.01f * _length) * _amplitude);
                }
            }
        }
    }
}