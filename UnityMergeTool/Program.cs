using System;
using System.IO;

namespace UnityMergeTool
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid args - Expecting: merge \"BASE\" \"REMOTE\" \"LOCAL\" \"MERGED\"");
                return;
            }

            if (args[0].Equals("merge"))
            {
                if (args.Length != 5)
                {
                    Console.WriteLine("Invalid args - Expecting: merge \"BASE\" \"REMOTE\" \"LOCAL\" \"MERGED\"");
                    return;     
                }
                
                // merge "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
                var curDir = Directory.GetCurrentDirectory();
                //var basePath   = $"{curDir}/{args[1].Substring(2)}";
                //var remotePath = $"{curDir}/{args[2].Substring(2)}";
                //var localPath  = $"{curDir}/{args[3].Substring(2)}";

                // 1 Conflict
                //var basePath   = $"{curDir}/../../../Scenes/BuyFrame_BASE.prefab";
                //var remotePath = $"{curDir}/../../../Scenes/BuyFrame_REMOTE.prefab";
                //var localPath  = $"{curDir}/../../../Scenes/BuyFrame_LOCAL.prefab";

                var basePath   = $"{curDir}/../../../Scenes/DebugUI_BASE.prefab";
                var remotePath = $"{curDir}/../../../Scenes/DebugUI_REMOTE.prefab";
                var localPath  = $"{curDir}/../../../Scenes/DebugUI_LOCAL.prefab";
                
                var mergedPath = $"{curDir}/{args[4]}";

                var millisStart = Millis();
                var fileBase    = new UnityFileData(basePath);
                var millisAfterBase = Millis();
                Console.WriteLine($"Loaded Base: {(millisAfterBase-millisStart)/1000.0f} s\n\n");
                //fileBase.LogGameObjects();
                
                var fileLocal  = new UnityFileData(localPath);
                var millisAfterLocal = Millis();
                Console.WriteLine($"Loaded Local: {(millisAfterLocal-millisAfterBase)/1000.0f} s\n\n");
                //fileLocal.LogGameObjects();
                
                var fileRemote   = new UnityFileData(remotePath);
                var millisAfterRemote = Millis();
                Console.WriteLine($"Loaded Remote: {(millisAfterRemote-millisAfterLocal)/1000.0f} s\n\n");
                //fileRemote.LogGameObjects();
                
                var fileMerged  = mergedPath;
                var reportFile = fileMerged + ".report.txt";
                var merged = fileLocal.Merge(fileBase, fileRemote, out MergeReport report);
                var millisAfterMerge = Millis();
                Console.WriteLine($"Merge: {(millisAfterMerge-millisAfterRemote)/1000.0f} s\n\n");
                //merged.LogGameObjects();

                merged.SaveFile(fileMerged);
                var millisAfterSave = Millis();
                Console.WriteLine($"Saving: {(millisAfterSave-millisAfterMerge)/1000.0f} s");
                Console.WriteLine($"Total Time: {(millisAfterSave-millisStart)/1000.0f} s");
                
                
                Console.WriteLine("\n\nREPORT:");
                var reportStr = report.ToString();
                Console.WriteLine(reportStr);
                
                File.WriteAllText(reportFile, reportStr);
            }
        }


        private static long Millis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        
    }
}
