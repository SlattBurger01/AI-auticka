using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeField : MonoBehaviour
{
    [SerializeField] private bool changable = true;

    [SerializeField] private Transform[] fields;

    public void ChangeCones()
    {
        if (!changable) return;

        if (fields.Length == 0) Debug.LogError("No fields assigned!");

        int rNum = Random.Range(0, fields.Length);

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].gameObject.SetActive(i == rNum);
        }
    }
}
