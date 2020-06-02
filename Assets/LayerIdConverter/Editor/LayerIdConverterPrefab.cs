using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace ConvertLayerId
{
	public class LayerIdConverterPrefab : LayerIdConverterBase
	{
		public override string AssetType => "Prefab";

		public override void Execute(List<string> pathList, ConvertData convertSettings)
		{
			List<GeneralEditorIndicator.Task> tasks = new List<GeneralEditorIndicator.Task>();
			foreach (string path in pathList) {
				string assetPath = path;
				tasks.Add(new GeneralEditorIndicator.Task(
					() => {
						try {
							this.ChangeLayer(assetPath, convertSettings);
						}
						catch (Exception e) {
							if (convertSettings.isStopConvertOnError) {
								throw;
							}
							Debug.LogException(e);
						}
					},
					assetPath
				));
			}

			GeneralEditorIndicator.Show("PrefabLayerIdConverter", tasks, () => {});
		}


		private void ChangeLayer(string assetPath, ConvertData convertSettings)
		{
			GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefabObject == null) {
				return;
			}

			List<string> results = new List<string>();
			if (convertSettings.isChangeChildren) {
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
					assetPath,
					convertSettings.isChangeChildren,
					string.Join("\n", results)
				));
				EditorUtility.SetDirty(prefabObject);
				AssetDatabase.SaveAssets();
			}
		}
	}
}