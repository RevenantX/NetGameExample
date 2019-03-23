using UnityEngine;

namespace Code.Shared
{
    public class BasePlayer
    {
        public readonly string Name;

        private float _speed = 3f;
        
        protected Vector2 _position;
        protected float _rotation;
        protected byte _health;

        public bool IsAlive
        {
            get { return _health > 0; }
        }

        public byte Health
        {
            get { return _health; }
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public float Rotation
        {
            get { return _rotation; }
        }

        public BasePlayer(string name)
        {
            Name = name;
        }

        public virtual void Spawn(Vector2 position)
        {
            _position = position;
            _rotation = 0;
            _health = 100;
        }

        public virtual void ApplyInput(PlayerInputPacket command, float delta)
        {
            Vector2 velocity = Vector2.zero;
            
            if ((command.Keys & MovementKeys.Up) != 0)
                velocity.y = -1f;
            if ((command.Keys & MovementKeys.Down) != 0)
                velocity.y = 1f;
            
            if ((command.Keys & MovementKeys.Left) != 0)
                velocity.x = -1f;
            if ((command.Keys & MovementKeys.Right) != 0)
                velocity.x = 1f;
            
            _position += velocity.normalized * _speed * delta;
            _rotation = command.Rotation;
        }

        public virtual void Update(float delta)
        {
            
        }
    }
}

