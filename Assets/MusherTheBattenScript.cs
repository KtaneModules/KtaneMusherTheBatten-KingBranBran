using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class MusherTheBattenScript : MonoBehaviour
{

	private KMBombModule _module;
	private KMSelectable _button;
	private KMBombInfo _info;
	private KMAudio _audio;
	private GameObject _buttonGameObject;
	private TextMesh _screenText;
	private TextMesh _buttonText;
	private Color[] _buttonColors = {Color.white, Color.magenta, Color.red, Color.yellow, Color.green, Color.blue, Color.black};
	private string[] _buttonColorNames = {"White", "Magenta", "Red", "Yellow", "Green", "Blue", "Black"};
	private string[] _buttonNames = {"Button", "Batten", "Breten", "Butten", "Batton", "Blutun", "Beaton", "Bob", "Putton"};
	private int _stages;

	private int _moduleId;
	private static int _moduleIdCounter = 1;

	private int _displayNumber;
	private int _buttonColor;
	private int _buttonName;
	private int _solutionNumber;
	private int _stagesPassed;

	private bool _init;
	private bool _solved;
	
	
	void Awake ()
	{
		_moduleId = _moduleIdCounter++;
		_module = GetComponent<KMBombModule>();
		_button = GetComponent<KMSelectable>().Children.First();
		_audio = GetComponent<KMAudio>();
		_buttonGameObject = transform.Find("Button").gameObject;
		_screenText = transform.Find("Counter/CounterText").GetComponent<TextMesh>();
		_buttonText = _buttonGameObject.transform.Find("Label").GetComponent<TextMesh>();
		_info = GetComponent<KMBombInfo>();
		_stages = Random.Range(1, 5);
		
		_module.OnActivate += () =>
		{
			DebugLog("This module will have {0} {1}.", _stages, _stages > 1 ? "stages" : "stage");
			Init();
		};

		_button.OnInteract += () =>
		{
			ButtonPressed();
			return false;
		};
	}

	void Init()
	{
		_displayNumber = Random.Range(0, 100);
		_buttonColor = Random.Range(0, _buttonColors.Length);
		_buttonName = Random.Range(0, _buttonNames.Length);

		_screenText.text = _displayNumber.ToString();
		_buttonGameObject.GetComponent<MeshRenderer>().material.color = _buttonColors[_buttonColor];
		_buttonText.text = _buttonNames[_buttonName];
		_buttonText.color = _buttonColors[_buttonColor].Equals(Color.black) ? Color.white : Color.black;

		var tableBatton = TableBatten.GetTableData(_buttonName, _buttonColor);

		if (tableBatton > 0)
		{
			_solutionNumber = 
				(_displayNumber + _info.GetSerialNumberNumbers().Sum())
				/ (_info.GetBatteryCount() == 0 ? 1 : _info.GetBatteryCount())
				* tableBatton
				% 60;
		}
		else
		{
			_solutionNumber = -1;
		}
		
		_init = true;
		
		DebugLog("[Stage {0}]", _stagesPassed + 1);
		DebugLog("The counter is {0}, the button word is {1}, and the button color is {2}.", _displayNumber, _buttonNames[_buttonName], _buttonColorNames[_buttonColor]);
		DebugLog("Solution: Press the button at {0}", _solutionNumber == -1 ? "any time!" : (_solutionNumber < 10 ? "0" : "") + _solutionNumber + ".");
	}

	void ButtonPressed()
	{
		_button.AddInteractionPunch();
		_audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (_init && !_solved)
		{
			var correct = (int) _info.GetTime() % 60 == _solutionNumber || _solutionNumber == -1 || _info.GetTime() < _solutionNumber;
			DebugLog("You pressed the button at {0}. That is {1}", (int) _info.GetTime() % 60, correct ? "correct!" : "incorrect...");
			
			if (correct)
			{
				_stagesPassed++;
				if (_stagesPassed >= _stages)
				{
					_module.HandlePass();
					_solved = true;
				}
				else
				{
					Init();
				}
			}
			else
			{
				_module.HandleStrike();
				Init();
			}
		}
	}
	
	private void DebugLog(string log, params object[] args)
	{
		var logData = string.Format(log, args);
		Debug.LogFormat("[Musher The Batten #{0}] {1}", _moduleId, logData);
	}
	
#pragma warning disable CS0414
	private string TwitchHelpMessage = "Use '!{0} press ##' to press the button when the two digits on the timer are ##. Use '!{0} press' to press the button.";
	bool TwitchPlaysSkipTimeAllowed = true;
#pragma warning restore CS0414

	IEnumerator ProcessTwitchCommand(string command)
	{
		if (!Regex.IsMatch(command, "^(?:press)(?: [0-9]{2})?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) yield break;

		var split = command.Split(' ');
		
		if (split.Length > 1 && int.Parse(command.Split(' ')[1]) > 59) yield break;
		
		if (split.Length == 1)
		{
			yield return null;
			ButtonPressed();
		}
		else
		{
			yield return null;
			
			var time = int.Parse(command.Split(' ')[1]);
			var timeLeft = (int) _info.GetTime() - (60 - time) - (int) _info.GetTime() % 60 + 1;
			yield return "skiptime " + timeLeft;
			
			do
			{
				time = int.Parse(command.Split(' ')[1]);

				yield return "trycancel";
			} 
			while (time != (int) _info.GetTime() % 60);
					
			ButtonPressed();
		}
			
	}
}

internal class TableBatten
{
	private static readonly string[] Table =
	{
		"0 8 3 7 2 20 -1",
		"-1 2 11 9 19 18 12",
		"69 14 7 19 15 7 5",
		"5 9 5 9 8 4 17",
		"3 2 2 3 0 20 2",
		"18 1 13 7 -1 1 10",
		"8 8 1 16 5 1 12",
		"15 -1 8 7 4 3 0",
		"5 2 3 6 9 9 2"
	};

	public static int GetTableData(int row, int column)
	{
		return int.Parse(Table[row].Split(' ')[column]);
	}
}

