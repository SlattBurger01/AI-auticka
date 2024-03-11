using UnityEngine;

public static class MathHelper
{
    public static float Sigmoid(float x) 
    { 
        return 1f / (1f + Mathf.Exp(-x)); 
    }

    public static float LeakyRelu(float x)
    {
        return Mathf.Max(0.01f * x, x);
    }

    public static float Tanh(float x)
    {
        return System.MathF.Tanh(x);
    }
}
