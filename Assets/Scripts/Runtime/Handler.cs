using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Cah = CarAppearenceHandler;
using Tx = TextHelper;

public class Handler : MonoBehaviour
{
    // -- OBSOLETE SETTINGS
    private static readonly bool createCopy = false;
    private static readonly bool enableCopiesText = true;
    private static readonly float maxRoundLenght = 999;
    // --

    public static readonly int bestCarLayerOrder = -1;
    public static readonly int sbestCarLayerOrder = -2;
    public static readonly int normalLayerOrder = -5;
    public static readonly int lavaOrderInLayerOrder = -10;
    public static readonly int disabledLayerOrder = -50;

    private static readonly int newGenFramePauses = 7;

    [SerializeField] private bool cinematic = false;
    [SerializeField] private GameObject canvas;
    [SerializeField] private Camera cinematicCam;

    [HideInInspector] public enum CameraType { _static, follow, cinematic }
    [HideInInspector] public bool running;

    [Header("Runtime Settings")] // values are private, because they are overwritten in runtime meny anyway
    private bool resetSave; // resets save & graphs
    private bool resetGraphs; // resets graphs
    private bool fastLearn; // resets round after two cars has finished

    [Header("Settings")]
    [SerializeField] private CameraType cameraType = CameraType._static;

    [SerializeField] private bool customBestCarColors;

    public bool displayCheckpoints;

    [Header(" -- Neuron dropout")]
    [SerializeField] private int disabledNeuronsCount = 2;
    [SerializeField] private bool disableNeuronsEveryOtherGen;

    [Header("")]
    [SerializeField] private int carCount = 1; // per batch
    [SerializeField] private int batchCountPerGen = 1;
    [SerializeField] private int bestCarToDisplay = -1; // -1 means disabled
    [SerializeField] private int saveFrequency = 1; // in generations

    public void SetPregameSettings(bool rS, bool rG, int bD, bool fl)
    {
        resetSave = rS;
        resetGraphs = rG;
        bestCarToDisplay = bD;

        fastLearn = fl;
    }

    [Range(.1f, 3f)]
    [SerializeField] private float timeScale = 1;

    [Header("Refferences")]
    [SerializeField] private Path targetPath;
    [SerializeField] private CameraHandler followCamera, staticCamera;

    [Header("Prefabs")]
    [SerializeField] private GameObject carPrefab;
    public GameObject conePrefab;

    public static Handler handler;

    private UIManager uiManager;
    private UITextsManager manager;
    private CinematicModeHandler cModeHandler;

    private float elapsedTime = 0; // elapsed time per round

    private List<GenerationResult> results = new List<GenerationResult>();

    private Data simData;

    private List<float> finishTimes = new List<float>();

    private Car[] cars = new Car[0]; // every car in current batch

    private Car bestCar, secondBestCar; // current
    private Car rBestCar, rSecondBestCar; // previous frame
    private Car bestCopy, secondBestCopy; // copied cars

    private List<NeuralNetwork> bestNetworkSaves = new List<NeuralNetwork>();
    private List<NeuralNetwork> secondbestNetworkSaves = new List<NeuralNetwork>();
    private List<TimeSpan> bestNetworkTrainingTimes = new List<TimeSpan>();

    private CarCreator creator;

    private static void SetupAppSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Application.runInBackground = true;
        Physics2D.IgnoreLayerCollision(6, 6); // car, car

