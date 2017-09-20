using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using KMBombInfoExtensions;

public class NonogramModule : MonoBehaviour {

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    public KMAudio sound;
    public KMBombModule module;
    public KMBombInfo bombInfo;
    public KMSelectable[] gridButtons;
    public KMSelectable submitButton, dotButton, toggleButton;

    public MeshRenderer[] gridColors;
    public MeshRenderer[] gridButtonsFill;
    public MeshRenderer[] gridButtonsDot;
    public MeshRenderer dotButtonStatus;

    public Material blackGridButton;
    public Material whiteGridButton;
    public Material dotStatusOff;
    public Material dotStatusOn;
    public Material offGridColor;
    public Material redGridColor;
    public Material blueGridColor;
    public Material greenGridColor;
    public Material yellowGridColor;
    public Material orangeGridColor;
    public Material purpleGridColor;

    int solutionAmountSquares;
    bool evenSerialNumber;
    bool canInteract = false;
    bool isDotActive = false;
    bool isSecondaryColor = false;
    List<string> hints = new List<string>();
    List<string> colors = new List<string>();
    List<int> solution = new List<int>();
    List<int> currentGrid = new List<int>(Enumerable.Repeat(0, 25).ToArray());

    int[][] colAndRows = new int[][] {
        new int[] { 0, 5, 10, 15, 20 },
        new int[] { 1, 6, 11, 16, 21 },
        new int[] { 2, 7, 12, 17, 22 },
        new int[] { 3, 8, 13, 18, 23 },
        new int[] { 4, 9, 14, 19, 24 },
        new int[] { 0, 1, 2, 3, 4 },
        new int[] { 5, 6, 7, 8, 9 },
        new int[] { 10, 11, 12, 13, 14 },
        new int[] { 15, 16, 17, 18, 19 },
        new int[] { 20, 21, 22, 23, 24 }
    };

    string[] gridIds = new string[] {
        "A1", "B1", "C1", "D1", "E1",
        "A2", "B2", "C2", "D2", "E2",
        "A3", "B3", "C3", "D3", "E3",
        "A4", "B4", "C4", "D4", "E4",
        "A5", "B5", "C5", "D5", "E5"
    };

    Material getGridColor(string color, bool second) {
        char c = second ?
            color.Split(' ')[0].ToCharArray()[0] :
            color.Split(' ')[1].ToCharArray()[0];

        return c == 'N' ? offGridColor :
            c == 'R' ? redGridColor :
            c == 'B' ? blueGridColor :
            c == 'G' ? greenGridColor :
            c == 'Y' ? yellowGridColor :
            c == 'O' ? orangeGridColor :
            c == 'P' ? purpleGridColor : null;
    }

    string randReverse(string original) {
        return UnityEngine.Random.Range(0, 2) == 0 ?
            new string(original.ToCharArray().Reverse().ToArray()) : original;
    }

    string fullColor(string original) {
        return original
            .Replace("R", "Red")
            .Replace("B", "Blue")
            .Replace("G", "Green")
            .Replace("Y", "Yellow")
            .Replace("O", "Orange")
            .Replace("P", "Purple");
    }

    string getHint(bool[] active) {
        string sequence = "";
        
        for (int i = 0; i < active.Length; i++)
            sequence += active[i] ? "o" : " ";

        string[] sqArray = sequence.Split(' ');
        string result = "";

        for (int j = 0; j < sqArray.Length; j++)
            result += sqArray[j].Length > 0 ? " " + sqArray[j].Length : "";

        return result.Length > 0 ? result.Substring(1) : result;
    }

    void Start() {
        _moduleId = _moduleIdCounter++;
        module.OnActivate += Activate;
    }

