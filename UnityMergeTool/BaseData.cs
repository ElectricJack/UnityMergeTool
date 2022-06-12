using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    
    abstract class BaseData
    {
        public string                                         typeName;
        public string                                         tag;
        
        public DiffableProperty<ulong>                        fileId                      = new DiffableProperty<ulong>(); // Assigned from anchor id of root node in document
        public DiffableProperty<ulong>                        gameObjectId                = new DiffableProperty<ulong>(); // Not every node has this but enough do it's worth keeping in base
        public DiffableProperty<int>                          objectHideFlags             = new DiffableProperty<int>();
        public DiffableProperty<int>                          serializedVersion           = new DiffableProperty<int>();
        public DiffableProperty<ulong>                        correspondingSourceObjectId = new DiffableProperty<ulong>();
        public DiffableProperty<ulong>                        prefabInstanceId            = new DiffableProperty<ulong>();
        public DiffableProperty<ulong>                        prefabAssetId               = new DiffableProperty<ulong>();

        public GameObjectData                                 gameObjectRef = null;
        
        public Dictionary<string, DiffableProperty<YamlNode>> additionalData = new Dictionary<string, DiffableProperty<YamlNode>>();
        protected List<string>                                _existingKeys = new List<string>();
        
        protected bool _wasModified;
        public    bool WasModified => _wasModified;
        public abstract string ScenePath { get; }
        public abstract bool Diff(object previous);
        public abstract void Merge(object thiers, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true);

        public abstract void Save(YamlMappingNode node);
        public abstract string LogString();

        
        protected void LoadBase(YamlMappingNode mappingNode, ulong fileId, string typeName, string tag)
        {
            this.typeName = typeName;
            this.fileId.value = fileId;
            this.tag = tag;
            
            _existingKeys.Clear();
           
            LoadIntProperty    (mappingNode, "m_ObjectHideFlags",           objectHideFlags);
            LoadFileIdProperty (mappingNode, "m_CorrespondingSourceObject", correspondingSourceObjectId);
            LoadFileIdProperty (mappingNode, "m_PrefabInstance",            prefabInstanceId);
            LoadFileIdProperty (mappingNode, "m_PrefabAsset",               prefabAssetId);
            LoadIntProperty    (mappingNode, "serializedVersion",           serializedVersion);
            LoadFileIdProperty (mappingNode, "m_GameObject",                gameObjectId);
        }

        protected void SaveBase(YamlMappingNode mappingNode)
        {
            mappingNode.Tag = new TagName(tag);

            SaveIntProperty(mappingNode,    "m_ObjectHideFlags",           objectHideFlags);
            SaveFileIdProperty(mappingNode, "m_CorrespondingSourceObject", correspondingSourceObjectId);
            SaveFileIdProperty(mappingNode, "m_PrefabInstance",            prefabInstanceId);
            SaveFileIdProperty(mappingNode, "m_PrefabAsset",               prefabAssetId);
            SaveIntProperty(mappingNode,    "serializedVersion",           serializedVersion);
            SaveFileIdProperty(mappingNode, "m_GameObject",                gameObjectId);
        }


        protected void LoadYamlProperties(YamlMappingNode mappingNode)
        {
            foreach (var item in mappingNode)
            {
                var keyName = ((YamlScalarNode) item.Key).Value;
                if (_existingKeys.Contains(keyName))
                    continue;
                
                // This could potentially recurse for better accuracy
                additionalData.Add(keyName,  new DiffableProperty<YamlNode>() { value = item.Value });
            }
        }

        protected void SaveYamlProperties(YamlMappingNode mappingNode)
        {
            foreach (var item in additionalData)
            {
                mappingNode.Add(new YamlScalarNode(item.Key), item.Value.value);
            }
        }
        protected void LoadIntProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<int> property)
        {
            LoadProperty(mappingNode, propertyName, property, () => {
                return int.Parse(Helpers.GetChildScalarValue(mappingNode, propertyName));
            });
        }
        protected void SaveIntProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<int> property)
        {
            SaveProperty(mappingNode, propertyName, property, (int value) =>
            {
                return new YamlScalarNode(value.ToString());
            });
        }
        protected void LoadVector3Property(YamlMappingNode mappingNode, string propertyName, DiffableProperty<float[]> property)
        {
            LoadProperty(mappingNode, propertyName, property, () => {
                return Helpers.GetChildVector3(mappingNode, propertyName);
            });
        }
        protected void SaveVector3Property(YamlMappingNode mappingNode, string propertyName, DiffableProperty<float[]> property)
        {
            SaveProperty(mappingNode, propertyName, property, (float[] value) =>
            {
                return Helpers.GetNodeFromVector3(value);
            });
        }
        protected void LoadVector4Property(YamlMappingNode mappingNode, string propertyName, DiffableProperty<float[]> property)
        {
            LoadProperty(mappingNode, propertyName, property, () => {
                return Helpers.GetChildVector4(mappingNode, propertyName);
            });
        }
        protected void SaveVector4Property(YamlMappingNode mappingNode, string propertyName, DiffableProperty<float[]> property)
        {
            SaveProperty(mappingNode, propertyName, property, (float[] value) =>
            {
                return Helpers.GetNodeFromVector4(value);
            });
        }
        protected void LoadStringProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<string> property)
        {
            LoadProperty(mappingNode, propertyName, property, () => {
                return Helpers.GetChildScalarValue(mappingNode, propertyName);
            });
        }
        protected void SaveStringProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<string> property)
        {
            SaveProperty(mappingNode, propertyName, property, (string value) =>
            {
                return new YamlScalarNode(value);
            });
        }
        protected void LoadFileIdProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<ulong> property)
        {
            LoadProperty(mappingNode, propertyName, property, () => {
                return ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, propertyName), "fileID"));
            });
        }
        protected void SaveFileIdProperty(YamlMappingNode mappingNode, string propertyName, DiffableProperty<ulong> property)
        {
            SaveProperty(mappingNode, propertyName, property, (ulong value) =>
            {
                var container = new YamlMappingNode();
                container.Add(new YamlScalarNode("fileID"), new YamlScalarNode(value.ToString()));
                container.Style = MappingStyle.Flow;
                return container;
            });
        }
        private void LoadProperty<T>(YamlMappingNode mappingNode, string propertyName, DiffableProperty<T> property, Func<T> handler )
        {
            if (mappingNode.Children.ContainsKey(new YamlScalarNode(propertyName)))
            {
                property.value = handler();
                property.assigned = true;
                _existingKeys.Add(propertyName);
                return;
            }

            property.assigned = false;
        }
        private void SaveProperty<T>(YamlMappingNode mappingNode, string propertyName, DiffableProperty<T> property, Func<T, YamlNode> handler)
        {
            if (property.assigned) 
            {
                mappingNode.Add(new YamlScalarNode(propertyName), handler(property.value));    
            }
        }
        
        protected bool DiffBase(BaseData previous)
        {
            _wasModified = false;
            _wasModified |= DiffProperty(fileId,            previous.fileId);
            _wasModified |= DiffProperty(gameObjectId,      previous.gameObjectId);
            _wasModified |= DiffProperty(objectHideFlags,   previous.objectHideFlags);
            _wasModified |= DiffProperty(serializedVersion, previous.serializedVersion);

            _wasModified |= DiffProperty(prefabInstanceId,  previous.prefabInstanceId);
            _wasModified |= DiffProperty(prefabAssetId,     previous.prefabAssetId);
            
            return WasModified;
        }
        static protected bool DiffProperty<T>(DiffableProperty<T> mine, DiffableProperty<T> theirs)
        {
            // Skip if neither are defined (nothing modified)
            if (!mine.assigned && !theirs.assigned)
                return false;

            if (!mine.assigned)
            {
                mine.value = theirs.value;
                mine.valueChanged = true;
                return true;
            }

            if (!theirs.assigned)
                return false;
            
            return mine.valueChanged = !mine.value.Equals(theirs.value);
        }
        static protected bool DiffArrayProperty<T>(DiffableProperty<T[]> mine, DiffableProperty<T[]> theirs)
        {
            // Skip if neither are defined (nothing modified)
            if (!mine.assigned && !theirs.assigned)
                return false;

            if (!mine.assigned)
            {
                mine.value = theirs.value;
                mine.valueChanged = true;
                return true;
            }

            if (!theirs.assigned)
                return false;
            
            return mine.valueChanged = !Helpers.ArraysEqual(mine.value, theirs.value);
        }

        protected void DiffYamlProperties(object previousObj)
        {
            BaseData previous = previousObj as BaseData;
            
            foreach (var pair in additionalData)
            {
                var thisNode = pair.Value.value;

                // If we couldn't find the key in previous, then this key was added/renamed and we should track this as a modification
                if (!previous.additionalData.ContainsKey(pair.Key))
                {
                    _wasModified = true;
                    pair.Value.valueChanged = true;
                    continue;
                }
                
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

        protected void MergeBase(object thiersObj, List<string> conflictReportLines, bool takeTheirs = true)
        {
            var thiers = thiersObj as BaseData;
            fileId.value                      = MergeProperties(nameof(fileId),                     fileId,                      thiers.fileId,                      conflictReportLines, takeTheirs);
            gameObjectId.value                = MergeProperties(nameof(gameObjectId),               gameObjectId,                thiers.gameObjectId,                conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),            objectHideFlags,             thiers.objectHideFlags,             conflictReportLines, takeTheirs);
            correspondingSourceObjectId.value = MergeProperties(nameof(correspondingSourceObjectId),correspondingSourceObjectId, thiers.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.value            = MergeProperties(nameof(prefabInstanceId),           prefabInstanceId,            thiers.prefabInstanceId,            conflictReportLines, takeTheirs); 
            prefabAssetId.value               = MergeProperties(nameof(prefabAssetId),              prefabAssetId,               thiers.prefabAssetId,               conflictReportLines, takeTheirs);
        }
        
        protected void MergeYamlProperties(object thiersObj, List<string> conflictReportLines, bool takeTheirs = true)
        {
            var theirs = thiersObj as BaseData;
            
            var toReplace = new List<KeyValuePair<string, DiffableProperty<YamlNode>>>();
            foreach (var pair in additionalData)
            {
                var thisNode = pair.Value;
                var theirNode = theirs.additionalData.ContainsKey(pair.Key) ? theirs.additionalData[pair.Key] : null;
                if (theirNode == null)
                    continue;

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