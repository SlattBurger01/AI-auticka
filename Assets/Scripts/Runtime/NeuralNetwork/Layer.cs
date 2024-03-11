[System.Serializable]
public class Layer
{
    public int NeuronCount => values.Length;
    public int WeightsCount => weights.Length;

    public float[] values;
    public float[] biases;
    public float[] weights;

    public static Layer NewLayer(int length, int nextLenth) => new Layer(length, length * nextLenth);

    /// <summary> Clones only lengths, not values </summary>
    public static Layer CloneLayer(Layer model) => new Layer(model.NeuronCount, model.WeightsCount);

    public static Layer DeepCloneLayer(Layer model)
    {
        Layer l = CloneLayer(model);

        l.values = (float[])model.values.Clone();
        l.biases = (float[])model.biases.Clone();
        l.weights = (float[])model.weights.Clone();

        return l;
    }

    private Layer(int l1, int l2)
    {
        values = new float[l1];
        biases = new float[l1];

        weights = new float[l2];
    }
}
