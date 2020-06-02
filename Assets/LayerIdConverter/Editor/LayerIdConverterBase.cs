using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ConvertLayerId
{
	public abstract class LayerIdConverterBase
	{
		public abstract string AssetType { get; }
		public abstract void Execute(List<string> pathList, ConvertData convertSettings);

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
	}
}