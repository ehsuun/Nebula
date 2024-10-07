using UnityEditor;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Add this line

namespace Nebula.Editor{

    public class DemucsSplitterWindow : EditorWindow
    {
        private string mp3FilePath = "";
        private string condaEnv = "myenv";
        private string condaPath = "C:\\path\\to\\conda.exe";
        private bool stemFilesReady = false;
        private string bassPath, drumsPath, otherPath, vocalsPath;
        private string originalMp3Path;

        private const string CondaEnvPrefKey = "Nebula_CondaEnv";
        private const string CondaPathPrefKey = "Nebula_CondaPath";

        private float drumStemOffset = 0f;
        private bool stemFilesAligned = false;

        [MenuItem("Tools/Nebula/Demucs Splitter")]
        public static void ShowWindow()
        {
            GetWindow<DemucsSplitterWindow>("Demucs Splitter");
        }

        private void OnEnable()
        {
            // Load saved preferences
            condaEnv = EditorPrefs.GetString(CondaEnvPrefKey, "myenv");
            condaPath = EditorPrefs.GetString(CondaPathPrefKey, "C:\\path\\to\\conda.exe");
        }

        private void OnDisable()
        {
            // Save preferences
            EditorPrefs.SetString(CondaEnvPrefKey, condaEnv);
            EditorPrefs.SetString(CondaPathPrefKey, condaPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("Drag and Drop MP3 File Here", EditorStyles.boldLabel);

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "");

            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (string path in DragAndDrop.paths)
                        {
                            if (Path.GetExtension(path).ToLower() == ".mp3")
                            {
                                mp3FilePath = path;
                                originalMp3Path = path; // Store the original MP3 path
                            }
                        }
                    }
                }
            }

            GUILayout.Label("Conda Environment Name:");
            EditorGUI.BeginChangeCheck();
            condaEnv = GUILayout.TextField(condaEnv);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(CondaEnvPrefKey, condaEnv);
            }

            GUILayout.Label("Conda Executable Path:");
            EditorGUI.BeginChangeCheck();
            condaPath = GUILayout.TextField(condaPath);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(CondaPathPrefKey, condaPath);
            }

            if (!string.IsNullOrEmpty(mp3FilePath))
            {
                GUILayout.Label("Selected File: " + mp3FilePath);

                if (GUILayout.Button("Run Demucs Splitter"))
                {
                    if (File.Exists(condaPath))
                    {
                        RunDemucsSplitter(mp3FilePath);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Conda executable not found at the specified path: " + condaPath);
                    }
                }
            }

            if (stemFilesReady)
            {
                if (GUILayout.Button("Auto Align Stem Files"))
                {
                    AlignStemFiles();
                }

                if (GUILayout.Button("Manual Align Stem Files"))
                {
                    OpenWaveformAlignmentWindow();
                }

                GUI.enabled = stemFilesAligned;
                if (GUILayout.Button("Assign Stem Files to MusicProcessor"))
                {
                    AssignStemFilesToMusicProcessor();
                }
                GUI.enabled = true;
            }

            GUILayout.Label("Note: Make sure Demucs is installed in the specified conda environment.", EditorStyles.helpBox);
        }

        private void RunDemucsSplitter(string filePath)
        {
            if (!File.Exists(condaPath))
            {
                UnityEngine.Debug.LogError($"Conda executable not found at: {condaPath}");
                return;
            }

            string absoluteFilePath = Path.GetFullPath(filePath);
            if (!File.Exists(absoluteFilePath))
            {
                UnityEngine.Debug.LogError($"Invalid file path: {absoluteFilePath}");
                return;
            }

            string workingDirectory = Path.GetDirectoryName(absoluteFilePath);
            string separatedPath = Path.Combine(workingDirectory, "separated");
            Directory.CreateDirectory(separatedPath);

            // Create a temporary batch file
            string batchFilePath = Path.Combine(workingDirectory, "run_demucs.bat");
            string batchFileContent = $@"@echo off
    ""{condaPath}"" run -n {condaEnv} demucs -o ""{separatedPath}"" ""{absoluteFilePath}""
    exit /b %errorlevel%";

            File.WriteAllText(batchFilePath, batchFileContent);

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = batchFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            Process process = new Process
            {
                StartInfo = processStartInfo
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.Log($"Demucs output: {args.Data}");
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Change this to log as info instead of error
                    UnityEngine.Debug.Log($"Demucs info: {args.Data}");
                }
            };

            try
            {
                UnityEngine.Debug.Log($"Starting Demucs process with batch file: {batchFilePath}");
                UnityEngine.Debug.Log($"Working directory: {workingDirectory}");
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                UnityEngine.Debug.Log($"Demucs process exited with code: {process.ExitCode}");

                // Print out the contents of the separated folder
                if (Directory.Exists(separatedPath))
                {
                    UnityEngine.Debug.Log($"Contents of separated folder:");
                    foreach (string file in Directory.GetFiles(separatedPath, "*", SearchOption.AllDirectories))
                    {
                        UnityEngine.Debug.Log(file);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"Separated folder not found: {separatedPath}");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to start Demucs process: {e.Message}");
                return;
            }
            finally
            {
                // Clean up the temporary batch file
                if (File.Exists(batchFilePath))
                {
                    File.Delete(batchFilePath);
                }
            }

            // Move stem files to Assets folder
            if (Directory.Exists(separatedPath))
            {
                string htdemucsPath = Path.Combine(separatedPath, "htdemucs");
                if (Directory.Exists(htdemucsPath))
                {
                    string[] stemFiles = Directory.GetFiles(htdemucsPath, "*.wav", SearchOption.AllDirectories);
                    string trackName = Path.GetFileNameWithoutExtension(filePath);
                    string destFolder = Path.Combine("Assets", "Music", "Separated", trackName);

                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    foreach (string stemFile in stemFiles)
                    {
                        string fileName = Path.GetFileName(stemFile);
                        string destPath = Path.Combine(destFolder, fileName);
                        
                        if (File.Exists(destPath))
                        {
                            File.Delete(destPath);
                        }
                        
                        File.Move(stemFile, destPath);
                        UnityEngine.Debug.Log("Moved stem to: " + destPath);

                        // Store paths for each stem
                        if (fileName.Contains("bass")) bassPath = destPath;
                        else if (fileName.Contains("drums")) drumsPath = destPath;
                        else if (fileName.Contains("other")) otherPath = destPath;
                        else if (fileName.Contains("vocals")) vocalsPath = destPath;
                    }

                    // Remove the htdemucs folder and its contents
                    Directory.Delete(htdemucsPath, true);
                    UnityEngine.Debug.Log("Removed htdemucs folder: " + htdemucsPath);

                    // Remove the separated folder if it's empty
                    if (!Directory.EnumerateFileSystemEntries(separatedPath).Any())
                    {
                        Directory.Delete(separatedPath, false);
                        UnityEngine.Debug.Log("Removed empty separated folder: " + separatedPath);
                    }

                    AssetDatabase.Refresh();
                    stemFilesReady = true;
                    UnityEngine.Debug.Log("Stem files are ready to be assigned.");
                }
                else
                {
                    UnityEngine.Debug.LogError("htdemucs folder not found in: " + separatedPath);
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Separated directory not found: " + separatedPath);
            }
        }

        private void AlignStemFiles()
        {
            AudioClip mainClip = AssetDatabase.LoadAssetAtPath<AudioClip>(mp3FilePath);
            AudioClip drumsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(drumsPath);

            if (mainClip != null && drumsClip != null)
            {
                drumStemOffset = WaveformAlignmentWindow.AutoAlignDrumStem(mainClip, drumsClip);
                stemFilesAligned = true;
                UnityEngine.Debug.Log($"Stem files aligned. Drum stem offset: {drumStemOffset} seconds");
            }
            else
            {
                UnityEngine.Debug.LogError("Main audio clip or drums stem could not be loaded for alignment.");
            }
        }

        private void OpenWaveformAlignmentWindow()
        {
            AudioClip mainClip = AssetDatabase.LoadAssetAtPath<AudioClip>(originalMp3Path);
            AudioClip drumsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(drumsPath);

            if (mainClip != null && drumsClip != null)
            {
                WaveformAlignmentWindow window = EditorWindow.GetWindow<WaveformAlignmentWindow>("Waveform Alignment");
                window.SetClips(mainClip, drumsClip);
                window.OnAlignmentApplied += OnManualAlignmentApplied;
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to load main audio or drums stem for alignment.");
            }
        }

        private void OnManualAlignmentApplied(float offset)
        {
            drumStemOffset = offset;
            stemFilesAligned = true;
            UnityEngine.Debug.Log($"Manual alignment applied. Drum stem offset: {drumStemOffset} seconds");
        }

        private void AssignStemFilesToMusicProcessor()
        {
            UnityEngine.Debug.Log("[DEBUG] AssignStemFilesToMusicProcessor called");

            MusicProcessor musicProcessor = FindObjectOfType<MusicProcessor>();
            if (musicProcessor != null)
            {
                // Load stem files
                AudioClip mainClip = AssetDatabase.LoadAssetAtPath<AudioClip>(mp3FilePath);
                AudioClip bassClip = AssetDatabase.LoadAssetAtPath<AudioClip>(bassPath);
                AudioClip drumsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(drumsPath);
                AudioClip otherClip = AssetDatabase.LoadAssetAtPath<AudioClip>(otherPath);
                AudioClip vocalsClip = AssetDatabase.LoadAssetAtPath<AudioClip>(vocalsPath);

                if (mainClip != null && bassClip != null && drumsClip != null && otherClip != null && vocalsClip != null)
                {
                    // Assign the original MP3 to the main AudioSource
                    musicProcessor.mainAudioSource.clip = mainClip;

                    // Assign stem files
                    musicProcessor.bassAudioSource.clip = bassClip;
                    musicProcessor.drumsAudioSource.clip = drumsClip;
                    musicProcessor.otherAudioSource.clip = otherClip;
                    musicProcessor.vocalsAudioSource.clip = vocalsClip;

                    // Set the stem offset in MusicProcessor
                    musicProcessor.stemOffset = drumStemOffset;

                    musicProcessor.processingMode = MusicProcessor.ProcessingMode.Stems;
                    UnityEngine.Debug.Log($"[DEBUG] Set MusicProcessor processing mode to {musicProcessor.processingMode}");
                    UnityEngine.Debug.Log($"Original MP3 and stem files assigned to MusicProcessor successfully. Stem offset: {drumStemOffset} seconds");
                }
                else
                {
                    UnityEngine.Debug.LogError("One or more audio clips could not be loaded.");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("[DEBUG] MusicProcessor not found in the scene!");
            }
        }

        private float AlignDrumStem(AudioClip mainClip, AudioClip drumClip)
        {
            // Convert AudioClips to float arrays
            float[] mainData = new float[mainClip.samples * mainClip.channels];
            float[] drumData = new float[drumClip.samples * drumClip.channels];
            mainClip.GetData(mainData, 0);
            drumClip.GetData(drumData, 0);

            // Define search range (0.5 seconds)
            int maxLag = Mathf.Min(mainClip.frequency / 2, mainData.Length, drumData.Length);
            float bestOffset = 0f;
            float maxCorrelation = float.MinValue;

            // Perform cross-correlation
            for (int lag = 0; lag < maxLag; lag++)
            {
                float correlation = 0f;
                for (int i = 0; i < maxLag; i++)
                {
                    correlation += mainData[i] * drumData[(i + lag) % drumData.Length];
                }

                if (correlation > maxCorrelation)
                {
                    maxCorrelation = correlation;
                    bestOffset = (float)lag / mainClip.frequency;
                }
            }

            return bestOffset;
        }

        private float[] DetectTransients(float[] audioData, int windowSize)
        {
            float[] transients = new float[audioData.Length / windowSize];
            for (int i = 0; i < transients.Length; i++)
            {
                float energy = 0f;
                for (int j = 0; j < windowSize; j++)
                {
                    energy += audioData[i * windowSize + j] * audioData[i * windowSize + j];
                }
                transients[i] = energy > 0.1f ? 1f : 0f; // Simple threshold-based transient detection
            }
            return transients;
        }
    }
}