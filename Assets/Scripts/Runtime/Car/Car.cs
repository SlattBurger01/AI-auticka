using System;
using UnityEngine;

public class Car : CarController
{
    public static readonly float maxRayDist = 6;
    private static readonly bool disableOnWrongCheckpointEnter = false;

    // ---------
    public const int raycastCount = 9;

    public const int inputsLength = raycastCount + 2;
    public const int outputsLength = 3;

    public float[] inputs { get; private set; } = new float[inputsLength];

    private static readonly string[] rn = new string[raycastCount];
    public static readonly string[] inputsNames = new string[inputsLength] { rn[0], rn[1], rn[2], rn[3], rn[4], rn[5], rn[6], rn[7], rn[8], "cSpeed", "cSteer" };

    public static readonly string[] outputNames = new string[outputsLength] { "tSpeed", "tSteer", "brake" };
    // ---------

    [SerializeField] private Transform calculationPoint;

    [Header("Refferences")]
    public Transform[] raycastsTrs;

    [Header("Settings")]
    public bool drawRaycasts;
    [SerializeField] private bool userControlled;
    [SerializeField] private bool selfUpdate;
    public bool cinematicUpdate = false;

    [Header("Auto assigned")]
    public NeuralNetwork network;
    public int id; // id in handlers array

    public bool enabled = true;
    public bool enabledLastFrame = true; // if car was enabled when job for its raycasts was scheduled

    public void InializeCar(NeuralNetwork network_)
    {
        network = network_;
        userControlled = false;
    }

    public void UpdateCar(float[] raycasts)
    {
        if (!enabled) return;

        if (false) // Debug
        {
            float[] d_rays = GetRaycastsLocally();

            for (int i = 0; i < d_rays.Length; i++)
            {
                if (Math.Round(d_rays[i] * 1000) / 1000 != Math.Round(raycasts[i] * 1000) / 1000)
                {
                    throw new Exception($"Raycasts are not same! ({d_rays[i]} (local), {raycasts[i]} (passed))");
                }
            }
        }

        UpdateInputsF(raycasts);

        float[] v = ProcessInput();

        TranslateCar(v[0], v[1], NeuralNetwork.FloatOutputToBool(v[2]));

        UpdateScore();

        if (drawRaycasts) // draw raycasts after car's position has been updated
        {
            float[] raysToDraw = GetRaycastsLocally();

            for (int i = 0; i < raysToDraw.Length; i++) UpdateDrawnRaycast(raycastsTrs[i], raysToDraw[i]);
        }

        ElapsedTime += Time.deltaTime;
    }

    public void SelfUpdate() => UpdateCar(GetRaycastsLocally());

    private SpriteRenderer[] carRenderers;

    private void Start()
    {
        Debug.LogWarning($"Car.cs has disabled fitness calculating ({nameof(RecalculateScore)})");

        carRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (raycastsTrs.Length != raycastCount) Debug.LogError($"Invalid raycast lenght (trs: {raycastsTrs.Length}, rC: {raycastCount})");

        if (selfUpdate) network = CarNetworkCreator.CreateNewRandomCarNetwork();
    }

    public float[] OutputsIntoArray() { return new float[] { base.o_speed, base.o_rotation }; }

    [Range(0, 1f)] public float sp;
    [Range(0, 1f)] public float st;

    private void Update()
    {
        if (cinematicUpdate)
        {
            if (enabled) TranslateCar(1, 1, false);
            return;
        }

        if (selfUpdate) SelfUpdate();
        if (userControlled) InputControl();
    }

    private void InputControl()
    {
        float speed = Input.GetAxis("Vertical") / 2 + .5f;
        float steer = Input.GetAxis("Horizontal") / 2 + .5f;

        bool brake = Input.GetKey(KeyCode.Space);

        if (enabled) TranslateCar(speed, steer, brake);
        else TranslateCar(0, 0, true);
    }

    /// <returns> speed, steer </returns>
    private float[] ProcessInput() => network.ProcessData(inputs);

    private float[] GetRaycastsLocally()
    {
        float[] raycasts = new float[raycastCount];

        for (int i = 0; i < raycastsTrs.Length; i++) raycasts[i] = GetRaycast(raycastsTrs[i]);

        return raycasts;
    }

    private void UpdateInputsF(float[] raycasts)
    {
        float[] inputs_ = new float[inputsLength];

        for (int i = 0; i < raycasts.Length; i++)
        {
            inputs_[i] = raycasts[i];
        }

        inputs_[^1] = currentSteer;
        inputs_[^2] = CurrentSpeed;

        inputs = inputs_;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        for (int i = 0; i < raycastsTrs.Length; i++)
        {
            Transform t = raycastsTrs[i];

            Gizmos.DrawLine(t.position, t.position + t.up * maxRayDist);
        }
    }
#endif

