using UnityEngine;

public class F3DMath : MonoBehaviour
{

    public static float ScaleRange(float value, float oldA, float oldB, float newA, float newB)
    {
        return (((newB - newA) * (value - oldA)) / (oldB - oldA)) + newA;
    }

    public static float EaseInOut(float value, float power)
    {
        float ease = Mathf.Pow(Mathf.Abs(value), power) /
                     (Mathf.Pow(Mathf.Abs(value), power) + Mathf.Pow((1 - Mathf.Abs(value)), power));
        if (value < 0)
            return -ease;
        else
            return ease;
    }
}
