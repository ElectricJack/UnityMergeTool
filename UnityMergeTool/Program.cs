using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using YamlDotNet.Core;

//using YamlDotNet.RepresentationModel;
//using YamlDotNet.NetCore;
//using Xunit.Abstractions;

namespace UnityMergeTool
{
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
