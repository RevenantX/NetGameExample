namespace Code.Shared
{
    public static class Collisions
    {
        public static bool CheckIntersection(float x1, float y1, float x2, float y2, BasePlayer player)
        {
            float cx = player.Position.x;
            float cy = player.Position.y;
            float distX = x2-x1;
            float distY = y2-y1;
            float dot = ( (cx-x1)*distX + (cy-y1)*distY ) / (distX*distX + distY*distY);
            distX = x1 + dot * distX - cx;
            distY = y1 + dot * distY - cy;
            return distX*distX + distY*distY <= BasePlayer.Radius * BasePlayer.Radius;
        }
    }
}