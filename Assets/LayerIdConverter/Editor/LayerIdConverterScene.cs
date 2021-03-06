﻿using UnityEngine;
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
		public LayerIdConverterScene() : base("Scene") {}

		private string currentScenePath;

		public override void Execute(ConvertData convertSettings)
		{
			base.Execute(convertSettings);

			EditorSceneManager.SaveOpenScenes();
			this.currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

			List<GeneralEditorIndicator.Task> tasks = new List<GeneralEditorIndicator.Task>();
			foreach (string path in this.TargetPaths) {
				string assetPath = path;
				tasks.Add(new GeneralEditorIndicator.Task(
					() => {
						try {
							this.ChangeLayer(assetPath, convertSettings);
						}
						catch (Exception e) {
							if (convertSettings.isStopConvertOnError) {
								this.IsInterruption = true;
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
				"LayerIdConverter - Scene",
				tasks,
				() => {
					EditorSceneManager.OpenScene(this.currentScenePath);
					this.IsCompleted = true;
				}
			);
		}


		private void ChangeLayer(string assetPath, ConvertData convertSettings)
		{
			EditorSceneManager.OpenScene(assetPath);
			Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(assetPath);
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
					"[LayerIdConverter - Scene] {0}, Change Children = {1}\n{2}",
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