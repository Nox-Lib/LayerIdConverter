using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class SelectionContentPopup : PopupWindowContent
{
	private const float DEFAULT_WINDOW_WIDTH = 200f;

	private readonly IEnumerable<string> contents;
	private readonly Action<int> onSelect;
	private readonly float windowWidth;
	private readonly float spacingX;
	private readonly GUIStyle guiStyle;

	public SelectionContentPopup(IEnumerable<string> contents, Action<int> onSelect) : this(contents, onSelect, DEFAULT_WINDOW_WIDTH, null, 0f) {}
	public SelectionContentPopup(IEnumerable<string> contents, Action<int> onSelect, float windowWidth) : this(contents, onSelect, windowWidth, null, 0f) {}
	public SelectionContentPopup(IEnumerable<string> contents, Action<int> onSelect, GUIStyle guiStyle, float spacingX = 0f) : this(contents, onSelect, DEFAULT_WINDOW_WIDTH, guiStyle, spacingX) {}

	public SelectionContentPopup(IEnumerable<string> contents, Action<int> onSelect, float windowWidth, GUIStyle guiStyle, float spacingX = 0f)
	{
		this.contents = contents;
		this.onSelect = onSelect;
		this.windowWidth = windowWidth;
		this.guiStyle = guiStyle;
		this.spacingX = spacingX;
		if (this.guiStyle == null) {
			this.guiStyle = GUIStyle.none;
			this.guiStyle.alignment = TextAnchor.MiddleCenter;
		}
	}

	public override Vector2 GetWindowSize()
	{
		Vector2 windowSize = new Vector2(this.windowWidth, 0f);
		windowSize.y += this.contents.Count() * EditorGUIUtility.singleLineHeight;
		windowSize.y += this.contents.Count() * EditorGUIUtility.standardVerticalSpacing;
		return windowSize;
	}

	public override void OnGUI(Rect rect)
	{
		Rect buttonRect = rect;
		buttonRect.height = EditorGUIUtility.singleLineHeight;

		buttonRect.xMin += this.spacingX;
		buttonRect.xMax -= this.spacingX;

		int index = 0;
		foreach (string content in this.contents) {
			int selectIndex = index;
			if (GUI.Button(buttonRect, content, this.guiStyle)) {
				this.onSelect?.Invoke(selectIndex);
				this.editorWindow.Close();
			}
			buttonRect.y += EditorGUIUtility.singleLineHeight;
			buttonRect.y += EditorGUIUtility.standardVerticalSpacing;
			index++;
		}
	}
}