using UnityEngine;

namespace Code.Client
{
    public class RemotePlayerView : MonoBehaviour
    {
        private RemotePlayer _player;
        
        public static void Create(RemotePlayerView prefab, RemotePlayer player)
        {
            Quaternion rot = Quaternion.Euler(0f, player.Rotation, 0f);
            var obj = Instantiate(prefab, player.Position, rot);
            obj._player = player;
        }

        private void Update()
        {
            _player.UpdatePosition(Time.deltaTime);
            transform.position = _player.Position;
            transform.rotation =  Quaternion.Euler(0f, 0f, _player.Rotation * Mathf.Rad2Deg );
        }
    }
}