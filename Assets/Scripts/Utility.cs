using UnityEngine;
using System;

public static class Utility
{
	public static void ScanningChildren(GameObject parent, Action<GameObject, string> onProcess)
	{
		if (parent != null) {
			ScanningChildren(parent, parent.name, onProcess);
		}
	}

	private static void ScanningChildren(GameObject parent, string layerName, Action<GameObject, string> onProcess)
	{
		onProcess?.Invoke(parent, layerName);
		foreach (Transform child in parent.transform) {
			ScanningChildren(child.gameObject, string.Format("{0}/{1}", layerName, child.name), onProcess);
		}
	}
}