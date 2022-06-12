using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class GameObjectData : BaseData
    {

        public DiffableProperty<ulong[]> componentIds = new DiffableProperty<ulong[]>() {value = new ulong[0]};

        public DiffableProperty<int>    layer             = new DiffableProperty<int>();
        public DiffableProperty<string> name              = new DiffableProperty<string>();
        public DiffableProperty<string> tagString         = new DiffableProperty<string>();
        public DiffableProperty<ulong>  iconId            = new DiffableProperty<ulong>();
        public DiffableProperty<int>    navMeshLayer      = new DiffableProperty<int>();
        public DiffableProperty<int>    staticEditorFlags = new DiffableProperty<int>();
        public DiffableProperty<int>    isActive          = new DiffableProperty<int>();

        public TransformData          transformRef = null;
        public List<BaseData>         componentRefs = new List<BaseData>();
        public List<GameObjectData>   childRefs = new List<GameObjectData>();
        public GameObjectData         parentRef = null;

        public GameObjectData Load(YamlMappingNode mappingNode, ulong fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);
            
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
            
            LoadYamlProperties(mappingNode);

            return this;
        }

        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            
            if (componentIds.assigned)
            {
                var childNodes = new YamlSequenceNode();
                foreach (var childId in componentIds.value)
                {
                    var childNode = new YamlMappingNode();
                    var componentNode = new YamlMappingNode();
                    componentNode.Add(new YamlScalarNode("fileID"), new YamlScalarNode(childId.ToString()));
                    componentNode.Style = MappingStyle.Flow;
                    childNode.Add(new YamlScalarNode("component"), componentNode);
                    childNode.Style = MappingStyle.Flow;
                    childNodes.Add(childNode);
                }
                mappingNode.Add(new YamlScalarNode("m_Component"), childNodes);
            }
            
            SaveIntProperty   (mappingNode, "m_Layer",             layer);
            SaveStringProperty(mappingNode, "m_Name",              name);
            SaveStringProperty(mappingNode, "m_TagString",         tagString);
            SaveFileIdProperty(mappingNode, "m_Icon",              iconId);
            SaveIntProperty   (mappingNode, "m_NavMeshLayer",      navMeshLayer);
            SaveIntProperty   (mappingNode, "m_StaticEditorFlags", staticEditorFlags);
            SaveIntProperty   (mappingNode, "m_IsActive",          isActive);

            SaveYamlProperties(mappingNode);
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
            
            DiffYamlProperties(previousObj);

            return WasModified;
        }
            
        public override void Merge(object thiersObj, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true)
        {
            var thiers = thiersObj as GameObjectData;
            var conflictReportLines = new List<string>();

            MergeBase(thiersObj, conflictReportLines, takeTheirs);
            
            componentIds.value      = MergePropArray (nameof(componentIds),      componentIds,      thiers.componentIds,      conflictReportLines, takeTheirs);
            layer.value             = MergeProperties(nameof(layer),             layer,             thiers.layer,             conflictReportLines, takeTheirs);
            name.value              = MergeProperties(nameof(name),              name,              thiers.name,              conflictReportLines, takeTheirs);
            tagString.value         = MergeProperties(nameof(tagString),         tagString,         thiers.tagString,         conflictReportLines, takeTheirs);
            iconId.value            = MergeProperties(nameof(iconId),            iconId,            thiers.iconId,            conflictReportLines, takeTheirs);
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