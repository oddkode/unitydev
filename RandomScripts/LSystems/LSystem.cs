using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem : MonoBehaviour
{
	public string alphabet = "AB";
	public readonly char axiom;
	public List<LSystemRule> rules;
	public int length = 100;
	public string sentence = "";
	private LSystem L;

	public LSystem(int len = 100)
	{
		length = len;
		rules = new List<LSystemRule>();
		axiom = alphabet[0];
	}

	public string Run(string s, bool all = true)
	{
		string result = "";

		if (rules != null && rules.Count > 0)
		{
			foreach (var r in rules)
			{
				result = r.Evaluate(s);
			}
		}

		return result;
	}

	public void Awake()
    {
		L = new LSystem(5);
		sentence = L.axiom.ToString();
		var rule1 = new LSystemRule(L.alphabet);

		L.rules.Add(rule1);		
	}

    public void Start()
    {
		for (int i = 0; i < L.length; i++)
		{			
			sentence = L.Run(sentence);
		}
	}
}

public class LSystemRule
{
	public Dictionary<char, string> Variables { get; set; }
	private string _alphabet;

	public LSystemRule(string alphabet, int positiveOffset = 1, int negativeOffset = 1)
	{
		_alphabet = alphabet;
		Variables = new Dictionary<char, string>(); // variable_name, sub_value
		var chars = alphabet.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			if (i % 2 == 0)
			{
				if ((i + positiveOffset) <= chars.Length)
				{
					Variables.Add(chars[i], $"{chars[i]}{chars[i + positiveOffset]}");
					continue;
				}
			}
			else
			{
				if ((i - negativeOffset) >= 0)
				{
					Variables.Add(chars[i], chars[i - negativeOffset].ToString());
					continue;
				}
			}
		}

	}

	public string Evaluate(string s)
	{
		string result = "";
		var chars = s.ToCharArray();

		foreach (char c in chars)
		{
			result += Variables[c];
		}

		return result;
	}

	public override string ToString()
	{
		return _alphabet;
	}
}