    void Awake() {
        List<int> selects = Enumerable.Range(0, 25).ToList();
        solutionAmountSquares = UnityEngine.Random.Range(16, 21);

        for (int i = 0; i < solutionAmountSquares; i++) {
            int index = UnityEngine.Random.Range(0, selects.Count - 1);
            solution.Add(selects[index]);
            selects.RemoveAt(index);
        }

        solution.Sort();

        for (int i = 0; i < colAndRows.Length; i++) {
            bool[] active = new bool[5];

            for (int j = 0; j < 5; j++)
                active[j] = solution.Contains(colAndRows[i][j]);

            hints.Add(getHint(active));
        }

        for (int i = 0; i < gridButtons.Length; i++) {
            int j = i;
            gridButtons[i].OnInteract += delegate () {
                onGridClick(j);
                return false;
            };
        }

        toggleButton.OnInteract += onLightToggle;
        dotButton.OnInteract += onDotToggle;
        submitButton.OnInteract += onSubmit;

        bombInfo.OnBombExploded += onGameEnd;
        bombInfo.OnBombSolved += onGameEnd;
    }

    void Activate() {
        evenSerialNumber = int.Parse(bombInfo.GetSerialNumber().Substring(5)) % 2 == 0;

        for (int i = 0; i < hints.Count; i++) {
            switch (hints[i]) {
                case "1": colors.Add(evenSerialNumber ? randReverse("B O") : randReverse("Y O")); break;
                case "2": colors.Add(evenSerialNumber ? randReverse("R B") : randReverse("G P")); break;
                case "3": colors.Add(evenSerialNumber ? randReverse("Y O") : randReverse("B O")); break;
                case "4": colors.Add(evenSerialNumber ? randReverse("R G") : randReverse("B Y")); break;
                case "5": colors.Add(evenSerialNumber ? randReverse("G Y") : randReverse("R G")); break;
                case "1 1": colors.Add(evenSerialNumber ? randReverse("O P") : randReverse("R O")); break;
                case "1 2": colors.Add(evenSerialNumber ? randReverse("G O") : randReverse("B G")); break;
                case "1 3": colors.Add(evenSerialNumber ? randReverse("G P") : randReverse("B P")); break;
                case "2 1": colors.Add(evenSerialNumber ? randReverse("Y P") : randReverse("Y P")); break;
                case "2 2": colors.Add(evenSerialNumber ? randReverse("B P") : randReverse("G Y")); break;
                case "3 1": colors.Add(evenSerialNumber ? randReverse("R O") : randReverse("R B")); break;
                case "1 1 1": colors.Add(evenSerialNumber ? randReverse("R P") : randReverse("R Y")); break;
                default:
                    int random = UnityEngine.Random.Range(1, 4);
                    colors.Add(
                        random == 1 ? evenSerialNumber ? randReverse("R Y") : randReverse("R P") :
                        random == 2 ? evenSerialNumber ? randReverse("B G") : randReverse("G O") :
                        random == 3 ? evenSerialNumber ? randReverse("B G") : randReverse("O P") : null
                    );
                    break;
            }

            gridColors[i].material = getGridColor(colors[i], false);
            canInteract = true;
        }
    }

    void onGridClick(int position) {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gridButtons[position].transform);

        if (!canInteract)
            return;

