using UnityEngine;

public static class UtilityLibrary
{
	private const float SmallNumber = 1E-08f;
	
	public static bool IsNearlyZero(this Vector3 v, float preciseness = SmallNumber)
	{
		return v.x < preciseness && v.x > -preciseness && v.y < preciseness && v.y > -preciseness && v.z < preciseness && v.z > -preciseness;
	}
}