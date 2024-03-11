using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicModeHandler : MonoBehaviour
{
    public bool _enabled = false;

    [SerializeField] private Camera cinematicCamera;

    [SerializeField] private AnimationClip[] clips;
    [SerializeField] private string[] customCommands;
    private int clipId;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!_enabled) return;

        if (Input.GetKeyDown(KeyCode.Space)) ExecuteNext();
    }

    private void ExecuteNext()
    {
        Animator a = cinematicCamera.GetComponent<Animator>();

        a.enabled = true;

        //clips[clipId].wrapMode = WrapMode.Once;
        a.Play(clips[clipId].name);
        ExecuteCustomCommand(customCommands[clipId]);

        clipId++;
    }

    private void ExecuteCustomCommand(string command)
    {
        if (command == "EnableCarCinematicMode1")
        {
            FindObjectOfType<Car>().cinematicUpdate = true;
        }
    }
}