        if (currentGrid[position] == 0) {
            currentGrid[position] = isDotActive ? 1 : 2;
            gridButtonsDot[position].material = blackGridButton;
            gridButtonsFill[position].material = isDotActive ? whiteGridButton : blackGridButton;
        } else if (currentGrid[position] == 1 && isDotActive || currentGrid[position] == 2 && !isDotActive) {
            currentGrid[position] = 0;
            gridButtonsDot[position].material = whiteGridButton;
            gridButtonsFill[position].material = whiteGridButton;
        }
    }

    bool onLightToggle() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, toggleButton.transform);

        if (!canInteract)
            return false;

        for (int i = 0; i < colors.Count; i++)
            gridColors[i].material = getGridColor(colors[i], !isSecondaryColor);

        isSecondaryColor = !isSecondaryColor;

        return false;
    }

    bool onDotToggle() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, dotButton.transform);

        if (!canInteract)
            return false;

        dotButtonStatus.material = isDotActive ? dotStatusOff : dotStatusOn;
        isDotActive = !isDotActive;

        return false;
    }

    bool onSubmit() {
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitButton.transform);
        toggleButton.AddInteractionPunch();

        if (!canInteract)
            return false;

        bool strike = false;

        for (int i = 0; i < colAndRows.Length; i++) {
            string currentHint = getHint(new bool[] {
                currentGrid[colAndRows[i][0]] > 1,
                currentGrid[colAndRows[i][1]] > 1,
                currentGrid[colAndRows[i][2]] > 1,
                currentGrid[colAndRows[i][3]] > 1,
                currentGrid[colAndRows[i][4]] > 1
            });

            if (hints[i] != currentHint) {
                strike = true;
                break;
            }
        }

        List<string> input = new List<string>();
        for (int i = 0; i < currentGrid.Count; i++)
            if (currentGrid[i] > 1)
                input.Add(gridIds[i]);

        if (input.Count > 0)
            Debug.LogFormat("[Nonogram #{0}] Submitted {1} answer: {2}", _moduleId, strike ? "incorrect" : "correct", string.Join(", ", input.ToArray()));
        else
            Debug.LogFormat("[Nonogram #{0}] Submitted empty and wrong answer.", _moduleId);

        if (strike) {
            module.HandleStrike();
            return false;
        }

        canInteract = false;

        dotButtonStatus.material = dotStatusOff;

        for (int i = 0; i < gridColors.Length; i++)
            gridColors[i].material = offGridColor;

        module.HandlePass();

        return false;
    }

    void onGameEnd() {
        string oddOrEven = evenSerialNumber ? "even" : "odd";

        List<string> input = new List<string>();
        for (int i = 0; i < solution.Count; i++)
            input.Add(gridIds[solution[i]]);

        Debug.LogFormat("[Nonogram #{0}]", _moduleId);
        Debug.LogFormat("[Nonogram #{0}] The serial number was {1}.", _moduleId, oddOrEven);
        Debug.LogFormat("[Nonogram #{0}] <Column Hints> A: {2} ({1})", _moduleId, fullColor(colors[0]), hints[0]);
        Debug.LogFormat("[Nonogram #{0}] <Column Hints> B: {2} ({1})", _moduleId, fullColor(colors[1]), hints[1]);
        Debug.LogFormat("[Nonogram #{0}] <Column Hints> C: {2} ({1})", _moduleId, fullColor(colors[2]), hints[2]);
        Debug.LogFormat("[Nonogram #{0}] <Column Hints> D: {2} ({1})", _moduleId, fullColor(colors[3]), hints[3]);
        Debug.LogFormat("[Nonogram #{0}] <Column Hints> E: {2} ({1})", _moduleId, fullColor(colors[4]), hints[4]);
        Debug.LogFormat("[Nonogram #{0}] <Row Hints> 1: {2} ({1})", _moduleId, fullColor(colors[5]), hints[5]);
        Debug.LogFormat("[Nonogram #{0}] <Row Hints> 2: {2} ({1})", _moduleId, fullColor(colors[6]), hints[6]);
        Debug.LogFormat("[Nonogram #{0}] <Row Hints> 3: {2} ({1})", _moduleId, fullColor(colors[7]), hints[7]);
        Debug.LogFormat("[Nonogram #{0}] <Row Hints> 4: {2} ({1})", _moduleId, fullColor(colors[8]), hints[8]);
        Debug.LogFormat("[Nonogram #{0}] <Row Hints> 5: {2} ({1})", _moduleId, fullColor(colors[9]), hints[9]);
        Debug.LogFormat("[Nonogram #{0}] Generated solution was: {1}", _moduleId, string.Join(", ", input.ToArray()));
    }

    string TwitchHelpMessage = "Toggle the colors with !{0} toggle. Switch between dot and fill with !{0} dot. Mark the squares with !{0} a1 b2 b3 c2 c3. (a-e is columns, 1-5 is rows) Submit your answer with !{0} submit";
    KMSelectable[] ProcessTwitchCommand(string command) {
        command = command.ToUpperInvariant().Trim();

        if (command == "SUBMIT")
            return new[] { submitButton };
        else if (command == "TOGGLE")
            return new[] { toggleButton };
        else if (command == "DOT")
            return new[] { dotButton };
        else {
            string[] args = command.Split(' ');
            List<KMSelectable> press = new List<KMSelectable>();

            for (int i = 0; i < args.Length; i++) {
                if (gridIds.Contains(args[i])) {
                    int index = Array.IndexOf(gridIds, args[i]);
                    press.Add(gridButtons[index]);
                }
            }

            return press.ToArray();
        }
    }
}
