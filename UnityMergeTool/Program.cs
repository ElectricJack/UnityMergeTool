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
                var fileBase    = new UnityFileData($"{curDir}/{args[1]}");
                var fileRemote  = new UnityFileData($"{curDir}/{args[2]}");
                var fileLocal   = new UnityFileData($"{curDir}/{args[3]}");
                var fileMerged  = $"{curDir}/{args[4]}";
                var conflictFile = fileMerged + ".conflicts.txt";
    
                var merged = fileLocal.Merge(fileBase, fileRemote, out string conflictReport, out bool conflictsFound);
                merged.SaveFile(fileMerged);
                if (conflictsFound) {
                    File.WriteAllText(conflictFile, conflictReport);
                }
            }
        }
        
    }
}
