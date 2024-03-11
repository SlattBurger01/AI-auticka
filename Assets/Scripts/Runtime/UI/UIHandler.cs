using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private GameObject optionsParent;

    [SerializeField] private Button playButton;

    [SerializeField] private Toggle resetSaveToggle;
    [SerializeField] private Toggle resetGraphsToggle;
    [SerializeField] private Toggle fastLearnToggle;

    [SerializeField] private TMP_InputField input;

    private void Awake()
    {
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(delegate { OnPlayButtonPressed(); });
    }

    private bool gameStarted;
    private bool running;

    private bool opened = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetState(!opened);
    }

    private void SetState(bool state)
    {
        opened = state;
        optionsParent.SetActive(opened);
    }

    private void OnPlayButtonPressed()
    {
        running = !running;

        if (gameStarted) Handler.handler.running = running;

        if (running)
        {
            if (!gameStarted)
            {
                if (!int.TryParse(input.text, out int i)) i = -1;

                Handler.handler.SetPregameSettings(resetSaveToggle.isOn, resetGraphsToggle.isOn, i, fastLearnToggle.isOn);

                StartCoroutine(StartGame(1));
                gameStarted = true;
            }

            SetState(false);
        }
    }

    // Spawn cars and stuff
    private void StartGameF()
    {
        Handler.handler.SetupSim();
        Handler.handler.running = true;
    }

    private IEnumerator StartGame(float pause)
    {
        yield return new WaitForSeconds(pause);
        StartGameF();
    }
}
