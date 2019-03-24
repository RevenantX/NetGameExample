using System.Collections.Generic;
using Code.Shared;
using UnityEngine;

namespace Code.Client
{
    public class ClientPlayerManager : BasePlayerManager
    {
        private readonly Dictionary<byte, PlayerHandler> _players;
        private readonly ClientLogic _clientLogic;
        private ClientPlayer _clientPlayer;
        public ClientPlayer OurPlayer => _clientPlayer;
        

        public ClientPlayerManager(ClientLogic clientLogic)
        {
            _clientLogic = clientLogic;
            _players = new Dictionary<byte, PlayerHandler>();
        }
        
        public override IEnumerator<BasePlayer> GetEnumerator()
        {
            foreach (var ph in _players)
                yield return ph.Value.Player;
        }

        public void Update()
        {
            
        }

        public void ApplyServerState(ref ServerState serverState)
        {
            for (int i = 0; i < serverState.PlayerStatesCount; i++)
            {
                var state = serverState.PlayerStates[i];
                if(!_players.TryGetValue(state.Id, out var handler))
                    return;

                if (handler.Player == _clientPlayer)
                {
                    _clientPlayer.ReceiveServerState(serverState, state);
                }
                else
                {
                    var rp = (RemotePlayer)handler.Player;
                    rp.OnPlayerState(state);
                }
            }
        }

        public void CallShoot(Vector2 from, Vector2 to)
        {
            _clientLogic.SpawnShoot(from, to);
        }

        public BasePlayer GetById(byte id)
        {
            return _players.TryGetValue(id, out var ph) ? ph.Player : null;
        }

        public BasePlayer RemovePlayer(byte id)
        {
            if (_players.TryGetValue(id, out var handler))
            {
                _players.Remove(id);
                handler.View.Destroy();
            }
        
            return handler.Player;
        }

        public override void LogicUpdate()
        {
            foreach (var kv in _players)
                kv.Value.Update(LogicTimer.FixedDelta);
        }

        public override int Count => _players.Count;

        public void AddClientPlayer(ClientPlayer player, ClientPlayerView view)
        {
            _clientPlayer = player;
            _players.Add(player.Id, new PlayerHandler(player, view));
        }
        
        public void AddPlayer(RemotePlayer player, RemotePlayerView view)
        {
            _players.Add(player.Id, new PlayerHandler(player, view));
        }
    }
}