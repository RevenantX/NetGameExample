using Code.Shared;

namespace Code.Client
{
    public class RemotePlayer : BasePlayer
    {
        public RemotePlayer(string name, PlayerState pjPacket) : base(name)
        {
            _position = pjPacket.Position;
            _health = pjPacket.Health;
            _rotation = pjPacket.Rotation;
        }
    }
}