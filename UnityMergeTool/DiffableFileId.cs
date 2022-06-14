using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace UnityMergeTool
{
    class DiffableFileId : PropertyMerge, IMergable
    {
        public bool                     assigned = false;
        public string                   propertyName;
        public DiffableProperty<ulong>  fileId = new DiffableProperty<ulong>();
        public DiffableProperty<string> guid   = new DiffableProperty<string>();
        public DiffableProperty<int>    type   = new DiffableProperty<int>();
        
        public DiffableFileId Load(YamlMappingNode parent, string propertyName=null, List<string> existingKeys=null)
        {
            this.propertyName = propertyName;
            var target = propertyName == null? parent : Helpers.GetChildMapNode(parent, propertyName);
            if (target != null)
            {
                LoadUlongProperty(target, "fileID", fileId);
                LoadStringProperty(target, "guid", guid);
                LoadIntProperty(target, "type", type);
                
                existingKeys?.Add(propertyName);
                assigned = true;
            }
            
            return this;
        }
        public void Save(YamlMappingNode parent)
        {
            if (!assigned) return;
            
            var target = propertyName == null? parent : new YamlMappingNode();
            
            SaveUlongProperty(target, "fileID", fileId);
            SaveStringProperty(target, "guid", guid);
            SaveIntProperty(target, "type", type);
            
            target.Style = MappingStyle.Flow;
            
            if (propertyName != null)
                parent.Add(new YamlScalarNode(propertyName), target);
        }

        public bool Diff(DiffableFileId theirs)
        {
            if (!assigned && !theirs.assigned)
                return false;
            
            _wasModified |= DiffProperty(fileId, theirs.fileId);
            _wasModified |= DiffProperty(guid, theirs.guid);
            _wasModified |= DiffProperty(type, theirs.type);
            
            return WasModified;
        }

        public void Merge(object baseObj, object theirsObj, ref string conflictReport, ref bool conflictsFound, bool takeTheirs = true)
        {
            DiffableFileId theirs = theirsObj as DiffableFileId;
            var conflictReportLines = new List<string>();
            Merge(theirs, conflictReportLines, takeTheirs);

            if (conflictReportLines.Count > 0)
            {
                conflictsFound = true;
                conflictReport += "\nConflict on FileId: " + fileId.value + " - " + guid.value +"\n";
                foreach (var line in conflictReportLines)
                {
                    conflictReport += "  " + line + "\n";
                }
            }
        }
        public void Merge(DiffableFileId theirs, List<string> conflictReportLines, bool takeTheirs = true)
        {
            MergeProperties(propertyName + ".fileID", fileId, theirs.fileId,  conflictReportLines, takeTheirs);
            MergeProperties(propertyName + ".guid", guid, theirs.guid,  conflictReportLines, takeTheirs);
            MergeProperties(propertyName + ".type", type, theirs.type,  conflictReportLines, takeTheirs);
        }
        
        public bool Matches(DiffableFileId other)
        {
            return fileId.value == other.fileId.value &&
                   guid.value   == other.guid.value &&
                   type.value   == other.type.value;
        }

        public bool Matches(IMergable other)
        {
            DiffableFileId diff = other as DiffableFileId;
            if (diff == null) return false;
            return Matches(diff);
        }


    }
}