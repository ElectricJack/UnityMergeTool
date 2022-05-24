using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class TransformData : BaseSceneData
    {
        public DiffableProperty<ulong>          gameObjectId         = new DiffableProperty<ulong>();
        public DiffableProperty<float[]>        localRotation        = new DiffableProperty<float[]>() {value = new float[4]};
        public DiffableProperty<float[]>        localPosition        = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<float[]>        localScale           = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<ulong[]>        childrenIds          = new DiffableProperty<ulong[]>() { value = Array.Empty<ulong>() };
        public DiffableProperty<ulong>          parentId             = new DiffableProperty<ulong>();
        public DiffableProperty<int>            rootOrder            = new DiffableProperty<int>();

        public DiffableProperty<float[]>        localEulerAnglesHint = new DiffableProperty<float[]>() {value = new float[3]};

        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";
            
        public GameObjectData      gameObjectRef = null;
        public List<TransformData> childRefs = new List<TransformData>();
        public TransformData       parentRef = null;

        public string LogString()
        {
            return "Transform "+fileId.value+" -"+
                   (localPosition.value != null? " pos: { " + localPosition.value[0] + ", " + localPosition.value[1] + ", " + localPosition.value[2] + " } " : "") +
                   (localRotation.value != null? " rot: { " + localRotation.value[0] + ", " + localRotation.value[1] + ", " + localRotation.value[2] + ", " + localRotation.value[3] + " } " : "") +
                   (localScale.value != null? " scale: { " + localScale.value[0] + ", " + localScale.value[1] + ", " + localScale.value[2] + " } " : "") +
                   " rootOrder: " + rootOrder.value;
        }
        public TransformData Load(YamlMappingNode mappingNode, ulong fileId)
        {
            // m_GameObject: { { fileID, 1308407462 } }
            // m_LocalRotation: { { x, 0 }, { y, 0 }, { z, 0 }, { w, 1 } }
            // m_LocalPosition: { { x, 0 }, { y, 0 }, { z, 0 } }
            // m_LocalScale: { { x, 1 }, { y, 1 }, { z, 1 } }
            // m_Children: [ { { fileID, 696452151 } } ]
            // m_Father: { { fileID, 1584127366 } }
            // m_RootOrder: 0
            // m_LocalEulerAnglesHint: { { x, 0 }, { y, 0 }, { z, 0 } }
                
            LoadBase(mappingNode, fileId);
            
            gameObjectId.value         = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_GameObject"), "fileID"));
            localRotation.value        = Helpers.GetChildVector4(mappingNode, "m_LocalRotation");
            localPosition.value        = Helpers.GetChildVector3(mappingNode, "m_LocalPosition");
            localScale.value           = Helpers.GetChildVector3(mappingNode, "m_LocalScale");
            var childNodes= Helpers.GetChildMapNodes(mappingNode, "m_Children");
            if (childNodes != null)
            {
                childrenIds.value = childNodes
                    .Select(node => ulong.Parse(Helpers.GetChildScalarValue(node, "fileID")))
                    .ToArray();
            }
            parentId.value             = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_Father"), "fileID"));
            rootOrder.value            = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_RootOrder"));
            localEulerAnglesHint.value = Helpers.GetChildVector3(mappingNode, "m_LocalEulerAnglesHint");

            return this;
        }
        public override bool Diff(object previousObj)
        {
            TransformData previous = previousObj as TransformData;
            _wasModified = DiffBase(previous);

            gameObjectId.valueChanged          = gameObjectId.value != previous.gameObjectId.value;
            localRotation.valueChanged         = !Helpers.ArraysEqual<float>(localRotation.value, previous.localRotation.value);         
            localPosition.valueChanged         = !Helpers.ArraysEqual<float>(localRotation.value, previous.localRotation.value);
            localScale.valueChanged            = !Helpers.ArraysEqual<float>(localScale.value, previous.localScale.value);
            childrenIds.valueChanged           = !Helpers.ArraysEqual<ulong>(childrenIds.value, previous.childrenIds.value);
            parentId.valueChanged              = parentId.value != previous.parentId.value;
            rootOrder.valueChanged             = rootOrder.value != previous.rootOrder.value;
            localEulerAnglesHint.valueChanged  = !Helpers.ArraysEqual(localEulerAnglesHint.value, previous.localEulerAnglesHint.value);

            _wasModified = _wasModified ||
                           gameObjectId.valueChanged ||
                           localRotation.valueChanged ||
                           localPosition.valueChanged ||
                           localScale.valueChanged ||
                           childrenIds.valueChanged ||
                           parentId.valueChanged ||
                           rootOrder.valueChanged ||
                           localEulerAnglesHint.valueChanged;

            return WasModified;
        }
        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var thiers = thiersObj as TransformData;
            var conflictReportLines = new List<string>();
                
            fileId.value                      = MergeProperties(nameof(fileId),                        fileId,                      thiers.fileId,                      conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),               objectHideFlags,             thiers.objectHideFlags,             conflictReportLines, takeTheirs);
            correspondingSourceObjectId.value = MergeProperties(nameof(correspondingSourceObjectId),   correspondingSourceObjectId, thiers.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.value            = MergeProperties(nameof(prefabInstanceId),              prefabInstanceId,            thiers.prefabInstanceId,            conflictReportLines, takeTheirs);
            prefabAssetId.value               = MergeProperties(nameof(prefabAssetId),                 prefabAssetId,               thiers.prefabAssetId,               conflictReportLines, takeTheirs);
                
            gameObjectId.value          = MergeProperties(nameof(gameObjectId),         gameObjectId,         thiers.gameObjectId,              conflictReportLines, takeTheirs);
            localRotation.value         = MergePropArray (nameof(localRotation),        localRotation,        thiers.localRotation,        conflictReportLines, takeTheirs);
            localPosition.value         = MergePropArray (nameof(localPosition),        localPosition,        thiers.localPosition,        conflictReportLines, takeTheirs);
            localScale.value            = MergePropArray (nameof(localScale),           localScale,           thiers.localScale,           conflictReportLines, takeTheirs);
            childrenIds.value           = MergePropArray (nameof(childrenIds),          childrenIds,          thiers.childrenIds,          conflictReportLines, takeTheirs);
            parentId.value              = MergeProperties(nameof(parentId),             parentId,             thiers.parentId,                  conflictReportLines, takeTheirs);
            rootOrder.value             = MergeProperties(nameof(rootOrder),            rootOrder,            thiers.rootOrder,                 conflictReportLines, takeTheirs);
            localEulerAnglesHint.value  = MergePropArray (nameof(localEulerAnglesHint), localEulerAnglesHint, thiers.localEulerAnglesHint, conflictReportLines, takeTheirs);

            if (conflictReportLines.Count > 0)
            {
                conflictReport += "Conflict on Transform of node: " + gameObjectRef.ScenePath + "\n";
                foreach (var line in conflictReportLines) {
                    conflictReport += "  " + line + "\n";
                }
            }

        }
    }
}