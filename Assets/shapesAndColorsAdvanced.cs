using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class shapesAndColorsAdvanced : MonoBehaviour {
	public KMBombModule module;
	public new KMAudio audio;
	public KMSelectable ClueUp;
	public KMSelectable ClueDown;
	public KMSelectable[] colorInput;
	public KMSelectable[] shapeInput;
	public KMSelectable[] grid;
	public KMSelectable submit;
	public MeshRenderer[] gridMeshRender;
	public MeshRenderer colorSelector;
	public MeshRenderer shapeSelector;
	public MeshRenderer[] clueMeshRender;
	public MeshRenderer[] backSpaces;
	public MeshRenderer moduleBackground;
	public Material[] images;
	public Material[] moduleBackgroundMats;
	public AudioClip LoadingSFX;
	public AudioClip BlinkSFX;
	public AudioClip StampSFX;
	public AudioClip PeelSFX;
	public AudioClip SelectSFX;
	public AudioClip[] Arrows;
	private List<Material[]> clues;
	private List<Material> moduleBackgrounds;
	private List<Clue> textClues;
	private int clueCursor;
	private float[] selectPositions = { 0.025f, -0.005f, -0.035f };
	private int moduleId;
	private static int moduleIdCounter = 1;
	private string[][] solution;
	private string[][] submission;
	private int colorCursor = -1;
	private int shapeCursor = -1;
	private bool notStart = false, solved = false;
	static readonly string[] posAbbrev = new[] { "TL", "TM", "TR", "ML", "MM", "MR", "BL", "BM", "BR" };
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[{0} #{1}] {2}", module.ModuleDisplayName, moduleId, string.Format(toLog, args));
	}
	void QuickLogDebug(string toLog, params object[] args)
    {
		Debug.LogFormat("<{0} #{1}> {2}", module.ModuleDisplayName, moduleId, string.Format(toLog, args));
	}
	void Awake()
	{
		moduleId = moduleIdCounter++;
	}
	void Start()
	{
		StartCoroutine(generatePuzzle());
	}
	private IEnumerator generatePuzzle()
	{
		yield return new WaitForSeconds(0f);
		submission = new string[][] { new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" } };
		PuzzleGenerator gen = new PuzzleGenerator();
		textClues = gen.GeneratePuzzle().Shuffle();
		solution = gen.getSolution();
		clueCursor = 0;
		QuickLog("Solution:");
		for (int i = 0; i < solution.Length; i++)
			QuickLog("{0}", solution[i].Join());

		for (int i = 0; i < textClues.Count; i++)
		{
			string[][] clue = textClues[i].getClue();
			Debug.LogFormat("Clue #{0} ({1}):", (i + 1), textClues[i].isPositive() ? "Positive" : "Negative");			
			for (int row = 0; row < 3; row++)
			{
				string output;
				if (row >= clue.Length)
					output = "KK KK KK";
				else
				{
					output = clue[row][0];
					for(int col = 1; col < 3; col++)
					{
						if (col >= clue[row].Length)
							output = output + " KK";
						else
							output = output + " " + clue[row][col];
					}
				}
				Debug.LogFormat("{0}", output);
			}
		}
		clues = new List<Material[]>();
		moduleBackgrounds = new List<Material>();
		foreach (Clue clueInfo in textClues)
		{
			clues.Add(new Material[9]);
			moduleBackgrounds.Add(clueInfo.isPositive() ? moduleBackgroundMats[0] : moduleBackgroundMats[1]);
			string[][] clue = clueInfo.getClue();
			for (int i = 0; i < clues[clues.Count - 1].Length; i++)
			{
				if ((i / 3) >= clue.Length || (i % 3) >= clue[i / 3].Length)
					clues[clues.Count - 1][i] = images[1];
				else
					clues[clues.Count - 1][i] = images[getMat(clue[i / 3][i % 3])];
			}
		}
		if (notStart)
		{
			foreach (MeshRenderer space in gridMeshRender)
			{
				space.material = images[0];
				space.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
			}
			audio.PlaySoundAtTransform(LoadingSFX.name, transform);
			for (int i = 0; i < 17; i++)
			{
				foreach (MeshRenderer mesh in clueMeshRender)
					mesh.material = images[UnityEngine.Random.Range(0, images.Length - 1)];
				yield return new WaitForSeconds(0.1f);
			}
			for (int i = 0; i < 3; i++)
			{
				audio.PlaySoundAtTransform(BlinkSFX.name, transform);
				displayClue();
				yield return new WaitForSeconds(0.25f);
				foreach (MeshRenderer mesh in clueMeshRender)
					mesh.material = images[1];
				yield return new WaitForSeconds(0.25f);
			}
			audio.PlaySoundAtTransform(BlinkSFX.name, transform);
		}
		displayClue();
		int[] indexes = { 0, 1, 2 };
		ClueUp.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[0].name, transform); clueCursor = mod(clueCursor - 1, clues.Count); displayClue(); return false; };
		ClueDown.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[1].name, transform); clueCursor = mod(clueCursor + 1, clues.Count); displayClue(); return false; };
		submit.OnInteract = delegate { StartCoroutine(pressedSubmit()); return false; };
		foreach (int index in indexes)
		{
			colorInput[index].OnInteract = delegate { selectColor(index); return false; };
			shapeInput[index].OnInteract = delegate { selectShape(index); return false; };
		}
		indexes = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
		foreach (int index in indexes)
			grid[index].OnInteract = delegate { pressedGrid(index); return false; };
		notStart = true;
	}
	private void selectColor(int cursor)
	{
		audio.PlaySoundAtTransform(SelectSFX.name, transform);
		if (cursor == colorCursor)
		{
			colorCursor = -1;
			colorSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		}
		else
		{
			colorCursor = cursor;
			colorSelector.transform.localPosition = new Vector3(0.035f, 0.0154f, selectPositions[colorCursor]);
		}
	}
	private void selectShape(int cursor)
	{
		audio.PlaySoundAtTransform(SelectSFX.name, transform);
		if (cursor == shapeCursor)
		{
			shapeCursor = -1;
			shapeSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		}
		else
		{
			shapeCursor = cursor;
			shapeSelector.transform.localPosition = new Vector3(0.065f, 0.0154f, selectPositions[shapeCursor]);
		}
	}
	private void pressedGrid(int i)
	{
		string combo = "WW";
		if (colorCursor >= 0)
			combo = "RYB"[colorCursor] + "" + combo[1];
		if(shapeCursor >= 0)
			combo = combo[0] + "" + "CTD"[shapeCursor];
		string place;
		if (submission[i / 3][i % 3].Equals(combo) || combo.Equals("WW"))
			place = "WW";
		else if (combo[0] == 'W')
		{
			if (combo[1] == submission[i / 3][i % 3][1])
				place = submission[i / 3][i % 3][0] + "W";
			else
				place = submission[i / 3][i % 3][0] + "" + combo[1];
		}
		else if (combo[1] == 'W')
		{
			if (combo[0] == submission[i / 3][i % 3][0])
				place = "W" + submission[i / 3][i % 3][1];
			else
				place = combo[0] + "" + submission[i / 3][i % 3][1];
		}
		else
			place = combo.ToUpperInvariant();
		gridMeshRender[i].material = images[getMat(place)];
		if ((place[0] == 'W' && submission[i / 3][i % 3][0] != 'W') || (place[1] == 'W' && submission[i / 3][i % 3][1] != 'W'))
			audio.PlaySoundAtTransform(PeelSFX.name, transform);
		else if(!(place.Equals("WW")))
		{
			grid[i].AddInteractionPunch();
			audio.PlaySoundAtTransform(StampSFX.name, transform);
		}
			
		submission[i / 3][i % 3] = place.ToUpperInvariant();
	}
	private IEnumerator pressedSubmit()
	{
		ClueUp.OnInteract = null;
		ClueDown.OnInteract = null;
		submit.OnInteract = null;
		foreach (KMSelectable input in colorInput)
			input.OnInteract = null;
		foreach (KMSelectable input in shapeInput)
			input.OnInteract = null;
		foreach (KMSelectable input in grid)
			input.OnInteract = null;
		colorSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		shapeSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		colorCursor = -1;
		shapeCursor = -1;
		Debug.LogFormat("[Shapes and Colors #{0}] Submitted:", moduleId);
		for (int i = 0; i < submission.Length; i++)
			Debug.LogFormat("[Shapes and Colors #{0}] {1}", moduleId, submission[i].Join());
		List<int> notFilled = new List<int>();
		for (int i = 0; i < submission.Length; i++)
		{
			string submitLog = "";
			for (int j = 0; j < submission[i].Length; j++)
			{
				if (submission[i][j].Contains("W"))
					notFilled.Add(i * 3 + j);
				submitLog = submitLog + "" + submission[i][j] + " ";
			}
		}
		if (!notFilled.Any())
		{
			//Next, check if all the clues can fit on the grid at least once.
			bool strike = false;
			clueCursor = -1;
            for (int n = 0; n < textClues.Count; n++)
			{
                Clue clueInfo = textClues[n];
                string[][] clue = clueInfo.getClue();
				clueCursor++;
				displayClue();
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				yield return new WaitForSeconds(0.5f);
				List<int> spacesToLight = new List<int>();
				bool foundItem = false;
				var missingVCnt = submission.Length - clue.Length + 1;
                for (var dy = 0; dy < missingVCnt; dy++)
                {
					var missingHCnt = submission[dy].Length - clue[dy % clue.Length].Length + 1;
                    for (var dx = 0; dx < missingHCnt; dx++)
                    {
						//QuickLogDebug("{0}, {1}", dy, dx);
						var countMatch = 0;
						spacesToLight.Clear();
						for (var y = 0; y < clue.Length; y++)
                        {
							for (var x = 0; x < clue[y].Length; x++)
                            {
								if (clue[y][x] == "KK") continue;
								if (clue[y][x] == "WW" || doesFit(submission[y + dy][x + dx], clue[y][x]))
                                {
									countMatch++;
									spacesToLight.Add(3 * (y + dy) + x + dx);
                                }
                            }
                        }
						QuickLogDebug("#{0} (+{1}, +{2}): {3}/{4}", n + 1, dy, dx, countMatch, clue.Sum(a => a.Where(b => b != "KK").Count()));
						if (countMatch >= clue.Sum(a => a.Where(b => b != "KK").Count()))
						{
							foundItem = true;
							break;
						}
                    }
					if (foundItem)
						break;
				}


				if (clueInfo.isPositive())
				{
					if (foundItem)
					{
						audio.PlaySoundAtTransform(ClueUp.name, transform);
						foreach (int space in spacesToLight)
							backSpaces[space].material = images[23];
						yield return new WaitForSeconds(0.5f);
						foreach (MeshRenderer backSpace in backSpaces)
							backSpace.material = images[1];
					}
					else
					{
						QuickLog("Clue #{0} violated. Clue must be found, not broken.", n + 1);
						module.HandleStrike();
						foreach (MeshRenderer backSpace in backSpaces)
							backSpace.material = images[getMat("R")];
						yield return new WaitForSeconds(5.0f);
						foreach (MeshRenderer backSpace in backSpaces)
							backSpace.material = images[1];
						StartCoroutine(generatePuzzle());
						strike = true;
						break;
					}
				}
				else
				{
					if (foundItem)
					{
						QuickLog("Clue #{0} violated. Clue must be broken, not found.", n + 1);
						module.HandleStrike();
						foreach (int space in spacesToLight)
							backSpaces[space].material = images[getMat("R")];
						yield return new WaitForSeconds(5.0f);
						foreach (MeshRenderer backSpace in backSpaces)
							backSpace.material = images[1];
						StartCoroutine(generatePuzzle());
						strike = true;
						break;
					}
					else
					{
						audio.PlaySoundAtTransform(ClueUp.name, transform);
						//Maybe make the clue screen green?
						yield return new WaitForSeconds(0.5f);
					}
				}
			}
			if(!strike)
			{
				//Finally, check if the grid has one of each color/shape combo
				foreach (MeshRenderer clueSpace in clueMeshRender)
					clueSpace.material = images[1];
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				moduleBackground.material = moduleBackgroundMats[0];
				yield return new WaitForSeconds(0.5f);
				string[] comboList = { "RC", "RT", "RD", "YC", "YT", "YD", "BC", "BT", "BD" };
				List<int> missed = new List<int>();
				for (int i = 0; i < comboList.Length; i++)
				{
					int cur = -1;
					for (int j = 0; j < submission.Length; j++)
					{
						for (int k = 0; k < submission[j].Length; k++)
						{
							if (submission[j][k].Equals(comboList[i]))
							{
								cur = (j * 3) + k;
								goto skip3;
							}
						}
					}
				skip3:
					if (cur >= 0)
					{
						audio.PlaySoundAtTransform(StampSFX.name, transform);
						gridMeshRender[cur].transform.localScale = new Vector3(0f, 0f, 0f);
						clueMeshRender[i].material = images[getMat(comboList[i])];
						submission[cur / 3][cur % 3] = "WW";
					}
					else
						missed.Add(i);
					yield return new WaitForSeconds(0.5f);
				}
				if (missed.Count == 0)
				{
					foreach (MeshRenderer space in gridMeshRender)
						space.transform.localScale = new Vector3(0f, 0f, 0f);
					QuickLog("Submission valid.");
					module.HandlePass();
					solved = true;
					audio.PlaySoundAtTransform(ClueUp.name, transform);
					audio.PlaySoundAtTransform(ClueDown.name, transform);
					moduleBackground.material = moduleBackgroundMats[1];
					yield return new WaitForSeconds(0.25f);
					audio.PlaySoundAtTransform(ClueUp.name, transform);
					audio.PlaySoundAtTransform(ClueDown.name, transform);
					moduleBackground.material = moduleBackgroundMats[0];
					yield return new WaitForSeconds(0.125f);
					audio.PlaySoundAtTransform(ClueUp.name, transform);
					audio.PlaySoundAtTransform(ClueDown.name, transform);
					moduleBackground.material = moduleBackgroundMats[1];
					yield return new WaitForSeconds(0.125f);
					audio.PlaySoundAtTransform(ClueUp.name, transform);
					audio.PlaySoundAtTransform(ClueDown.name, transform);
					moduleBackground.material = moduleBackgroundMats[0];
				}
				else
				{
					QuickLog("Following positions contain a repeated shape: {0}", missed.Select(a => posAbbrev[a]).Join());
					module.HandleStrike();
					foreach (int blank in missed)
						clueMeshRender[blank].material = images[getMat("R")];
					for (int i = 0; i < submission.Length; i++)
					{
						for (int j = 0; j < submission[i].Length; j++)
						{
							if (!(submission[i][j].Equals("WW")))
								backSpaces[i * 3 + j].material = images[getMat("R")];
						}
					}
					yield return new WaitForSeconds(3.0f);
					foreach (MeshRenderer backSpace in backSpaces)
						backSpace.material = images[1];
					StartCoroutine(generatePuzzle());
				}
			}
		}
		else
		{
			QuickLog("Empty tiles detected: {0}", notFilled.Select(a => posAbbrev[a]).Join());
			module.HandleStrike();
			foreach (int space in notFilled)
				backSpaces[space].material = images[getMat("R")];
			yield return new WaitForSeconds(1.0f);
			foreach (int space in notFilled)
				backSpaces[space].material = images[1];
			ClueUp.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[0].name, transform); clueCursor = mod(clueCursor - 1, clues.Count); displayClue(); return false; };
			ClueDown.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[1].name, transform); clueCursor = mod(clueCursor + 1, clues.Count); displayClue(); return false; };
			submit.OnInteract = delegate { StartCoroutine(pressedSubmit()); return false; };
			int[] indexes = { 0, 1, 2 };
			foreach (int index in indexes)
			{
				colorInput[index].OnInteract = delegate { selectColor(index); return false; };
				shapeInput[index].OnInteract = delegate { selectShape(index); return false; };
			}
			indexes = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
			foreach (int index in indexes)
				grid[index].OnInteract = delegate { pressedGrid(index); return false; };
		}
		//end:
		yield return new WaitForSeconds(0f);
	}
	private void displayClue()
	{
		moduleBackground.material = moduleBackgrounds[clueCursor];
		for (int i = 0; i < clues[clueCursor].Length; i++)
			clueMeshRender[i].material = clues[clueCursor][i];
	}
	private int mod(int n, int m)
	{
		while (n < 0)
			n += m;
		return (n % m);
	}
	private int getMat(string space)
	{
		switch(space)
		{
			case "WW": return 0;
			case "KK": return 1;
			case "C": case "WC": return 2;
			case "T": case "WT": return 3;
			case "D": case "WD": return 4;
			case "R": case "RW": return 5;
			case "Y": case "YW": return 6;
			case "B": case "BW": return 7;
			case "-C": return 8;
			case "-T": return 9;
			case "-D": return 10;
			case "-R": return 11;
			case "-Y": return 12;
			case "-B": return 13;
			case "RC": return 14;
			case "RT": return 15;
			case "RD": return 16;
			case "YC": return 17;
			case "YT": return 18;
			case "YD": return 19;
			case "BC": return 20;
			case "BT": return 21;
			case "BD": return 22;
		}
		return -1;
	}
	private bool doesFit(string space, string clue)
	{
		if (clue[0] == '-')
			return !(space.Contains(clue[1]));
		else if (clue.Equals(space))
			return true;
		return (space.Contains(clue));
	}
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press|p up down (R)ed (Y)ellow (B)lue (C)ircle (T)riangle (D)iamond TL/1 TM/2 TR/3 ML/4 MM/5 MR/6 BL/7 BM/8 BR/9 to press those buttons on the module. !{0} submit to submit your current grid. !{0} clear to clear the entire grid.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		var upperCmd = command.ToUpper();
		string[] param = command.ToUpper().Split(' ');
		var rgxMatchCycle = Regex.Match(upperCmd, @"^CYCLE(\s(UP?|D(OWN)?))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (rgxMatchCycle.Success)
        {
			var matchCycle = rgxMatchCycle.Value.Split();
			if (matchCycle.Length == 1 || matchCycle.Last().StartsWith("D"))
            {
				yield return null;
				for (var x = 0; x < textClues.Count; x++)
				{
					yield return "trywaitcancel 2";
					ClueDown.OnInteract();
				}
            }
			else
			{
				yield return null;
				for (var x = 0; x < textClues.Count; x++)
				{
					yield return "trywaitcancel 2";
					ClueUp.OnInteract();
				}
			}
		}
		else if ((Regex.IsMatch(param[0], @"^\s*PRESS\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(param[0], @"^\s*P\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) && param.Length > 1)
		{
			if (isButton(param))
			{
				yield return null;
				for (int i = 1; i < param.Length; i++)
				{
					switch (param[i])
					{
						case "UP":
							ClueUp.OnInteract();
							break;
						case "DOWN":
							ClueDown.OnInteract();
							break;
						case "RED": case "R":
							colorInput[0].OnInteract();
							break;
						case "YELLOW": case "Y":
							colorInput[1].OnInteract();
							break;
						case "BLUE": case "B":
							colorInput[2].OnInteract();
							break;
						case "CIRCLE": case "C":
							shapeInput[0].OnInteract();
							break;
						case "TRIANGLE": case "T":
							shapeInput[1].OnInteract();
							break;
						case "DIAMOND": case "D":
							shapeInput[2].OnInteract();
							break;
						case "TL": case "1":
							grid[0].OnInteract();
							break;
						case "TM": case "2":
							grid[1].OnInteract();
							break;
						case "TR": case "3":
							grid[2].OnInteract();
							break;
						case "ML": case "4":
							grid[3].OnInteract();
							break;
						case "MM": case "5":
							grid[4].OnInteract();
							break;
						case "MR": case "6":
							grid[5].OnInteract();
							break;
						case "BL": case "7":
							grid[6].OnInteract();
							break;
						case "BM": case "8":
							grid[7].OnInteract();
							break;
						case "BR": case "9":
							grid[8].OnInteract();
							break;
					}
					yield return new WaitForSeconds(0.2f);
				}
			}
			else
				yield return "sendtochat An error occured because the user inputted something wrong.";
		}
		else if (Regex.IsMatch(param[0], @"^\s*SUBMIT\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && param.Length == 1)
		{
			yield return null;
			submit.OnInteract();
			yield return "strike";
			yield return "solve";
		}
		else if (Regex.IsMatch(param[0], @"^\s*CLEAR\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && param.Length == 1)
		{
			yield return null;
			if(colorCursor >= 0)
			{
				colorInput[colorCursor].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			if (shapeCursor >= 0)
			{
				shapeInput[shapeCursor].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			foreach(KMSelectable space in grid)
			{
				space.OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}
		else
			yield return "sendtochat An error occured because the user inputted something wrong.";
	}
	private bool isButton(string[] param)
	{
		for(int i = 1; i < param.Length; i++)
		{
			switch(param[i])
			{
				case "UP": case "DOWN":
				case "RED":		case "R":
				case "YELLOW":	case "Y":
				case "BLUE":	case "B":
				case "CIRCLE":		case "C":
				case "TRIANGLE":	case "T":
				case "DIAMOND":		case "D":
				case "TL":	case "1":
				case "TM":	case "2":
				case "TR":	case "3":
				case "ML":	case "4":
				case "MM":	case "5":
				case "MR":	case "6":
				case "BL":	case "7":
				case "BM":	case "8":
				case "BR":	case "9":
					break;
				default:
					return false;
			}
		}
		return true;
	}
	IEnumerator TwitchHandleForcedSolve()
    {
		string colorsAbbrev = "RYB";
		string shapesAbbrev = "CTD";
		while (Enumerable.Range(0, 3).Any(a => !submission[a].SequenceEqual(solution[a])))
        {
			for (var x = 0; x < 3; x++)
            {
				for (var y = 0; y < 3; y++)
				{
					if (submission[x][y] == solution[x][y]) continue;

					var curSolution = solution[x][y];
					var colorIdx = colorsAbbrev.IndexOf(curSolution[0]);
					var shapeIdx = shapesAbbrev.IndexOf(curSolution[1]);
					if (colorIdx != colorCursor)
					{
						colorInput[colorIdx].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
					if (shapeIdx != shapeCursor)
					{
						shapeInput[shapeIdx].OnInteract();
						yield return new WaitForSeconds(0.1f);
					}
					grid[3 * x + y].OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
			}
        }
		submit.OnInteract();
		while (!solved)
			yield return true;
    }
}
