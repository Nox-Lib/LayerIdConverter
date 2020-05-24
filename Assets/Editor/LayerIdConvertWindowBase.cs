using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public abstract class LayerIdConvertWindowBase : EditorWindow
{
	protected abstract string AssetType { get; }
	protected abstract void Execute(List<string> pathList, ConvertData convertSettings, bool isChangeChildren);

	protected enum ProcessingMode : int {
		Normal,
		CameraOnly
	}


	[Serializable]
	protected class ConvertData
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
		public CameraOption cameraOption = new CameraOption();

		public ConvertData Clone()
		{
			return new ConvertData {
				processingMode = this.processingMode,
				patterns = this.patterns.Select(x => x.Clone()).ToList(),
				cameraOption = this.cameraOption.Clone()
			};
		}
	}


	private Dictionary<int, string> layerDefines;
	private List<string> allPathList;
	private List<string> targetPathList;
	private string matchPattern;
	private string matchPatternError;
	private string ignorePattern;
	private string ignorePatternError;
	private bool isChangeChildren = true;
	private ConvertData convertSettings;
	private ReorderableList drawConvertPatterns;
	private Vector2 scrollPosition;


	private void OnEnable()
	{
		this.allPathList = AssetDatabase.FindAssets(string.Format("t:{0}", this.AssetType), new string[] { "Assets" })
			.Select(AssetDatabase.GUIDToAssetPath)
			.ToList();

		this.CreateLayerDefines();

		this.convertSettings = this.convertSettings ?? new ConvertData();

		this.drawConvertPatterns = new ReorderableList(this.convertSettings.patterns, typeof(ConvertData), true, true, true, true);
		this.drawConvertPatterns.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, "Layer Convert Patterns"); };
		this.drawConvertPatterns.onAddCallback += (list) => { this.convertSettings.patterns.Add(new ConvertData.Pattern()); };
		this.drawConvertPatterns.onRemoveCallback += (list) => { this.convertSettings.patterns.RemoveAt(list.index); };
		this.drawConvertPatterns.drawElementCallback += this.OnDrawElementConvertPattern;
	}


	private void CreateLayerDefines()
	{
		this.layerDefines = new Dictionary<int, string>();

		UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
		if (asset == null || asset.Length <= 0) {
			return;
		}
		SerializedObject serializedObject = new SerializedObject(asset[0]);
		SerializedProperty layers = serializedObject.FindProperty("layers");

		for (int i = 0; i < layers.arraySize; i++) {
			string layerName = layers.GetArrayElementAtIndex(i).stringValue;
			if (!string.IsNullOrEmpty(layerName)) {
				this.layerDefines.Add(i, layerName);
			}
		}
	}


	private void OnDrawElementConvertPattern(Rect rect, int index, bool isActive, bool isFocused)
	{
		GUILayout.BeginHorizontal();

		List<ConvertData.Pattern> convertPatterns = this.convertSettings.patterns;
		float x, w;

		x = rect.x;
		w = 45f;
		EditorGUI.LabelField(new Rect(x, rect.y, w, rect.height), "Old Id");

		x += w;
		w = 40f;
		convertPatterns[index].oldLayerId = EditorGUI.IntField(new Rect(x, rect.y, w, rect.height - 2f), convertPatterns[index].oldLayerId);

		x += w + 2f;
		w = 12f;
		if (EditorGUI.DropdownButton(new Rect(x, rect.y, w, rect.height), new GUIContent(""), FocusType.Passive, new GUIStyle("ToolbarDropDown"))) {
			Rect popupRect = new Rect(x + w, rect.y, 0f, 0f);
			SelectionContentPopup popup = new SelectionContentPopup(
				this.layerDefines.Values,
				selectIndex => { convertPatterns[index].oldLayerId = this.layerDefines.Keys.ElementAt(selectIndex); },
				150f,
				new GUIStyle("toolbarbutton")
			);
			PopupWindow.Show(popupRect, popup);
		}

		x += w + 15f;
		w = 45f;
		EditorGUI.LabelField(new Rect(x, rect.y, w, rect.height), "New Id");

		x += w;
		w = 40f;
		convertPatterns[index].newLayerId = EditorGUI.IntField(new Rect(x, rect.y, w, rect.height - 2f), convertPatterns[index].newLayerId);

		x += w + 2f;
		w = 12f;
		if (EditorGUI.DropdownButton(new Rect(x, rect.y, w, rect.height), new GUIContent(""), FocusType.Passive, new GUIStyle("ToolbarDropDown"))) {
			Rect popupRect = new Rect(x + w, rect.y, 0f, 0f);
			SelectionContentPopup popup = new SelectionContentPopup(
				this.layerDefines.Values,
				selectIndex => { convertPatterns[index].newLayerId = this.layerDefines.Keys.ElementAt(selectIndex); },
				150f,
				new GUIStyle("toolbarbutton")
			);
			PopupWindow.Show(popupRect, popup);
		}

		if (!convertPatterns[index].IsValid) {
			x += w + 15f;
			w = 150f;
			EditorGUI.LabelField(new Rect(x, rect.y, w, rect.height), "Invalid LayerId!", new GUIStyle("ErrorLabel"));
		}

		GUILayout.EndHorizontal();
	}


	private void OnGUI()
	{
		this.Filter();

		EditorGUILayout.BeginVertical(GUI.skin.box);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Processing Mode", GUILayout.Width(96f));
		this.convertSettings.processingMode = (ProcessingMode)EditorGUILayout.EnumPopup(this.convertSettings.processingMode);
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(5f);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Match Pattern", GUILayout.Width(83f));
		EditorGUILayout.LabelField(":", GUILayout.Width(9f));
		this.matchPattern = EditorGUILayout.TextField(this.matchPattern);
		EditorGUILayout.EndHorizontal();

		if (!string.IsNullOrEmpty(this.matchPatternError)) {
			EditorGUILayout.LabelField(this.matchPatternError, new GUIStyle("ErrorLabel"));
			GUILayout.Space(5f);
		}

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Ignore Pattern", GUILayout.Width(83f));
		EditorGUILayout.LabelField(":", GUILayout.Width(9f));
		this.ignorePattern = EditorGUILayout.TextField(this.ignorePattern);
		EditorGUILayout.EndHorizontal();

		if (!string.IsNullOrEmpty(this.ignorePatternError)) {
			EditorGUILayout.LabelField(this.ignorePatternError, new GUIStyle("ErrorLabel"));
			GUILayout.Space(5f);
		}

		GUILayout.Space(7f);
		this.drawConvertPatterns.DoLayoutList();

		GUI.enabled = this.convertSettings.processingMode == ProcessingMode.Normal;
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Change Children", GUILayout.Width(120f));
		this.isChangeChildren = EditorGUILayout.Toggle(this.isChangeChildren);
		EditorGUILayout.EndHorizontal();
		GUI.enabled = true;

		if (this.convertSettings.processingMode == ProcessingMode.CameraOnly) {
			this.convertSettings.cameraOption.isEnabled = true;
		}
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Camera Culling Mask", GUILayout.Width(120f));
		this.convertSettings.cameraOption.isEnabled = EditorGUILayout.Toggle(this.convertSettings.cameraOption.isEnabled);
		EditorGUILayout.EndHorizontal();

		GUI.enabled = this.convertSettings.cameraOption.isEnabled;
		EditorGUI.indentLevel++;
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Leave Old Layer", GUILayout.Width(120f));
		this.convertSettings.cameraOption.isLeaveOldCullingMask = EditorGUILayout.Toggle(this.convertSettings.cameraOption.isLeaveOldCullingMask);
		EditorGUILayout.EndHorizontal();
		EditorGUI.indentLevel--;
		GUI.enabled = true;

		if (this.convertSettings.cameraOption.isEnabled) {
			EditorGUILayout.HelpBox("Cameras with culling mask 'Everything' will not be processed.", MessageType.Info);
		}

		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical();
		GUILayout.Space(5f);
		GUILayout.Label(string.Format("Targets ({0}) :", this.targetPathList.Count));
		EditorGUILayout.EndVertical();

		this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
		EditorGUILayout.BeginVertical();

		foreach (string target in this.targetPathList) {
			EditorGUILayout.SelectableLabel(target, GUILayout.Height(EditorGUIUtility.singleLineHeight));
		}

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();

		EditorGUILayout.BeginVertical();
		if (GUILayout.Button("Execute")) {
			this.Execute();
		}
		GUILayout.Space(5f);
		EditorGUILayout.EndVertical();
	}


	private void Filter()
	{
		this.targetPathList = new List<string>(this.allPathList);

		this.matchPatternError = string.Empty;
		if (!string.IsNullOrEmpty(this.matchPattern)) {
			try {
				Regex regex = new Regex(this.matchPattern);
				this.targetPathList = this.targetPathList.Where(x => regex.IsMatch(x, 0)).ToList();
			}
			catch (Exception e) {
				this.matchPatternError = e.Message;
				this.targetPathList = new List<string>();
			}
		}

		this.ignorePatternError = string.Empty;
		if (!string.IsNullOrEmpty(this.ignorePattern)) {
			try {
				Regex regex = new Regex(this.ignorePattern);
				this.targetPathList = this.targetPathList.Where(x => !regex.IsMatch(x, 0)).ToList();
			}
			catch (Exception e) {
				this.ignorePatternError = e.Message;
			}
		}
	}


	private void Execute()
	{
		List<ConvertData.Pattern> convertPatterns = this.convertSettings.patterns;

		if (!string.IsNullOrEmpty(this.matchPatternError) || !string.IsNullOrEmpty(this.ignorePatternError)) {
			EditorUtility.DisplayDialog("Error", "絞り込みのマッチパターンにエラーがあるため実行できません。", "OK");
			return;
		}
		if (convertPatterns.Count <= 0) {
			EditorUtility.DisplayDialog("Error", "レイヤーIdの変換パターンを設定してください。", "OK");
			return;
		}
		if (convertPatterns.Any(x => !x.IsValid)) {
			EditorUtility.DisplayDialog("Error", "無効なレイヤーIdが設定されているため実行できません。", "OK");
			return;
		}

		bool isDuplication = false;
		isDuplication |= convertPatterns.Any(x => x.oldLayerId == x.newLayerId);
		isDuplication |= convertPatterns.Select(x => x.oldLayerId).Distinct().Count() < convertPatterns.Count;
		isDuplication |= convertPatterns.Select(x => x.newLayerId).Distinct().Count() < convertPatterns.Count;
		if (isDuplication) {
			EditorUtility.DisplayDialog("Error", "重複しているレイヤーIdの設定があるため実行できません。", "OK");
			return;
		}

		bool isExecute = EditorUtility.DisplayDialog(
			"実行確認",
			string.Format("{0}件を対象に処理を開始します。よろしいですか？", this.targetPathList.Count),
			"OK",
			"Cancel"
		);
		if (isExecute) {
			this.Execute(new List<string>(this.targetPathList), this.convertSettings.Clone(), this.isChangeChildren);
		}
	}


	protected List<string> ChangeLayer(string layerName, GameObject gameObject, ConvertData convertSettings)
	{
		List<string> result = new List<string>();

		if (convertSettings.processingMode == ProcessingMode.Normal) {
			ConvertData.Pattern convertPattern = convertSettings.patterns.FirstOrDefault(x => gameObject.layer == x.oldLayerId);
			if (convertPattern != null) {
				gameObject.layer = convertPattern.newLayerId;
				EditorUtility.SetDirty(gameObject);
				result.Add(string.Format("{0} ({1} => {2})", layerName, convertPattern.oldLayerId, convertPattern.newLayerId));
			}
		}
		if (convertSettings.cameraOption.isEnabled) {
			Camera camera = gameObject.GetComponent<Camera>();
			if (camera != null && camera.cullingMask != -1) {
				foreach (ConvertData.Pattern convertPattern in convertSettings.patterns) {
					int beforeCullingMask = camera.cullingMask;
					int oldMask = 1 << convertPattern.oldLayerId;
					if ((camera.cullingMask & oldMask) >= 1 && !convertSettings.cameraOption.isLeaveOldCullingMask) {
						camera.cullingMask &= ~oldMask;
					}
					int newMask = 1 << convertPattern.newLayerId;
					if ((camera.cullingMask & newMask) == 0) {
						camera.cullingMask |= newMask;
					}
					if (beforeCullingMask == camera.cullingMask) {
						continue;
					}
					result.Add(string.Format(
						"{0} (Camera Culling Mask {1} => {2}), Leave Old layer = {3}",
						layerName,
						convertPattern.oldLayerId,
						convertPattern.newLayerId,
						convertSettings.cameraOption.isLeaveOldCullingMask
					));
					EditorUtility.SetDirty(camera);
				}
			}
		}
		return result;
	}
}