        for (int i = 0; i < 9; i++) Physics2D.IgnoreLayerCollision(i, 8); // i, disabled path
    }

    public static bool customBestCarCols;

    private void Awake()
    {
        customBestCarCols = customBestCarColors;

        if (cinematic) SetUpCinematicMode();

        handler = this;
        uiManager = GetComponent<UIManager>();
        manager = GetComponent<UITextsManager>();

        HandlerUtils.SetupPaths(targetPath);
        SetupAppSettings();
    }

    private void SetUpCinematicMode()
    {
        cameraType = CameraType.cinematic;
        canvas.SetActive(false);

        cModeHandler = GetComponent<CinematicModeHandler>();
        cModeHandler._enabled = true;
    }

    private void Start()
    {
        UpdateCams(cameraType);

        if (targetPath.selfUpdate) targetPath.selfUpdate = false;
        targetPath.InializePath();
        staticCamera.SetStaticPosition(targetPath.staticCamPos.position, targetPath.staticCamSize);

        creator = new CarCreator(carPrefab, targetPath);

        if (running) SetupSim();

        uiManager.PresetNetwork();
    }

    private void UpdateCams(CameraType type)
    {
        staticCamera.Enable(type == CameraType._static);
        followCamera.Enable(type == CameraType.follow);
        cinematicCam.enabled = type == CameraType.cinematic;
    }

    public void SetupSim()
    {
        if (bestCarToDisplay != -1) cameraType = CameraType.follow;
        UpdateCams(cameraType);

        uiManager.UpdateCanvasLayout(cameraType);

        Save save = null;

        bool loaded = false;

        if (!resetSave) loaded = TryToLoadSave(out save);
        else print("Reseting save");

        NeuralNetwork bestCar = genBestCar = save?.parent1;
        NeuralNetwork secondBestCar = genSecondBestCar = save?.parent2;

        if (bestCarToDisplay == -1)
        {
            if (!loaded || resetGraphs) AddResultPoint(GenerationResult.zero);

            CreateNewGeneration(bestCar, secondBestCar);
            uiManager.SetNeuralNetworkDisplayer(bestCar ? bestCar : cars[0].network);
        }
        else BestCarDisplayModeSetupSim();
    }

    private void BestCarDisplayModeSetupSim()
    {
        if (bestNetworkSaves.Count <= bestCarToDisplay)
        {
            Debug.Log($"Can't display car at {bestCarToDisplay} - displaying at {bestNetworkSaves.Count - 1} instead");
            bestCarToDisplay = bestNetworkSaves.Count - 1;
        }

        uiManager.DisableGraphs();

        manager.UpdateGenText($"{(bestCarToDisplay + 1) * saveFrequency}");
        manager.UpdateTimeText(Tx.TimeSpanToString(bestNetworkTrainingTimes[bestCarToDisplay]));

        bestCopy = creator.CreateNewCarF(bestNetworkSaves[bestCarToDisplay]);
        uiManager.SetNeuralNetworkDisplayer(bestCopy.network);
    }

    /// <summary> Loads gens scores (if !resetGraphs) & Redraws renderers </summary>
    private void LoadGraphs(Save save) => uiManager.LoadGraphs(save, resetGraphs);

    public void AddFinishTime(float f)
    {
        uiManager.AddFinishTime(f);
        finishTimes.Add(f);
    }

    private void DisplayBestCar()
    {
        bestCopy.drawRaycasts = true;

        bestCopy.SelfUpdate();

        UpdateBestCarStatsTexts(bestCopy);

        followCamera.UpdatePosition(bestCopy.transform.position);

        FindObjectOfType<NeuralNetworkDisplayer>().displayActivatedNeurons = true;

        int active = bestCopy.enabled ? 1 : 0;
        int finished = bestCopy.Finished ? 1 : 0;

        manager.UpdateCarsLeftext($"Active cars: {active}/{1}");
        manager.UpdateFinishedCars($"Finished: {finished} ({GetPercentages(finished, carCount)}%)");
    }

    private void UpdateTargetPath()
    {
        if (targetPath.selfUpdate) targetPath.selfUpdate = false;
        targetPath.UpdateLava();
    }

    private void Update()
    {
        if (!running) return;

        Time.timeScale = timeScale;

        manager.UpdateFpsText(FpsCounter.GetFps().ToString("F1"));

        UpdateTargetPath();

        if (bestCarToDisplay != -1) { DisplayBestCar(); return; }

        if (genPauses > 0) { genPauses--; return; }

        UpdateCars(out int activeCars, out int finishedCars);

        UpdateCopiesTexts();

        HandlerUtils.CheckForCarChange(ref bestCar, ref rBestCar, delegate { OnBestCarChanged(bestCar, rBestCar); });
        HandlerUtils.CheckForCarChange(ref secondBestCar, ref rSecondBestCar, delegate { OnSecondBestCarChanged(secondBestCar, rSecondBestCar); });

        uiManager.TryUpdateNeuralNetworkDisplayer(bestCar);

        UpdateBestCarStatsTexts(bestCar);

        followCamera.UpdatePosition(bestCar.transform.position);

        elapsedTime += Time.deltaTime;

        bool enoughFinishes = fastLearn && finishedCars >= 2;

        if (activeCars == 0 || elapsedTime > maxRoundLenght || enoughFinishes) EndRound();

        UpdateGameStatsTexts(activeCars, finishedCars);
    }

    private NeuralNetwork genBestCar, genSecondBestCar; // cars used for creating new batches (best cars of previous generation)

    private NeuralNetwork currentGenBestCar, currentSecondBestCar;
    private CarStats currentBestCarStats, currentSecondBestCarStats;

    private int batchesLeft;

    private void EndRound()
    {
        RaycastsHandler.first = true;

        simData.samplesCount += carCount;

        for (int i = 0; i < cars.Length; i++) gensScores.Add(cars[i].Fitness);

        UpdateCurrentGenBestCars(bestCar);
        UpdateCurrentGenBestCars(secondBestCar);
        // --- -

        print($"Batches left {batchesLeft}");

        if (batchesLeft - 1 <= 0)
        {
            EndGeneration();

            genBestCar = NeuralNetwork.DeepCloneNetwork(currentGenBestCar);
            genSecondBestCar = NeuralNetwork.DeepCloneNetwork(currentSecondBestCar);

            CreateNewGeneration(currentGenBestCar, currentSecondBestCar);
        }
        else
        {
            CreateNewBatch(genBestCar, genSecondBestCar);
            batchesLeft--;

            UpdateGensTexts();
        }

        targetPath.ResetAndStartLava();
        targetPath.UpdateConeFields();
    }

    private void UpdateCurrentGenBestCars(Car car)
    {
        //print($"Updating cur gen best car: {car}");

        if (currentBestCarStats.fitness < car.Fitness)
        {
            //print("-- Updated as best car");

            currentSecondBestCar = currentGenBestCar;
            currentSecondBestCarStats = currentBestCarStats;

            currentGenBestCar = NeuralNetwork.DeepCloneNetwork(car.network);
            currentBestCarStats = car.stats;
        }
        else if (currentSecondBestCarStats.fitness < car.Fitness)
        {
            //print("-- Updated as second best car");

            currentSecondBestCar = NeuralNetwork.DeepCloneNetwork(car.network);
            currentSecondBestCarStats = car.stats;
        }
    }

    private List<float> gensScores = new List<float>();

    private void EndGeneration()
    {
        if (simData.generation % saveFrequency == 0)
        {
            bestNetworkSaves.Add(NeuralNetwork.DeepCloneNetwork(currentGenBestCar));
            secondbestNetworkSaves.Add(NeuralNetwork.DeepCloneNetwork(currentSecondBestCar));
            bestNetworkTrainingTimes.Add(simData.totalElapsedTime);

            print($"Saving best network ({currentBestCarStats.fitness}) ({simData.generation}) - {bestNetworkSaves.Count}");
        }

        if (currentGenBestCar && currentBestCarStats.finished)
        {
            AddFinishTime(currentBestCarStats.elapsedTime);
        }

        GenerationResult result = new GenerationResult(gensScores.ToArray());
        gensScores.Clear();

        AddResultPoint(result);

        currentBestCarStats = CarStats.Empty();
        currentSecondBestCarStats = CarStats.Empty();

        SaveGen(currentGenBestCar, currentSecondBestCar);
    }

    public void AddResultPoint(GenerationResult result)
    {
        results.Add(result);
        uiManager.AddScoreRenderPoints(result);
    }

    private void UpdateCars(out int activeCars, out int finishedCars)
    {
        bestCar = null;
        secondBestCar = null;

        activeCars = 0;
        finishedCars = 0;

        float bestScore = float.MinValue;
        float secondBestScore = float.MinValue;

        // --- Get raycasts
        List<Transform> raycastPoints = new List<Transform>(cars.Length * 14);
        for (int i = 0; i < cars.Length; i++)
        {
            if (!cars[i].enabled) continue;
            for (int y = 0; y < cars[i].raycastsTrs.Length; y++)
            {
                raycastPoints.Add(cars[i].raycastsTrs[y]);
            }
        }

        float[] raycasts = RaycastsHandler.GetRaycasts(raycastPoints.ToArray(), Car.maxRayDist);

        int rayIndex = 0;

        for (int i = 0; i < cars.Length; i++)
        {
            // ---
            float[] rays = new float[Car.raycastCount];

            if (cars[i].enabledLastFrame)
            {
                for (int y = 0; y < rays.Length; y++)
                {
                    rays[y] = raycasts[y + rayIndex * Car.raycastCount];
                }

                rayIndex++;
            }
            // ---

            //if (cars[i].enabledLastFrame) rayIndex++;

            if (!cars[i].enabled) cars[i].enabledLastFrame = false;

            cars[i].UpdateCar(rays);
            if (cars[i].enabled) activeCars++;
            if (cars[i].Finished) finishedCars++;

            if (cars[i].Fitness > bestScore)
            {
                bestScore = cars[i].Fitness;
                if (bestCar) secondBestScore = bestCar.Fitness;

                secondBestCar = bestCar;
                bestCar = cars[i];
            }
            else if (cars[i].Fitness > secondBestScore && cars[i] != bestCar)
            {
                secondBestScore = cars[i].Fitness;

                secondBestCar = cars[i];
            }
        }
    }

    private void UpdateCopiesTexts() => uiManager.UpdateCopiesTexts(bestCopy && enableCopiesText, bestCopy, secondBestCopy);

    private int genPauses = 0;

    private bool disableNeurons = false;

    private void CreateNewGeneration(NeuralNetwork bestCar_, NeuralNetwork secondBestCar_)
    {
        int neuronsToDisable = disableNeuronsEveryOtherGen && (disableNeurons = !disableNeurons) ? 0 : disabledNeuronsCount;
        NeuralNetwork.DisableRandomNeurons(neuronsToDisable, CarNetworkCreator.CreateNewCarNetwork());

        genPauses = newGenFramePauses;

        batchesLeft = batchCountPerGen;
        simData.generation++;
        elapsedTime = 0;
        rBestCar = bestCopy;

        UpdateGensTexts();

        CreateNewBatch(bestCar_, secondBestCar_);
    }

    private void CreateNewBatch(NeuralNetwork bestCar_, NeuralNetwork secondBestCar_) => CreateNewCars(bestCar_, secondBestCar_);

    private void CreateNewCars(NeuralNetwork bestCar_, NeuralNetwork secondBestCar_)
    {
        if (cars != null) HandlerUtils.DestroyCars(cars);

        if (createCopy && bestCar_)
        {
            cars = creator.CreateCars(carCount, 2, bestCar_, secondBestCar_);

            bestCopy = cars[^1] = creator.CopyCar(bestCar_);
            secondBestCopy = cars[^2] = creator.CopyCar(secondBestCar_);
        }
        else cars = creator.CreateCars(carCount, 0, bestCar_, secondBestCar_);
    }

    public Checkpoint GetCheckpoint(int id) => targetPath.points[id];

    // --- On Best Car change
    public void OnCarDisabled(Car car)
    {
        if (car == bestCar) Cah.UpdateBestCar(car);
        else if (car == secondBestCar) Cah.UpdateSecondBestCar(car);
        else Cah.UpdateDefaultCar(car);
    }

    private void OnBestCarChanged(Car bestCar, Car rBestCar)
    {
        if (rBestCar != secondBestCar) Cah.UpdateDefaultCar(rBestCar);
        Cah.UpdateBestCar(bestCar);
    }

    private void OnSecondBestCarChanged(Car sbCar, Car rSbCar)
    {
        if (rSbCar != bestCar) Cah.UpdateDefaultCar(rSbCar);
        Cah.UpdateSecondBestCar(sbCar);
    }
    // --- --------- ---

    #region Saving & loading
    // --- Saving & loading ---
    private bool TryToLoadSave(out Save save)
    {
        save = SaveAndLoadSystem.LoadDataFromDisk<Save>();

        print($"Loading save ({save.genResults.Length})");

        if (save != null && save.genResults.Length != 0)
        {
            LoadSave(save);
            return true;
        }

        return false;
    }

    private void LoadSave(Save save)
    {
        simData = save.simData;

        bestNetworkSaves = save.bestNeuralNetworks.ToList();
        secondbestNetworkSaves = save.secondbestNeuralNetworks.ToList();
        bestNetworkTrainingTimes = save.bestNetworkTrainingTimes.ToList();

        LoadGraphs(save);

        UpdateGensTexts();
        UpdateGameStatsTexts(0, 0);
    }

    /// <summary> Creates new save & writes it onto the drive </summary>
    private void SaveGen(NeuralNetwork bestCar, NeuralNetwork secondBestCar)
    {
        Save save = new()
        {
            simData = simData,

            parent1 = NeuralNetwork.DeepCloneNetwork(bestCar),
            parent2 = NeuralNetwork.DeepCloneNetwork(secondBestCar),

            genResults = results.ToArray(),
            finishTimes = finishTimes.ToArray(),

            bestNeuralNetworks = bestNetworkSaves.ToArray(),
            secondbestNeuralNetworks = secondbestNetworkSaves.ToArray(),
            bestNetworkTrainingTimes = bestNetworkTrainingTimes.ToArray()
        };

        print($"Saving gen! - {save.genResults.Length}");

        SaveAndLoadSystem.SaveDataOntoDisk(save);
    }
    // --- --------- ---
    #endregion

    #region Kind of Utils or someting
    // --- Texts ---
    private void UpdateGensTexts()
    {
        string batchesText = batchCountPerGen > 1 ? $"({batchCountPerGen - batchesLeft + 1}/{batchCountPerGen})" : "";
        manager.UpdateGenText($"{simData.generation}", $"{batchesText}");
    }

    private void UpdateGameStatsTexts(int activeCars, int finishedCars)
    {
        manager.UpdateElapsedTimeText($"{maxRoundLenght - elapsedTime:F1)}s left");
        manager.UpdateCarsLeftext($"Active cars: {activeCars}/{cars.Length}");

        int doneBatches = (simData.generation - 1) * batchCountPerGen + (batchCountPerGen - batchesLeft);
        manager.UpdateSamplesText($"Samples: {HandlerUtils.LargeIntToString(simData.samplesCount)}");
        manager.UpdateFinishedCars($"Finished: {finishedCars} ({GetPercentages(finishedCars, carCount):F2}%)");

        TimeSpan nTime = TimeSpan.FromSeconds(Time.deltaTime);
        simData.totalElapsedTime = simData.totalElapsedTime.Add(nTime);
        manager.UpdateTimeText(Tx.TimeSpanToString(simData.totalElapsedTime));
    }

    private float GetPercentages(float v, float t)
    {
        if (t == 0) return 0;
        return (v / t) * 100;
    }

    private const string bestCarStatsRounding = "f2";

    private void UpdateBestCarStatsTexts(Car bestCar)
    {
        string dir = bestCar.o_rotation > 0 ? "left" : "right";
        manager.UpdateTurnText($"{(-bestCar.o_rotation).ToString(bestCarStatsRounding)} ({dir})");

        string brake = bestCar.o_brake ? " (braking)" : "";
        manager.UpdateEngineText($"{bestCar.o_speed.ToString(bestCarStatsRounding)}{brake}");
        manager.UpdateAvgSpeedText($"{bestCar.avgSpeed.ToString(bestCarStatsRounding)}");
        manager.UpdateScoreText($"{bestCar.Fitness.ToString(bestCarStatsRounding)}");
    }
    // --- --------- ---
    #endregion
}

