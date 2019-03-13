using UnityEngine;

namespace Code.Shared
{
    public class BasePlayer
    {
        public string Name;
        private Vector2 _position;
        private float _rotation;

        public Vector2 Position
        {
            get { return _position; }
        }

        public float Rotation
        {
            get { return _rotation; }
        }

        public virtual void Spawn(Vector2 position)
        {
            _position = position;
            _rotation = 0;
        }

        public void Move(MovementPacket command, float delta)
        {
            _position += command.Velocity.normalized * delta;
            _rotation = command.Rotation;
        }

        public virtual void Update(float delta)
        {
            
        }
    }
}

