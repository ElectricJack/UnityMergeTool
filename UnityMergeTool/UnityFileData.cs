using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace UnityMergeTool
{
    class UnityFileData
    {
        class UnityRefMap
        {
            public string unityRef;
            public bool   stripped;
        }
        
        private string                              _yamlVersionInfo;
        private string                              _unityVersionInfo;
        private Dictionary<string,UnityRefMap>      _unityRefDict = new Dictionary<string, UnityRefMap>();
        private YamlStream                          _yaml = null;

        // Internal state after document loaded
        private List<GameObjectData>                  _goDatas;
        private Dictionary<long, GameObjectData>      _gameObjectsById;
        private List<MonoBehaviorData>                _monoDatas;
        private Dictionary<long, MonoBehaviorData>    _monosById;
        private List<TransformData>                   _transDatas;
        private Dictionary<long, TransformData>       _transformsById;
        private List<TransformData>                   _roots;
        private List<UnmappedData>                    _unmappedDatas;
        private Dictionary<long, BaseData>            _allDatasById;
        private List<BaseData>                        _allDatas;
        
        
        public UnityFileData() {}
        public UnityFileData(string path) { LoadFile(path); }


        private string GetPathWithoutHash(string path)
        {
            Regex pathRegex = new Regex(@"(.*)(_[0-9]+)(\..*)");
            var match = pathRegex.Match(path);
            if (match.Success)
            {
                return match.Groups[1].Value + match.Groups[3].Value;    
            }

            return path;
        }
        

        public void LoadFile(string path)
        {
            // Check that the file exists
            if (!File.Exists(path))
            {
                Console.WriteLine("Input file doesn't exist");
                return;
            }

            var parseLog = new StringBuilder();
            
            // PreProcess and load the file
            _yaml = new YamlStream();

            // Load the previously saved off file instead of the file generated for this 
            //  diff it exists. If this exists it means we wanted to make an edit of the file.
            var noHashPath = GetPathWithoutHash(path);
            if (File.Exists($"{noHashPath}.txt"))
                path = $"{noHashPath}.txt";
            
            var fileString = File.ReadAllText(path);
            var processedLines = PreProcessUnityYAML(fileString);
            using (var reader = new StringReader(processedLines))
            {
                try
                {
                    _yaml.Load(reader);
                    
                    // Process yaml here into internal node representation
                    LoadYamlStream();
                }
                catch (Exception e)
                {
                    parseLog.AppendLine($"Error Parsing: {path}\n");
                    parseLog.AppendLine(e.Message);
                    parseLog.AppendLine(e.StackTrace);

                    if (e.Message.Contains("Line:"))
                    {
                        Regex lineNumberRegex = new Regex(@"Line:\s(\d+)");
                        Match match = lineNumberRegex.Match(e.Message);
                        
                        if (match.Success) {
                            int lineNumber = int.Parse(match.Groups[1].Value);
                            var lines = processedLines.Split("\n");

                            parseLog.AppendLine("\n");

                            var startLine = Math.Max(0, lineNumber - 5);
                            var endLine = Math.Min(lineNumber + 5, lines.Length);
                            for (int i = startLine; i < endLine; ++i)
                            {
                                parseLog.AppendLine($"{i+1}: {lines[i]}");
                            }
                        }
                    }
                    
                    File.WriteAllText($"{noHashPath}.parse-error.txt", parseLog.ToString());
                    File.WriteAllText($"{noHashPath}.txt", fileString);

                    throw;
                }
            }
        }

        public void SaveFile(string path)
        {
            if (_yaml == null)
                return;
            
            // Save out updated yaml document
            var sw = new StringWriter();
            sw.WriteLine(_yamlVersionInfo);
            sw.WriteLine(_unityVersionInfo);
            var yaml = SaveYamlStream();
            yaml.Save(sw);
            
            File.WriteAllText(path, PostProcessUnityYAML(sw.ToString()));
        }

        public bool Diff(UnityFileData baseFile)
        {
            bool foundDifferences = _allDatas.Count != baseFile._allDatas.Count;

            return DiffData(baseFile._allDatasById, _allDatas) || foundDifferences;
        }

        public UnityFileData Merge(UnityFileData baseFile, UnityFileData thierFile, MergeReport report)
        {
            bool myDiff    = Diff(baseFile);
            bool thierDiff = thierFile.Diff(baseFile);

            // Nothing to do
            if (!myDiff && !thierDiff) return this;

            // If my document has no changes take theirs
            if (!myDiff) return thierFile;

            report.Begin();
            _allDatas = MergeData(baseFile._allDatas, _allDatas, thierFile._allDatas, report);
            RebuildLinks();
            report.End();
            
            // @TODO: Take the newer of the two versions? In practice everything seems to use the same versions
            //var mineYamlChanged   = baseFile._yamlVersionInfo != _yamlVersionInfo;
            //var theirYamlChanged  = baseFile._yamlVersionInfo != thierFile._yamlVersionInfo;
            //var mineUnityChanged  = baseFile._unityVersionInfo != _unityVersionInfo;
            //var theirUnityChanged = baseFile._unityVersionInfo != thierFile._unityVersionInfo;

            // Combine all the unity ref dictionaries for post processing step
            foreach (var item in baseFile._unityRefDict)
            {
                if(!_unityRefDict.ContainsKey(item.Key))
                    _unityRefDict.Add(item.Key, item.Value);
            }
            foreach (var item in thierFile._unityRefDict)
            {
                if(!_unityRefDict.ContainsKey(item.Key))
                    _unityRefDict.Add(item.Key, item.Value);
            }

            return this;
        }

        public void LogGameObjects()
        {
            foreach (var root in _roots)
            {
                if (root.gameObjectRef == null)
                {
                    Console.WriteLine(root.LogString());
                }
                else
                    LogGameObjectRecurse(root.gameObjectRef);                
            }
        }
        private void LogGameObjectRecurse(GameObjectData node, string indent = "")
        {
            if (node == null) return;
            
            Console.WriteLine(node.ScenePath + " " + node.LogString());
            Console.WriteLine(indent + "  - " + node.transformRef.LogString());
            foreach (var componentRef in node.componentRefs)
                Console.WriteLine(indent + "  - " + componentRef.LogString());
            
            foreach (var child in node.childRefs) {
                LogGameObjectRecurse(child, indent + "  ");
            }
        }
        
        
        private bool DiffData<T>(Dictionary<long, T> baseDataById, List<T> myData) where T : BaseData
        {
            var foundDifferences = false;
            foreach (var monoData in myData)
            {
                var monoPath = monoData.ScenePath;
                if (baseDataById.ContainsKey(monoData.fileId.value))
                {
                    var baseComponent = baseDataById[monoData.fileId.value];
                    var basePath = baseComponent.ScenePath;
                    
                    if (!basePath.Equals(monoPath))
                        foundDifferences = true;

                    if (monoData.Diff(baseComponent))
                        foundDifferences = true;
                }
                else
                    foundDifferences = true;
            }

            return foundDifferences;
        }
        public static List<T> MergeData<T>(List<T> baseDatas, List<T> myData, List<T> thierData, MergeReport report) where T : IMergable
        {
            // First determine what I added and removed and what they added and removed.
            FindAddedAndRemoved<T>(baseDatas, myData,    out List<T> myAdded,    out List<T> myRemoved );
            FindAddedAndRemoved<T>(baseDatas, thierData, out List<T> thierAdded, out List<T> thierRemoved );

            List<T> toKeep = new List<T>();
            
            // Check if the they modified anything that we removed or vice versa, if so we need to report it and KEEP the data that was removed (easier to remove it again later then to add it back)
            foreach (var myRemovedData in myRemoved)
            {
                // If we find data I removed in their dataset and it was modified
                var foundRemoved = thierData.FirstOrDefault(item => item.Matches(myRemovedData));
                if (foundRemoved is {WasModified: true})
                {
                    report.Push(foundRemoved.LogString(), foundRemoved.ScenePath);
                    var takeTheirs = report.Changed(foundRemoved, true,$"Element was removed in mine, but modified in theirs.", true);
                    if (takeTheirs) toKeep.Add(foundRemoved);
                    report.Pop();
                }
            }

            foreach (var thierRemovedData in thierRemoved)
            {
                // If we find data they removed that I changed
                var foundRemoved = myData.FirstOrDefault(item => item.Matches(thierRemovedData));
                if (foundRemoved is {WasModified: true})
                {
                    report.Push(foundRemoved.LogString(), foundRemoved.ScenePath);
                    var takeTheirs = report.Changed(foundRemoved, true, $"Element was removed in theirs, but modified in mine.", true);
                    if (!takeTheirs) toKeep.Add(foundRemoved);
                    report.Pop();
                }
            }
            
            // Take the base minus everything either of us removed, store this as all shared data.
            var shared = new List<T>();
            foreach (var baseData in baseDatas)
            {
                // Skip any that we removed
                var myFoundRemoved = myRemoved.FirstOrDefault(item => item.Matches(baseData));
                if (myFoundRemoved != null)
                {
                    report.Push(myFoundRemoved.LogString(), myFoundRemoved.ScenePath);
                    var takeTheirs = report.Changed(myFoundRemoved,  false,"Element removed in mine", false);
                    if (takeTheirs) toKeep.Add(myFoundRemoved);
                    report.Pop();
                    // Log the change
                    continue;
                }
                    
                
                // Skip any that they removed
                var theyFoundRemoved = thierRemoved.FirstOrDefault(item => item.Matches(baseData));
                if (theyFoundRemoved != null)
                {
                    report.Push(theyFoundRemoved.LogString(), theyFoundRemoved.ScenePath);
                    var takeTheirs = report.Changed(theyFoundRemoved,  false,"Element removed in theirs", true);
                    if (!takeTheirs) toKeep.Add(theyFoundRemoved);
                    report.Pop();
                    continue;
                }
                    

                // Add to shared list
                shared.Add(baseData);
            }
            
            // Merge and check for conflicts within this shared data.
            foreach (var sharedData in shared)
            {
                // Finding these should be guaranteed because we already determined it's in all sets
                var foundInBase   = baseDatas.First(item => item.Matches(sharedData));
                var foundInMine   = myData.First(item => item.Matches(sharedData));
                var foundInTheirs = thierData.First(item => item.Matches(sharedData));

                foundInMine.Merge(foundInBase, foundInTheirs, report);
            }
            
            // Handle anything added from either side
            var dontAdd = new List<T>();
            foreach (var added in myAdded) {
                report.Push(added.LogString(), added.ScenePath);
                var takeTheirs = report.Changed(added, false, "Element added in mine", false);
                if (takeTheirs) dontAdd.Add(added);
                report.Pop();
            }
            foreach (var toRemove in dontAdd)
                myData.Remove(toRemove);

            dontAdd.Clear();
            foreach (var added in thierAdded) {
                report.Push(added.LogString(), added.ScenePath);
                var takeTheirs = report.Changed(added, false, "Element added in theirs", true);
                if(!takeTheirs) dontAdd.Add(added);
                report.Pop();
            }
            foreach (var toRemove in dontAdd)
                thierAdded.Remove(toRemove);
            
            // Add anything new from theirs to mine, there will be no conflicts here
            myData.AddRange(thierAdded);
            
            // Add anything we decided to keep from removed, but don't add duplicate data
            foreach (var item in toKeep)
            {
                var foundDuplicate = myData.FirstOrDefault(data => data.FileId == item.FileId);
                if(foundDuplicate != null) continue;
                
                myData.Add(item);
            }

            return myData;
        }
        private static void FindAddedAndRemoved<T>(List<T> baseData, List<T> currentData, out List<T> added, out List<T> removed) where T : IMergable
        {
            // Create output lists
            added   = new List<T>();
            removed = new List<T>();

            foreach (var data in baseData)
            {
                // Check if we can find  base data inside current
                var foundItem = currentData.FirstOrDefault(item => data.Matches(item) );
                if (foundItem == null) {
                    // It was removed in the current data
                    removed.Add(data);
                }
            }
            
            foreach (var data in currentData)
            {
                // Check if we can find current data inside base
                var foundItem = baseData.FirstOrDefault(item => data.Matches(item));
                if (foundItem == null) {
                    // It was added in the current data
                    added.Add(data);
                }
            }
        }
        
        private void LoadYamlStream()
        {
            _goDatas       = new List<GameObjectData>();
            _monoDatas     = new List<MonoBehaviorData>();
            _transDatas    = new List<TransformData>();
            _unmappedDatas = new List<UnmappedData>();
            _allDatas      = new List<BaseData>();

            var total = _yaml.Documents.Count;
            Console.Write($"Documents to load: {total} ");
            foreach (var doc in _yaml.Documents) {
                LoadYamlDoc(doc.RootNode);
            }
            Console.WriteLine("Done.");

            _gameObjectsById = new Dictionary<long, GameObjectData>();
            _transformsById  = new Dictionary<long, TransformData>();
            _monosById       = new Dictionary<long, MonoBehaviorData>();
            _allDatasById    = new Dictionary<long, BaseData>();
            
            RebuildLinks();
        }
        private void LoadYamlDoc(YamlNode node)
        {
            var fileId = long.Parse(node.Anchor.Value);
            
            // Examine the stream
            var mapping =  (YamlMappingNode)node;
            foreach (var entry in mapping.Children)
            {
                var scalarNode = (YamlScalarNode)entry.Key;
                if (entry.Value.NodeType != YamlNodeType.Mapping)
                    continue;

                if (scalarNode.Value.Equals("GameObject"))
                {
                    var data = new GameObjectData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    _goDatas.Add(data);
                    _allDatas.Add(data);
                }
                else if (scalarNode.Value.Equals("Transform"))
                {
                    var data = new TransformData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    _transDatas.Add(data);
                    _allDatas.Add(data);
                }
                else if (scalarNode.Value.Equals("RectTransform"))
                {
                    var data = new RectTransformData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    _transDatas.Add(data);
                    _allDatas.Add(data);
                }
                else if (scalarNode.Value.Equals("MonoBehaviour"))
                {
                    var data = new MonoBehaviorData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    _monoDatas.Add(data);
                    _allDatas.Add(data);
                }
                else if (scalarNode.Value.Equals("PrefabInstance"))
                {
                    var data = new PrefabInstanceData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    //_prefabDatas.Add(data);
                    _allDatas.Add(data);
                }
                else
                {
                    var data = new UnmappedData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value, mapping.Tag.Value);
                    _unmappedDatas.Add(data);
                    _allDatas.Add(data);
                }
            }
        }

        private YamlStream SaveYamlStream()
        {
            var yamlOut = new YamlStream();
            foreach (var allData in _allDatas)
            {
                var root = new YamlMappingNode();
                var doc  = new YamlDocument(root);
                
                doc.RootNode.Anchor = new AnchorName(allData.fileId.value.ToString());
                
                var dataNode = new YamlMappingNode();
                root.Add(allData.typeName, dataNode);
                allData.Save(dataNode);
                
                yamlOut.Add(doc);
            }
            return yamlOut;
        }

        private void RebuildLinks()
        {
            _gameObjectsById.Clear();
            _transformsById.Clear();
            _allDatasById.Clear();
            _monosById.Clear();
            
            // We have to clear and rebuild these incase items were deleted from allData during a merge
            //  in that case these lists need to be updated
            _transDatas.Clear();
            _goDatas.Clear();
            _unmappedDatas.Clear();
            _monoDatas.Clear();
            
            // Store all gameObjects, transforms and components by id first
            foreach (var allData in _allDatas)
            {
                if (allData.GetType() == typeof(MonoBehaviorData))
                {
                    var monoData = allData as MonoBehaviorData;
                    _monoDatas.Add(monoData);
                    if (!_monosById.ContainsKey(monoData.fileId.value))
                    {
                        _monosById.Add(monoData.fileId.value, monoData);
                        _allDatasById.Add(allData.fileId.value, allData);
                    }
                    else
                        Console.WriteLine($"Duplicate MonoBehaviour with id {monoData.fileId.value} found.");
                }
                else if (allData.GetType() == typeof(TransformData) || allData.GetType() == typeof(RectTransformData) )
                {
                    var transData = allData as TransformData;
                    _transDatas.Add(transData);
                    if (!_transformsById.ContainsKey(transData.fileId.value))
                    {
                        _transformsById.Add(transData.fileId.value, transData);
                        _allDatasById.Add(allData.fileId.value, allData);
                    }
                    else
                        Console.WriteLine($"Duplicate transform with id {transData.fileId.value} found.");
                }
                else if (allData.GetType() == typeof(GameObjectData))
                {
                    var gameObjectData = allData as GameObjectData;
                    _goDatas.Add(gameObjectData);
                    if (!_gameObjectsById.ContainsKey(gameObjectData.fileId.value))
                    {
                        _gameObjectsById.Add(gameObjectData.fileId.value, gameObjectData);
                        _allDatasById.Add(allData.fileId.value, allData);
                    }
                    else
                        Console.WriteLine($"Duplicate gameobject with id {gameObjectData.fileId.value} found.");
                }
                else if(allData.GetType() == typeof(UnmappedData))
                {
                    _unmappedDatas.Add(allData as UnmappedData);
                }
            }
            
            // Next build transform hierarchy, and save off any roots
            _roots = new List<TransformData>();
            foreach (var transData in _transDatas)
            {
                if (transData.parentId.fileId.value == 0)
                {
                    _roots.Add(transData);
                }

                // Associate transform with its game object
                if (_gameObjectsById.ContainsKey(transData.gameObjectId.fileId.value))
                {
                    var gameObj = _gameObjectsById[transData.gameObjectId.fileId.value];
                    gameObj.transformRef = transData;
                    transData.gameObjectRef = gameObj;
                }

                // Update parent/child relationships
                if (transData.parentId.fileId.value != 0 && _transformsById.ContainsKey(transData.parentId.fileId.value))
                {
                    transData.parentRef = _transformsById[transData.parentId.fileId.value];
                }
                
                // Find every child and make sure the parentId is set, some nodes do not serialize this, and we need this 
                //  to correctly rebuild the child array
                if (transData.childrenIds.value != null)
                {
                    foreach(var childId in transData.childrenIds.value)
                    {
                        var foundChild = _transDatas.FirstOrDefault(data => data.fileId.value == childId);
                        if (foundChild != null)
                        {
                            foundChild.parentId.fileId.value = transData.FileId;    
                        }
                    }
                }
            }

            foreach (var transData in _transDatas)
            {
                var children = _transDatas.Where(data => data.parentId.fileId.value == transData.fileId.value).ToArray();
                if (children != null && children.Length > 0)
                {
                    var sortedChildren = children.OrderBy(data => data.rootOrder.value).ToArray();
                    transData.childRefs.Clear();
                    transData.childrenIds.value = new long[sortedChildren.Length];
                    var index = 0;
                    foreach (var child in sortedChildren)
                    {
                        transData.childrenIds.value[index++] = child.fileId.value;
                        if (!transData.childRefs.Contains(child))
                            transData.childRefs.Add(child);
                    }
                }
                else
                {
                    transData.childrenIds.value = Array.Empty<long>();
                }
            }

            // Update game object parent/child refs based off transform reference
            foreach (var gameObjectData in _goDatas) {
                if (gameObjectData.transformRef?.childrenIds == null)
                    continue;

                if (gameObjectData.transformRef.parentRef != null)
                    gameObjectData.parentRef = gameObjectData.transformRef.parentRef.gameObjectRef;
                
                gameObjectData.componentRefs.Clear();
                gameObjectData.childRefs.Clear();
                foreach (var childTransform in gameObjectData.transformRef.childRefs) {
                    if (childTransform.gameObjectRef != null)
                        gameObjectData.childRefs.Add(childTransform.gameObjectRef);
                }
            }
            
            // Associate all monobehaviors with thier game objects
            foreach (var monoData in _monoDatas)
            {
                if (!monoData.gameObjectId.assigned || monoData.gameObjectId.fileId.value == 0 ||
                    !_gameObjectsById.ContainsKey(monoData.gameObjectId.fileId.value)) continue;

                monoData.gameObjectRef = _gameObjectsById[monoData.gameObjectId.fileId.value];
                monoData.gameObjectRef.componentRefs.Add(monoData);
            }

            // Associate any unmapped data to gameobjects
            foreach (var unmappedData in _unmappedDatas)
            {
                if (!unmappedData.gameObjectId.assigned || unmappedData.gameObjectId.fileId.value == 0 ||
                    !_gameObjectsById.ContainsKey(unmappedData.gameObjectId.fileId.value)) continue;

                unmappedData.gameObjectRef = _gameObjectsById[unmappedData.gameObjectId.fileId.value];
                unmappedData.gameObjectRef.componentRefs.Add(unmappedData);
            }
        }
 
        private string PreProcessUnityYAML(string input)
        {
            Console.WriteLine($"PreProcessing YAML");
            var lines = input.Split('\n');
            Console.Write($"Total lines: {lines.Length} ");
            
            // Need to store off header comment for version info
            _unityRefDict.Clear();
            _yamlVersionInfo  = lines[0];
            _unityVersionInfo = lines[1];

            var options = RegexOptions.Singleline | RegexOptions.Compiled;
            var fileId =  new Regex("--- (!u![0-9]+) (&[0-9]+) ?(\\w*)", options);

            var processedLines = new StringBuilder();
            foreach (var line in lines)
            {
                var match = fileId.Match(line);
                if (match.Success && match.Groups.Count == 4)
                {
                    var unityRef = match.Groups[1].Value;
                    var yamlRef = match.Groups[2].Value;
                    _unityRefDict.Add(yamlRef, new UnityRefMap()
                    {
                        unityRef = unityRef,
                        stripped = match.Groups[3].Value.Equals("stripped")
                    });

                    processedLines.Append($"--- {unityRef} {yamlRef}\n");
                }
                else
                {
                    processedLines.Append($"{line}\n");
                }
            }
            
            Console.WriteLine("Done.");

            return processedLines.ToString();
        }
        private string PostProcessUnityYAML(string input)
        {
            var lines = input.Split('\n');
            

            var r0 = new Regex("(\\s*[\\-_\\w]:? )''", RegexOptions.Singleline | RegexOptions.Compiled);
            var r1 = new Regex("(\\s*[_\\w]+: )''", RegexOptions.Singleline | RegexOptions.Compiled);
            var r2 = new Regex("(\\s*[_\\w]+:) (&[0-9]+)", RegexOptions.Singleline | RegexOptions.Compiled);
            var r3 = new Regex("(&[0-9]+) ([_\\w]+:)", RegexOptions.Singleline | RegexOptions.Compiled);
            var r4 = new Regex("{(component: {fileID: [0-9]+})}", RegexOptions.Singleline | RegexOptions.Compiled);
            var r5 = new Regex("(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})", RegexOptions.Singleline | RegexOptions.Compiled);
            var r6 = new Regex("(\\s*)-?( ?)[_\\w]+:", RegexOptions.Singleline | RegexOptions.Compiled);
            var r7 = new Regex("(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})", RegexOptions.Singleline | RegexOptions.Compiled);
            var r8 = new Regex("(--- )?(&[0-9]+)", RegexOptions.Singleline | RegexOptions.Compiled);
            //var r9 = new Regex(, RegexOptions.Singleline | RegexOptions.Compiled);

            var processedLines = new StringBuilder();
            for(int i=0; i<lines.Length; ++i)
            {
                var line     = lines[i];
                if (line.Equals("..."))
                    continue;

                //var newLine = Regex.Replace(line, "(\\s*[\\-_\\w]:? )''", "$1");
                var newLine = r0.Replace(line, "$1");
                
                //newLine = Regex.Replace(newLine, "(\\s*[_\\w]+: )''", "$1");
                newLine = r1.Replace(newLine, "$1");
                
                //newLine = Regex.Replace(newLine, "(\\s*[_\\w]+:) (&[0-9]+)", "$1");
                newLine = r2.Replace(newLine, "$1");
                
                //newLine = Regex.Replace(newLine, "(&[0-9]+) ([_\\w]+:)", "$2");
                newLine = r3.Replace(newLine, "$2");
                
                //newLine = Regex.Replace(newLine, "{(component: {fileID: [0-9]+})}", "$1");
                newLine = r4.Replace(newLine, "$1");
                
                //var targetMatch = Regex.Match(newLine, "(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})");
                var targetMatch = r5.Match(newLine);
                if (targetMatch.Groups.Count == 4)
                {
                    var ident = targetMatch.Groups[1].Value;
                    if (newLine.Length >= 90)
                    {
                        //var leadingSpace = Regex.Replace(ident, "(\\s*)-?( ?)[_\\w]+:", "$1$2$2");
                        var leadingSpace = r6.Replace(ident, "$1$2$2");
                        //newLine = Regex.Replace(newLine, "(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})", "$1 $2\n"+leadingSpace+"  $3");
                        newLine = r7.Replace(newLine, "$1 $2\n"+leadingSpace+"  $3");
                    }
                }

                //var match = Regex.Match(newLine, "(--- )?(&[0-9]+)");
                var match = r8.Match(newLine);
                if (line.Equals("&1"))
                {
                    var map = _unityRefDict["&1"];
                    //processedLines += "--- " + map.unityRef + " &1" + (map.stripped? " stripped" : "") + "\n";
                    processedLines.Append($"--- {map.unityRef} &1{(map.stripped ? " stripped" : "")}\n");
                }
                else if (match.Success && match.Groups.Count == 3)
                {
                    var yamlRef = match.Groups[2].Value;

                    if (!_unityRefDict.ContainsKey(yamlRef))
                    {
                        Console.WriteLine("yaml ref not found: " + yamlRef);
                        //processedLines += newLine + "\n";
                        processedLines.Append($"{newLine}\n");
                        continue;
                    }

                    var map = _unityRefDict[yamlRef];
                    
                    //processedLines += "--- " + map.unityRef + " " + yamlRef + (map.stripped? " stripped" : "") + "\n";
                    processedLines.Append($"--- {map.unityRef} {yamlRef}{(map.stripped ? " stripped" : "")}\n");
                }
                else
                {
                    //processedLines += newLine + (i < lines.Length-1 ? "\n" : "");    
                    processedLines.Append(newLine + (i < lines.Length - 1 ? "\n" : ""));
                }
            }

            return processedLines.ToString();
        }
    }
}
