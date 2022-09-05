using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace UnityMergeTool
{
    public class MergeReport
    {
        public bool  HasConflicts => _hasConflicts;
        private bool _hasConflicts = false;



        public void Begin()
        {
            Push("Root", "/");
        }
        public void End()
        {
            Pop();
        }

        class ReportNode
        {
            public string                         name;
            public string?                        type = null;
            public Dictionary<string, ReportNode> children = new Dictionary<string, ReportNode>();
            public List<string>                   report = new List<string>();
        }

        private ReportNode       _root = new ReportNode();
        private List<ReportNode> _stack = new List<ReportNode>();
        private ReportNode       _activeItem = null;

        public override string ToString()
        {
            var sb = new StringBuilder();
            LogReportRecursive(_root, sb);
            return sb.ToString();
        }
        
        public void Push(string type, string scenePath)
        {
            var pathItems = scenePath.Split("/");
            _stack.Add(AddNodesRecursive(_root, pathItems));
            _activeItem = _stack[_stack.Count - 1];
        }

        private bool LogReportRecursive(ReportNode node, StringBuilder sb, string indent = "")
        {
            if (node == null) return false;
            
            bool hasReport = false;
            StringBuilder childReport = new StringBuilder();
            foreach (var child in node.children) {
                hasReport = LogReportRecursive(child.Value, childReport, $"{indent}    ") || hasReport;
            }

            hasReport = node.report.Count > 0 || hasReport;
            
            if (hasReport)
            {
                sb.Append($"{indent}/{node.name}\n");
                foreach (var line in node.report) {
                    sb.Append($"{indent}    {line}\n");
                }
                
                sb.Append(childReport.ToString());    
            }
            

            return hasReport;
        }

        private ReportNode AddNodesRecursive(ReportNode node, string[] nodeNames)
        {
            if (nodeNames == null || nodeNames.Length == 0)
                return node;

            if (node.children.ContainsKey(nodeNames[0]))
            {
                return AddNodesRecursive(node.children[nodeNames[0]], nodeNames.Skip(1).ToArray());
            }

            var newChild = new ReportNode() {
                name = nodeNames[0]
            };
            node.children.Add(newChild.name, newChild);
            return AddNodesRecursive(newChild, nodeNames.Skip(1).ToArray());
        }

        public void Pop()
        {
            _stack.RemoveAt(_stack.Count-1);
            if (_stack.Count > 0)
                _activeItem = _stack[_stack.Count - 1];
            else
                _activeItem = null;
        }
        
        public void AddConflict(IMergable conflicted, string message)
        {
            _hasConflicts = true;
            //Console.WriteLine(conflicted.ScenePath + $" Conflicts! {message}");
            _activeItem.report.Add($"CONFLICT! {message}");
        }

        public void AddConflictProperty<T>(string propertyName, DiffableProperty<T> mine, DiffableProperty<T> theirs)
        {
            _hasConflicts = true;
            //Console.WriteLine($"{parent} property {propertyName} conflicts! {mine.value} {mine.oldValue} {theirs.value} {theirs.oldValue}");
            _activeItem.report.Add($"Property {propertyName} conflicts! {mine.value} {mine.oldValue} {theirs.value} {theirs.oldValue}");
        }

        public void LogPropertyModification<T>(string propertyName, DiffableProperty<T> property, bool mine = true)
        {
            if (mine) //Console.WriteLine($"{parent} property {propertyName} modified in mine - new: {property.value} old: {property.oldValue}");
                _activeItem.report.Add($"Property {propertyName} modified in mine - new: {property.value} old: {property.oldValue}");
            else      //Console.WriteLine($"{parent} property {propertyName} modified in theirs - new: {property.value} old: {property.oldValue}");
                _activeItem.report.Add($"Property {propertyName} modified in theirs - new: {property.value} old: {property.oldValue}");
        }


    }
}