    private float GetRaycast(Transform t) => RaycastsHandler.GetCarRaycast(t, maxRayDist);

    private static readonly float raycastWidth = .04f;

    private bool needsToBeDisabled = false;

    [SerializeField] private Material lineMaterial;

    public void UpdateDrawnRaycast(Transform t, float distance)
    {
        if (drawRaycasts)
        {
            needsToBeDisabled = true;

            Color lerpedColor = Color.Lerp(Color.green, Color.red, MapRange(distance, 0, maxRayDist, 0, 1));

            LineRenderer r = t.GetComponent<LineRenderer>();
            if (!r) r = t.gameObject.AddComponent<LineRenderer>();

            r.startWidth = raycastWidth;
            r.endWidth = raycastWidth;

            r.SetPosition(0, t.position);
            r.SetPosition(1, t.position + t.up * distance);

            r.material = lineMaterial;

            r.startColor = lerpedColor;
            r.endColor = lerpedColor;
        }
        else if (needsToBeDisabled)
        {
            LineRenderer r = t.GetComponent<LineRenderer>();
            Destroy(r);

            needsToBeDisabled = false;
        }
    }

    public void DestroyRenderers()
    {
        for (int i = 0; i < raycastsTrs.Length; i++)
        {
            LineRenderer r = raycastsTrs[i].GetComponent<LineRenderer>();
            Destroy(r);
        }
    }

    public static float MapRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        value = Mathf.Max(fromMin, Mathf.Min(fromMax, value));

        float normalizedValue = (value - fromMin) / (fromMax - fromMin);

        return toMin + normalizedValue * (toMax - toMin);
    }

    private float CurOpacity => enabled ? 1 : .15f;

    public void SetMainColor(Color color, float oMult)
    {
        color.a = CurOpacity * oMult;

        GetComponentInChildren<SpriteRenderer>().color = color;
    }

    public void SetColor(Color color)
    {
        color.a = CurOpacity;

        for (int i = 0; i < carRenderers.Length; i++)
        {
            carRenderers[i].color = color;
        }
    }

    private int currentCheckpointId = 0;

    private void UpdateScore()
    {
        if (userControlled) return;
        Fitness = RecalculateScore(calculationPoint.position);
    }

    private float RecalculateScore(Vector2 carPosition)
    {
        return 0; // remove this for actuall training, it's here only for random path generator

        Checkpoint nextPoint = Handler.handler.GetCheckpoint(currentCheckpointId).next;
        return ScoreCalculation(nextPoint.totalScore, nextPoint.GetDistance(carPosition));
    }

    public static float ScoreCalculation(float nextTotScore, float dist) { return nextTotScore - dist; }

    // ---- STATS ----
    public CarStats stats;
    public float Fitness { get { return stats.fitness; } set { stats.fitness = value; } }
    public bool Finished { get { return stats.finished; } set { stats.finished = value; } }
    public float ElapsedTime { get { return stats.elapsedTime; } set { stats.elapsedTime = value; } }

    // ---- COLLISIONS ----
    public void OnCollisionWithWall()
    {
        UpdateScore();
        DisableCar();
    }

    public void OnTriggerWithFinish(Finish finish)
    {
        UpdateScore();

        float scoreToAdd = (Fitness / 2) - (ElapsedTime / 1.5f);

        if (scoreToAdd < 0) scoreToAdd = 0;

        Fitness += scoreToAdd;

        if (finish.final)
        {
            DisableCar();
            Finished = true;
        }
    }

    public void OnCheckpointEnter(Checkpoint point)
    {
        if (disableOnWrongCheckpointEnter && point.id < currentCheckpointId)
        {
            DisableCar();
            Fitness = 0;
        }

        currentCheckpointId = point.id;
    }

    private void DisableCar()
    {
        enabled = false;
        Handler.handler.OnCarDisabled(this);
    }

    public void SetOrderInLayer(int order)
    {
        for (int i = 0; i < carRenderers.Length; i++) carRenderers[i].sortingOrder = order;
    }
}

[System.Serializable]
public struct CarStats
{
    public float fitness;
    public bool finished;
    public float elapsedTime;

    public static CarStats Empty() { return new CarStats(float.MinValue, false, 0); }

    private CarStats(float f, bool fin, float eT)
    {
        fitness = f;
        finished = fin;
        elapsedTime = eT;
    }
}