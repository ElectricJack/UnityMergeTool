using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class MonoBehaviorData : BaseSceneData
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
        public MonoBehaviorData Load(YamlMappingNode mappingNode, ulong fileId)
        {
            LoadBase(mappingNode, fileId);
                
            gameObjectId.value    = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_GameObject"), "fileID"));
            enabled.value         = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_Enabled"));
            editorHideFlags.value = int.Parse(Helpers.GetChildScalarValue(mappingNode, "m_EditorHideFlags"));

            var scriptNode   = Helpers.GetChildMapNode(mappingNode, "m_Script");
            scriptId.value   = ulong.Parse(Helpers.GetChildScalarValue(scriptNode, "fileID"));
            scriptGuid.value = Helpers.GetChildScalarValue(scriptNode, "guid");
            scriptType.value = int.Parse(Helpers.GetChildScalarValue(scriptNode, "type"));
            
            _existingKeys.Add("m_GameObject");
            _existingKeys.Add("m_Enabled");
            _existingKeys.Add("m_EditorHideFlags");
            _existingKeys.Add("m_Script");
                
            // Now we need to find and load any remaining data
            LoadYamlProperties(mappingNode);

            return this;
        }
        
        public override bool Diff(object previousObj)
        {
            MonoBehaviorData previous = previousObj as MonoBehaviorData;
            _wasModified = DiffBase(previous);
                
            gameObjectId.valueChanged    = gameObjectId.value    != previous.gameObjectId.value;
            enabled.valueChanged         = enabled.value         != previous.enabled.value;
            editorHideFlags.valueChanged = editorHideFlags.value != previous.editorHideFlags.value;
                
            scriptId.valueChanged        = scriptId.value   != previous.scriptId.value;
            scriptGuid.valueChanged      = scriptGuid.value != previous.scriptGuid.value;
            scriptType.valueChanged      = scriptType.value != previous.scriptType.value;

            _wasModified = _wasModified || gameObjectId.valueChanged || enabled.valueChanged ||
                           editorHideFlags.valueChanged || scriptId.valueChanged || scriptGuid.valueChanged ||
                           scriptType.valueChanged;

            DiffYamlProperties(previousObj);

            return WasModified;
        }


        public override void Merge(object thiersObj, ref string conflictReport, bool takeTheirs = true)
        {
            var theirs = thiersObj as MonoBehaviorData;
            var conflictReportLines = new List<string>();
                
            fileId.value                      = MergeProperties(nameof(fileId),                        fileId,                      theirs.fileId,                      conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),               objectHideFlags,             theirs.objectHideFlags,             conflictReportLines, takeTheirs);
            correspondingSourceObjectId.value = MergeProperties(nameof(correspondingSourceObjectId),   correspondingSourceObjectId, theirs.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.value            = MergeProperties(nameof(prefabInstanceId),              prefabInstanceId,            theirs.prefabInstanceId,            conflictReportLines, takeTheirs);
            prefabAssetId.value               = MergeProperties(nameof(prefabAssetId),                 prefabAssetId,               theirs.prefabAssetId,               conflictReportLines, takeTheirs);
                
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
