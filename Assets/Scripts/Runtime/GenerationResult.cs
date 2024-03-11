
[System.Serializable]
public struct GenerationResult
{
    public static readonly GenerationResult zero = new(0, 0, 0);

    public float averageScore;
    public float bestScore;
    public float worstScore;

    public GenerationResult(float[] scores)
    {
        float totalScore = 0;

        float bestScore = float.MinValue;
        float worstScore = float.MaxValue;

        for (int i = 0; i < scores.Length; i++)
        {
            totalScore += scores[i];

            if (scores[i] > bestScore) bestScore = scores[i];
            if (scores[i] < worstScore) worstScore = scores[i];
        }

        this = new GenerationResult(totalScore / scores.Length, bestScore, worstScore);
    }

    public GenerationResult(float a, float b, float w)
    {
        averageScore = a;
        bestScore = b;
        worstScore = w;
    }
}
