using System.Collections.Generic;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    public interface IMergable
    {
        bool WasModified { get; }
        string ScenePath { get; }
        bool Matches(IMergable other);
        void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true);
    }
    abstract class BaseData : PropertyMerge, IMergable
    {
        public string                    typeName;
        public string                    tag;
        
        public DiffableProperty<long>    fileId                      = new DiffableProperty<long>(); // Assigned from anchor id of root node in document
        public DiffableFileId            gameObjectId                = new DiffableFileId();
        public DiffableProperty<int>     objectHideFlags             = new DiffableProperty<int>();
        public DiffableProperty<int>     serializedVersion           = new DiffableProperty<int>();
        public DiffableFileId            correspondingSourceObjectId = new DiffableFileId();
        public DiffableFileId            prefabInstanceId            = new DiffableFileId();
        public DiffableFileId            prefabAssetId               = new DiffableFileId();

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

        
        protected void LoadBase(YamlMappingNode mappingNode, long fileId, string typeName, string tag)
        {
            this.typeName = typeName;
            this.fileId.value = fileId;
            this.tag = tag;
            
            _existingKeys.Clear();
           
            LoadIntProperty                 (mappingNode, "m_ObjectHideFlags", objectHideFlags);
            correspondingSourceObjectId.Load(mappingNode, "m_CorrespondingSourceObject", _existingKeys); 
            prefabInstanceId.Load           (mappingNode, "m_PrefabInstance", _existingKeys);
            prefabAssetId.Load              (mappingNode, "m_PrefabAsset", _existingKeys);
            LoadIntProperty                 (mappingNode, "serializedVersion", serializedVersion);
            gameObjectId.Load               (mappingNode, "m_GameObject", _existingKeys);
        }

        protected void SaveBase(YamlMappingNode mappingNode)
        {
            mappingNode.Tag = new TagName(tag);

            SaveIntProperty                 (mappingNode,"m_ObjectHideFlags", objectHideFlags);
            correspondingSourceObjectId.Save(mappingNode);
            prefabInstanceId.Save           (mappingNode);
            prefabAssetId.Save              (mappingNode);
            SaveIntProperty                 (mappingNode,"serializedVersion", serializedVersion);
            gameObjectId.Save               (mappingNode);
        }

        protected bool DiffBase(BaseData previous)
        {
            _wasModified = false;
            _wasModified |= DiffProperty(fileId,            previous.fileId);

            _wasModified |= correspondingSourceObjectId.Diff(previous.correspondingSourceObjectId);
            _wasModified |= gameObjectId.Diff(previous.gameObjectId);
            _wasModified |= DiffProperty(objectHideFlags,   previous.objectHideFlags);
            _wasModified |= DiffProperty(serializedVersion, previous.serializedVersion);

            _wasModified |= prefabInstanceId.Diff(previous.prefabInstanceId);
            _wasModified |= prefabAssetId.Diff(previous.prefabAssetId);
            
            return WasModified;
        }
        
        protected void MergeBase(object thiersObj, List<string> conflictReportLines, bool takeTheirs = true)
        {
            var thiers = thiersObj as BaseData;
            fileId.value                      = MergeProperties(nameof(fileId),                     fileId,                      thiers.fileId,                      conflictReportLines, takeTheirs);
            objectHideFlags.value             = MergeProperties(nameof(objectHideFlags),            objectHideFlags,             thiers.objectHideFlags,             conflictReportLines, takeTheirs);
            
            gameObjectId.Merge               (thiers.gameObjectId, conflictReportLines, takeTheirs);
            correspondingSourceObjectId.Merge(thiers.correspondingSourceObjectId, conflictReportLines, takeTheirs);
            prefabInstanceId.Merge           (thiers.prefabInstanceId, conflictReportLines, takeTheirs);
            prefabAssetId.Merge              (thiers.prefabAssetId, conflictReportLines, takeTheirs);
        }
    }
    
    
}