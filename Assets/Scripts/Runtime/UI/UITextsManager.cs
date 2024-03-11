using System;
using TMPro;
using UnityEngine;
using Tx = TextHelper;

public class UITextsManager : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text genText;
    [SerializeField] private TMP_Text timeText;

    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text carsLeftText;

    [SerializeField] private TMP_Text samplesText;
    [SerializeField] private TMP_Text finishedCars;

    [Header("BestCarStats")]
    [SerializeField] private TMP_Text engineText;
    [SerializeField] private TMP_Text steerText;
    [SerializeField] private TMP_Text avgSpeedText;
    [SerializeField] private TMP_Text fitnessText;

    public void UpdateGenText(string s, string s1 = "") => UpdateGenText($"Generation {s} {s1}");

    public void UpdateTimeText(string t) => UpdateTimeText_($"Training time: {t}");
    public void UpdateFpsText(string s) => UpdateFpsText_($"Fps: {s}");

    public void UpdateAvgSpeedText(string s) => UpdateAvgSpeedText_($"Avg speed: {s}");

    public void UpdateEngineText(string s) => UpdateEngineText_($"Speed: {s}");
    public void UpdateTurnText(string s) => UpdateTurnText_($"Steer: {s}");
    public void UpdateScoreText(string s) => UpdateScoreText_($"Fitness: {s}");

    // ----
    private void UpdateGenText(string s) => Tx.TrySetText(genText, s);
    private void UpdateTimeText_(string s) => Tx.TrySetText(timeText, s);
    private void UpdateAvgSpeedText_(string s) => Tx.TrySetText(avgSpeedText, s);

    private void UpdateEngineText_(string s) => Tx.TrySetText(engineText, s);
    private void UpdateTurnText_(string s) => Tx.TrySetText(steerText, s);
    private void UpdateScoreText_(string s) => Tx.TrySetText(fitnessText, s);

    private void UpdateFpsText_(string s) => Tx.TrySetText(fpsText, s);
    public void UpdateElapsedTimeText(string s) => Tx.TrySetText(elapsedTimeText, s);
    public void UpdateCarsLeftext(string s) => Tx.TrySetText(carsLeftText, s);

    public void UpdateSamplesText(string s) => Tx.TrySetText(samplesText, s);
    public void UpdateFinishedCars(string s) => Tx.TrySetText(finishedCars, s);
}

public static class TextHelper
{
    public static void TrySetTextActive(TMP_Text text, bool b) { if (text) text.gameObject.SetActive(b); }

    public static void TrySetText(TMP_Text text, string s) { if (text) text.text = s; }

    public static string TimeSpanToString(TimeSpan time)
    {
        string h = GetTimeSpanValue(time.Hours);
        string m = GetTimeSpanValue(time.Minutes);
        string s = GetTimeSpanValue(time.Seconds);

        return $"{h}:{m}:{s}";
    }

    public static string GetTimeSpanValue(int value) { return value >= 10 ? $"{value}" : $"0{value}"; }
}
