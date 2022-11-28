using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AssetYamlTree.AssetYamlTreeUtil;

namespace AssetYamlTree
{
    [System.Flags]
    internal enum AssetYamlTreeDisplayNameOption
    {
        Default = 0,
        ClassIdToClassName = 1,
        GuidToAssetName = 2,
    }

    internal class AssetYamlTreeElement
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public List<AssetYamlTreeElement> Children { get; set; } = new List<AssetYamlTreeElement>();
        public Texture2D Icon { get; set; }
        public string AssetPath { get; set; }
        private readonly Dictionary<AssetYamlTreeDisplayNameOption, string> _nameCaches = new Dictionary<AssetYamlTreeDisplayNameOption, string>();

        public string GetDisplayName(AssetYamlTreeDisplayNameOption option)
        {
            if (_nameCaches.TryGetValue(option, out var name)) return name;
            name = CreateDisplayName(option);
            _nameCaches[option] = name;
            return name;
        }

        protected virtual string CreateDisplayName(AssetYamlTreeDisplayNameOption option)
        {
            if (HasFlags(option, AssetYamlTreeDisplayNameOption.GuidToAssetName) && Name == "guid" && string.IsNullOrEmpty(AssetPath) == false)
            {
                return Name + ": " + AssetPath;
            }
            else
            {
                if (string.IsNullOrEmpty(Value)) return Name;
                return Name + ": " + Value;
            }
        }
    }

    internal class AssetYamlObjectHeaderElement : AssetYamlTreeElement
    {
        // public string FileId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        protected override string CreateDisplayName(AssetYamlTreeDisplayNameOption option)
        {
            if (HasFlags(option, AssetYamlTreeDisplayNameOption.ClassIdToClassName) == false)
            {
                return base.CreateDisplayName(option);
            }

            var name = ClassName;
            if (ClassId == (int)AssetYamlTreeUtil.ClassId.MonoBehaviour)
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(GetValue(this, "MonoBehaviour/m_Script/guid")));
                if (asset != null) name = asset.GetClass()?.Name ?? name;
            }

            return Name.Replace($"!u!{ClassId} ", $"{name} ");
        }
    }
}
