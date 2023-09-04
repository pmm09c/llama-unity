using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO.Compression;
using System.Linq;
using Debug = UnityEngine.Debug;
using UnityEditor.Android;
using UnityEngine;
using SystemCompressionLevel =  System.IO.Compression.CompressionLevel;
public class CustomBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        switch (report.summary.platform)
        {
            case BuildTarget.Android:
                OnAndroidPostprocessBuild(report.summary.outputPath);
                break;

            case BuildTarget.StandaloneWindows64:
                OnWindows64PostprocessBuild(Path.GetDirectoryName(report.summary.outputPath));
                break;
            
            default:
                UnityEngine.Debug.LogWarning("No post-process action defined for this platform: " + report.summary.platform);
                break;
        }
    }

    private static void OnWindows64PostprocessBuild(string buildPath)
    {
        // Path to the source directory which contains the dll files you want to copy
        string sourceDirectory = Application.dataPath + "/Plugins/Windows";

        // Ensure source directory exists
        if (Directory.Exists(sourceDirectory))
        {
            // Get all dll files from source directory including subdirectories
            string[] dllFiles = Directory.GetFiles(sourceDirectory, "*.dll", SearchOption.AllDirectories);

            // Copy each dll file to the build directory
            foreach (string dllFile in dllFiles)
            {
         
                string relativePath = dllFile.Substring(sourceDirectory.Length);
                string destFile = buildPath + relativePath;
                string destDirectory = Path.GetDirectoryName(destFile);
                // Create directory if it doesn't exist
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(dllFile, destFile, true);
            }

            UnityEngine.Debug.Log("DLL files copied successfully.");
        }
        else
        {
            UnityEngine.Debug.LogError("Source DLL directory does not exist: " + sourceDirectory);
        }
    }
    
    private static void OnAndroidPostprocessBuild(string apkPath)
    {
            string androidSdkRoot = AndroidExternalToolsSettings.sdkRootPath;
            string buildToolsVersion = GetLatestBuildToolsVersion(androidSdkRoot);
            UnityEngine.Debug.Log($"Build Tool Version {buildToolsVersion}");

            string zipalignPath = Path.Combine(androidSdkRoot, "build-tools", buildToolsVersion, "zipalign");
            string apksignerPath = Path.Combine(buildToolsVersion, "apksigner.bat");
            
            // 1. Extract the APK
            string decompiledDir = Path.Combine(Path.GetDirectoryName(apkPath), "tempExtraction");
            string javaPath = UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath + "/bin/java";
            string apktoolJarPath = Application.dataPath + "/Editor/Tools/apktool_2.8.1.jar"; 
            ExecuteProcess(javaPath, $"-jar \"{apktoolJarPath}\" d -o \"{decompiledDir}\" \"{apkPath}\"");
            
            // 2. Move the libllama.so file
            string sourceSoFile = Path.Combine(decompiledDir, "lib", "arm64-v8a", "libllama.so");
            string destinationDir = Path.Combine(decompiledDir, "lib", "arm64-v8a", "LLamaCppLib");
            Directory.CreateDirectory(destinationDir);
            
            if (File.Exists(sourceSoFile))
            {
                Debug.Log("Got here 3");
            
                File.Move(sourceSoFile, Path.Combine(destinationDir, "libllama.so"));
            }

            // 3. Repackage the APK
            string repackedApkPath = apkPath.Replace(".apk", "_temp.apk");
            ExecuteProcess(javaPath, $"-jar \"{apktoolJarPath}\" b -o \"{repackedApkPath}\" \"{decompiledDir}\"");

           // 4. Zipalign the APK
            string alignedApkPath = apkPath.Replace(".apk", "_aligned.apk");
            File.Delete(apkPath);
            ExecuteProcess(zipalignPath, $"-v 4 {repackedApkPath} {alignedApkPath}");
            File.Delete(repackedApkPath);
            
            // 5. Sign the APK
            string keystorePath = PlayerSettings.Android.keystoreName ; 
            string keystoreAlias = PlayerSettings.Android.keyaliasName;
            string keystorePass = PlayerSettings.Android.keystorePass;
            string keyAliasPass = PlayerSettings.Android.keyaliasPass;
            
            // Check if keystore details are not set and default to Unity's debug keystore
            if (string.IsNullOrEmpty(keystorePath))
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        keystorePath = System.Environment.ExpandEnvironmentVariables("%USERPROFILE%") + @"\.android\debug.keystore";
                        break;
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxEditor:
                        keystorePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/.android/debug.keystore";
                        break;
                }
                
                // For debug keystore
                keystoreAlias = "androiddebugkey";
                keystorePass = "android";
                keyAliasPass = "android";
            }
            
            ExecuteProcess(apksignerPath, $"sign --ks {keystorePath} --ks-key-alias {keystoreAlias} --ks-pass pass:{keystorePass} --key-pass pass:{keyAliasPass} --out {apkPath} {alignedApkPath}");
            // 6. Clean up the temporary files
            
            Directory.Delete(decompiledDir, true);
            File.Delete(repackedApkPath);
            File.Delete(alignedApkPath);
            File.Delete(apkPath + ".idsig");
    }
    
    private static string GetLatestBuildToolsVersion(string sdkRootPath)
    {
        var buildToolsDir = Path.Combine(sdkRootPath, "build-tools");
        if (!Directory.Exists(buildToolsDir))
        {
            return null;
        }

        var versions = Directory.GetDirectories(buildToolsDir);
        return versions.Max();
    }
    
    private static void ExecuteProcess(string processPath, string arguments)
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError($"Error during execution of {processPath}: {error}");
        }
    }
    
}