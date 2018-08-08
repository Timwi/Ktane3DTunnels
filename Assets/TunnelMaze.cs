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
    public GameObject Symbol;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private int _location;
    private Facing _facing;
    private Bank _bank;
    private enum Facing { North, East, South, West, Top, Bottom };
    private enum Bank { _0, _90, _180, _270 };
    private enum Direction { Forward, Backward, Left, Right, Up, Down };
    private Dictionary<Direction, bool> _tunnels;
    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (int i = 0; i < Buttons.Length; i++)
        {
            var j = i;
            Buttons[i].OnInteract += delegate () { PressButton(j); return false; };
        }

        _location = 0;
        _facing = Facing.East;
        _bank = Bank._0;

        UpdateDisplay();
        StartCoroutine(RotateSymbol());
        StartCoroutine(ScaleSymbol());
    }

    private void UpdateDisplay()
    {
        // Determine where the tunnels are for facing north, no bank
        var x = _location % 3;
        var y = Math.Floor(_location % 9 / 3f);
        var z = Math.Floor(_location / 9f);
        _tunnels = new Dictionary<Direction, bool> {
            { Direction.Forward, z < 2 },
            { Direction.Backward, z > 0 },
            { Direction.Left, x > 0 },
            { Direction.Right, x < 2 },
            { Direction.Up, y < 2 },
            { Direction.Down, y > 0 }
        };

        // Consider facing
        if (_facing != Facing.North)
        {
            Dictionary<Direction, bool> orig = new Dictionary<Direction, bool>(_tunnels);
            if (_facing == Facing.East)
            {
                _tunnels[Direction.Forward] = orig[Direction.Right];
                _tunnels[Direction.Right] = orig[Direction.Backward];
                _tunnels[Direction.Backward] = orig[Direction.Left];
                _tunnels[Direction.Left] = orig[Direction.Forward];
            }
            else if (_facing == Facing.South)
            {
                _tunnels[Direction.Forward] = orig[Direction.Backward];
                _tunnels[Direction.Right] = orig[Direction.Left];
                _tunnels[Direction.Backward] = orig[Direction.Forward];
                _tunnels[Direction.Left] = orig[Direction.Right];
            }
            else if (_facing == Facing.West)
            {
                _tunnels[Direction.Forward] = orig[Direction.Left];
                _tunnels[Direction.Right] = orig[Direction.Forward];
                _tunnels[Direction.Backward] = orig[Direction.Right];
                _tunnels[Direction.Left] = orig[Direction.Backward];
            }
            else if (_facing == Facing.Top)
            {
                _tunnels[Direction.Forward] = orig[Direction.Left];
                _tunnels[Direction.Right] = orig[Direction.Forward];
                _tunnels[Direction.Backward] = orig[Direction.Right];
                _tunnels[Direction.Left] = orig[Direction.Backward];
            }
        }



        Display.transform.Find("forward-tunnel").gameObject.SetActive(_tunnels[Direction.Forward]);
        Display.transform.Find("forward-wall").gameObject.SetActive(!_tunnels[Direction.Forward]);
        Display.transform.Find("left-tunnel").gameObject.SetActive(_tunnels[Direction.Left]);
        Display.transform.Find("left-wall").gameObject.SetActive(!_tunnels[Direction.Left]);
        Display.transform.Find("right-tunnel").gameObject.SetActive(_tunnels[Direction.Right]);
        Display.transform.Find("right-wall").gameObject.SetActive(!_tunnels[Direction.Right]);
        Display.transform.Find("up-tunnel").gameObject.SetActive(_tunnels[Direction.Up]);
        Display.transform.Find("up-wall").gameObject.SetActive(!_tunnels[Direction.Up]);
        Display.transform.Find("down-tunnel").gameObject.SetActive(_tunnels[Direction.Down]);
        Display.transform.Find("down-wall").gameObject.SetActive(!_tunnels[Direction.Down]);
    }

    private void PressButton(int i)
    {
        throw new NotImplementedException();
    }

    void Update()
    {

    }

    private IEnumerator RotateSymbol()
    {
        const float durationPerPing = 10f;

        Vector3 localEulerAngles = Symbol.transform.localEulerAngles;
        var time = 0f;

        while (true)
        {
            yield return null;

            time += Time.deltaTime;
            localEulerAngles.z = time / durationPerPing * -360;
            Symbol.transform.localEulerAngles = localEulerAngles;
        }
    }

    private IEnumerator ScaleSymbol()
    {
        const float durationPerPing = 2f;

        Vector3 localScale = Symbol.transform.localScale;
        float scaleDirection = 1f;

        while (true)
        {
            for (float time = 0f; time < durationPerPing; time += Time.deltaTime)
            {
                yield return null;

                localScale.x = Mathf.SmoothStep(-scaleDirection, scaleDirection, time / durationPerPing);
                Symbol.transform.localScale = localScale;
            }

            scaleDirection *= -1f;
        }
    }
}
