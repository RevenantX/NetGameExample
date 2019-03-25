using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Shared
{
    public abstract class BasePlayerManager : IEnumerable<BasePlayer>
    {
        public abstract IEnumerator<BasePlayer> GetEnumerator();
        public abstract int Count { get; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BasePlayer CastToPlayer(Vector2 from, Vector2 dir, float length, BasePlayer exclude)
        {
            BasePlayer result = null;
            Vector2 target = from + dir * length;
            foreach(var p in this)
            {
                if(p == exclude)
                    continue;
                if (Collisions.CheckIntersection(from.x, from.y, target.x, target.y, p))
                {
                    //TODO: check near
                    if(result == null)
                        result = p;
                }
            }
            
            return result;
        }

        public abstract void LogicUpdate();
        public abstract void OnShoot(BasePlayer from, Vector2 to, BasePlayer hit);
    }
}