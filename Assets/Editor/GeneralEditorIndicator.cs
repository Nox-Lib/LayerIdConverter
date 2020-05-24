using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class GeneralEditorIndicator
{
	public class Task
	{
		public Action Job { get; private set; }
		public string Description { get; private set; }

		public Task(Action job, string description)
		{
			this.Job = job;
			this.Description = description;
		}
	}

	private List<Task> taskList;
	private Action onComplete;
	private string indicatorTitle;
	private int cursor;
	private bool isCompleted;


	public static void Show(string title, List<Task> taskList, Action onComplete)
	{
		if (taskList == null || taskList.Count <= 0) {
			onComplete?.Invoke();
			return;
		}
		GeneralEditorIndicator generalEditorIndicator = new GeneralEditorIndicator();
		generalEditorIndicator.Prepare(title, taskList, onComplete);
	}


	private void Prepare(string title, List<Task> taskList, Action onComplete)
	{
		if (this.isCompleted) {
			return;
		}
		this.indicatorTitle = title;
		this.taskList = taskList;
		this.onComplete = onComplete;
		this.cursor = 0;

		EditorCoroutine.Start(this.Execute());
	}

	private void SetProgressBar()
	{
		if (this.cursor < this.taskList.Count) {
			EditorUtility.DisplayProgressBar(this.indicatorTitle, this.taskList[this.cursor].Description, (float)this.cursor / this.taskList.Count);
		}
		else {
			EditorUtility.ClearProgressBar();
		}
	}


	private IEnumerator Execute()
	{
		this.SetProgressBar();
		yield return null;

		while (this.cursor < this.taskList.Count) {
			try {
				this.taskList[this.cursor].Job();
			}
			catch {
				EditorUtility.ClearProgressBar();
				throw;
			}
			this.cursor++;
			this.SetProgressBar();
			yield return null;
		}
		this.SetProgressBar();

		this.isCompleted = true;
		this.onComplete?.Invoke();

		yield break;
	}
}