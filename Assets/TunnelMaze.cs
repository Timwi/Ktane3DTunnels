using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;

public class TunnelMaze : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMSelectable Module;
    public KMSelectable[] Buttons;
    public GameObject Display;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private int _location;
    private int _direction;
    private int _rotation;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < Buttons.Length; i++)
        {
            var j = i;
            Buttons[i].OnInteract += delegate () { PressButton(j); return false; };
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Display.transform.Find("down-tunnel").gameObject.SetActive(false);
    }

    private void PressButton(int i)
    {
        throw new NotImplementedException();
    }

    void Update()
    {

    }
}
