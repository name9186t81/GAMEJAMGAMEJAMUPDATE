using Health;
using System;
using UnityEngine;

namespace Mechanics
{
    public class PlayerEvents : MonoSingletone<PlayerEvents>
    {
        public event Action<Vector2> OnPictureTaken;
        public event Action<DamageArgs> OnDeath;
        public event Action<Player> OnPlayerRegistry;
        public event Action<Vector2> OnPlayerPhoto;
        private Player _player;

        public void RegisterPlayer(Player player)
        {
            _player = player;
            player.OnPictureTaken += ReadPictureEvent;
            player.OnDeath += PlayerDeath;
            player.Camera.OnPictureTaken += PhotoTaken;

            OnPlayerRegistry?.Invoke(player);
        }

        private void PhotoTaken(Vector2 obj)
        {
            OnPlayerPhoto?.Invoke(obj);
        }

        private void PlayerDeath(DamageArgs obj)
        {
            OnDeath?.Invoke(obj);
            _player.OnDeath -= PlayerDeath;
        }

        private void ReadPictureEvent(Vector2 obj)
        {
            Debug.Log("Picture3");
            OnPictureTaken?.Invoke(obj);
        }

        protected override PlayerEvents GetInstance()
        {
            return this;
        }

        public Player Player => _player;
    }
}
