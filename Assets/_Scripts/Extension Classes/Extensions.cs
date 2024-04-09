using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class Extension
{
	public static string ConcatAllElems<T>(this List<T> list, string delim = "||") { 
		string output = string.Empty;

		foreach (var elem in list)
		{
			output += elem.ToString() + delim;
		}
		return output;
	}
}
