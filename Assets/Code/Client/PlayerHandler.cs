using Code.Shared;

namespace Code.Client
{
    public struct PlayerHandler
    {
        public readonly BasePlayer Player;
        public readonly IPlayerView View;

        public PlayerHandler(BasePlayer player, IPlayerView view)
        {
            Player = player;
            View = view;
        }

        public void Update(float delta)
        {
            Player.Update(delta);
        }
    }
}