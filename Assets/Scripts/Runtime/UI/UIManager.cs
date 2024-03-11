using UnityEngine;
using CameraType = Handler.CameraType;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GraphRenderer scoreRenderer;
    [SerializeField] private GraphRenderer timesRenderer;

    [SerializeField] private GameObject followCamBg;
    [SerializeField] private Transform textsParent;

    [SerializeField] private GameObject bestCarStats;

    /// <summary> Loads gens scores (if !resetGraphs) & Redraws renderers </summary>
    public void LoadGraphs(Save save, bool reset)
    {
        if (reset) return;

        //print("LoadedTimes");
        //for (int i = 0; i < save.finishTimes.Length; i++) print(save.finishTimes[i]);

        for (int i = 0; i < save.genResults.Length; i++) Handler.handler.AddResultPoint(save.genResults[i]);
        for (int i = 0; i < save.finishTimes.Length; i++) Handler.handler.AddFinishTime(save.finishTimes[i]);
    }

    public void DisableGraphs()
    {
        scoreRenderer.gameObject.SetActive(false);
        timesRenderer.gameObject.SetActive(false);
    }

    public void AddFinishTime(float f) => timesRenderer.AddPointsOnly(f);

    //public void AddFinishTimeAndRedraw(float f) => timesRenderer.AddPointsOnly(f);

    //public void AddScoreRenderPoints(GenerationResult result) => AddRenderPointsOnly(result);

    public void AddScoreRenderPoints(GenerationResult result)
    {
        scoreRenderer.AddPointsOnly(new float[] { result.averageScore, result.bestScore, result.worstScore });
    }

    // ---- Network renderer
    public NeuralNetworkDisplayer networkDisplayer;
    private Car rfCar;

    private static readonly float minFitnessDifferenceToSwitch = .1f;

    public void PresetNetwork() => networkDisplayer.PresetNetwork(CarNetworkCreator.CreateNewCarNetwork());

    public void SetNeuralNetworkDisplayer(NeuralNetwork car) => networkDisplayer.SetTargetNetwork(car, Car.inputsNames, Car.outputNames);

    public void TryUpdateNeuralNetworkDisplayer(Car bestCar)
    {
        if (bestCar == rfCar) return;

        if (!rfCar || Mathf.Abs(bestCar.Fitness - rfCar.Fitness) > minFitnessDifferenceToSwitch)
        {
            SetNeuralNetworkDisplayer(bestCar.network);
            rfCar = bestCar;
        }
    }

    // ---- Camera type
    public void UpdateCanvasLayout(CameraType type)
    {
        bestCarStats.SetActive(type != CameraType._static);
        followCamBg.SetActive(type != CameraType._static);

        if (type == CameraType.follow)
        {
            textsParent.transform.localPosition = new Vector2(-336, /*textsParent.transform.localPosition.y*/ 508);

            TextArrayDisplayer[] disps = textsParent.GetComponentsInChildren<TextArrayDisplayer>();

            for (int i = 0; i < disps.Length; i++)
            {
                Color c = disps[i].background.color;

                disps[i].background.color = new Color(c.r, c.g, c.b, .59f);
            }
        }
    }

    // -------- OBSOLETE --------
    [Header("Refferences - clone ids - OBSOLETE")]
    [SerializeField] private GameObject bestCloneIdentifier;
    [SerializeField] private GameObject secBestCloneIdentifier;

    public void UpdateCopiesTexts(bool b, Car bc, Car sbc)
    {
        HandlerUtils.UpdateCopyText(bestCloneIdentifier, b, bc);
        HandlerUtils.UpdateCopyText(secBestCloneIdentifier, b, sbc);
    }
}
