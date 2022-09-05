using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class TransformData : BaseData
    {
        public DiffableProperty<float[]>        localRotation        = new DiffableProperty<float[]>() {value = new float[4]};
        public DiffableProperty<float[]>        localPosition        = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<float[]>        localScale           = new DiffableProperty<float[]>() {value = new float[3]};
        public DiffableProperty<long[]>         childrenIds          = new DiffableProperty<long[]>()  { value = new long[0]};
        public DiffableFileId                   parentId             = new DiffableFileId();
        public DiffableProperty<int>            rootOrder            = new DiffableProperty<int>();

        public DiffableProperty<float[]>        localEulerAnglesHint = new DiffableProperty<float[]>() {value = new float[3]};


        //public List<long> lostChildren = new List<long>();

        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";
        
        public List<TransformData> childRefs = new List<TransformData>();
        public TransformData       parentRef = null;

        public override string LogString()
        {
            return "Transform " + fileId.value;
        }
        public TransformData Load(YamlMappingNode mappingNode, long fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);
            
            LoadVector4Property (mappingNode, "m_LocalRotation", localRotation);
            LoadVector3Property (mappingNode, "m_LocalPosition", localPosition);
            LoadVector3Property (mappingNode, "m_LocalScale",    localScale);

            if (mappingNode.Children.ContainsKey(new YamlScalarNode("m_Children")))
            {
                var childNodes= Helpers.GetChildMapNodes(mappingNode, "m_Children");
                if (childNodes != null)
                {
                    childrenIds.value = childNodes
                        .Select(node => long.Parse(Helpers.GetChildScalarValue(node, "fileID")))
                        .ToArray();
                    childrenIds.assigned = true;
                    _existingKeys.Add("m_Children");
                }
            }
            
            parentId.Load(mappingNode, "m_Father", _existingKeys);
            LoadIntProperty     (mappingNode, "m_RootOrder",            rootOrder);
            LoadVector3Property (mappingNode, "m_LocalEulerAnglesHint", localEulerAnglesHint);
            
            LoadYamlProperties (mappingNode);
            
            return this;
        }
        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            SaveVector4Property (mappingNode, "m_LocalRotation", localRotation);
            SaveVector3Property (mappingNode, "m_LocalPosition", localPosition);
            SaveVector3Property (mappingNode, "m_LocalScale",    localScale);
            
            if (childrenIds.assigned)
            {
                var childNodes = new YamlSequenceNode();
                foreach (var childId in childrenIds.value)
                {
                    var childNode = new YamlMappingNode();
                    childNode.Style = MappingStyle.Flow;
                    childNode.Add(new YamlScalarNode("fileID"), new YamlScalarNode(childId.ToString()));
                    childNodes.Add(childNode);
                }
                mappingNode.Add(new YamlScalarNode("m_Children"), childNodes);
            }
            
            parentId.Save(mappingNode);
            SaveIntProperty     (mappingNode, "m_RootOrder",            rootOrder);
            SaveVector3Property (mappingNode, "m_LocalEulerAnglesHint", localEulerAnglesHint);
            
            SaveYamlProperties  (mappingNode);
        }
        public override bool Diff(object previousObj)
        {
            TransformData previous = previousObj as TransformData;
            _wasModified = DiffBase(previous);
            
            _wasModified |= DiffArrayProperty (localRotation,       previous.localRotation);
            _wasModified |= DiffArrayProperty (localPosition,       previous.localPosition);
            _wasModified |= DiffArrayProperty (localScale,          previous.localScale);
            _wasModified |= DiffArrayProperty (childrenIds,         previous.childrenIds);
            _wasModified |= parentId.Diff(previous.parentId);
            _wasModified |= DiffProperty      (rootOrder,           previous.rootOrder);
            _wasModified |= DiffArrayProperty (localEulerAnglesHint,previous.localEulerAnglesHint);

            DiffYamlProperties(previousObj);
            
            return WasModified;
        }
        public override void Merge(object baseObj, object thiersObj, MergeReport report, bool takeTheirs = true)
        {
            var thiers = thiersObj as TransformData;
            
            report.Push(LogString(), ScenePath);
                
            MergeBase(thiersObj, report, takeTheirs);
            
            localRotation.value         = MergePropArray (nameof(localRotation),        localRotation,        thiers.localRotation,        report, takeTheirs);
            localPosition.value         = MergePropArray (nameof(localPosition),        localPosition,        thiers.localPosition,        report, takeTheirs);
            localScale.value            = MergePropArray (nameof(localScale),           localScale,           thiers.localScale,           report, takeTheirs);
            
            // Children ID's in this case are duplicate information, we will rebuild these after the merge is complete from
            //  all transforms known parents
            childrenIds.value = null;

            parentId.Merge(thiers.parentId, report, takeTheirs);
            rootOrder.value             = MergeProperties(nameof(rootOrder),            rootOrder,            thiers.rootOrder,            report, takeTheirs);
            localEulerAnglesHint.value  = MergePropArray (nameof(localEulerAnglesHint), localEulerAnglesHint, thiers.localEulerAnglesHint, report, takeTheirs);

            MergeYamlProperties(thiersObj, report, takeTheirs);
            
            report.Pop();
        }
    }
}