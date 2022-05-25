using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

//using YamlDotNet.RepresentationModel;
//using YamlDotNet.NetCore;
//using Xunit.Abstractions;

namespace UnityMergeTool
{
    class UnityFileData
    {
        class UnityRefMap
        {
            public string unityRef;
            public bool   stripped;
        }
        
        private string                         _yamlVersionInfo;
        private string                         _unityVersionInfo;
        private Dictionary<string,UnityRefMap> _unityRefDict = new Dictionary<string, UnityRefMap>();
        private YamlStream                     _yaml = null;

        
        public UnityFileData() {}
        public UnityFileData(string path) { LoadFile(path); }
        

        public void LoadFile(string path)
        {
            // Check that the file exists
            if (!File.Exists(path))
            {
                Console.WriteLine("Input file doesn't exist");
                return;
            }
            
            // PreProcess and load the file
            _yaml = new YamlStream();
            var processedLines = PreProcessUnityYAML(File.ReadAllText(path));
            using (var reader = new StringReader(processedLines))
            {
                _yaml.Load(reader);
                
                // Process yaml here into internal node representation
                LoadYamlStream();
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
            _yaml.Save(sw);
            
            File.WriteAllText(path, PostProcessUnityYAML(sw.ToString()));
        }

        public bool Diff(UnityFileData baseFile)
        {
            bool foundDifferences = _monoDatas.Count  != baseFile._monoDatas.Count ||
                                    _goDatas.Count    != baseFile._goDatas.Count ||
                                    _transDatas.Count != baseFile._transDatas.Count;

            foundDifferences = DiffData(baseFile._monosById,       _monoDatas)  || foundDifferences;
            foundDifferences = DiffData(baseFile._gameObjectsById, _goDatas)    || foundDifferences;
            foundDifferences = DiffData(baseFile._transformsById,  _transDatas) || foundDifferences;
            
            return foundDifferences;
        }

        public UnityFileData Merge(UnityFileData baseFile, UnityFileData thierFile, out string conflictReport, bool takeTheirs = true)
        {
            conflictReport = "";
            
            bool myDiff    = Diff(baseFile);
            bool thierDiff = thierFile.Diff(baseFile);

            // Nothing to do
            if (!myDiff && !thierDiff)
                return this;

            // If my document has no changes take theirs
            if (!myDiff)
                return thierFile;

            conflictReport = "Merge Conflict Report: \n\n";

            _transDatas = MergeData(baseFile._transDatas, _transDatas, thierFile._transDatas, ref conflictReport, takeTheirs);
            _goDatas    = MergeData(baseFile._goDatas, _goDatas, thierFile._goDatas, ref conflictReport, takeTheirs);
            _monoDatas  = MergeData(baseFile._monoDatas, _monoDatas, thierFile._monoDatas, ref conflictReport, takeTheirs);

            RebuildLinks();

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
        
        
        private bool DiffData<T>(Dictionary<ulong, T> baseDataById, List<T> myData) where T : BaseData
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
        private static List<T> MergeData<T>(List<T> baseDatas, List<T> myData, List<T> thierData, ref string conflictReport, bool takeTheirs = true) where T : BaseData
        {
            // First determine what I added and removed and what they added and removed.
            FindAddedAndRemoved<T>(baseDatas, myData,    out List<T> myAdded, out List<T> myRemoved );
            FindAddedAndRemoved<T>(baseDatas, thierData, out List<T> thierAdded, out List<T> thierRemoved );

            var conflictedToPreserve = new List<T>();
            
            // Check if the they modified anything that we removed or vice versa, if so we need to report it and KEEP the data that was removed (easier to remove it again later then to add it back)
            foreach (var myRemovedData in myRemoved)
            {
                // If we find data I removed in their dataset and it was modified
                var foundRemoved = thierData.FirstOrDefault(item => item.fileId.value == myRemovedData.fileId.value);
                if (foundRemoved != default && foundRemoved.WasModified)
                {
                    // @TODO: Report it, and add it to data we should preserve
                    conflictReport += "Conflict found! Remote changed data I removed";
                    
                    conflictedToPreserve.Add(foundRemoved); // Note we are saving the version with their changes
                }
            }

            foreach (var thierRemovedData in thierRemoved)
            {
                // If we find data they removed that I changed
                var foundRemoved = myData.FirstOrDefault(item => item.fileId.value == thierRemovedData.fileId.value);
                if (foundRemoved != default && foundRemoved.WasModified)
                {
                    // @TODO: Report it, and add it to data we should preserve
                    conflictReport += "Conflict found! I changed data remote removed";
                    
                    conflictedToPreserve.Add(foundRemoved); // Note we are saving the version with their changes
                }
            }
            
            // Take the base minus everything either of us removed, store this as all shared data.
            var shared = new List<T>();
            foreach (var baseData in baseDatas)
            {
                // Skip any that we removed
                var myFoundRemoved = myRemoved.FirstOrDefault(item => item.fileId.value == baseData.fileId.value);
                if (myFoundRemoved != default)
                    continue;
                
                // Skip any that they removed
                var theyFoundRemoved = thierRemoved.FirstOrDefault(item => item.fileId.value == baseData.fileId.value);
                if (theyFoundRemoved != default)
                    continue;

                // Add to shared list
                shared.Add(baseData);
            }
            
            // Merge and check for conflicts within this shared data.
            foreach (var sharedData in shared)
            {
                // Finding these should be guaranteed because we already determined it's in all sets
                var foundInMine = myData.First(item => item.fileId.value == sharedData.fileId.value);
                var foundInThiers = thierData.First(item => item.fileId.value == sharedData.fileId.value);

                foundInMine.Merge(foundInThiers, ref conflictReport, takeTheirs);
            }
            
            // Add anything new from theirs to mine, there will be no conflicts here
            myData.AddRange(thierAdded);
            
            // Add back any conflicted modified data
            myData.AddRange(conflictedToPreserve);

            return myData;
        }
        private static void FindAddedAndRemoved<T>(List<T> baseData, List<T> currentData, out List<T> added, out List<T> removed) where T : BaseData
        {
            // Create output lists
            added   = new List<T>();
            removed = new List<T>();

            foreach (var data in baseData)
            {
                // Check if we can find  base data inside current
                var foundItem = currentData.FirstOrDefault(item => data.fileId.value.Equals(item.fileId.value));
                if (foundItem == default) {
                    // It was removed in the current data
                    removed.Add(data);
                }
            }
            
            foreach (var data in currentData)
            {
                // Check if we can find current data inside base
                var foundItem = baseData.FirstOrDefault(item => data.fileId.value.Equals(item.fileId.value));
                if (foundItem == default) {
                    // It was added in the current data
                    added.Add(data);
                }
            }
        }
        private void LogGameObjectRecurse(GameObjectData node, string indent = "")
        {
            if (node == null) return;
            
            Console.WriteLine(indent + node.LogString());
            Console.WriteLine(indent + "  - " + node.transformRef.LogString());
            foreach (var componentRef in node.componentRefs)
            {
                Console.WriteLine(indent + "  - " + componentRef.LogString());
            }
            
            foreach (var child in node.childRefs) {
                LogGameObjectRecurse(child, indent + "  ");
            }
        }

        // Internal state after document loaded
        private List<GameObjectData>                _goDatas;
        private Dictionary<ulong, GameObjectData>   _gameObjectsById;
        private List<MonoBehaviorData>              _monoDatas;
        private Dictionary<ulong, MonoBehaviorData> _monosById;
        private List<TransformData>                 _transDatas;
        private Dictionary<ulong, TransformData>    _transformsById;
        private List<TransformData>                 _roots;
        
        private void LoadYamlStream()
        {
            _goDatas    = new List<GameObjectData>();
            _monoDatas  = new List<MonoBehaviorData>();
            _transDatas = new List<TransformData>();
            
            foreach (var doc in _yaml.Documents) {
                LoadYamlDoc(doc.RootNode);
            }

            _gameObjectsById = new Dictionary<ulong, GameObjectData>();
            _transformsById  = new Dictionary<ulong, TransformData>();
            _monosById       = new Dictionary<ulong, MonoBehaviorData>();

            RebuildLinks();
        }
        private void LoadYamlDoc(YamlNode node)
        {
            var fileId = ulong.Parse(node.Anchor.Value);
            
            // Examine the stream
            var mapping =  (YamlMappingNode)node;
            foreach (var entry in mapping.Children)
            {
                var scalarNode = (YamlScalarNode)entry.Key;
                if (entry.Value.NodeType != YamlNodeType.Mapping)
                    continue;

                if (scalarNode.Value.Equals("GameObject"))
                {
                    _goDatas.Add(new GameObjectData().Load((YamlMappingNode)entry.Value, fileId, scalarNode.Value));
                }
                else if (scalarNode.Value.Equals("Transform"))
                {
                    _transDatas.Add(new TransformData().Load((YamlMappingNode)entry.Value,  fileId, scalarNode.Value));
                }
                else if (scalarNode.Value.Equals("MonoBehaviour"))
                {
                    _monoDatas.Add(new MonoBehaviorData().Load((YamlMappingNode) entry.Value, fileId, scalarNode.Value));
                }
                else
                {
                    Console.WriteLine(scalarNode.Value + " ------ ");/*
                    string[] notSupportedYet =
                    {
                        "OcclusionCullingSettings",
                        "RenderSettings",
                        "LightmapSettings",
                        "NavMeshSettings",

                        "Animator",
                        "PrefabInstance",
                        "AudioListener",
                        "AudioSource",
                        "Camera",
                        "SortingGroup",
                        "MeshCollider",
                        "MeshRenderer",
                        "MeshFilter",
                
                        "Light",
                        "Camera",
                        "SpriteMask", 
                        "ParticleSystemRenderer",
                        "ParticleSystem"
                    };

                    var properties = (YamlMappingNode) entry.Value;
                    foreach (var prop in properties.Children)
                    {
                        var name = ((YamlScalarNode) prop.Key).Value;
                        Console.WriteLine("  " + name + ": " + prop.Value);
                    }*/
                }
            }
        }


        

        private YamlStream SaveYamlStream()
        {
            
            //var yamlRoot = new YamlMappingNode();
            var yamlOut = new YamlStream();
            
            foreach (var transformData in _transDatas)
            {
                //yamlOut.AllNodes
            }

            return yamlOut;
        }
        
        private void RebuildLinks()
        {
            _gameObjectsById.Clear();
            _transformsById.Clear();  
            _monosById.Clear();
            
            // Store all gameObjects, transforms and components by id first
            foreach (var gameObjectData in _goDatas) { _gameObjectsById.Add(gameObjectData.fileId.value, gameObjectData); }
            foreach (var monoData in _monoDatas)     { _monosById.Add(monoData.fileId.value, monoData); }
            foreach (var transData in _transDatas)   { _transformsById.Add(transData.fileId.value, transData); }

            // Next build transform hierarchy, and save off any roots
            _roots = new List<TransformData>();
            foreach (var transData in _transDatas)
            {
                if (transData.parentId.value == 0)
                {
                    _roots.Add(transData);
                }

                // Associate transform with its game object
                if (_gameObjectsById.ContainsKey(transData.gameObjectId.value))
                {
                    var gameObj = _gameObjectsById[transData.gameObjectId.value];
                    gameObj.transformRef = transData;
                    transData.gameObjectRef = gameObj;
                }

                // Update parent/child relationships
                if (transData.parentId.value != 0 && _transformsById.ContainsKey(transData.parentId.value))
                {
                    transData.parentRef = _transformsById[transData.parentId.value];
                }
                if (transData.childrenIds.value != null && transData.childrenIds.value.Length > 0)
                {
                    transData.childRefs.Clear();
                    foreach (var childId in transData.childrenIds.value)
                    {
                        var childTransform = _transformsById[childId];
                        if (!transData.childRefs.Contains(childTransform))
                            transData.childRefs.Add(childTransform);
                    }
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
                if (monoData.gameObjectId.value == 0 ||
                    !_gameObjectsById.ContainsKey(monoData.gameObjectId.value)) continue;

                monoData.gameObjectRef = _gameObjectsById[monoData.gameObjectId.value];
                monoData.gameObjectRef.componentRefs.Add(monoData);
            }
        }
 
        private string PreProcessUnityYAML(string input)
        {
            var lines            = input.Split('\n');
            var processedLines          = "";
            
            // Need to store off header comment for version info
            _unityRefDict.Clear();
            _yamlVersionInfo  = lines[0];
            _unityVersionInfo = lines[1];
            
            foreach (var line in lines)
            {
                var match = Regex.Match(line, "--- (!u![0-9]+) (&[0-9]+) ?(\\w*)");
                if (match.Success && match.Groups.Count == 4)
                {
                    var unityRef = match.Groups[1].Value;
                    var yamlRef = match.Groups[2].Value;
                    _unityRefDict.Add(yamlRef, new UnityRefMap()
                    {
                        unityRef = unityRef,
                        //yamlRef = yamlRef,
                        stripped = match.Groups[3].Value.Equals("stripped")
                    });

                    var newLine = "--- " + unityRef + " " + yamlRef + "\n";
                    processedLines += newLine;
                }
                else
                {
                    processedLines += line + "\n";    
                }
            }

            return processedLines;
        }
        private string PostProcessUnityYAML(string input)
        {
            var lines = input.Split('\n');
            var processedLines = "";
            
            //foreach (var line in lines)
            for(int i=0; i<lines.Length; ++i)
            {
                var line     = lines[i];
                if (line.Equals("..."))
                    continue;

                if (i < lines.Length - 1)
                {
                    var nextLine = lines[i + 1];
                    var combinedLines = line + nextLine;
                    var matchResult = Regex.Match(combinedLines, "\\s*[\\-]:? &[0-9]+\\s*serializedVersion: [0-9]+");
                    if (matchResult.Success)
                    {
                        processedLines += Regex.Replace(combinedLines, "(\\s*[\\-]:? )&[0-9]+\\s*(serializedVersion: [0-9]+)", "$1$2\n");
                        ++i;
                        continue;
                    }
                }
                
                var newLine = Regex.Replace(line, "(\\s*[\\-_\\w]:? )''", "$1");
                newLine = Regex.Replace(newLine, "(\\s*[_\\w]+: )''", "$1");
                newLine = Regex.Replace(newLine, "(\\s*[_\\w]+:) (&[0-9]+)", "$1");
                
                var targetMatch = Regex.Match(newLine, "(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})");
                if (targetMatch.Groups.Count == 4)
                {
                    var ident = targetMatch.Groups[1].Value;
                    if (newLine.Length >= 90)
                    {
                        var leadingSpace = Regex.Replace(ident, "(\\s*)-?( ?)[_\\w]+:", "$1$2$2");
                        newLine = Regex.Replace(newLine, "(\\s*-? ?[_\\w]+:) ({fileID: [\\-0-9]+, guid: [\\w]+,) (type: [0-9]+})", "$1 $2\n"+leadingSpace+"  $3");
                    }
                }


                var match = Regex.Match(newLine, "--- (&[0-9]+)");
                if (line.Equals("&1"))
                {
                    var map = _unityRefDict["&1"];
                    processedLines += "--- " + map.unityRef + " &1" + (map.stripped? " stripped" : "") + "\n";
                }
                else if (match.Success && match.Groups.Count == 2)
                {
                    var yamlRef = match.Groups[1].Value;

                    if (!_unityRefDict.ContainsKey(yamlRef))
                    {
                        Console.WriteLine("yaml ref not found: " + yamlRef);
                        processedLines += newLine + "\n";
                        continue;
                    }

                    var map = _unityRefDict[yamlRef];
                    
                    processedLines += "--- " + map.unityRef + " " + yamlRef + (map.stripped? " stripped" : "") + "\n";
                }
                else
                {
                    processedLines += newLine + (i < lines.Length-1 ? "\n" : "");    
                }
            }

            return processedLines;
        }
    }
    
    
    
    internal class Program
    {
        public static void Main(string[] args)
        {
            //var file  = new UnityFileData("Scenes/Prefab.prefab");
           
            
            var fileBase  = new UnityFileData("../../Scenes/SampleScene-Base.unity");
            var fileA  = new UnityFileData("../../Scenes/SampleScene-A.unity");
            var fileB  = new UnityFileData("../../Scenes/SampleScene-B.unity");

            
            //Console.WriteLine("--- Prefab: --------------------------------------------------");
            //filePrefab.LogGameObjects();
            Console.WriteLine("\n--- Base: --------------------------------------------------");
            fileBase.LogGameObjects();
            Console.WriteLine("\n--- A: --------------------------------------------------");
            fileA.LogGameObjects();
            Console.WriteLine("\n--- B: --------------------------------------------------");
            fileB.LogGameObjects();


            var merged = fileA.Merge(fileBase, fileB, out string conflictReport);
            Console.WriteLine("-------------------------------------------------------------------");
            Console.WriteLine(conflictReport);
            Console.WriteLine("\n\n");
            merged.LogGameObjects();
        }
        
    }
}
