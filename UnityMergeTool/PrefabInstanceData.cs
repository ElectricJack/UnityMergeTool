using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class PrefabInstanceData : BaseData
    {
        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";

        class Modification : PropertyMerge, IMergable
        {
            public DiffableProperty<ulong>    targetFileId    = new DiffableProperty<ulong>();
            public DiffableProperty<string>   targetGuid      = new DiffableProperty<string>();
            public DiffableProperty<int>      targetType      = new DiffableProperty<int>();
            public DiffableProperty<string>   propertyPath    = new DiffableProperty<string>();
            
            public DiffableProperty<YamlNode> value           = new DiffableProperty<YamlNode>();
            public DiffableProperty<ulong>    objectReference = new DiffableProperty<ulong>();

            public Modification Load(YamlMappingNode mappingNode)
            {
                var target = Helpers.GetChildMapNode(mappingNode, "target");
                if (target != null)
                {
                    LoadProperty(target, "fileID", targetFileId, (node) => {
                        return ulong.Parse(((YamlScalarNode) node).Value);
                    });
                    LoadStringProperty(target, "guid", targetGuid);
                    LoadIntProperty(target, "type", targetType);
                }
                
                LoadStringProperty(mappingNode, "propertyPath", propertyPath);
                LoadProperty<YamlNode>(mappingNode, "value", value, (node) => node);
                LoadFileIdProperty(mappingNode, "objectReference", objectReference);
                
                return this;
            }

            public void Save(YamlMappingNode mappingNode)
            {
                var target = new YamlMappingNode();
                target.Add(new YamlScalarNode("fileID"), targetFileId.value.ToString());
                target.Add(new YamlScalarNode("guid"), targetGuid.value);
                target.Add(new YamlScalarNode("type"), targetType.value.ToString());
                mappingNode.Add(new YamlScalarNode("target"), target);
                
                SaveStringProperty(mappingNode, "propertyPath", propertyPath);
                SaveProperty<YamlNode>(mappingNode, "value", value, (node) => node);
                SaveFileIdProperty(mappingNode, "objectReference", objectReference);
            }
            public bool Diff(Modification previous)
            {
                _wasModified |= DiffProperty(value, previous.value);
                _wasModified |= DiffProperty(objectReference, previous.objectReference);

                return WasModified;
            }

            public bool Matches(IMergable other)
            {
                Modification theirs = other as Modification;
                if (theirs == null) return false;
                
                return targetFileId.value == theirs.targetFileId.value &&
                       targetGuid.value   == theirs.targetGuid.value &&
                       targetType.value   == theirs.targetType.value &&
                       propertyPath.value == theirs.propertyPath.value;
            }

            public void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound,
                bool takeTheirs = true)
            {
                var thiers = theirsObj as Modification;
                var conflictReportLines = new List<string>();
                
                value.value           = MergeProperties(nameof(value),           value,             thiers.value,             conflictReportLines, takeTheirs);
                objectReference.value = MergeProperties(nameof(objectReference), objectReference,   thiers.objectReference,   conflictReportLines, takeTheirs);
                
                if (conflictReportLines.Count > 0)
                {
                    conflictsFound = true;
                    conflictReport += "\n   Conflict on modification target id: " + targetFileId.value + " property: " + propertyPath.value + "\n";
                    foreach (var line in conflictReportLines)
                    {
                        conflictReport += "  " + line + "\n";
                    }
                }
            }
        }
        
        class RemovedComponent : PropertyMerge
        {
            public RemovedComponent Load(YamlMappingNode mappingNode)
            {
                //@TODO - Are removed components even used?
                return this;
            }
        }

        private DiffableProperty<ulong> _transformParent = new DiffableProperty<ulong>();
        private List<Modification>      _modifications;
        private List<RemovedComponent>  _removedComponents;
        
        public PrefabInstanceData Load(YamlMappingNode mappingNode, ulong fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);

            var modification = Helpers.GetChildMapNode(mappingNode, "m_Modification");

            LoadFileIdProperty(modification, "m_TransformParent", _transformParent);
            _modifications = new List<Modification>();
            foreach (var mod in Helpers.GetChildMapNodes(modification, "m_Modifications")) {
               _modifications.Add(new Modification().Load(mod));
            }

            _removedComponents = new List<RemovedComponent>();
            foreach (var removed in Helpers.GetChildMapNodes(modification, "m_RemovedComponents"))
            {
                _removedComponents.Add(new RemovedComponent().Load(removed));
            }
            
            var sourcePrefab = Helpers.GetChildMapNode(mappingNode, "m_SourcePrefab");

            return this;
        }

        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            
            var modification = new YamlMappingNode();
            if (_modifications.Count > 0)
            {
                var childNodes = new YamlSequenceNode();
                foreach (var mod in _modifications)
                {
                    var childNode = new YamlMappingNode();
                    mod.Save(childNode);
                    childNodes.Add(childNode);
                }
                
                SaveFileIdProperty(modification, "m_TransformParent", _transformParent);
                modification.Add(new YamlScalarNode("m_Modifications"), childNodes);
                
                mappingNode.Add(new YamlScalarNode("m_Modification"), modification);
            }
        }

        public override bool Diff(object previousObj)
        {
            PrefabInstanceData previous = previousObj as PrefabInstanceData;
            _wasModified = DiffBase(previous);
            _wasModified |= DiffProperty(_transformParent, previous._transformParent);

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
            
            //@TODO compare removed components in a sane way

            return WasModified;
        }

        public override void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound,
            bool takeTheirs = true)
        {
            PrefabInstanceData baseData = baseObj as PrefabInstanceData;
            PrefabInstanceData theirs = theirsObj as PrefabInstanceData;
            var conflictReportLines = new List<string>();
            
            MergeBase(theirsObj, conflictReportLines, takeTheirs);
            MergeProperties("m_TransformParent", _transformParent, theirs._transformParent, conflictReportLines, takeTheirs);

            var modificationConflicts = "";
            _modifications = UnityFileData.MergeData(baseData._modifications, _modifications, theirs._modifications, ref modificationConflicts,
                ref conflictsFound, takeTheirs);

            if (conflictReportLines.Count > 0 || conflictsFound)
            {
                conflictsFound = true;
                conflictReport += "\nConflict on PrefabInstance: " + ScenePath + "\n";
                foreach (var line in conflictReportLines)
                {
                    conflictReport += "  " + line + "\n";
                }

                conflictReport += modificationConflicts;
            }
        }
        
        
        public override string LogString()
        {
            return ""; // @TODO
        }
    }
}