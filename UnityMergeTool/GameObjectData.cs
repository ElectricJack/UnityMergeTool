using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class GameObjectData : BaseData
    {

        //public DiffableProperty<ulong[]> componentIds = new DiffableProperty<ulong[]>() {value = new ulong[0]};
        public List<DiffableFileId> componentIds = new List<DiffableFileId>();

        public DiffableProperty<int>    layer             = new DiffableProperty<int>();
        public DiffableProperty<string> name              = new DiffableProperty<string>();
        public DiffableProperty<string> tagString         = new DiffableProperty<string>();
        public DiffableFileId           iconId            = new DiffableFileId();
        public DiffableProperty<int>    navMeshLayer      = new DiffableProperty<int>();
        public DiffableProperty<int>    staticEditorFlags = new DiffableProperty<int>();
        public DiffableProperty<int>    isActive          = new DiffableProperty<int>();

        public TransformData          transformRef = null;
        public List<BaseData>         componentRefs = new List<BaseData>();
        public List<GameObjectData>   childRefs = new List<GameObjectData>();
        public GameObjectData         parentRef = null;

        public GameObjectData Load(YamlMappingNode mappingNode, long fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);

            LoadIntProperty(mappingNode, "m_Layer", layer);
            LoadStringProperty(mappingNode, "m_Name", name);
            LoadStringProperty(mappingNode, "m_TagString", tagString);
            iconId.Load(mappingNode, "m_Icon", _existingKeys);
            LoadIntProperty(mappingNode, "m_NavMeshLayer", navMeshLayer);
            LoadIntProperty(mappingNode, "m_StaticEditorFlags", staticEditorFlags);
            LoadIntProperty(mappingNode, "m_IsActive", isActive);

            if (mappingNode.Children.ContainsKey(new YamlScalarNode("m_Component")))
            {
                var componentNodes = Helpers.GetChildMapNodes(mappingNode, "m_Component");
                if (componentNodes != null)
                {
                    foreach (var node in componentNodes) {
                        componentIds.Add(new DiffableFileId().Load(node, "component", null)); 
                    }
                    
                    _existingKeys.Add("m_Component");
                }
            }

            LoadYamlProperties(mappingNode);

            return this;
        }

        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            
            if (componentIds.Count > 0)
            {
                var childNodes = new YamlSequenceNode();
                foreach (var id in componentIds)
                {
                    var childNode = new YamlMappingNode();
                    id.Save(childNode);
                    childNode.Style = MappingStyle.Flow;
                    childNodes.Add(childNode);
                }
                mappingNode.Add(new YamlScalarNode("m_Component"), childNodes);
            }
            
            SaveIntProperty   (mappingNode, "m_Layer",             layer);
            SaveStringProperty(mappingNode, "m_Name",              name);
            SaveStringProperty(mappingNode, "m_TagString",         tagString);
            iconId.Save(mappingNode);
            SaveIntProperty   (mappingNode, "m_NavMeshLayer",      navMeshLayer);
            SaveIntProperty   (mappingNode, "m_StaticEditorFlags", staticEditorFlags);
            SaveIntProperty   (mappingNode, "m_IsActive",          isActive);

            SaveYamlProperties(mappingNode);
        }
        public override bool Diff(object previousObj)
        {
            GameObjectData previous = previousObj as GameObjectData;
            _wasModified = DiffBase(previous);

            _wasModified |= DiffFileIdList(componentIds, previous.componentIds);
            
            _wasModified |= DiffProperty(layer, previous.layer);
            _wasModified |= DiffProperty(name, previous.name);
            _wasModified |= DiffProperty(tagString, previous.tagString);
            _wasModified |= iconId.Diff(previous.iconId);
            _wasModified |= DiffProperty(navMeshLayer, previous.navMeshLayer);
            _wasModified |= DiffProperty(staticEditorFlags, previous.staticEditorFlags);
            _wasModified |= DiffProperty(isActive, previous.isActive);
            
            DiffYamlProperties(previousObj);

            return WasModified;
        }
            
        public override void Merge(object baseObj, object thiersObj, ref string conflictReport, ref bool conflictsFound,
            bool takeTheirs = true)
        {
            var thiers = thiersObj as GameObjectData;
            var baseData = baseObj as GameObjectData;
            var conflictReportLines = new List<string>();

            MergeBase(thiersObj, conflictReportLines, takeTheirs);
            
            
            string componentConflictReport = "";
            bool componentConflictsFound = false;
            UnityFileData.MergeData(baseData.componentIds, componentIds, thiers.componentIds, ref componentConflictReport, ref componentConflictsFound, takeTheirs);
            if (componentConflictsFound)
            {
                conflictReportLines.Add(conflictReport);
                conflictsFound = true;
            }
            
            layer.value             = MergeProperties(nameof(layer),             layer,             thiers.layer,             conflictReportLines, takeTheirs);
            name.value              = MergeProperties(nameof(name),              name,              thiers.name,              conflictReportLines, takeTheirs);
            tagString.value         = MergeProperties(nameof(tagString),         tagString,         thiers.tagString,         conflictReportLines, takeTheirs);
            iconId.Merge(thiers.iconId, conflictReportLines, takeTheirs);
            navMeshLayer.value      = MergeProperties(nameof(navMeshLayer),      navMeshLayer,      thiers.navMeshLayer,      conflictReportLines, takeTheirs);
            staticEditorFlags.value = MergeProperties(nameof(staticEditorFlags), staticEditorFlags, thiers.staticEditorFlags, conflictReportLines, takeTheirs);
            isActive.value          = MergeProperties(nameof(isActive),          isActive,          thiers.isActive,          conflictReportLines, takeTheirs);

            MergeYamlProperties(thiersObj, conflictReportLines, takeTheirs);
            
            if (conflictReportLines.Count > 0)
            {
                conflictsFound = true;
                conflictReport += "\nConflict on GameObject: " + ScenePath + "\n";
                foreach (var line in conflictReportLines)
                {
                    conflictReport += "  " + line + "\n";
                }
            }
        }

        public override string LogString()
        {
            return "GameObject '" + name.value + "' (" + fileId.value + ") { isActive: " + isActive.value +
                   " layer: " + layer.value + " }";
        }

        public override string ScenePath
        {
            get {
                string parentPath = "";
                if (parentRef != null)
                    parentPath = parentRef.ScenePath;

                return parentPath + "/" + name.value;
            }
        }
            
    }
}