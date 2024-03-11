using MutationOption = NeuralNetworkSettings.MutationOption;
using Random = UnityEngine.Random;
using Settings = NeuralNetworkSettings;

public static class NetworkRandomizer
{
    private static readonly System.Random random = new System.Random();

    public static void FullRandomizeLayer(ref Layer layer)
    {
        for (int i = 0; i < layer.WeightsCount; i++) layer.weights[i] = RandomizeBaseValue();

        for (int i = 0; i < layer.NeuronCount; i++) layer.biases[i] = RandomizeBaseValue();
    }

    public static void FullRandomizeLayers(ref Layer[] layers)
    {
        for (int i = 0; i < layers.Length - 1; i++) FullRandomizeLayer(ref layers[i]);
    }

    public static float RandomizeBaseValue() => Random.Range(-Settings.baseRange, Settings.baseRange);

    public static float RandomizedNeuron(float value, float mutationValue)
    {
        if (!NeuralNetwork.CanMutate()) return value;

        MutationOption option = GetRandomOption();

        return RandomizeNeuronValue(value, mutationValue, option);
    }

    private static MutationOption GetRandomOption()
    {
        float chance = 0;

        for (int i = Settings.chances.Length - 1; i >= 0; i--)
        {
            if (CanDo(chance += Settings.chances[i])) return (MutationOption)i;
        }

        return MutationOption.Addition;
    }

    /// <param name="mutationValue"> 0 <= x < 1 </param>
    /// <returns> Randomized value </returns>
    public static float RandomizeNeuronValue(float value, float mutationValue, MutationOption option)
    {
        if (mutationValue < 0 || mutationValue >= 1) throw new System.ArgumentOutOfRangeException("");

        switch (option)
        {
            case MutationOption.Addition: return value + RandomAddition(mutationValue);
            case MutationOption.Multiplication: return value * RandomMultiplication(mutationValue);
            case MutationOption.Replacement: return RandomizeBaseValue();
            case MutationOption.Sign: return -value;

            default: throw new System.Exception("Unimplemented mutation option! ");
        }
    }

    public static float RandomAddition(float range) => RandomFloat(range);

    /// <param name="range"> 0 <= x < 1 </param>
    public static float RandomMultiplication(float range) => RandomFloat(1 - range, 1 + range);

    private static float RandomFloat(float range) => RandomFloat(-range, range);
    private static float RandomFloat(float min, float max) { return (float)random.NextDouble() * (max - min) + min; }

    public static bool CanDo(float chance) { return random.NextDouble() < chance; }
}
