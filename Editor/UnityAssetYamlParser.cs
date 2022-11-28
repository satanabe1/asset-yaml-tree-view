using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if AYTV_YAMLDOTNET_11_2_OR_NEWER
using YamlDotNet.RepresentationModel;
#elif AYTV_VISUALSCRIPTING_1_6_0_OR_NEWER
using Unity.VisualScripting.YamlDotNet.RepresentationModel;
#else
#error require '"yamldotnet": "11.2.1"' or '"com.unity.visualscripting": "1.6.0"'
#endif
using UnityEditor;
using Object = UnityEngine.Object;

namespace AssetYamlTree
{
    internal static class UnityAssetYamlParser
    {
        private const string ObjectHeaderPrefix = "--- !u!";

        public static int GetClassIdByObjectHeader(string objectHeader)
        {
            return int.Parse(objectHeader.Substring(ObjectHeaderPrefix.Length).Split(' ')[0]);
        }

        public static string GetFileIdByObjectHeader(string objectHeader)
        {
            return objectHeader.Substring(ObjectHeaderPrefix.Length).Split(' ')[1].Substring(1);
        }

        public static IEnumerable<(string objectHeader, YamlDocument[] documents)> Parse(Object asset)
        {
            return Parse(AssetDatabase.GetAssetPath(asset));
        }

        public static IEnumerable<(string objectHeader, YamlDocument[] documents)> Parse(string yamlPath)
        {
            var lines = File.ReadLines(yamlPath);
            var sb = new StringBuilder();
            string header = null;
            foreach (var line in lines)
            {
                if (line.StartsWith(ObjectHeaderPrefix))
                {
                    if (header != null)
                    {
                        yield return (header, ParseText(sb.ToString()));
                        sb.Clear();
                    }

                    header = line;
                    continue;
                }

                if (header != null) sb.AppendLine(line);
            }

            if (header == null) yield break;
            yield return (header, ParseText(sb.ToString()));

            static YamlDocument[] ParseText(string text)
            {
                var stream = new YamlStream();
                using var sr = new StringReader(text);
                stream.Load(sr);
                return stream.Documents.ToArray();
            }
        }
    }
}
