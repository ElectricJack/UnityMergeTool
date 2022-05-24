using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    abstract class BaseSceneData : BaseData
    {
        public DiffableProperty<ulong> correspondingSourceObjectId = new DiffableProperty<ulong>();
        public DiffableProperty<ulong> prefabInstanceId            = new DiffableProperty<ulong>();
        public DiffableProperty<ulong> prefabAssetId               = new DiffableProperty<ulong>();
            
        protected void LoadBase(YamlMappingNode mappingNode, ulong fileId)
        {
            base.LoadBase(mappingNode, fileId);
            correspondingSourceObjectId.value = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_CorrespondingSourceObject"), "fileID"));
            prefabInstanceId.value            = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_PrefabInstance"), "fileID"));
            prefabAssetId.value               = ulong.Parse(Helpers.GetChildScalarValue(Helpers.GetChildMapNode(mappingNode, "m_PrefabAsset"), "fileID"));
            
            _existingKeys.Add("m_CorrespondingSourceObject");
            _existingKeys.Add("m_PrefabInstance");
            _existingKeys.Add("m_PrefabAsset");
        }

        protected bool DiffBase(BaseSceneData previous)
        {
            base.DiffBase(previous);
            
            correspondingSourceObjectId.valueChanged = correspondingSourceObjectId.value != previous.correspondingSourceObjectId.value;
            prefabInstanceId.valueChanged            = prefabInstanceId.value            != previous.prefabInstanceId.value;
            prefabAssetId.valueChanged               = prefabAssetId.value               != previous.prefabAssetId.value;

            _wasModified = _wasModified || correspondingSourceObjectId.valueChanged || prefabInstanceId.valueChanged || prefabAssetId.valueChanged;
            return WasModified;
        }
    }
}