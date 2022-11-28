using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if AYTV_YAMLDOTNET_11_2_OR_NEWER
using YamlDotNet.RepresentationModel;
#elif AYTV_VISUALSCRIPTING_1_6_0_OR_NEWER
using Unity.VisualScripting.YamlDotNet.RepresentationModel;
#else
#error require '"yamldotnet": "11.2.1"' or '"com.unity.visualscripting": "1.6.0"'
#endif
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace AssetYamlTree
{
    internal static class AssetYamlTreeUtil
    {
        public enum ClassId
        {
            MonoBehaviour = 114,
            PrefabInstance = 1001,
        }

        private static int _currentId;

        public static bool HasFlags(AssetYamlTreeDisplayNameOption option, AssetYamlTreeDisplayNameOption flag)
        {
            return (option & flag) == flag;
        }

        public static string GetValue(AssetYamlTreeElement element, string path)
        {
            return GetValue(element, path, x => x.Value);
        }

        public static Texture2D GetIcon(AssetYamlTreeElement element, string path)
        {
            return GetValue(element, path, x => x.Icon);
        }

        private static T GetValue<T>(AssetYamlTreeElement element, string path, System.Func<AssetYamlTreeElement, T> getValue)
        {
            if (element == null || path == null) return default;
            return GetValueRecursive(element, path.Split('/').ToList(), getValue);

            static T GetValueRecursive(AssetYamlTreeElement element, List<string> pathParts, System.Func<AssetYamlTreeElement, T> getValue)
            {
                var first = pathParts.First();
                pathParts.RemoveAt(0);
                if (element.Children == null) return default;
                foreach (var child in element.Children)
                {
                    if (child.Name != first) continue;
                    return pathParts.Count == 0 ? getValue.Invoke(child) : GetValueRecursive(child, pathParts, getValue);
                }

                return default;
            }
        }

        public static (AssetYamlTreeElement[] elements, int nextId) BuildElements(int startElementId, string path)
        {
            _currentId = startElementId;
            var root = new AssetYamlTreeElement
            {
                Id = _currentId++,
                Name = Path.GetFileName(path),
                Icon = GetIcon(path),
                AssetPath = path,
            };
            var objectRoots = new List<AssetYamlTreeElement>();
            foreach (var (objectHeader, documents) in UnityAssetYamlParser.Parse(path))
            {
                var classId = UnityAssetYamlParser.GetClassIdByObjectHeader(objectHeader);
                var icon = GetMiniTypeThumbnailFromClassID(classId);
                var objectHeaderElement = new AssetYamlObjectHeaderElement
                {
                    Id = _currentId++,
                    Name = objectHeader,
                    // FileId = UnityAssetYamlParser.GetFileIdByObjectHeader(objectHeader),
                    ClassId = classId,
                    ClassName = GetTypeNameByPersistentTypeID(classId),
                    Icon = icon,
                };

                foreach (var document in documents)
                {
                    objectHeaderElement.Children.AddRange(YamlNodeToTreeElement(
                        objectHeaderElement, document.RootNode));
                }

                // classId:1001(PrefabInstance)の時はm_SourcePrefab.guidからAssetPreview.GetAssetPreviewFromGUID()を実行する
                if (classId == (int)ClassId.PrefabInstance)
                {
                    var guid = objectHeaderElement
                        .Children?.FirstOrDefault(x => x.Name == "PrefabInstance")
                        ?.Children?.FirstOrDefault(x => x.Name == "m_SourcePrefab")
                        ?.Children?.FirstOrDefault(x => x.Name == "guid")?.Value;
                    objectHeaderElement.Icon = GetAssetPreviewFromGUID(guid) ?? icon;
                }
                else if (classId == (int)ClassId.MonoBehaviour)
                {
                    objectHeaderElement.Icon = GetIcon(objectHeaderElement, "MonoBehaviour/m_Script");
                }


                objectRoots.Add(objectHeaderElement);
            }

            root.Children = objectRoots;

            return (new[] { root }, _currentId);
        }

        private static Texture2D GetMiniTypeThumbnailFromClassID(int classId)
        {
            return typeof(AssetPreview).InvokeMember("GetMiniTypeThumbnailFromClassID",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null,
                new object[] { classId }) as Texture2D;
        }

        private static Texture2D GetAssetPreviewFromGUID(string guid)
        {
            return typeof(AssetPreview).InvokeMember("GetAssetPreviewFromGUID",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null,
                new object[] { guid }) as Texture2D;
        }

        private static Texture2D GetIcon(System.Type type) => type == null ? null : AssetPreview.GetMiniTypeThumbnail(type);

        private static Texture2D GetIcon(string assetPath)
        {
            var cachedIcon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
            if (cachedIcon != null) return cachedIcon;

            if (AssetDatabase.IsValidFolder(assetPath)) return EditorGUIUtility.FindTexture(EditorResources.folderIconName);

            var tex = UnityEditorInternal.InternalEditorUtility.FindIconForFile(assetPath);
            if (tex != null) return tex;

            if (AssetImporter.GetAtPath(assetPath) is PluginImporter) return EditorGUIUtility.FindTexture("Assembly Icon");

            return GetIcon(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        private static IEnumerable<AssetYamlTreeElement> YamlNodeToTreeElement(AssetYamlTreeElement parentElement, YamlNode node)
        {
            if (node is YamlScalarNode scalarNode)
            {
                yield return new AssetYamlTreeElement
                {
                    Id = _currentId++,
                    Name = scalarNode.Value,
                };
                yield break;
            }

            if (node is YamlSequenceNode sequenceNode)
            {
                foreach (var sequenceNodeChild in sequenceNode.Children)
                {
                    foreach (var seq in YamlNodeToTreeElement(parentElement, sequenceNodeChild))
                    {
                        yield return seq;
                    }
                }

                yield break;
            }

            if (node is YamlMappingNode mappingNode)
            {
                foreach (var n in mappingNode.Children)
                {
                    var key = (YamlScalarNode)n.Key;
                    var value = n.Value;
                    var parent = YamlNodeToTreeElement(parentElement, key).First();
                    if (value is YamlScalarNode c)
                    {
                        parent.Value = c.Value;
                    }
                    else
                    {
                        var child = YamlNodeToTreeElement(parent, value);
                        parent.Children = child.ToList();
                    }

                    if (key.Value == "guid" && value is YamlScalarNode valueNode)
                    {
                        parentElement.Icon = GetAssetPreviewFromGUID(valueNode.Value);
                        var path = AssetDatabase.GUIDToAssetPath(valueNode.Value);
                        parentElement.AssetPath = path;
                        parent.AssetPath = path;
                    }

                    yield return parent;
                }
            }
        }

        private static string GetTypeNameByPersistentTypeID(int id)
        {
            const BindingFlags flags = BindingFlags.Public
                                       | BindingFlags.Static
                                       | BindingFlags.Instance
                                       | BindingFlags.InvokeMethod
                                       | BindingFlags.GetProperty;
            var assembly = Assembly.GetAssembly(typeof(MonoScript));
            var _unityType = assembly.GetType("UnityEditor.UnityType");
            var _findTypeByPersistentTypeID = _unityType.GetMethod("FindTypeByPersistentTypeID", flags);
            var _nameProperty = _unityType.GetProperty("name", flags);
            var typeInstance = _findTypeByPersistentTypeID?.Invoke(null, new object[] { id });
            return typeInstance != null ? _nameProperty?.GetValue(typeInstance) as string : null;
        }
    }
}
