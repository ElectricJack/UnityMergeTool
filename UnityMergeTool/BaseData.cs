using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    
    abstract class BaseData
    {
        public string                                         typeName;
        public DiffableProperty<ulong>                        fileId                      = new DiffableProperty<ulong>(); // Assigned from anchor id of root node in document

        public DiffableProperty<int>                          objectHideFlags             = new DiffableProperty<int>();
        public DiffableProperty<int>                          serializedVersion           = new DiffableProperty<int>();
        
        public Dictionary<string, DiffableProperty<YamlNode>> additionalData = new Dictionary<string, DiffableProperty<YamlNode>>();
        protected List<string>                                _existingKeys = new List<string>();
        
        protected bool _wasModified;
        public    bool WasModified => _wasModified;
        public abstract string ScenePath { get; }
        public abstract bool Diff(object previous);
        public abstract void Merge(object thiers, ref string conflictReport, bool takeTheirs = true);
        
        
        protected void LoadBase(YamlMappingNode mappingNode, ulong fileId)
        {
            this.fileId.value = fileId;
            objectHideFlags.value             = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_ObjectHideFlags"));
            serializedVersion.value           = int.Parse(Helpers.GetChildScalarValue(mappingNode, "serializedVersion"));
            
            _existingKeys.Clear();
            _existingKeys.Add("m_ObjectHideFlags");
            _existingKeys.Add("serializedVersion");
        }
        protected void LoadYamlProperties(YamlMappingNode mappingNode)
        {
            foreach (var item in mappingNode)
            {
                var keyName = ((YamlScalarNode) item.Key).Value;
                if (_existingKeys.Contains(keyName))
                    continue;

                additionalData.Add(keyName,  new DiffableProperty<YamlNode>() { value = item.Value });
            }
        }
        protected bool DiffBase(BaseSceneData previous)
        {
            fileId.valueChanged                      = fileId.value                      != previous.fileId.value;
            objectHideFlags.valueChanged             = objectHideFlags.value             != previous.objectHideFlags.value;
            serializedVersion.valueChanged           = serializedVersion.value           != previous.serializedVersion.value;

            _wasModified = fileId.valueChanged || objectHideFlags.valueChanged || serializedVersion.valueChanged;
            return WasModified;
        }
        
        protected void DiffYamlProperties(object previousObj)
        {
            MonoBehaviorData previous = previousObj as MonoBehaviorData;
            
            foreach (var pair in additionalData)
            {
                var thisNode = pair.Value.value;
                var prevNode = previous.additionalData[pair.Key].value;

                if (prevNode.Tag != thisNode.Tag || prevNode.NodeType != thisNode.NodeType)
                {
                    pair.Value.valueChanged = true;
                    _wasModified = true;
                }
                else
                {
                    pair.Value.valueChanged = DiffNodes(prevNode, thisNode);
                    _wasModified = _wasModified || pair.Value.valueChanged;
                }
            }
        }
        
        protected void MergeYamlProperties(object thiersObj, List<string> conflictReportLines, bool takeTheirs = true)
        {
            var theirs = thiersObj as MonoBehaviorData;
            
            var toReplace = new List<KeyValuePair<string, DiffableProperty<YamlNode>>>();
            foreach (var pair in additionalData)
            {
                var thisNode = pair.Value;
                var theirNode = theirs.additionalData[pair.Key];

                if (thisNode.valueChanged && theirNode.valueChanged)
                {
                    // If the two nodes are not different, this is not actually a conflict. Change was 
                    //  made in both places
                    if (!DiffNodes(thisNode.value, theirNode.value))
                        continue;

                    conflictReportLines.Add("Property: " + pair.Key + " Thiers: " + theirNode.value + " Mine: " + thisNode.value);
                    continue;
                }
                    
                // Save off any merged in changes
                if (theirNode.valueChanged)
                    toReplace.Add(new KeyValuePair<string, DiffableProperty<YamlNode>>(pair.Key, theirNode));
            }

            // Apply merged in changes
            foreach (var pair in toReplace)
            {
                additionalData[pair.Key] = pair.Value;
            }
        }

            
        protected T MergeProperties<T>( string propertyName, DiffableProperty<T> mine, DiffableProperty<T> theirs, List<string> propConflict, bool takeThiers = true)
        {
            // If there was a conflict between these properties, then report it and 
            if (mine.valueChanged && theirs.valueChanged)
            {
                // Its possible that both sides were changed to the same new value, if so, this is not a conflict
                if (mine.value.Equals(theirs.value)) {
                    return mine.value;
                }
                    
                propConflict.Add("Property: " + propertyName + " Thiers: " + theirs.value + " Mine: " + mine.value);
                return takeThiers ? theirs.value : mine.value;
            }

            // If thiers was changed take it, otherwise always take mine
            if (theirs.valueChanged)
                return theirs.value;
                
            return mine.value;
        }
        protected T[] MergePropArray<T>(string propertyName, DiffableProperty<T[]> mine, DiffableProperty<T[]> theirs, List<string> propConflict, bool takeThiers = true)
        {
            // If there was a conflict between these properties, then report it and 
            if (mine.valueChanged && theirs.valueChanged)
            {
                // Its possible that both sides were changed to the same new value, if so, this is not a conflict
                if (Helpers.ArraysEqual(mine.value, theirs.value)) {
                    return mine.value;
                }

                var mineStr = "{ ";
                var theirsStr = "{ ";
                for (int i = 0; i < mine.value.Length; ++i)
                {
                    if (i > 0)
                    {
                        mineStr += ", ";
                        theirsStr += ", ";
                    }
                    mineStr += mine.value[i];
                    theirsStr += theirs.value[i];
                }

                mineStr += " }";
                theirsStr += " }";
                    
                propConflict.Add("Property: " + propertyName + " Thiers: " + theirsStr + " Mine: " + mineStr);

                return takeThiers ? theirs.value : mine.value;
            }

            // If thiers was changed take it, otherwise always take mine
            if (theirs.valueChanged)
                return theirs.value;
                
            return mine.value;
        }
        
        

        private bool DiffNodes(YamlNode prevNode, YamlNode thisNode)
        {
            if (thisNode.NodeType == YamlNodeType.Mapping)  return DiffMapping(prevNode, thisNode);
            if (thisNode.NodeType == YamlNodeType.Scalar)   return DiffScalar(prevNode, thisNode);
            if (thisNode.NodeType == YamlNodeType.Sequence) return DiffSequence(prevNode, thisNode);
            return false;
        }
        private bool DiffMapping(YamlNode prevNode, YamlNode thisNode)
        {
            var thisMapping = (YamlMappingNode) thisNode;
            var prevMapping = (YamlMappingNode) prevNode;

            if (thisMapping.Children.Count != prevMapping.Children.Count)
                return true;

            foreach (var thisKey in thisMapping.Children.Keys)
            {
                if (DiffNodes(prevMapping[thisKey], thisMapping[thisKey]))
                    return true;
            }

            return false;
        }
        private bool DiffScalar(YamlNode prevNode, YamlNode thisNode)
        {
            return !((YamlScalarNode) prevNode).Value.Equals(((YamlScalarNode) thisNode).Value);
        }
        private bool DiffSequence(YamlNode prevNode, YamlNode thisNode)
        {
            var thisSequence = (YamlSequenceNode) thisNode;
            var prevSequence = (YamlSequenceNode) prevNode;
                
            if (thisSequence.Children.Count != prevSequence.Children.Count)
                return true;

            for (int i = 0; i < thisSequence.Children.Count; ++i)
            {
                if (DiffNodes(prevSequence.Children[i], thisSequence.Children[i]))
                    return true;
            }
                
            return false;
        }
    }
    
    
}