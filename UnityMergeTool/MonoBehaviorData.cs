using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class MonoBehaviorData : BaseData
    {
        public DiffableProperty<ulong>     gameObjectId    = new DiffableProperty<ulong>();
        public DiffableProperty<int>       enabled         = new DiffableProperty<int>();
        public DiffableProperty<int>       editorHideFlags = new DiffableProperty<int>();
        public DiffableProperty<ulong>     scriptId        = new DiffableProperty<ulong>();
        public DiffableProperty<string>    scriptGuid      = new DiffableProperty<string>();
        public DiffableProperty<int>       scriptType      = new DiffableProperty<int>();
        
        public GameObjectData              gameObjectRef = null;

        public override string ScenePath => gameObjectRef != null? gameObjectRef.ScenePath : "";
        public string LogString()
        {
            var str = "MonoBehavior "+fileId.value+" - guid: " + scriptGuid.value + " enabled: " + enabled.value + " { ";
            bool first = true;
            foreach (var pair in additionalData)
            {
                str += (!first? ", " : "") + pair.Key + ": " + pair.Value.value;
                first = false;
            }
            return str + " }";
        }
        public MonoBehaviorData Load(YamlMappingNode mappingNode, ulong fileId, string typeName)
        {
            LoadBase(mappingNode, fileId, typeName);
            
            LoadFileIdProperty(mappingNode, "m_GameObject",   gameObjectId);
            LoadIntProperty(mappingNode, "m_Enabled",         enabled);
            LoadIntProperty(mappingNode, "m_EditorHideFlags", editorHideFlags);

            if (mappingNode.Children.ContainsKey(new YamlScalarNode("m_Script")))
            {
                var scriptNode   = Helpers.GetChildMapNode(mappingNode, "m_Script");
                scriptId.value   = ulong.Parse(Helpers.GetChildScalarValue(scriptNode, "fileID"));
                scriptGuid.value = Helpers.GetChildScalarValue(scriptNode, "guid");
                scriptType.value = int.Parse(Helpers.GetChildScalarValue(scriptNode, "type"));
                scriptId.assigned = true;
                scriptGuid.assigned = true;
                scriptType.assigned = true;
                _existingKeys.Add("m_Script");
            }
                
            // Now we need to find and load any remaining data
            LoadYamlProperties(mappingNode);

            return this;
        }
        
        public override bool Diff(object previousObj)
        {
            MonoBehaviorData previous = previousObj as MonoBehaviorData;
            _wasModified = DiffBase(previous);
            
            _wasModified |= DiffProperty      (gameObjectId,    previous.gameObjectId);
            _wasModified |= DiffProperty      (enabled,         previous.enabled);
            _wasModified |= DiffProperty      (editorHideFlags, previous.editorHideFlags);
            _wasModified |= DiffProperty      (scriptId,        previous.scriptId);
            _wasModified |= DiffProperty      (scriptGuid,      previous.scriptGuid);
            _wasModified |= DiffProperty      (scriptType,      previous.scriptType);

            DiffYamlProperties(previousObj);

            return WasModified;
        }


        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var theirs = thiersObj as MonoBehaviorData;
            var conflictReportLines = new List<string>();
            
            MergeBase(thiersObj, conflictReportLines, takeTheirs);
                
            gameObjectId.value    = MergeProperties(nameof(gameObjectId),    gameObjectId,    theirs.gameObjectId,    conflictReportLines, takeTheirs);
            enabled.value         = MergeProperties(nameof(enabled),         enabled,         theirs.enabled,         conflictReportLines, takeTheirs);
            editorHideFlags.value = MergeProperties(nameof(editorHideFlags), editorHideFlags, theirs.editorHideFlags, conflictReportLines, takeTheirs);
            scriptId.value        = MergeProperties(nameof(scriptId),        scriptId,        theirs.scriptId,        conflictReportLines, takeTheirs);
            scriptGuid.value      = MergeProperties(nameof(scriptGuid),      scriptGuid,      theirs.scriptGuid,      conflictReportLines, takeTheirs);
            scriptType.value      = MergeProperties(nameof(scriptType),      scriptType,      theirs.scriptType,      conflictReportLines, takeTheirs);

            MergeYamlProperties(thiersObj, conflictReportLines, takeTheirs);

            if (conflictReportLines.Count > 0)
            {
                conflictReport += "Conflict on MonoBehavior (guid: " + scriptGuid.value + ") at: " + gameObjectRef.ScenePath + "\n";
                foreach (var line in conflictReportLines) {
                    conflictReport += "  " + line + "\n";
                }
            }
        }


        
    }
}
