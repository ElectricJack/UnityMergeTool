using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class PrefabInstanceData : BaseData
    {
        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";

        class Modification : PropertyMerge, IMergable
        {
            public DiffableFileId             target          = new DiffableFileId();
            public DiffableProperty<string>   propertyPath    = new DiffableProperty<string>();
            public DiffableProperty<YamlNode> value           = new DiffableProperty<YamlNode>();
            public DiffableFileId             objectReference = new DiffableFileId();

            public string ScenePath => LogString();
            public long FileId => target.fileId.value;

            public Modification Load(YamlMappingNode mappingNode)
            {
                target.Load(mappingNode, "target", _existingKeys);
                LoadStringProperty(mappingNode, "propertyPath", propertyPath);
                LoadProperty<YamlNode>(mappingNode, "value", value, (node) => node);
                objectReference.Load(mappingNode, "objectReference", _existingKeys);
                
                return this;
            }

            public void Save(YamlMappingNode mappingNode)
            {
                target.Save(mappingNode);
                SaveStringProperty(mappingNode, "propertyPath", propertyPath);
                SaveProperty<YamlNode>(mappingNode, "value", value, (node) => node);
                objectReference.Save(mappingNode);
            }
            public bool Diff(Modification previous)
            {
                _wasModified |= DiffProperty(value, previous.value);
                _wasModified |= objectReference.Diff(previous.objectReference);

                return WasModified;
            }

            public string LogString()
            {
                return $"PrefabInstance.Modification fileId: {target.fileId.value} {target.guid.value} {target.type.value}";
            }

            public bool Matches(IMergable other)
            {
                Modification theirs = other as Modification;
                if (theirs == null) return false;
                
                return target.Matches(theirs.target) &&
                       propertyPath.value == theirs.propertyPath.value;
            }

            public void Merge(object baseObj, object theirsObj, MergeReport report)
            {
                var thiers = theirsObj as Modification;
                value.value = MergeProperties(nameof(value), value,thiers.value, report);
                objectReference.Merge(thiers.objectReference, report);
            }
        }
       

        private DiffableFileId          _transformParent = new DiffableFileId();
        private List<Modification>      _modifications;
        private List<DiffableFileId>    _removedComponents;
        private DiffableFileId          _sourcePrefab = new DiffableFileId();
        
        
        public PrefabInstanceData Load(YamlMappingNode mappingNode, long fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);

            var modification = Helpers.GetChildMapNode(mappingNode, "m_Modification");
            

            _transformParent.Load(modification, "m_TransformParent", _existingKeys);
            _modifications = new List<Modification>();
            foreach (var mod in Helpers.GetChildMapNodes(modification, "m_Modifications")) {
               _modifications.Add(new Modification().Load(mod));
            }

            _removedComponents = new List<DiffableFileId>();
            foreach (var removed in Helpers.GetChildMapNodes(modification, "m_RemovedComponents"))
            {
                _removedComponents.Add(new DiffableFileId().Load(removed));
            }

            _sourcePrefab.Load(mappingNode, "m_SourcePrefab", _existingKeys);

            return this;
        }

        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            
            var modification = new YamlMappingNode();

            var childNodes = new YamlSequenceNode();
            foreach (var mod in _modifications)
            {
                var childNode = new YamlMappingNode();
                mod.Save(childNode);
                childNodes.Add(childNode);
            }
            
            _transformParent.Save(modification);
            modification.Add(new YamlScalarNode("m_Modifications"), childNodes);
            
            var removedComponents = new YamlSequenceNode();
            foreach (var removed in _removedComponents)
            {
                var childNode = new YamlMappingNode();
                removed.Save(childNode);
                removedComponents.Add(childNode);
            }
 
            modification.Add(new YamlScalarNode("m_RemovedComponents"), removedComponents);
            
            mappingNode.Add(new YamlScalarNode("m_Modification"), modification);
            
            _sourcePrefab.Save(mappingNode);
        }

        public override bool Diff(object previousObj)
        {
            PrefabInstanceData previous = previousObj as PrefabInstanceData;
            _wasModified = DiffBase(previous);
            _wasModified |= _transformParent.Diff(previous._transformParent);

            _wasModified |= _modifications.Count != previous._modifications.Count;
            foreach (var modification in _modifications)
            {
                // Does the component match? Does the property path match? This means we're talking about the same value
                // Attempt to find a matching modification to the same target and property path
                var found = previous._modifications.FirstOrDefault(mod =>
                {
                    return modification.Matches(mod);
                });

                // If we couldn't find it then bail (we have a difference)
                if (found == null)
                {
                    _wasModified = true;
                    continue;
                }

                // Otherwise compare the two modifications
                _wasModified |= modification.Diff(found);
            }
            
            
            _wasModified |= _removedComponents.Count != previous._removedComponents.Count;
            foreach (var removed in _removedComponents)
            {
                // Does the component match? Does the property path match? This means we're talking about the same value
                // Attempt to find a matching modification to the same target and property path
                var found = previous._removedComponents.FirstOrDefault(rem =>
                {
                    return removed.Matches(rem);
                });

                // If we couldn't find it then bail (we have a difference)
                if (found == null)
                {
                    _wasModified = true;
                    continue;
                }

                // Otherwise compare the two modifications
                _wasModified |= removed.Diff(found);
            }

            return WasModified;
        }

        public override void Merge(object baseObj, object theirsObj, MergeReport report)
        {
            PrefabInstanceData baseData = baseObj as PrefabInstanceData;
            PrefabInstanceData theirs = theirsObj as PrefabInstanceData;
            
            report.Push(LogString(), ScenePath);
            
            MergeBase(theirsObj, report);
            _transformParent.Merge(theirs._transformParent, report);

            var modificationConflicts = "";
            var localConflicts = false;
            _modifications     = UnityFileData.MergeData(baseData._modifications, _modifications, theirs._modifications, report);
            _removedComponents = UnityFileData.MergeData(baseData._removedComponents, _removedComponents, theirs._removedComponents, report);
            
            report.Pop();
        }
        
        
        public override string LogString()
        {
            return "PrefabInstance " + fileId.value;
        }
    }
}