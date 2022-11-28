using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal class AssetYamlTreeView : TreeView
{
    private AssetYamlTreeElement[] _baseElements;
    public bool IsInitialized => _baseElements != null;

    public AssetYamlTreeDisplayNameOption DisplayNameOption { get; set; }
    public bool ShowObjectHeaderIcon { get; set; }

    public AssetYamlTreeView(TreeViewState treeViewState) : base(treeViewState)
    {
    }

    public void Setup(AssetYamlTreeElement[] baseElements)
    {
        _baseElements = baseElements;
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
        foreach (var baseElement in _baseElements)
        {
            var baseItem = CreateTreeViewItem(baseElement);
            root.AddChild(baseItem);
            AddChildrenRecursive(baseElement, baseItem);
        }

        SetupDepthsFromParentsAndChildren(root);
        return root;
    }

    private static void AddChildrenRecursive(AssetYamlTreeElement model, TreeViewItem item)
    {
        foreach (var childModel in model.Children)
        {
            var childItem = CreateTreeViewItem(childModel);
            item.AddChild(childItem);
            AddChildrenRecursive(childModel, childItem);
        }
    }

    private static TreeViewItem CreateTreeViewItem(AssetYamlTreeElement model)
    {
        return new AssetYamlTreeViewItem(model);
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        if (!(args.item is AssetYamlTreeViewItem item))
        {
            base.RowGUI(args);
            return;
        }

        item.DisplayNameOption = DisplayNameOption;
        item.ShowIcon = ShowObjectHeaderIcon;

        base.RowGUI(args);
    }

    protected override void DoubleClickedItem(int id)
    {
        base.DoubleClickedItem(id);
        var first = FindRows(new List<int> { id }).FirstOrDefault() as AssetYamlTreeViewItem;
        if (first?.Data?.AssetPath == null) return;
        AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(first.Data.AssetPath));
    }

    protected override void SingleClickedItem(int id)
    {
        base.SingleClickedItem(id);
        if (hasSearch) SetExpandedParent(FindRows(new[] { id }));
    }

    private void SetExpandedParent(IList<TreeViewItem> rows)
    {
        var expands = new List<int>(GetExpanded());
        for (int i = 0, count = rows.Count; i < count; i++)
        {
            var row = rows[i];
            var p = row;
            while (p.parent != null)
            {
                expands.Add(p.parent.id);
                p = p.parent;
            }
        }

        SetExpanded(expands);
    }

    protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
    {
        var assetYamlTreeViewItem = item as AssetYamlTreeViewItem;
        if (assetYamlTreeViewItem == null) return base.DoesItemMatchSearch(item, search);
        if (Hit(assetYamlTreeViewItem.Data.Name, search)) return true;
        if (Hit(assetYamlTreeViewItem.Data.Value, search)) return true;
        if (Hit(assetYamlTreeViewItem.Data.AssetPath, search)) return true;
        var headerObj = assetYamlTreeViewItem.Data as AssetYamlObjectHeaderElement;
        if (headerObj == null) return base.DoesItemMatchSearch(item, search);
        if (Hit(headerObj.ClassName, search)) return true;
        if (Hit(headerObj.ClassId.ToString(), search)) return true;
        return base.DoesItemMatchSearch(item, search);

        static bool Hit(string str, string search)
            => str != null && str.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    protected override void ContextClickedItem(int id)
    {
        var ev = Event.current;
        ev.Use();

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Copy Text"), false, () =>
        {
            var rows = FindRows(GetSelection());
            var sb = new StringBuilder();
            foreach (var treeViewItem in rows) sb.AppendLine(treeViewItem.displayName);
            GUIUtility.systemCopyBuffer = sb.ToString();
        });
        menu.ShowAsContext();
    }
}
