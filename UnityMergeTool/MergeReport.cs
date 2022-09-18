using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityMergeTool
{
    public class MergeReport
    {
        

        public void Begin() { _activeItem = _root; }
        public void End() { }

        
        class ReportNode
        {
            public string                         name;
            //public string?                        type = null;
            public Dictionary<string, ReportNode> children = new Dictionary<string, ReportNode>();
            public List<ChangeLog>                report = new List<ChangeLog>();
        }

        class ChangeLog
        {
            public string ident;
            public string message;
            public bool   takeTheirs;
            public bool   conflict;
        }

        private ReportNode       _root        = new ReportNode();
        private List<ReportNode> _stack       = new List<ReportNode>();
        private ReportNode       _activeItem  = null;
        private bool             _applyReport = false;

        public override string ToString()
        {
            var sb = new StringBuilder();
            LogReportRecursive(_root, sb);
            return sb.ToString();
        }

        public void SaveReport(string reportFile)
        {
            File.WriteAllText(reportFile, ToString());
        }
        public void LoadReport(string reportFile)
        {
            var matchPath = new Regex("([\\s]+)\\/(.*)", RegexOptions.Singleline | RegexOptions.Compiled);
            var matchPathWithOverride = new Regex("([\\s]+)\\/(.*)\\[OVERRIDE: (MINE|THEIRS)\\]", RegexOptions.Singleline | RegexOptions.Compiled);
            var matchLog  = new Regex("([\\s]+)((\\[THEIRS\\] |\\[MINE\\] |\\[CONFLICT\\] )+)'(.+)'(.*)", RegexOptions.Singleline | RegexOptions.Compiled);
            
            _stack.Add(_root);
            _activeItem = _root;

            bool overrideWithTheirs = true;
            int  overrideBelowIndent = -1;
            
            var lines = File.ReadAllText(reportFile).Split("\n");
            for (int i = 1; i < lines.Length; ++i)
            {
                var line = lines[i];
                Match lineMatch;
                var lineMatchWithOverride = matchPathWithOverride.Match(line);
                if (lineMatchWithOverride.Success)
                {
                    var indentLevel  = lineMatchWithOverride.Groups[1].Value.Length / 4;
                    overrideWithTheirs  = lineMatchWithOverride.Groups[3].Value.Equals("THEIRS");
                    overrideBelowIndent = indentLevel;
                    lineMatch = lineMatchWithOverride;
                }
                else
                {
                    lineMatch = matchPath.Match(line);
                }
                
                if (lineMatch.Success)
                {
                    var indentLevel = lineMatch.Groups[1].Value.Length / 4;
                    if (indentLevel < _stack.Count)
                    {
                        while (indentLevel < _stack.Count)
                            _stack.RemoveAt(_stack.Count-1);

                        // Clear override if we go below where it was set
                        if (_stack.Count <= overrideBelowIndent) {
                            overrideBelowIndent = -1;
                        }
                        
                        _activeItem = _stack[_stack.Count-1];
                    }
                    if (indentLevel == _stack.Count)
                    {
                        var node = new ReportNode();
                        node.name = lineMatch.Groups[2].Value.Trim();
                        _activeItem.children.Add(node.name, node);
                        _activeItem = node;
                        _stack.Add(node);
                    }

                    continue;
                }
                
                var logMatch = matchLog.Match(line);
                if (logMatch.Success)
                {
                    var indentLevel = logMatch.Groups[1].Value.Length / 4;
                    if (indentLevel != _stack.Count)
                    {
                        Console.WriteLine($"ERROR - Invalid indentation in report on line {i}");
                        continue;
                    }

                    var flags = logMatch.Groups[2].Value;
                    var conflict = flags.Contains("CONFLICT");
                    var takeTheirs = flags.Contains("THEIRS");

                    if (overrideBelowIndent != -1 && indentLevel > overrideBelowIndent)
                    {
                        takeTheirs = overrideWithTheirs;
                        conflict = false;
                    }
                    
                    var ident = logMatch.Groups[4].Value;
                    var message = logMatch.Groups[5].Value.Trim();
                        
                    _activeItem.report.Add(new ChangeLog()
                    {
                        ident      = ident,
                        conflict   = conflict,
                        takeTheirs = takeTheirs,
                        message    = message
                    });
                }
            }
            
            _stack.Clear();
            _stack.Add(_root);
            _activeItem = _root;
            _applyReport = true;
        }
        
        public void Push(string type, string scenePath)
        {
            var pathItems = scenePath.Split("/").Where(item => !string.IsNullOrEmpty(item)).ToArray();
            if (_applyReport)
            {
                _activeItem = _root;
                foreach (var item in pathItems)
                {
                    // If we can't locate the full path, it's not in the saved report and we need to just
                    //  use default behavior when merging
                    if (!_activeItem.children.ContainsKey(item))
                    {
                        _activeItem = null;
                        break;
                    }
                    _activeItem = _activeItem.children[item];
                }
            }
            else
            {
                _activeItem = AddNodesRecursive(_root, pathItems);
            }
            _stack.Add(_activeItem);
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
                foreach (var changeLog in node.report) {
                    sb.Append($"{indent}    {(changeLog.conflict? "[CONFLICT] " : "")}[{(changeLog.takeTheirs? "THEIRS" : "MINE")}] '{changeLog.ident}' {changeLog.message}\n");
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

        public bool Changed(IMergable modified, bool conflict, string message, bool takeTheirs)
        {
            if (_applyReport)
            {
                if (_activeItem != null)
                {
                    var match = _activeItem.report.FirstOrDefault(item => item.ident.Equals(modified.LogString()));
                    if (match != null) {
                        return match.takeTheirs;
                    }
                }
                // Default to take theirs if we don't have this in the report
                return true;
            } 

            LogChange(modified, conflict, message, takeTheirs);
            return false;
        }

        private void LogChange(IMergable modified, bool conflict, string message, bool takeTheirs)
        {
            _activeItem.report.Add(new ChangeLog()
            {
                ident      = modified.LogString(),
                conflict   = conflict,
                message    = message,
                takeTheirs = takeTheirs
            });
        }

        public bool PropertyChange<T>(string propertyName, DiffableProperty<T> mine, DiffableProperty<T> thiers)
        {
            if (_applyReport)
            {
                if (_activeItem != null)
                {
                    var match = _activeItem.report.FirstOrDefault(item => item.ident.Equals(propertyName));
                    if (match != null)
                    {
                        return match.takeTheirs;
                    }
                }
                
                if (mine   == null) { return true; }
                if (thiers == null) { return false; }

                if (mine.valueChanged && thiers.valueChanged)
                {
                    return true; // Take theirs on unresolved conflict 
                }
                
                if (mine.valueChanged)
                {
                    return false; // Default to theirs
                }

                return true; // Default to ours
            }

            LogPropertyChange(propertyName, mine, thiers);
            return false;
        }
        
        private void LogPropertyChange<T>(string propertyName, DiffableProperty<T> mine, DiffableProperty<T> thiers)
        {
            if (mine == null)
            {
                if (thiers.valueChanged)
                {
                    LogPropertyConflict(propertyName, mine, thiers);
                    return; 
                }
                    
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = false,
                    message    = $"Property removed in mine - {thiers.value}",
                    takeTheirs = false
                });
                
                return;
            }

            if (thiers == null)
            {
                if (mine.valueChanged)
                {
                    LogPropertyConflict(propertyName, mine, thiers);
                    return;
                }
                
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = false,
                    message    = $"Property removed in thiers - {mine.value}",
                    takeTheirs = true 
                });
                
                return;
            }

            if (mine.valueChanged && thiers.valueChanged)
            {
                LogPropertyConflict(propertyName, mine, thiers);
                return; 
            }
            
            if (mine.valueChanged)
            {
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = false,
                    message    = mine.oldValue != null? $"Property modified in mine - new: {mine.value} old: {mine.oldValue}" : $"Property added in mine - {mine.value}",
                    takeTheirs = false // Default to ours
                });
            }

            if (thiers.valueChanged)
            {
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = false,
                    message    = thiers.oldValue != null? $"Property modified in theirs - new: {thiers.value} old: {thiers.oldValue}" : $"Property added in theirs - {thiers.value}",
                    takeTheirs = true // Default to theirs?
                });
            }
        }

        private void LogPropertyConflict<T>(string propertyName, DiffableProperty<T> mine, DiffableProperty<T> theirs)
        {
            if (mine != null && theirs != null)
            {
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = true,
                    message    = $"Property conflict - mine [ new: {mine.value} old: {mine.oldValue} ] thiers [ new: {theirs.value} old: {theirs.oldValue} ]",
                    takeTheirs = true // Default to theirs?
                });
            }
            else if (mine == null && theirs.oldValue != null)
            {
                _activeItem.report.Add(new ChangeLog()
                {
                    ident       = propertyName,
                    conflict   = true,
                    message    = $"Property conflict - mine [ deleted ] thiers [ new: {theirs.value} old: {theirs.oldValue} ]",
                    takeTheirs = true // Default to theirs?
                });
            }
            else if (theirs == null && mine.oldValue != null)
            {
                _activeItem.report.Add(new ChangeLog()
                {
                    ident = propertyName,
                    conflict = true,
                    message = $"Property conflict - mine [ new: {mine.value} old: {mine.oldValue} ] theirs [ deleted ]",
                    takeTheirs = false // Default to theirs?
                });
            }
        }


    }
}