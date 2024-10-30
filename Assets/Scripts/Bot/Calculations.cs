using UnityEngine;

namespace Scripts.Bot
{
    public class Calculations
    {
        public static float GetDistance(Vector2 vector1, Vector2 vector2)
        {
            float distanceX = Mathf.Abs(vector1.x - vector2.x);
            float distanceY = Mathf.Abs(vector1.y - vector2.y);
            var difference = Mathf.Abs(distanceX - distanceY);
            
            if (distanceX > distanceY)
                return 14 * distanceY + 10 * difference;
            
            return 14 * distanceX + 10 * difference;
        }
    }
}