using UnityEditor.IMGUI.Controls;
using UnityEngine;

internal class AssetYamlTreeViewItem : TreeViewItem
{
    public AssetYamlTreeElement Data { get; }

    public override int id
    {
        get => Data.Id;
        set => Data.Id = value;
    }

    public override string displayName
    {
        get => Data.GetDisplayName(DisplayNameOption);
        set => Data.Name = value;
    }

    public override Texture2D icon
    {
        get => ShowIcon ? Data.Icon : null;
        set => Data.Icon = value;
    }

    public AssetYamlTreeDisplayNameOption DisplayNameOption { get; set; }
    public bool ShowIcon { get; set; }

    public AssetYamlTreeViewItem(AssetYamlTreeElement data)
    {
        Data = data;
    }
}
