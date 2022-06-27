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
                var basePath   = $"{curDir}/{args[1].Substring(2)}";
                var remotePath = $"{curDir}/{args[2].Substring(2)}";
                var localPath  = $"{curDir}/{args[3].Substring(2)}";
                var mergedPath = $"{curDir}/{args[4]}";

                var fileBase    = new UnityFileData(basePath);
                var fileRemote  = new UnityFileData(localPath);
                var fileLocal   = new UnityFileData(remotePath);
                var fileMerged  = mergedPath;
                
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
