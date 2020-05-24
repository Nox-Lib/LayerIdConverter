using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class PrefabLayerIdConvertWindow : LayerIdConvertWindowBase
{
	protected override string AssetType => "Prefab";

	[MenuItem("Tools/LayerIdConverter/Prefab")]
	private static void OpenPrefab()
	{
		PrefabLayerIdConvertWindow window = GetWindow<PrefabLayerIdConvertWindow>();
		window.titleContent = new GUIContent("PrefabLayerIdConverter");
		window.minSize = new Vector2(300f, 300f);
	}


	protected override void Execute(List<string> pathList, ConvertData convertSettings, bool isChangeChildren)
	{
		List<GeneralEditorIndicator.Task> tasks = new List<GeneralEditorIndicator.Task>();
		foreach (string path in pathList) {
			tasks.Add(new GeneralEditorIndicator.Task(
				() => { this.ChangeLayer(path, convertSettings, isChangeChildren); },
				path
			));
		}
		GeneralEditorIndicator.Show(
			"PrefabLayerIdConverter",
			tasks,
			() => {
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		);
	}


	private void ChangeLayer(string path, ConvertData convertSettings, bool isChangeChildren)
	{
		int startIndex = path.IndexOf("Resources/", 0, StringComparison.Ordinal);
		string prefabPath = path;
		prefabPath = prefabPath.Substring(startIndex, prefabPath.Length - startIndex);
		prefabPath = prefabPath.Replace("Resources/", string.Empty);
		prefabPath = prefabPath.Replace(Path.GetExtension(prefabPath), string.Empty);

		GameObject prefabObject = Resources.Load<GameObject>(prefabPath);
		if (prefabObject == null) {
			return;
		}

		List<string> results = new List<string>();
		if (isChangeChildren) {
			Utility.ScanningChildren(
				prefabObject,
				(child, layerName) => {
					List<string> result = this.ChangeLayer(layerName, child, convertSettings);
					if (result != null && result.Count > 0) {
						results.AddRange(result);
					}
				}
			);
		}
		else {
			List<string> result = this.ChangeLayer(prefabObject.name, prefabObject, convertSettings);
			if (result != null && result.Count > 0) {
				results.AddRange(result);
			}
		}

		if (results.Count > 0) {
			Debug.Log(string.Format(
				"[PrefabLayerIdConverter] {0}, Change Children = {1}\n{2}",
				path,
				isChangeChildren,
				string.Join("\n", results)
			));
			EditorUtility.SetDirty(prefabObject);
		}
	}
}