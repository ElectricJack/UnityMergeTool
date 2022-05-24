using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    public static class Helpers
    {
        public static bool ArraysEqual<T>(T[] a, T[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; ++i)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }
                
            return true;
        }
        public static float[] GetChildVector3(YamlMappingNode mappingNode, string keyName)
        {
            var keyNode = new YamlScalarNode(keyName);
            if (mappingNode.Children.ContainsKey(keyNode))
            {
                var vectorNode = (YamlMappingNode)mappingNode.Children[keyNode];
                return new float[3]
                {
                    float.Parse(((YamlScalarNode)vectorNode.Children["x"]).Value),
                    float.Parse(((YamlScalarNode)vectorNode.Children["y"]).Value),
                    float.Parse(((YamlScalarNode)vectorNode.Children["z"]).Value)
                };
            }
            return null;
        }
        public static float[] GetChildVector4(YamlMappingNode mappingNode, string keyName)
        {
            var keyNode = new YamlScalarNode(keyName);
            if (mappingNode.Children.ContainsKey(keyNode))
            {
                var vectorNode = (YamlMappingNode)mappingNode.Children[keyNode];
                return new float[4]
                {
                    float.Parse(((YamlScalarNode)vectorNode.Children["x"]).Value),
                    float.Parse(((YamlScalarNode)vectorNode.Children["y"]).Value),
                    float.Parse(((YamlScalarNode)vectorNode.Children["z"]).Value),
                    float.Parse(((YamlScalarNode)vectorNode.Children["w"]).Value)
                };
            }
            return null;
        }
        public static YamlMappingNode[] GetChildMapNodes(YamlMappingNode mappingNode, string keyName)
        {
            var keyNode = new YamlScalarNode(keyName);
            if (mappingNode.Children.ContainsKey(keyNode))
            {
                var sequenceNode = (YamlSequenceNode) mappingNode.Children[keyNode];
                var mappingNodes = new YamlMappingNode[sequenceNode.Children.Count];
                for (int i = 0; i < mappingNodes.Length; ++i)
                {
                    mappingNodes[i] = (YamlMappingNode) sequenceNode.Children[i];
                }

                return mappingNodes;
            }

            return null;
        }
        public static YamlMappingNode GetChildMapNode(YamlMappingNode mappingNode, string keyName)
        {
            var keyNode = new YamlScalarNode(keyName);
            if (mappingNode.Children.ContainsKey(keyNode))
                return (YamlMappingNode) mappingNode.Children[keyNode];

            return null;
        }
        public static string GetChildScalarValue(YamlMappingNode mappingNode, string keyName, bool isString = false)
        {
            if (mappingNode == null)
                return isString? "" : "0";
                
            var keyNode = new YamlScalarNode(keyName);
            if (mappingNode.Children.ContainsKey(keyNode))
                return ((YamlScalarNode) mappingNode.Children[keyNode]).Value;

            return isString? "" : "0";
        }

    }
}