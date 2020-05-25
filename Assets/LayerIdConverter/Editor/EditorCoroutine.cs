using UnityEditor;
using System.Collections;

namespace ConvertLayerId
{
	public class EditorCoroutine
	{
		private readonly IEnumerator enumerator;

		public static void Start(IEnumerator enumerator)
		{
			EditorCoroutine editorCoroutine = new EditorCoroutine(enumerator);
		}

		public EditorCoroutine(IEnumerator enumerator)
		{
			this.enumerator = enumerator;
			if (this.enumerator != null) {
				EditorApplication.update += this.Update;
			}
		}

		private void Update()
		{
			if (!this.enumerator.MoveNext()) {
				EditorApplication.update -= this.Update;
			}
		}
	}
}