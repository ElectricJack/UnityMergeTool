using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class GameObjectData : BaseSceneData
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

        public GameObjectData Load(YamlMappingNode mappingNode, ulong fileId)
        {
            // serializedVersion: 6
            // m_Component: [
            //      { { component, { { fileID, 1165256316 } } } },
            //      { { component, { { fileID, 1165256318 } } } },
            //      { { component, { { fileID, 1165256317 } } } }
            // ]
            // m_Layer: 0
            // m_Name: StarSurgeMusic
            // m_TagString: Untagged
            // m_Icon: { { fileID, 0 } }
            // m_NavMeshLayer: 0
            // m_StaticEditorFlags: 0
            // m_IsActive: 0

            LoadBase(mappingNode, fileId);

            serializedVersion.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "serializedVersion"));
            layer.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_Layer"));
            name.value = Helpers.GetChildScalarValue(mappingNode, "m_Name", true);
            tagString.value = Helpers.GetChildScalarValue(mappingNode, "m_TagString", true);
            iconId.value = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_Icon"), "fileID"));
            navMeshLayer.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_NavMeshLayer"));
            staticEditorFlags.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_StaticEditorFlags"));
            isActive.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_IsActive"));

            var componentNodes = Helpers.GetChildMapNodes(mappingNode, "m_Component");
            if (componentNodes != null)
            {
                componentIds.value = componentNodes.Select(node =>
                {
                    return ulong.Parse(Helpers.GetChildScalarValue((YamlMappingNode) node["component"], "fileID"));
                }).ToArray();
            }

            return this;
        }
        public override bool Diff(object previousObj)
        {
            GameObjectData previous = previousObj as GameObjectData;
            _wasModified = DiffBase(previous);

            serializedVersion.valueChanged = serializedVersion.value != previous.serializedVersion.value;
            componentIds.valueChanged = !Helpers.ArraysEqual(componentIds.value, previous.componentIds.value);
            layer.valueChanged = layer.value != previous.layer.value;
            name.valueChanged = name.value != previous.name.value;
            tagString.valueChanged = !tagString.value.Equals(previous.tagString.value);
            iconId.valueChanged = iconId.value != previous.iconId.value;
            navMeshLayer.valueChanged = navMeshLayer.value != previous.navMeshLayer.value;

            staticEditorFlags.valueChanged = staticEditorFlags.value != previous.staticEditorFlags.value;
            isActive.valueChanged = isActive.value != previous.isActive.value;

            _wasModified = _wasModified ||
                           serializedVersion.valueChanged ||
                           componentIds.valueChanged ||
                           layer.valueChanged ||
                           name.valueChanged ||
                           tagString.valueChanged ||
                           iconId.valueChanged ||
                           navMeshLayer.valueChanged ||
                           staticEditorFlags.valueChanged ||
                           isActive.valueChanged;

            return WasModified;
        }
            
        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var thiers = thiersObj as GameObjectData;
            var conflictReportLines = new List<string>();

            fileId.value                      = MergeProperties(nameof(fileId),                     fileId,            thiers.fileId, conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),            objectHideFlags,   thiers.objectHideFlags, conflictReportLines, takeTheirs);
            correspondingSourceObjectId.value = MergeProperties(nameof(correspondingSourceObjectId),correspondingSourceObjectId, thiers.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.value            = MergeProperties(nameof(prefabInstanceId),           prefabInstanceId,  thiers.prefabInstanceId, conflictReportLines, takeTheirs); 
            prefabAssetId.value               = MergeProperties(nameof(prefabAssetId),              prefabAssetId,     thiers.prefabAssetId, conflictReportLines, takeTheirs);

            serializedVersion.value           = MergeProperties(nameof(serializedVersion),          serializedVersion, thiers.serializedVersion, conflictReportLines, takeTheirs);
            componentIds.value                = MergePropArray (nameof(componentIds),               componentIds, thiers.componentIds, conflictReportLines, takeTheirs);
            layer.value                       = MergeProperties(nameof(layer),                      layer,             thiers.layer, conflictReportLines, takeTheirs);
            name.value                        = MergeProperties(nameof(name),                       name,              thiers.name, conflictReportLines, takeTheirs);
            tagString.value                   = MergeProperties(nameof(tagString),                  tagString,         thiers.tagString, conflictReportLines, takeTheirs);
            iconId.value                      = MergeProperties(nameof(iconId),                     iconId,            thiers.iconId, conflictReportLines, takeTheirs);
            navMeshLayer.value                = MergeProperties(nameof(navMeshLayer),               navMeshLayer,      thiers.navMeshLayer, conflictReportLines, takeTheirs);
            staticEditorFlags.value           = MergeProperties(nameof(staticEditorFlags),          staticEditorFlags, thiers.staticEditorFlags, conflictReportLines, takeTheirs);
            isActive.value                    = MergeProperties(nameof(isActive),                   isActive,          thiers.isActive, conflictReportLines, takeTheirs);

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