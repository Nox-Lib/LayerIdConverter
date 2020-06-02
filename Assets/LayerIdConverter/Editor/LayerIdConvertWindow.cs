using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace ConvertLayerId
{
	public class LayerIdConvertWindow : EditorWindow
	{
		[MenuItem("Tools/LayerIdConverter")]
		private static void Open()
		{
			LayerIdConvertWindow window = GetWindow<LayerIdConvertWindow>();
			window.titleContent = new GUIContent("LayerIdConverter");
			window.minSize = new Vector2(300f, 300f);
		}


		private const int TARGET_SCENE	= 1;
		private const int TARGET_PREFAB	= 1 << 1;

		private List<LayerIdConverterBase> converters;

		private Dictionary<int, string> layerDefines;
		private int targetFlags;
		private List<string> allPathList;
		private List<string> targetPathList;
		private string matchPattern;
		private string matchPatternError;
		private string ignorePattern;
		private string ignorePatternError;
		private ConvertData convertSettings;
		private ReorderableList drawConvertPatterns;
		private Vector2 scrollPosition;


		private void OnEnable()
		{
			this.CreateLayerDefines();

			if (this.targetFlags == 0) {
				this.targetFlags = TARGET_SCENE | TARGET_PREFAB;
			}
			this.Filter(true);

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
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			int beforeTargetFlags = this.targetFlags;

			bool isUp = (this.targetFlags & TARGET_SCENE) >= 1;
			isUp = EditorGUILayout.ToggleLeft("Scene", isUp, GUILayout.Width(60f));
			this.targetFlags = isUp ? this.targetFlags | TARGET_SCENE : this.targetFlags & ~TARGET_SCENE;

			isUp = (this.targetFlags & TARGET_PREFAB) >= 1;
			isUp = EditorGUILayout.ToggleLeft("Prefab", isUp, GUILayout.Width(60f));
			this.targetFlags = isUp ? this.targetFlags | TARGET_PREFAB : this.targetFlags & ~TARGET_PREFAB;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			bool isChangeTargets = beforeTargetFlags != this.targetFlags;
			if (this.targetFlags <= 0) {
				this.targetFlags = beforeTargetFlags;
				this.Repaint();
				return;
			}

			this.Filter(isChangeTargets);

			EditorGUILayout.BeginVertical(GUI.skin.box);

			GUILayout.Space(2f);
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
			EditorGUILayout.LabelField("Change Children", GUILayout.Width(130f));
			this.convertSettings.isChangeChildren = EditorGUILayout.Toggle(this.convertSettings.isChangeChildren);
			EditorGUILayout.EndHorizontal();
			GUI.enabled = true;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Stop Convert On Error", GUILayout.Width(130f));
			this.convertSettings.isStopConvertOnError = EditorGUILayout.Toggle(this.convertSettings.isStopConvertOnError);
			EditorGUILayout.EndHorizontal();

			if (this.convertSettings.processingMode == ProcessingMode.CameraOnly) {
				this.convertSettings.cameraOption.isEnabled = true;
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Camera Culling Mask", GUILayout.Width(130f));
			this.convertSettings.cameraOption.isEnabled = EditorGUILayout.Toggle(this.convertSettings.cameraOption.isEnabled);
			EditorGUILayout.EndHorizontal();

			GUI.enabled = this.convertSettings.cameraOption.isEnabled;
			EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Leave Old Layer Id", GUILayout.Width(130f));
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


		private void Filter(bool isChangeTargets)
		{
			if (isChangeTargets) {
				this.converters = new List<LayerIdConverterBase>();
				this.allPathList = new List<string>();

				string[] searchInFolders = { "Assets" };

				if ((this.targetFlags & TARGET_SCENE) >= 1) {
					LayerIdConverterScene sceneConverter = new LayerIdConverterScene();
					this.converters.Add(sceneConverter);
					string filter = string.Format("t:{0}", sceneConverter.AssetType);
					this.allPathList.AddRange(AssetDatabase.FindAssets(filter, searchInFolders).Select(AssetDatabase.GUIDToAssetPath));
				}
				if ((this.targetFlags & TARGET_PREFAB) >= 1) {
					LayerIdConverterPrefab prefabConverter = new LayerIdConverterPrefab();
					this.converters.Add(prefabConverter);
					string filter = string.Format("t:{0}", prefabConverter.AssetType);
					this.allPathList.AddRange(AssetDatabase.FindAssets(filter, searchInFolders).Select(AssetDatabase.GUIDToAssetPath));
				}
			}

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
				EditorUtility.DisplayDialog("Error", "絞り込みのパターンにエラーがあるため実行できません。", "OK");
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
				foreach (LayerIdConverterBase converter in this.converters) {
					converter.Execute(new List<string>(this.targetPathList), this.convertSettings.Clone());
				}
			}
		}
	}
}