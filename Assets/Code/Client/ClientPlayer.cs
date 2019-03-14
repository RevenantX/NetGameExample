using Code.Shared;

namespace Code.Client
{
    public class ClientPlayer : BasePlayer
    {
        public readonly long Id;

        public ClientPlayer(long id)
        {
            Id = id;
        }
        public override void Update(float delta)
        {
            base.Update(delta);
        }
    }
}