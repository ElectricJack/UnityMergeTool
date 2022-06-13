using System.Collections.Generic;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    public interface IMergable
    {
        bool WasModified { get; }
        bool Matches(IMergable other);
        void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true);
    }
    abstract class BaseData : PropertyMerge, IMergable
    {
        public string                    typeName;
        public string                    tag;
        
        public DiffableProperty<ulong>   fileId                      = new DiffableProperty<ulong>(); // Assigned from anchor id of root node in document
        public DiffableProperty<ulong>   gameObjectId                = new DiffableProperty<ulong>(); // Not every node has this but enough do it's worth keeping in base
        public DiffableProperty<int>     objectHideFlags             = new DiffableProperty<int>();
        public DiffableProperty<int>     serializedVersion           = new DiffableProperty<int>();
        public DiffableProperty<ulong>   correspondingSourceObjectId = new DiffableProperty<ulong>();
        public DiffableProperty<ulong>   prefabInstanceId            = new DiffableProperty<ulong>();
        public DiffableProperty<ulong>   prefabAssetId               = new DiffableProperty<ulong>();

        public GameObjectData            gameObjectRef = null;
        
        public bool Matches(IMergable other)
        {
            var baseDataOther = other as BaseData;
            if (baseDataOther == null)
                return false;

            return fileId.value.Equals(baseDataOther.fileId.value);
        }

        public abstract string ScenePath { get; }
        public abstract bool Diff(object previous);
        public abstract void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true);

        public abstract void Save(YamlMappingNode node);
        public abstract string LogString();

        
        protected void LoadBase(YamlMappingNode mappingNode, ulong fileId, string typeName, string tag)
        {
            this.typeName = typeName;
            this.fileId.value = fileId;
            this.tag = tag;
            
            _existingKeys.Clear();
           
            LoadIntProperty    (mappingNode, "m_ObjectHideFlags",           objectHideFlags);
            LoadFileIdProperty (mappingNode, "m_CorrespondingSourceObject", correspondingSourceObjectId);
            LoadFileIdProperty (mappingNode, "m_PrefabInstance",            prefabInstanceId);
            LoadFileIdProperty (mappingNode, "m_PrefabAsset",               prefabAssetId);
            LoadIntProperty    (mappingNode, "serializedVersion",           serializedVersion);
            LoadFileIdProperty (mappingNode, "m_GameObject",                gameObjectId);
        }

        protected void SaveBase(YamlMappingNode mappingNode)
        {
            mappingNode.Tag = new TagName(tag);

            SaveIntProperty(mappingNode,    "m_ObjectHideFlags",           objectHideFlags);
            SaveFileIdProperty(mappingNode, "m_CorrespondingSourceObject", correspondingSourceObjectId);
            SaveFileIdProperty(mappingNode, "m_PrefabInstance",            prefabInstanceId);
            SaveFileIdProperty(mappingNode, "m_PrefabAsset",               prefabAssetId);
            SaveIntProperty(mappingNode,    "serializedVersion",           serializedVersion);
            SaveFileIdProperty(mappingNode, "m_GameObject",                gameObjectId);
        }

        protected bool DiffBase(BaseData previous)
        {
            _wasModified = false;
            _wasModified |= DiffProperty(fileId,            previous.fileId);
            _wasModified |= DiffProperty(gameObjectId,      previous.gameObjectId);
            _wasModified |= DiffProperty(objectHideFlags,   previous.objectHideFlags);
            _wasModified |= DiffProperty(serializedVersion, previous.serializedVersion);

            _wasModified |= DiffProperty(prefabInstanceId,  previous.prefabInstanceId);
            _wasModified |= DiffProperty(prefabAssetId,     previous.prefabAssetId);
            
            return WasModified;
        }
        
        protected void MergeBase(object thiersObj, List<string> conflictReportLines, bool takeTheirs = true)
        {
            var thiers = thiersObj as BaseData;
            fileId.value                      = MergeProperties(nameof(fileId),                     fileId,                      thiers.fileId,                      conflictReportLines, takeTheirs);
            gameObjectId.value                = MergeProperties(nameof(gameObjectId),               gameObjectId,                thiers.gameObjectId,                conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),            objectHideFlags,             thiers.objectHideFlags,             conflictReportLines, takeTheirs);
            correspondingSourceObjectId.value = MergeProperties(nameof(correspondingSourceObjectId),correspondingSourceObjectId, thiers.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.value            = MergeProperties(nameof(prefabInstanceId),           prefabInstanceId,            thiers.prefabInstanceId,            conflictReportLines, takeTheirs); 
            prefabAssetId.value               = MergeProperties(nameof(prefabAssetId),              prefabAssetId,               thiers.prefabAssetId,               conflictReportLines, takeTheirs);
        }
    }
    
    
}