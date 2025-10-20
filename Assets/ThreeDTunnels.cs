using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ktane3DTunnels;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class ThreeDTunnels : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable ButtonUp;
    public KMSelectable ButtonDown;
    public KMSelectable ButtonLeft;
    public KMSelectable ButtonRight;
    public KMSelectable ButtonTarget;
    public GameObject Display;
    public TextMesh Symbol;
    public TextMesh TargetSymbol;
    public KMRuleSeedable RuleSeedable;

    private static readonly string _ruleSeed1Symbols = "ghidefabcpqrmnojklyz.vwxstu";
    private static readonly string[] _ruleSeed1SymbolNames = {
        "Chip", "Ring", "Drop", "Cube", "Cloud", "Command", "Heart monitor", "Anchor", "Medal",
        "Lock", "Crossing", "Moon", "Globe", "Heart", "Link", "Eye", "Feather", "Flag",
        "Chart", "Umbrella", "Wind", "Shield", "Star", "Sun", "Quarter", "Radio", "Gear"
    };

    private string _symbols;
    private string[] _symbolNames;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private int _location;
    private Direction3D _direction;
    private List<int> _identifiedNodes;
    private int[] _targetNodes;
    private const int _numIdentifiedNodes = 6;
    private const int _numTargetNodes = 3;
    private int _currentTarget = 0;
    private bool _solved = false;
    private readonly List<Action> _actionLog = new List<Action>();
    private enum Button { Up, Right, Down, Left, Target };
    private enum StrikeReason { None, FlyIntoWall, NotOnTarget };

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var rnd = RuleSeedable.GetRNG();
        if (rnd.Seed == 1)
        {
            _symbols = _ruleSeed1Symbols;
            _symbolNames = _ruleSeed1SymbolNames;
        }
        else
        {
            var shuffled = rnd.ShuffleFisherYates(Enumerable.Range(0, 27).ToArray());
            _symbols = Enumerable.Range(0, 27).Select(i => _ruleSeed1Symbols[shuffled[i]]).Join("");
            _symbolNames = Enumerable.Range(0, 27).Select(i => _ruleSeed1SymbolNames[shuffled[i]]).ToArray();
        }

        Debug.Log($"[3D Tunnels #{_moduleId}] Using rule seed: {rnd.Seed}");

        ButtonUp.OnInteract += delegate () { PressButton(Button.Up, dir => dir.TurnUpDown(up: true)); return false; };
        ButtonDown.OnInteract += delegate () { PressButton(Button.Down, dir => dir.TurnUpDown(up: false)); return false; };
        ButtonLeft.OnInteract += delegate () { PressButton(Button.Left, dir => dir.TurnLeftRight(right: false)); return false; };
        ButtonRight.OnInteract += delegate () { PressButton(Button.Right, dir => dir.TurnLeftRight(right: true)); return false; };
        ButtonTarget.OnInteract += delegate () { PressTargetButton(); return false; };

        var found = false;
        var nodeOrder = Enumerable.Range(0, 27).ToArray();
        while (!found)
        {
            // Random identified nodes (because it's a HashSet, it will only add unique values)
            nodeOrder.Shuffle();

            // Check if there is at least a pair that's in the same square,
            // middle node (13) doesn't give enough information.
            for (var i1 = 0; i1 < _numIdentifiedNodes && !found; i1++)
            {
                if (nodeOrder[i1] == 13) continue;
                for (var i2 = i1 + 1; i2 < _numIdentifiedNodes && !found; i2++)
                {
                    if (nodeOrder[i2] == 13) continue;
                    int x1, y1, z1, x2, y2, z2;
                    DirectionUtils.GetXYZ(nodeOrder[i1], out x1, out y1, out z1);
                    DirectionUtils.GetXYZ(nodeOrder[i2], out x2, out y2, out z2);
                    var distances = new[] { Math.Abs(x1 - x2), Math.Abs(y1 - y2), Math.Abs(z1 - z2) };
                    found = (distances.Count(d => d == 0) == 2 && distances.Count(d => d == 1) == 1) || (distances.Count(d => d == 0) == 1 && distances.Count(d => d == 1) == 2);
                }
            }
        }
        _identifiedNodes = nodeOrder.Take(_numIdentifiedNodes).ToList();
        Debug.Log($"[3D Tunnels #{_moduleId}] Identified nodes: {_identifiedNodes.Select(x => _symbolNames[x]).Join(", ")}");

        _targetNodes = nodeOrder.Skip(_numIdentifiedNodes).Take(_numTargetNodes).ToArray();
        var centerIx = Array.IndexOf(_targetNodes, 13);
        if (centerIx != -1)
            _targetNodes[centerIx] = nodeOrder[_numIdentifiedNodes + _numTargetNodes];
        Debug.Log($"[3D Tunnels #{_moduleId}] Target nodes: {_targetNodes.Select(x => _symbolNames[x]).Join(", ")}");

        // Random starting location
        _location = nodeOrder[_numIdentifiedNodes + _numTargetNodes + 1];

        // Random starting direction
        var directions = Enum.GetValues(typeof(Direction3D));
        _direction = (Direction3D) directions.GetValue(Rnd.Range(0, directions.Length));

        Debug.LogFormat("[3D Tunnels #{0}] Starting at {1}. {2}", _moduleId, _symbolNames[_location], GetOrientationDescription(_location, _direction));

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
        Symbol.gameObject.SetActive(_identifiedNodes.Contains(_location));
        Symbol.text = _symbols[_location].ToString();
        TargetSymbol.text = _symbols[_targetNodes[_currentTarget]].ToString();
    }

    private void PressButton(Button button, Func<Direction3D, Direction3D> turn)
    {
        if (_solved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        var action = new Action() { StartLocation = _location, StartOrientation = _direction, Button = button };

        // Log move
        if (_identifiedNodes.Contains(_location))
        {
            _actionLog.Clear();
            action.LocationIsIdentified = true;
        }

        // Turn
        _direction = turn(_direction);
        action.EndOrientation = _direction;

        // Check if you can move forward
        if (!_direction.IsWallForward(_location))
        {
            // If so, move forward
            _location = _direction.MoveForward(_location);
            action.EndLocation = _location;
        }
        else
        {
            // Else, give a strike
            Module.HandleStrike();
            action.StrikeReason = StrikeReason.FlyIntoWall;
        }

        _actionLog.Add(action);
        if (action.StrikeReason != StrikeReason.None) LogActions();
        UpdateDisplay();
    }

    private void PressTargetButton()
    {
        if (_solved) return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        var action = new Action() { StartLocation = _location, StartOrientation = _direction, Button = Button.Target };

        // Check if current location matches current target
        if (_location == _targetNodes[_currentTarget])
        {
            // If so, go to next stage, or module solved if this was the last stage
            Debug.LogFormat("[3D Tunnels #{0}] {1} identified correctly.", _moduleId, _symbolNames[_location]);
            if (_currentTarget == _numTargetNodes - 1)
            {
                Debug.LogFormat("[3D Tunnels #{0}] Module solved!", _moduleId);
                _solved = true;
                TargetSymbol.gameObject.SetActive(false);
                Module.HandlePass();
            }
            else
            {
                _identifiedNodes.Add(_location);
                _currentTarget++;
            }
            UpdateDisplay();
        }
        else
        {
            // If not, give strike
            Module.HandleStrike();
            action.StrikeReason = StrikeReason.NotOnTarget;
        }

        _actionLog.Add(action);
        if (action.StrikeReason != StrikeReason.None) LogActions();
    }

    private void LogActions()
    {
        Debug.LogFormat("[3D Tunnels #{0}] You got a strike. Action log:", _moduleId);

        for (var i = 0; i < _actionLog.Count; i++)
        {
            var action = _actionLog[i];
            var msg = "";

            if (i == 0)
            {
                msg += "Starting at " + _symbolNames[action.StartLocation] + ". ";
                msg += GetOrientationDescription(action.StartLocation, action.StartOrientation);
                if (action.LocationIsIdentified)
                    msg += " This is the most recent location where the symbol is shown on the module. ";
            }

            msg += "Pressing " + action.Button.ToString() + ". ";
            if (action.StrikeReason == StrikeReason.NotOnTarget)
                msg += "You are not at " + _symbolNames[_targetNodes[_currentTarget]] + ", you are at " + _symbolNames[_location] + "!";
            else if (action.Button != Button.Target)
            {
                msg += "New orientation: " + GetOrientationDescription(action.StartLocation, action.EndOrientation);
                if (action.StrikeReason == StrikeReason.FlyIntoWall)
                    msg += "Moving forward. You fly into a wall! ";
                else
                    msg += "Moving forward to " + _symbolNames[action.EndLocation] + ". ";
            }
            Debug.LogFormat("[3D Tunnels #{0}] {1}", _moduleId, msg);
        }
        _actionLog.Clear();
    }

    private string GetOrientationDescription(int location, Direction3D direction)
    {
        var msg = "";
        if (direction.IsWallForward(location))
            msg += "Behind you is " + _symbolNames[direction.TurnLeftRight(true).TurnLeftRight(true).MoveForward(location)];
        else
            msg += "In front of you is " + _symbolNames[direction.MoveForward(location)];
        if (direction.TurnUpDown(up: true).IsWallForward(location))
            msg += ", below you is " + _symbolNames[direction.TurnUpDown(up: false).MoveForward(location)] + ". ";
        else
            msg += ", above you is " + _symbolNames[direction.TurnUpDown(up: true).MoveForward(location)] + ". ";

        return msg;
    }

    private IEnumerator RotateSymbol()
    {
        const float durationPerPing = 10f;

        var localEulerAngles = Symbol.transform.localEulerAngles;
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

        var localScale = Symbol.transform.localScale;
        var scaleDirection = 1f;

        while (true)
        {
            for (var time = 0f; time < durationPerPing; time += Time.deltaTime)
            {
                yield return null;

                localScale.x = Mathf.SmoothStep(-scaleDirection, scaleDirection, time / durationPerPing);
                Symbol.transform.localScale = localScale;
            }

            scaleDirection *= -1f;
        }
    }

#pragma warning disable CS0414
    private readonly string TwitchHelpMessage = @"Use '!{0} move u d l r' to move around the grid. Use '!{0} submit' to press the goal button.";

    IEnumerator ProcessTwitchCommand(string command)
    {
        var parts = command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length > 1 && parts[0] == "move" && parts.Skip(1).All(part => part.Length == 1 && "udlr".Contains(part)))
        {
            yield return null;

            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i] == "u")
                {
                    PressButton(Button.Up, dir => dir.TurnUpDown(up: true));
                }
                else if (parts[i] == "d")
                {
                    PressButton(Button.Down, dir => dir.TurnUpDown(up: false));
                }
                else if (parts[i] == "l")
                {
                    PressButton(Button.Left, dir => dir.TurnLeftRight(right: false));
                }
                else if (parts[i] == "r")
                {
                    PressButton(Button.Right, dir => dir.TurnLeftRight(right: true));
                }

                yield return new WaitForSeconds(.2f);
            }
        }
        else if (parts.Length == 1 && parts[0] == "submit")
        {
            yield return null;
            PressTargetButton();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_solved)
        {
            var cur = ((int) _direction << 5) | _location;
            var queue = new Queue<int>();
            var parent = new Dictionary<int, int>();
            queue.Enqueue(cur);

            int last;
            while (queue.Count > 0)
            {
                var data = queue.Dequeue();
                if ((data & 0x1f) == _targetNodes[_currentTarget])
                {
                    last = data;
                    goto found;
                }

                int newData, newParent;
                for (var btn = 0; btn < 4; btn++)
                    if (TryTpMove(data, btn, out newData, out newParent) && !parent.ContainsKey(newData))
                    {
                        queue.Enqueue(newData);
                        parent[newData] = newParent;
                    }
            }
            throw new InvalidOperationException("No solution found");

            found:
            var buttons = new List<KMSelectable>();
            var btns = new[] { ButtonUp, ButtonRight, ButtonDown, ButtonLeft };
            while (last != cur)
            {
                var p = parent[last];
                buttons.Add(btns[p & 3]);
                last = p >> 2;
            }
            for (var i = buttons.Count - 1; i >= 0; i--)
            {
                buttons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            yield return new WaitForSeconds(.5f);
            ButtonTarget.OnInteract();
            yield return new WaitForSeconds(.5f);
        }
    }

    bool TryTpMove(int data, int btn, out int newData, out int newParent)
    {
        var pos = data & 0x1f;
        var dir = (Direction3D) (data >> 5);
        var newDir =
            btn == 0 ? dir.TurnUpDown(up: true) :
            btn == 1 ? dir.TurnLeftRight(right: true) :
            btn == 2 ? dir.TurnUpDown(up: false) :
            dir.TurnLeftRight(right: false);
        var valid = !newDir.IsWallForward(pos);
        var newPos = valid ? newDir.MoveForward(pos) : 0;
        newData = valid ? ((int) newDir << 5) | newPos : 0;
        newParent = valid ? (data << 2) | btn : 0;
        return valid;
    }

    class Action
    {
        public int StartLocation { get; set; }
        public Direction3D StartOrientation { get; set; }
        public bool LocationIsIdentified { get; set; }
        public Button Button { get; set; }
        public int EndLocation { get; set; }
        public Direction3D EndOrientation { get; set; }
        public StrikeReason StrikeReason { get; set; }
    }
}
