using System;
using System.IO;

namespace UnityMergeTool
{
    internal class Program
    {
        private static readonly string[] testPrefabs = new string[] {
            "BetGroup",		
            "BuyFrame",
            "Concierge_Canopy",		
            "Concierge_Rug",			
            "Concierge_Stand",			
            "DebugUI",				
            "Fountain_Base",			
            "Fountain_Top",
            "Hotel_Sign_Base",			
            "NPC_SeaLion",			
            "Pier_Bridge",			
            "Pier_Decorative_Plants",		
            "Pier_FishingPoint_Viewfinders",	
            "Pier_FishingShelter",		
            "Pier_FishingShelter_Damaged",	
            "Pier_FishingShelter_Sign",
            "Pier_Lamp_Posts",
            "Pier_Lei_Stand",
            "Pier_SeaLionDeck",
            "Pier_ShelterDeck",
            "Pier_ShelterDeck_Cleanup",
            "Pier_Sunglasses_Hut",
            "ResortAmbientSoundEmmitter",
            "SlotsSocialUI",
            "SlotsUI-Old",
            "StoreUI",
            "Topiary_Archways",
            "Topiary_Benches",
            "Topiary_Main"
        };
        private static readonly string[] testScenes = new string[] {
            "characterAnimations",
            "metasceneEditor"
        };
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
                
                // merge "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
                var curDir = Directory.GetCurrentDirectory();
                var basePath = $"{curDir}/{args[1].Substring(2)}";
                var remotePath = $"{curDir}/{args[2].Substring(2)}";
                var localPath = $"{curDir}/{args[3].Substring(2)}";
                var fileMerged = $"{curDir}/{args[4]}";
                var reportFile = fileMerged + ".report.txt";

                return ExecuteMerge(basePath, remotePath, localPath, fileMerged, reportFile);
            }
            else if (args[0].Equals("test"))
            {
                int i = 9;
                //for (int i = 0; i < testPrefabs.Length; ++i)
                {
                    var name = testPrefabs[i];
                    Console.WriteLine($"\n\n\n--------------------------------------------------------\nRunning test '{name}'");
                    try
                    {
                        ExecuteTest(name);
                        Console.WriteLine("Test Finished.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error running test!");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    
                }
            }

            return 1;
        }

        private static void ExecuteTest(string name, string type = "prefab")
        {
            var curDir= Directory.GetCurrentDirectory();
            var basePath   = $"{curDir}/../../../Scenes/{name}/{name}_BASE.{type}";
            var remotePath = $"{curDir}/../../../Scenes/{name}/{name}_REMOTE.{type}";
            var localPath  = $"{curDir}/../../../Scenes/{name}/{name}_LOCAL.{type}";

            //var fileMerged = $"{curDir}/{args[4]}";
            var fileMerged = $"{curDir}/../../../Scenes/{name}/{name}.{type}";
            var reportFile = fileMerged + ".report.txt";
            
            ExecuteMerge(basePath, remotePath, localPath, fileMerged, reportFile);
        }

        private static int ExecuteMerge(string basePath, string remotePath, string localPath, string fileMerged, string reportFile)
        {
            // First time through we generate the report, and exit with error code so SourceTree doesn't resolve conflict
            // Second time through we load the report and parse updated commands, use the commands to apply 

            var millisStart = Millis();
            var fileBase = new UnityFileData(basePath);
            var millisAfterBase = Millis();
            Console.WriteLine($"Loaded Base: {(millisAfterBase - millisStart) / 1000.0f} s\n\n");

            var fileLocal = new UnityFileData(localPath);
            var millisAfterLocal = Millis();
            Console.WriteLine($"Loaded Local: {(millisAfterLocal - millisAfterBase) / 1000.0f} s\n\n");

            var fileRemote = new UnityFileData(remotePath);
            var millisAfterRemote = Millis();
            Console.WriteLine($"Loaded Remote: {(millisAfterRemote - millisAfterLocal) / 1000.0f} s\n\n");


            if (File.Exists(reportFile))
            {
                MergeReport report = new MergeReport();
                report.LoadReport(reportFile);
                var merged = fileLocal.Merge(fileBase, fileRemote, report);
                var millisAfterMerge = Millis();
                Console.WriteLine($"Merge: {(millisAfterMerge - millisAfterRemote) / 1000.0f} s\n\n");

                merged.SaveFile(fileMerged);
                report.SaveReport(fileMerged + ".report-final.txt");

                var millisAfterSave = Millis();
                Console.WriteLine($"Saving: {(millisAfterSave - millisAfterMerge) / 1000.0f} s");
                Console.WriteLine($"Total Time: {(millisAfterSave - millisStart) / 1000.0f} s");

                return 0; // Actually done with merge
            }
            else
            {
                var report = new MergeReport();
                var merged = fileLocal.Merge(fileBase, fileRemote, report);
                report.SaveReport(reportFile);
                var millisAfterMerge = Millis();
                Console.WriteLine($"Merge: {(millisAfterMerge - millisAfterRemote) / 1000.0f} s\n\n");
                Console.WriteLine($"Total Time: {(millisAfterMerge - millisStart) / 1000.0f} s");
            }

            return 1;
        }

 



        private static long Millis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
        
    }
}
