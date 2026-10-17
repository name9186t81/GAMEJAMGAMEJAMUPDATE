using System;
using UnityEngine;

namespace Mechanics
{
    public class PlayerEvents : MonoSingletone<PlayerEvents>
    {
        public event Action<Vector2> OnPictureTaken;
        private Player _player;

        public void RegisterPlayer(Player player)
        {
            _player = player;
            player.Camera.OnPictureTaken += ReadPictureEvent;
        }

        private void ReadPictureEvent(Vector2 obj)
        {
            OnPictureTaken?.Invoke(obj);
        }

        protected override PlayerEvents GetInstance()
        {
            return this;
        }

        public Player Player => _player;
    }
}
