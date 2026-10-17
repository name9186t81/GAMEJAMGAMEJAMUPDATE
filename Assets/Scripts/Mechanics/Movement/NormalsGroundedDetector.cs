using System;
using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
    public class NormalsGroundedDetector : GroundDetector
    {
        private bool _isGrounded;
        private bool _wasGrounded;

        private bool _isTouchingWall;
        private bool _wasTouchingWall;

        private bool _isTouchingCeiling;
        private bool _wasTouchingCeiling;

        private float _timeSinceGrounded;

        private Dictionary<int, CollisionInfo> _collisionsNormals = new Dictionary<int, CollisionInfo>();
        private CollisionDetails _mask;

        public override bool IsGrounded => _isGrounded;

        public override float TimeSinceGrounded => _timeSinceGrounded;

        public override CollisionDetails Details => _mask;

        public override CollisionInfo GetCollisionInfo(CollisionDetails type)
        {
            return _collisionsNormals[(int)type];
        }

        private void Awake()
        {
            int length = Enum.GetValues(typeof(CollisionDetails)).Length;
            for(int i = 0; i < length; i++)
            {
                _collisionsNormals.Add(1 << i, default);
            }
        }

        private void FixedUpdate()
        {
            if (!_isGrounded)
            {
                _timeSinceGrounded += Time.deltaTime;
            }
            else
            {
                _timeSinceGrounded = 0f;
            }

            _wasGrounded = _isGrounded;
            _isGrounded = false;

            _wasTouchingWall = _isTouchingWall;
            _isTouchingWall = false;

            _wasTouchingCeiling = _isTouchingCeiling;
            _isTouchingCeiling = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            SolveGrounding(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            SolveGrounding(collision);
        }

        private void SolveGrounding(Collision2D collision)
        {
            Vector2 perp = new Vector2(-GravityDirection.y, GravityDirection.x);
            Vector2 groundNormal = Vector2.zero;
            Vector2 wallNormal = Vector2.zero;
            Vector2 ceilingNormal = Vector2.zero;

            Vector2 groundPoint = Vector2.zero;
            Vector2 wallPoint = Vector2.zero;
            Vector2 ceilingPoint = Vector2.zero;

            for (int i = 0; i < collision.contactCount; i++)
            {
                var cont = collision.contacts[i];

                bool prevGround = _isGrounded;
                bool prevTouchingWall = _isTouchingWall;
                bool prevTouchingCeiling = _isTouchingCeiling;

                _isGrounded |= Vector2.Dot(cont.normal, -GravityDirection) > 0.8f;
                _isTouchingWall |= Mathf.Abs(Vector2.Dot(cont.normal, perp)) > 0.8f;
                _isTouchingCeiling |= Vector2.Dot(cont.normal, GravityDirection) > 0.8f;

                if(!prevGround && _isGrounded)
                {
                    groundNormal = cont.normal;
                    groundPoint = cont.point;
                }
                if(!prevTouchingWall && _isTouchingWall)
                {
                    wallNormal = cont.normal;
                    wallPoint = cont.point;
                }
                if(!prevTouchingCeiling && _isTouchingCeiling)
                {
                    ceilingPoint = cont.point;
                    ceilingNormal = cont.normal;
                }
            }

            _mask = (CollisionDetails)((_isGrounded ? 1 << 0 : 0) | (_isTouchingWall ? 1 << 1 : 0) | (_isTouchingCeiling ? 1 << 2 : 0));
            _collisionsNormals[1 << 0] = new CollisionInfo() { Normal = groundNormal, Point = groundPoint };
            _collisionsNormals[1 << 1] = new CollisionInfo() { Normal = wallNormal, Point = wallPoint };
            _collisionsNormals[1 << 2] = new CollisionInfo() { Normal = ceilingNormal, Point = ceilingPoint };

            if (_wasGrounded && !_isGrounded)
            {
                InvokeLeavingGround();
                InvokeCollisionLoss(0);
            }
            if(!_wasGrounded && _isGrounded)
            {
                InvokeLanding();
                InvokeCollision(0);
            }

            if(_wasTouchingWall && !_isTouchingWall)
            {
                InvokeCollisionLoss(1);
            }
            if(!_wasTouchingWall && _isTouchingWall)
            {
                InvokeCollision(1);
            }

            if (_wasTouchingCeiling && !_isTouchingCeiling)
            {
                InvokeCollisionLoss(2);
            }
            if (!_wasTouchingCeiling && _isTouchingCeiling)
            {
                InvokeCollision(2);
            }
        }
    }
}
