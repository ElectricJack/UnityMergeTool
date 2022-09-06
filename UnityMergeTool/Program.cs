using System;
using System.IO;

namespace UnityMergeTool
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Invalid args - Expecting: merge \"BASE\" \"REMOTE\" \"LOCAL\" \"MERGED\"");
                return 1;
            }

            if (args[0].Equals("merge"))
            {
                if (args.Length != 5)
                {
                    Console.WriteLine("Invalid args - Expecting: merge \"BASE\" \"REMOTE\" \"LOCAL\" \"MERGED\"");
                    return 1;
                }
                
                // First time through we generate the report, and exit with error code so SourceTree doesn't resolve conflict
                // Second time through we load the report and parse updated commands, use the commands to apply 
                
                
                // merge "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
                var curDir = Directory.GetCurrentDirectory();
                var basePath   = $"{curDir}/{args[1].Substring(2)}";
                var remotePath = $"{curDir}/{args[2].Substring(2)}";
                var localPath  = $"{curDir}/{args[3].Substring(2)}";

                //var name = "SlotsUI-Old"; // No conflicts???
                //var name = "StoreUI";     // No conflicts (Confirm why? Can the report clarify better?)
                //var name = "DebugUI";     // Many conflicts (Parent node deleted in local, children modified in remote
                //var name = "BuyFrame";    // 6 Conflict
                //var name = "BetGroup";    
                //basePath   = $"{curDir}/../../../Scenes/{name}_BASE.prefab";
                //remotePath = $"{curDir}/../../../Scenes/{name}_REMOTE.prefab";
                //localPath  = $"{curDir}/../../../Scenes/{name}_LOCAL.prefab";
                
                var fileMerged = $"{curDir}/{args[4]}";
                var reportFile = fileMerged + ".report.txt";

                var millisStart = Millis();
                var fileBase    = new UnityFileData(basePath);
                var millisAfterBase = Millis();
                Console.WriteLine($"Loaded Base: {(millisAfterBase-millisStart)/1000.0f} s\n\n");
                
                var fileLocal  = new UnityFileData(localPath);
                var millisAfterLocal = Millis();
                Console.WriteLine($"Loaded Local: {(millisAfterLocal-millisAfterBase)/1000.0f} s\n\n");
                
                var fileRemote   = new UnityFileData(remotePath);
                var millisAfterRemote = Millis();
                Console.WriteLine($"Loaded Remote: {(millisAfterRemote-millisAfterLocal)/1000.0f} s\n\n");
                

                if (File.Exists(reportFile))
                {
                    MergeReport report = new MergeReport();
                    report.LoadReport(reportFile);
                    var merged = fileLocal.Merge(fileBase, fileRemote, report);
                    var millisAfterMerge = Millis();
                    Console.WriteLine($"Merge: {(millisAfterMerge-millisAfterRemote)/1000.0f} s\n\n");
                    
                    merged.SaveFile(fileMerged);
                    report.SaveReport(fileMerged + ".report-final.txt");
                    
                    var millisAfterSave = Millis();
                    Console.WriteLine($"Saving: {(millisAfterSave-millisAfterMerge)/1000.0f} s");
                    Console.WriteLine($"Total Time: {(millisAfterSave-millisStart)/1000.0f} s");
                    
                    return 0; // Actually done with merge
                }
                else
                {
                    var report = new MergeReport();
                    var merged = fileLocal.Merge(fileBase, fileRemote, report);
                    report.SaveReport(reportFile);
                    var millisAfterMerge = Millis();
                    Console.WriteLine($"Merge: {(millisAfterMerge-millisAfterRemote)/1000.0f} s\n\n");
                    Console.WriteLine($"Total Time: {(millisAfterMerge-millisStart)/1000.0f} s");
                }
            }
            
            return 1;
        }


        private static long Millis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        
    }
}
