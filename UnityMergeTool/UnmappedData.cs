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
        public UnmappedData Load(YamlMappingNode mappingNode, long fileId, string typeName, string tag)
        {
            LoadBase(mappingNode, fileId, typeName, tag);
            LoadYamlProperties(mappingNode);
            return this;
        }
        public override void Save(YamlMappingNode mappingNode)
        {
            SaveBase(mappingNode);
            SaveYamlProperties(mappingNode);
        }

        public override bool Diff(object previousObj)
        {
            DiffBase((BaseData)previousObj);
            DiffYamlProperties(previousObj);
            return WasModified;
        }

        public override void Merge(object baseObj, object thiersObj, ref string conflictReport, ref bool conflictsFound,
            bool takeTheirs = true)
        {
            var conflictReportLines = new List<string>();
            MergeYamlProperties(thiersObj, conflictReportLines, takeTheirs);
            
            if (conflictReportLines.Count > 0)
            {
                MergeYamlProperties(thiersObj, conflictReportLines, takeTheirs);
                
                conflictsFound = true;
                conflictReport += "\nConflict on "+typeName+" at "+ScenePath+"\n";
                foreach (var line in conflictReportLines) {
                    conflictReport += "  " + line + "\n";
                }
            }
        }
    }
}