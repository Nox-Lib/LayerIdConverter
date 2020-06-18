using System;
using System.Collections.Generic;
using System.Linq;

namespace ConvertLayerId
{
	public enum ProcessingMode {
		Normal		= 0,
		LayerIdOnly	= 1,
		CameraOnly	= 2
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

		public ProcessingMode processingMode = ProcessingMode.Normal;
		public List<Pattern> patterns = new List<Pattern>();
		public bool isStopConvertOnError = true;
		public bool isChangeChildren = true;
		public bool isLeaveOldCameraCullingMask;

		public bool IsEnabledLayerId {
			get {
				bool result = false;
				result |= this.processingMode == ProcessingMode.Normal;
				result |= this.processingMode == ProcessingMode.LayerIdOnly;
				return result;
			}
		}

		public bool IsEnabledCameraCullingMask {
			get {
				bool result = false;
				result |= this.processingMode == ProcessingMode.Normal;
				result |= this.processingMode == ProcessingMode.CameraOnly;
				return result;
			}
		}

		public ConvertData Clone()
		{
			return new ConvertData {
				processingMode = this.processingMode,
				patterns = this.patterns.Select(x => x.Clone()).ToList(),
				isStopConvertOnError = this.isStopConvertOnError,
				isChangeChildren = this.isChangeChildren,
				isLeaveOldCameraCullingMask = this.isLeaveOldCameraCullingMask
			};
		}
	}
}