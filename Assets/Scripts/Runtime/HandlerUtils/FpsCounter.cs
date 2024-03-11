using UnityEngine;

public static class FpsCounter
{
    private static float fpsTimeLeft;
    private static float fps;

    public static float GetFps()
    {
        if (fpsTimeLeft <= 0)
        {
            fps = 1f / Time.deltaTime;
            fpsTimeLeft = .1f;
        }
        else fpsTimeLeft -= Time.deltaTime;

        return fps;
    }
}