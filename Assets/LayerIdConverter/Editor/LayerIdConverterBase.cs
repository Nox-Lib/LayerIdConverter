using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConvertLayerId
{
	public abstract class LayerIdConverterBase
	{
		protected List<string> allPathList = new List<string>();
		protected List<string> targetPathList = new List<string>();

		public bool IsCompleted { get; protected set; }
		public bool IsInterruption { get; protected set; }
		public List<string> TargetPaths { get { return this.targetPathList; } }

		protected LayerIdConverterBase(string assetType)
		{
			string filter = string.Format("t:{0}", assetType);
			string[] searchInFolders = { "Assets" };

			this.allPathList = AssetDatabase.FindAssets(filter, searchInFolders)
				.Select(AssetDatabase.GUIDToAssetPath)
				.ToList();
		}

		public virtual void Execute(ConvertData convertSettings)
		{
			this.IsCompleted = this.IsInterruption = false;
		}

		public void Filter(string matchPattern, string ignorePattern, bool isError)
		{
			if (isError) {
				this.targetPathList = new List<string>();
				return;
			}
			this.targetPathList = new List<string>(this.allPathList);

			if (!string.IsNullOrEmpty(matchPattern)) {
				this.targetPathList = this.targetPathList.Where(x => Regex.IsMatch(x, matchPattern)).ToList();
			}
			if (!string.IsNullOrEmpty(ignorePattern)) {
				this.targetPathList = this.targetPathList.Where(x => !Regex.IsMatch(x, ignorePattern)).ToList();
			}
		}

		protected List<string> ChangeLayer(string layerName, GameObject gameObject, ConvertData convertSettings)
		{
			List<string> result = new List<string>();

			if (convertSettings.IsEnabledLayerId) {
				ConvertData.Pattern convertPattern = convertSettings.patterns.FirstOrDefault(x => gameObject.layer == x.oldLayerId);
				if (convertPattern != null) {
					gameObject.layer = convertPattern.newLayerId;
					EditorUtility.SetDirty(gameObject);
					result.Add(string.Format("{0} ({1} => {2})", layerName, convertPattern.oldLayerId, convertPattern.newLayerId));
				}
			}
			if (convertSettings.IsEnabledCameraCullingMask) {
				Camera camera = gameObject.GetComponent<Camera>();
				if (camera != null && camera.cullingMask != -1) {
					foreach (ConvertData.Pattern convertPattern in convertSettings.patterns) {
						int beforeCullingMask = camera.cullingMask;
						int oldMask = 1 << convertPattern.oldLayerId;
						int newMask = 1 << convertPattern.newLayerId;
						if ((camera.cullingMask & oldMask) >= 1) {
							camera.cullingMask |= newMask;
							if (!convertSettings.isLeaveOldCameraCullingMask) {
								camera.cullingMask &= ~oldMask;
							}
						}
						if (beforeCullingMask == camera.cullingMask) {
							continue;
						}
						result.Add(string.Format(
							"{0} (Camera Culling Mask {1} => {2}), Leave Old Camera Culling Mask = {3}",
							layerName,
							convertPattern.oldLayerId,
							convertPattern.newLayerId,
							convertSettings.isLeaveOldCameraCullingMask
						));
						EditorUtility.SetDirty(camera);
					}
				}
			}
			return result;
		}

		protected void ScanningChildren(GameObject parent, Action<GameObject, string> onProcess)
		{
			if (parent != null) {
				ScanningChildren(parent, parent.name, onProcess);
			}
		}

		private void ScanningChildren(GameObject parent, string layerName, Action<GameObject, string> onProcess)
		{
			onProcess?.Invoke(parent, layerName);
			foreach (Transform child in parent.transform) {
				ScanningChildren(child.gameObject, string.Format("{0}/{1}", layerName, child.name), onProcess);
			}
		}
	}
}