public static class HandlerUtils
{
    public static string LargeIntToString(int val) => LargeIntToString((long)val);

    public static string LargeIntToString(long val)
    {
        string str = val.ToString();

        StringBuilder builder = new StringBuilder();

        for (int i = str.Length - 1; i >= 0; i--)
        {
            builder.Insert(0, str[i]);

            int pos = str.Length - 1 - i;

            if ((pos + 1) % 3 == 0 && pos != 0) builder.Insert(0, ' ');
        }

        return builder.ToString();
    }

    public static void UpdateCopyText(GameObject g, bool b, Car c)
    {
        if (!g) return;
        Vector2 pos = b ? c.transform.position : Vector2.one * -999;
        g.transform.position = pos;
    }

    /// <summary> If c != rC: a will be invoked </summary>
    public static void CheckForCarChange(ref Car c, ref Car rC, Action a)
    {
        if (c == rC) return;

        a.Invoke();
        rC = c;
    }

    public static void DestroyCars(Car[] cars)
    {
        for (int i = 0; i < cars.Length; i++) GameObject.Destroy(cars[i].gameObject);
    }

    public static void SetupPaths(Path targetPath)
    {
        if (targetPath.selfUpdate) targetPath.selfUpdate = false;

        Path[] paths = GameObject.FindObjectsOfType<Path>();

        for (int i = 0; i < paths.Length; i++)
        {
            if (paths[i] == targetPath) continue;

            foreach (Transform obj in paths[i].GetComponentsInChildren<Transform>()) obj.gameObject.layer = 8;
        }
    }
}

[System.Serializable]
public struct Data
{
    public int generation;
    public TimeSpan totalElapsedTime;
    public long samplesCount;
}