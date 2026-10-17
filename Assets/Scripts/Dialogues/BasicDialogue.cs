using Dialogues;
using TMPro;
using UnityEngine;

namespace Mechanics
{
    public class BasicDialogue : MonoBehaviour
    {
        [SerializeField] private string _dialogueName;
        [SerializeField] private float _lineDelay;
        [SerializeField] private float _lineDelayPerCharacter;
        [SerializeField] private float _newLineDelay;
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private SpriteRenderer _wrapper;
        private string[] _dialogue;
        private int _index;
        private float _elapsed;
        private float _newLineElapsed;
        private FadeInText _fadeIn;
        private TextEffectsParser _parser;

        private void Start()
        {
            if (DialoguesProvider.Exists && DialoguesProvider.Instance.TryGetDialogue(_dialogueName, out string[] dialogue))
            {
                _dialogue = dialogue;
                _index = 0;
                _elapsed = 0f;
            }
            else
            {
                Debug.LogError("DIALOGUE ERROR - DIALOGUE WASNT FOUND");
                return;
            }

            if(_text.gameObject.TryGetComponent<FadeInText>(out _fadeIn))
            {
                _wrapper.size = Vector2.zero;
                _wrapper.gameObject.SetActive(false);
                _text.gameObject.TryGetComponent<TextEffectsParser>(out _parser);
            }

            _text.text = _dialogue[0];
            if(_parser != null)
            {
                _parser.ResetEffects();
            }
        }

        private void LateUpdate()
        {
            if (_dialogue == null || _index == _dialogue.Length) return;

            if(_newLineElapsed < _newLineDelay)
            {
                _text.enabled = false;
                _newLineElapsed += Time.deltaTime;
            }
            else
            {
                _text.enabled = true;

            }

            float endDelay = 0f;
            bool displayFinished;

            if (_fadeIn != null)
            {
                displayFinished = _fadeIn.Finished;
                endDelay = _lineDelay;
            }
            else
            {
                displayFinished = _elapsed > _text.text.Length * _lineDelayPerCharacter;
                endDelay = _text.text.Length * _lineDelayPerCharacter + _lineDelay;
            }

            if (displayFinished)
            {
                _elapsed += Time.deltaTime;
            }

            if(_elapsed > endDelay)
            {
                _index++;
                _elapsed = _newLineElapsed = 0f;

                if (_index == _dialogue.Length)
                {
                    _index = 0;
                }

                if (_parser != null)
                {
                    _text.text = _dialogue[_index];
                    _parser.ResetEffects();
                }
            }
            var bounds = _text.bounds;

            _wrapper.transform.position = _text.transform.TransformPoint(_text.textBounds.center);
            _wrapper.size = new Vector2(bounds.size.x * _text.transform.localScale.x + 0.3f, 1f);
            _wrapper.transform.localScale = new Vector3(1f, bounds.size.y * _text.transform.localScale.y + 0.1f, 1f);
        }
    }
}
