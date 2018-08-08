using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KmHelper;
using Rnd = UnityEngine.Random;
using Assets;

public class TunnelMaze : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ButtonUp;
    public KMSelectable ButtonDown;
    public KMSelectable ButtonLeft;
    public KMSelectable ButtonRight;
    public GameObject Display;
    public TextMesh Symbol;

    private static readonly string _symbols = "ghidefabcpqrmnojklyz.vwxstu";

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private int _location;
    private Direction _direction;
    private HashSet<int> _showSymbol = new HashSet<int>();
    private List<int> _toVisit;
    private int _numShowSymbol = 4;
    private int _numToVisit = 3;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        ButtonUp.OnInteract += delegate () { PressButton(dir => dir.TurnUpDown(up: true)); return false; };
        ButtonDown.OnInteract += delegate () { PressButton(dir => dir.TurnUpDown(up: false)); return false; };
        ButtonLeft.OnInteract += delegate () { PressButton(dir => dir.TurnLeftRight(right: false)); return false; };
        ButtonRight.OnInteract += delegate () { PressButton(dir => dir.TurnLeftRight(right: true)); return false; };

        _location = Rnd.Range(0, 27);

        var directions = Enum.GetValues(typeof(Direction));
        _direction = (Direction)directions.GetValue(Rnd.Range(0, directions.Length));

        while (_showSymbol.Count < _numShowSymbol)
            _showSymbol.Add(Rnd.Range(0, 27));

        // Initialize with shown symbols so we can exclude them later
        var toVisit = new HashSet<int>(_showSymbol);

        while (toVisit.Count < (_numShowSymbol + _numToVisit))
            toVisit.Add(Rnd.Range(0, 27));
        _toVisit = toVisit.Except(_showSymbol).ToList();

        UpdateDisplay();
        StartCoroutine(RotateSymbol());
        StartCoroutine(ScaleSymbol());
    }

    private void UpdateDisplay()
    {
        Display.transform.Find("forward-tunnel").gameObject.SetActive(!_direction.IsWallForward(_location));
        Display.transform.Find("forward-wall").gameObject.SetActive(_direction.IsWallForward(_location));
        Display.transform.Find("left-tunnel").gameObject.SetActive(!_direction.TurnLeftRight(right: false).IsWallForward(_location));
        Display.transform.Find("left-wall").gameObject.SetActive(_direction.TurnLeftRight(right: false).IsWallForward(_location));
        Display.transform.Find("right-tunnel").gameObject.SetActive(!_direction.TurnLeftRight(right: true).IsWallForward(_location));
        Display.transform.Find("right-wall").gameObject.SetActive(_direction.TurnLeftRight(right: true).IsWallForward(_location));
        Display.transform.Find("up-tunnel").gameObject.SetActive(!_direction.TurnUpDown(up: true).IsWallForward(_location));
        Display.transform.Find("up-wall").gameObject.SetActive(_direction.TurnUpDown(up: true).IsWallForward(_location));
        Display.transform.Find("down-tunnel").gameObject.SetActive(!_direction.TurnUpDown(up: false).IsWallForward(_location));
        Display.transform.Find("down-wall").gameObject.SetActive(_direction.TurnUpDown(up: false).IsWallForward(_location));
        Symbol.gameObject.SetActive(_showSymbol.Contains(_location));
        Symbol.text = _symbols[_location].ToString();
    }

    private void PressButton(Func<Direction, Direction> turn)
    {
        // Turn
        _direction = turn(_direction);

        // If you can move forward
        if (!_direction.IsWallForward(_location))
        {
            // Do so
            _location = _direction.MoveForward(_location);
        }
        else
        {
            // Else, give strike
            Module.HandleStrike();
        }

        UpdateDisplay();
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
