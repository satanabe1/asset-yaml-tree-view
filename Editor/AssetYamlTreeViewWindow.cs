using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

internal class AssetYamlTreeViewWindow : EditorWindow
{
    [SerializeField]
    private TreeViewState _treeViewState;

    private AssetYamlTreeView _treeView;
    private SearchField _searchField;

    [MenuItem("Window/Asset Yaml Tree")]
    private static void Open() =>
        GetWindow<AssetYamlTreeViewWindow>(ObjectNames.NicifyVariableName(nameof(AssetYamlTreeViewWindow)));

    private void OnEnable()
    {
        _treeViewState ??= new TreeViewState();
        _treeView = new AssetYamlTreeView(_treeViewState);
        // var (elements, _) = AssetYamlTreeUtil.BuildElements(_unityTypeTable, 1, UnityAssetYamlParser.DebugAssetPath);
        // _treeView.Setup(elements);
        _searchField = new SearchField();
        _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
        ReloadTreeView();
    }

    [SerializeField]
    private AssetYamlTreeDisplayNameOption _displayNameOption;

    [SerializeField]
    private bool _showObjectHeaderIcon;

    [SerializeField]
    private string _searchString;

    [SerializeField]
    private string[] _selecteds;

    private void ReloadTreeView()
    {
        if (_selecteds == null || _selecteds.Length == 0) return;
        int id = 1;
        var merged = Enumerable.Empty<AssetYamlTreeElement>();
        foreach (var selected in _selecteds)
        {
            var path = AssetDatabase.GUIDToAssetPath(selected);
            var (elements, nextId) = AssetYamlTreeUtil.BuildElements(id, path);
            merged = merged.Concat(elements);
            id = nextId;
        }

        _treeView.Setup(merged.ToArray());
    }

    private void Update() => Repaint();

    private void OnGUI()
    {
        if (_selecteds == null || (Selection.assetGUIDs.Length > 0 && !_selecteds.SequenceEqual(Selection.assetGUIDs)))
        {
            _selecteds = Selection.assetGUIDs;
            ReloadTreeView();
        }

        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var classIdToClassName = _displayNameOption.HasFlag(AssetYamlTreeDisplayNameOption.ClassIdToClassName);
            var guidToAssetPath = _displayNameOption.HasFlag(AssetYamlTreeDisplayNameOption.GuidToAssetName);
            _showObjectHeaderIcon = EditorGUILayout.ToggleLeft("Icon", _showObjectHeaderIcon, GUILayout.Width(50f));
            classIdToClassName = EditorGUILayout.ToggleLeft("ClassIdToName", classIdToClassName, GUILayout.Width(110f));
            guidToAssetPath = EditorGUILayout.ToggleLeft("GuidToName", guidToAssetPath);
            GUILayout.FlexibleSpace();
            _searchString = _searchField.OnToolbarGUI(_treeView.searchString);
            _treeView.searchString = _searchString;
            _displayNameOption = AssetYamlTreeDisplayNameOption.Default;
            if (classIdToClassName) _displayNameOption |= AssetYamlTreeDisplayNameOption.ClassIdToClassName;
            if (guidToAssetPath) _displayNameOption |= AssetYamlTreeDisplayNameOption.GuidToAssetName;
        }

        _treeView.ShowObjectHeaderIcon = _showObjectHeaderIcon;
        _treeView.DisplayNameOption = _displayNameOption;
        if (EditorGUI.EndChangeCheck()) _treeView.Reload();

        if (_treeView.IsInitialized)
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            _treeView.OnGUI(rect);
        }
    }
}
