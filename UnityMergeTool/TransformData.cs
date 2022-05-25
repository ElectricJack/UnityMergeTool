using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class TransformData : BaseData
    {
        public DiffableProperty<float[]>        localRotation        = new DiffableProperty<float[]>() {value = new float[4]};
        public DiffableProperty<float[]>        localPosition        = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<float[]>        localScale           = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<ulong[]>        childrenIds          = new DiffableProperty<ulong[]>() { value = Array.Empty<ulong>() };
        public DiffableProperty<ulong>          parentId             = new DiffableProperty<ulong>();
        public DiffableProperty<int>            rootOrder            = new DiffableProperty<int>();

        public DiffableProperty<float[]>        localEulerAnglesHint = new DiffableProperty<float[]>() {value = new float[3]};

        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";
        
        public List<TransformData> childRefs = new List<TransformData>();
        public TransformData       parentRef = null;

        public override string LogString()
        {
            return "Transform "+fileId.value+" -"+
                   (localPosition.value != null? " pos: { " + localPosition.value[0] + ", " + localPosition.value[1] + ", " + localPosition.value[2] + " } " : "") +
                   (localRotation.value != null? " rot: { " + localRotation.value[0] + ", " + localRotation.value[1] + ", " + localRotation.value[2] + ", " + localRotation.value[3] + " } " : "") +
                   (localScale.value != null? " scale: { " + localScale.value[0] + ", " + localScale.value[1] + ", " + localScale.value[2] + " } " : "") +
                   " rootOrder: " + rootOrder.value;
        }
        public TransformData Load(YamlMappingNode mappingNode, ulong fileId, string typeName)
        {
            LoadBase(mappingNode, fileId, typeName);
            
            LoadVector4Property (mappingNode, "m_LocalRotation", localRotation);
            LoadVector3Property (mappingNode, "m_LocalPosition", localPosition);
            LoadVector3Property (mappingNode, "m_LocalScale",    localScale);

            if (mappingNode.Children.ContainsKey(new YamlScalarNode("m_Children")))
            {
                var childNodes= Helpers.GetChildMapNodes(mappingNode, "m_Children");
                if (childNodes != null)
                {
                    childrenIds.value = childNodes
                        .Select(node => ulong.Parse(Helpers.GetChildScalarValue(node, "fileID")))
                        .ToArray();
                    childrenIds.assigned = true;
                    _existingKeys.Add("m_Children");
                }
            }
            
            LoadFileIdProperty  (mappingNode, "m_Father",               parentId);
            LoadIntProperty     (mappingNode, "m_RootOrder",            rootOrder);
            LoadVector3Property (mappingNode, "m_LocalEulerAnglesHint", localEulerAnglesHint);
            return this;
        }
        public override bool Diff(object previousObj)
        {
            TransformData previous = previousObj as TransformData;
            _wasModified = DiffBase(previous);
            
            _wasModified |= DiffArrayProperty (localRotation,       previous.localRotation);
            _wasModified |= DiffArrayProperty (localPosition,       previous.localPosition);
            _wasModified |= DiffArrayProperty (localScale,          previous.localScale);
            _wasModified |= DiffArrayProperty (childrenIds,         previous.childrenIds);
            _wasModified |= DiffProperty      (parentId,            previous.parentId);
            _wasModified |= DiffProperty      (rootOrder,           previous.rootOrder);
            _wasModified |= DiffArrayProperty (localEulerAnglesHint,previous.localEulerAnglesHint);

            return WasModified;
        }
        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var thiers = thiersObj as TransformData;
            var conflictReportLines = new List<string>();
                
            MergeBase(thiersObj, conflictReportLines, takeTheirs);
            
            localRotation.value         = MergePropArray (nameof(localRotation),        localRotation,        thiers.localRotation,        conflictReportLines, takeTheirs);
            localPosition.value         = MergePropArray (nameof(localPosition),        localPosition,        thiers.localPosition,        conflictReportLines, takeTheirs);
            localScale.value            = MergePropArray (nameof(localScale),           localScale,           thiers.localScale,           conflictReportLines, takeTheirs);
            childrenIds.value           = MergePropArray (nameof(childrenIds),          childrenIds,          thiers.childrenIds,          conflictReportLines, takeTheirs);
            parentId.value              = MergeProperties(nameof(parentId),             parentId,             thiers.parentId,                  conflictReportLines, takeTheirs);
            rootOrder.value             = MergeProperties(nameof(rootOrder),            rootOrder,            thiers.rootOrder,                 conflictReportLines, takeTheirs);
            localEulerAnglesHint.value  = MergePropArray (nameof(localEulerAnglesHint), localEulerAnglesHint, thiers.localEulerAnglesHint, conflictReportLines, takeTheirs);

            if (conflictReportLines.Count > 0)
            {
                conflictReport += "Conflict on Transform of node: " + ScenePath + "\n";
                foreach (var line in conflictReportLines) {
                    conflictReport += "  " + line + "\n";
                }
            }

        }
    }
}