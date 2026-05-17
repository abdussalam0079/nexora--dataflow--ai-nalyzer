namespace DataFlow.UI.Helpers;

public static class AnimationHelper
{
    public static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

    public static int Lerp(int from, int to, float t) => from + (int)((to - from) * t);

    public static Color Lerp(Color from, Color to, float t) =>
        Color.FromArgb(
            LerpChannel(from.A, to.A, t),
            LerpChannel(from.R, to.R, t),
            LerpChannel(from.G, to.G, t),
            LerpChannel(from.B, to.B, t));

    private static int LerpChannel(int a, int b, float t) => a + (int)((b - a) * t);
}
