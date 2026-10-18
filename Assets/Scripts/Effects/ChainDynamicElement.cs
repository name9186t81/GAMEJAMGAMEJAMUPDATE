using UnityEngine;

namespace IK
{
    [RequireComponent(typeof(Chain))]
    public class ChainDynamicElement : MonoBehaviour
    {
        [SerializeField] private GameObject _endAttachment;
        [SerializeField] private bool _addCollisionToEntireChain;
        [SerializeField] private bool _addCollisionToLastElement;

        private Chain _chain;

        private void Start()
        {
            _chain = GetComponent<Chain>();

            _chain.OnStep += Step;
        }

        private void Step()
        {
            if (_addCollisionToEntireChain)
            {
                for(int i = 0; i < _chain.Points.Length; i++)
                {
                    _chain.Points[i].Update();
                }
            }
            else if (_addCollisionToLastElement)
            {
                _chain.Points[_chain.Points.Length - 1].Update();
            }

            if(_endAttachment != null)
            {
                var point = _chain.Points[_chain.Points.Length - 1];
                _endAttachment.transform.position = point.Position;
                _endAttachment.transform.up = point.GetDirection(_chain.Points[_chain.Points.Length - 2]);
            }
        }
    }
}
