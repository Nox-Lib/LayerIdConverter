using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertLayerId
{
	public enum ProcessingMode : int {
		Normal		= 0,
		CameraOnly	= 1
	}

	[Serializable]
	public class ConvertData
	{
		[Serializable]
		public class Pattern
		{
			public int oldLayerId;
			public int newLayerId;

			public bool IsValid {
				get {
					bool result = true;
					result &= this.oldLayerId >= 0 && this.oldLayerId <= 31;
					result &= this.newLayerId >= 0 && this.newLayerId <= 31;
					return result;
				}
			}

			public Pattern Clone()
			{
				return new Pattern {
					oldLayerId = this.oldLayerId,
					newLayerId = this.newLayerId
				};
			}
		}

		[Serializable]
		public class CameraOption
		{
			public bool isEnabled;
			public bool isLeaveOldCullingMask;

			public CameraOption Clone()
			{
				return new CameraOption {
					isEnabled = this.isEnabled,
					isLeaveOldCullingMask = this.isLeaveOldCullingMask
				};
			}
		}

		public ProcessingMode processingMode = ProcessingMode.Normal;
		public List<Pattern> patterns = new List<Pattern>();
		public bool isChangeChildren = true;
		public bool isStopConvertOnError = true;
		public CameraOption cameraOption = new CameraOption();

		public ConvertData Clone()
		{
			return new ConvertData {
				processingMode = this.processingMode,
				patterns = this.patterns.Select(x => x.Clone()).ToList(),
				isChangeChildren = this.isChangeChildren,
				isStopConvertOnError = this.isStopConvertOnError,
				cameraOption = this.cameraOption.Clone()
			};
		}
	}
}