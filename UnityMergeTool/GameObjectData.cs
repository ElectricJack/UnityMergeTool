using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class GameObjectData : BaseData
    {
        public DiffableProperty<int> serializedVersion = new DiffableProperty<int>();

        public DiffableProperty<ulong[]> componentIds = new DiffableProperty<ulong[]>()
            {value = Array.Empty<ulong>()};

        public DiffableProperty<int>    layer             = new DiffableProperty<int>();
        public DiffableProperty<string> name              = new DiffableProperty<string>();
        public DiffableProperty<string> tagString         = new DiffableProperty<string>();
        public DiffableProperty<ulong>  iconId            = new DiffableProperty<ulong>();
        public DiffableProperty<int>    navMeshLayer      = new DiffableProperty<int>();
        public DiffableProperty<int>    staticEditorFlags = new DiffableProperty<int>();
        public DiffableProperty<int>    isActive          = new DiffableProperty<int>();

        public TransformData          transformRef = null;
        public List<MonoBehaviorData> componentRefs = new List<MonoBehaviorData>();
        public List<GameObjectData>   childRefs = new List<GameObjectData>();
        public GameObjectData         parentRef = null;

        public GameObjectData Load(YamlMappingNode mappingNode, ulong fileId, string typeName)
        {
            LoadBase(mappingNode, fileId, typeName);
            
            LoadIntProperty   (mappingNode, "m_Layer",             layer);
            LoadStringProperty(mappingNode, "m_Name",              name);
            LoadStringProperty(mappingNode, "m_TagString",         tagString);
            LoadFileIdProperty(mappingNode, "m_Icon",              iconId);
            LoadIntProperty   (mappingNode, "m_NavMeshLayer",      navMeshLayer);
            LoadIntProperty   (mappingNode, "m_StaticEditorFlags", staticEditorFlags);
            LoadIntProperty   (mappingNode, "m_IsActive",          isActive);
            
            if (mappingNode.Children.ContainsKey(new YamlScalarNode("m_Component")))
            {
                var componentNodes = Helpers.GetChildMapNodes(mappingNode, "m_Component");
                if (componentNodes != null)
                {
                    componentIds.value = componentNodes.Select(node => {
                        return ulong.Parse(Helpers.GetChildScalarValue((YamlMappingNode) node["component"], "fileID"));
                    }).ToArray();
                    componentIds.assigned = true;
                    _existingKeys.Add("m_Component");
                }
            }
            else
            {
                componentIds.assigned = false;
            }

            return this;
        }
        public override bool Diff(object previousObj)
        {
            GameObjectData previous = previousObj as GameObjectData;
            _wasModified = DiffBase(previous);

            _wasModified |= DiffArrayProperty(componentIds, previous.componentIds);
            _wasModified |= DiffProperty(layer, previous.layer);
            _wasModified |= DiffProperty(name, previous.name);
            _wasModified |= DiffProperty(tagString, previous.tagString);
            _wasModified |= DiffProperty(iconId, previous.iconId);
            _wasModified |= DiffProperty(navMeshLayer, previous.navMeshLayer);
            _wasModified |= DiffProperty(staticEditorFlags, previous.staticEditorFlags);
            _wasModified |= DiffProperty(isActive, previous.isActive);

            return WasModified;
        }
            
        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var thiers = thiersObj as GameObjectData;
            var conflictReportLines = new List<string>();

            MergeBase(thiersObj, conflictReportLines, takeTheirs);
            
            serializedVersion.value = MergeProperties(nameof(serializedVersion), serializedVersion, thiers.serializedVersion, conflictReportLines, takeTheirs);
            componentIds.value      = MergePropArray (nameof(componentIds),      componentIds,      thiers.componentIds,      conflictReportLines, takeTheirs);
            layer.value             = MergeProperties(nameof(layer),             layer,             thiers.layer,             conflictReportLines, takeTheirs);
            name.value              = MergeProperties(nameof(name),              name,              thiers.name,              conflictReportLines, takeTheirs);
            tagString.value         = MergeProperties(nameof(tagString),         tagString,         thiers.tagString,         conflictReportLines, takeTheirs);
            iconId.value            = MergeProperties(nameof(iconId),            iconId,            thiers.iconId,            conflictReportLines, takeTheirs);
            navMeshLayer.value      = MergeProperties(nameof(navMeshLayer),      navMeshLayer,      thiers.navMeshLayer,      conflictReportLines, takeTheirs);
            staticEditorFlags.value = MergeProperties(nameof(staticEditorFlags), staticEditorFlags, thiers.staticEditorFlags, conflictReportLines, takeTheirs);
            isActive.value          = MergeProperties(nameof(isActive),          isActive,          thiers.isActive,          conflictReportLines, takeTheirs);

            if (conflictReportLines.Count > 0)
            {
                conflictReport += "Conflict on GameObject: " + ScenePath + "\n";
                foreach (var line in conflictReportLines)
                {
                    conflictReport += "  " + line + "\n";
                }
            }
        }

        public string LogString()
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