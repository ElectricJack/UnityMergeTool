using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class UnmappedData : BaseData
    {
        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";

        public override string LogString()
        {
            var str = typeName + " "+fileId.value+" { ";
            bool first = true;
            foreach (var pair in additionalData)
            {
                str += (!first? ", " : "") + pair.Key + ": " + pair.Value.value;
                first = false;
            }
            return str + " }";
        }
        public UnmappedData Load(YamlMappingNode mappingNode, ulong fileId, string typeName)
        {
            LoadBase(mappingNode, fileId, typeName);
            LoadYamlProperties(mappingNode);
            return this;
        }

        public override bool Diff(object previousObj)
        {
            DiffYamlProperties(previousObj);
            return WasModified;
        }

        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var conflictReportLines = new List<string>();
            MergeYamlProperties(thiersObj, conflictReportLines, takeTheirs);
            
            if (conflictReportLines.Count > 0)
            {
                conflictReport += "Conflict on "+typeName+" at "+ScenePath+"\n";
                foreach (var line in conflictReportLines) {
                    conflictReport += "  " + line + "\n";
                }
            }
        }
    }
}