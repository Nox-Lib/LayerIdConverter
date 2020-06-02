using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertLayerId
{
	public class LayerIdConverterScene : LayerIdConverterBase
	{
		public override string AssetType => "Scene";

		private string currentScenePath;

		public override void Execute(List<string> pathList, ConvertData convertSettings)
		{
			EditorSceneManager.SaveOpenScenes();
			this.currentScenePath = SceneManager.GetActiveScene().path;

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
								EditorSceneManager.OpenScene(this.currentScenePath);
								throw;
							}
							Debug.LogException(e);
						}
					},
					assetPath
				));
			}
		
			GeneralEditorIndicator.Show(
				"SceneLayerIdConverter",
				tasks,
				() => { EditorSceneManager.OpenScene(this.currentScenePath); }
			);
		}


		private void ChangeLayer(string assetPath, ConvertData convertSettings)
		{
			EditorSceneManager.OpenScene(assetPath);
			Scene scene = SceneManager.GetSceneByPath(assetPath);
			List<GameObject> gameObjects = scene.GetRootGameObjects().ToList();
			List<string> results = new List<string>();

			foreach (GameObject target in gameObjects) {
				bool isChanged = false;
				if (convertSettings.isChangeChildren) {
					this.ScanningChildren(
						target,
						(child, layerName) => {
							List<string> result = this.ChangeLayer(layerName, child, convertSettings);
							if (result != null && result.Count > 0) {
								results.AddRange(result);
								isChanged = true;
							}
						}
					);
				}
				else {
					List<string> result = this.ChangeLayer(target.name, target, convertSettings);
					if (result != null && result.Count > 0) {
						results.AddRange(result);
						isChanged = true;
					}
				}
				if (isChanged) {
					EditorUtility.SetDirty(target);
				}
			}

			if (results.Count > 0) {
				Debug.Log(string.Format(
					"[SceneLayerIdConverter] {0}, Change Children = {1}\n{2}",
					assetPath,
					convertSettings.isChangeChildren,
					string.Join("\n", results)
				));
			}

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			AssetDatabase.SaveAssets();
		}
	}
}