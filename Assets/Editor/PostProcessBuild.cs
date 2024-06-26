using System.Diagnostics;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using Debug = UnityEngine.Debug;

public class PostProcessBuild
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        //const string defaultModsFolderName = "StreamingAssets";
        //const string manualFileName = "Daggerfall Unity Manual.pdf";
        //const string readMeFilename = "readme.txt";
        //const string modReadMeText = "Place your .dfmod files in this folder for the mod system.";

        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 ||
            target == BuildTarget.StandaloneLinux64 ||
            target == BuildTarget.StandaloneOSX)
        {
            // Get build path
            string pureBuildPath = Path.GetDirectoryName(pathToBuiltProject);
            Debug.LogFormat("Running OnPostprocessBuild at path `{0}`", pureBuildPath);

            // Remove PDB files
            RemoveFilePattern(pureBuildPath, "*.pdb");

            // Remove release.yml files
            RemoveFilePattern(pureBuildPath, ".release.yml");

            // Remove .gitignore files
            RemoveFilePattern(pureBuildPath, ".gitignore", SearchOption.AllDirectories);

            // Remove "Daggerfall Unity_BurstDebugInformation_DoNotShip" directory
            RemoveDirectoryPattern(pureBuildPath, "Daggerfall Unity_BurstDebugInformation_DoNotShip");

            // Remove "DaggerfallUnity_BurstDebugInformation_DoNotShip" directory (variant generated by Cloud Build)
            RemoveDirectoryPattern(pureBuildPath, "DaggerfallUnity_BurstDebugInformation_DoNotShip");

            var processInfo = new ProcessStartInfo(@"C:\Games\Daggerfall\Developer\Link Arena2.bat");
            var process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                process.Close();
            }


            //// Create default mods folder
            //string modsPath = Path.Combine(pureBuildPath, defaultModsFolderName);
            //Directory.CreateDirectory(modsPath);

            //// Write readme text
            //StreamWriter stream = File.CreateText(Path.Combine(modsPath, readMeFilename));
            //stream.WriteLine(modReadMeText);
            //stream.Close();

            // Copy manual
            //FileUtil.CopyFileOrDirectory(Path.Combine("Assets/Docs", manualFileName), Path.Combine(pureBuildPath, manualFileName));
        }

        void RemoveFilePattern(string pureBuildPath, string pattern, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            foreach (string file in Directory.GetFiles(pureBuildPath, pattern, option))
            {
                File.Delete(file);
                Debug.Log(file + " deleted!");
            }
        }

        void RemoveDirectoryPattern(string pureBuildPath, string pattern, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            foreach (string directory in Directory.GetDirectories(pureBuildPath, pattern, option))
            {
                Directory.Delete(directory, true);
                Debug.Log(directory + " deleted!");
            }
        }
    }
}