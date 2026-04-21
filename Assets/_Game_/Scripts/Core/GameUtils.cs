using System.Linq;
using UnityEngine;

public static class GameUtils
{
	public static bool TagCheck(string filters, object obj)
	{
		return obj == null || string.IsNullOrWhiteSpace(filters) ||
                                         filters.Split(',').ToList().Any(tag =>
                                         {
	                                         if (obj is Component)
		                                         return (obj as Component).CompareTag(tag);
                               
	                                         if (obj is GameObject)
		                                         return (obj as GameObject).CompareTag(tag);
								
	                                         return false;
                                         });
	}
}