using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Knight.Framework.AssetBundles.Editor;

namespace UnityEditor.AssetBundles
{
    [System.Serializable]
    public class AssetBundleBuildTab
    {
        const string kBuildPrefPrefix = "ABBBuild:";
        // gui vars
        ABBuilder.BuildPlatform m_buildTarget = ABBuilder.BuildPlatform.Windows;
        CompressOptions m_compression = CompressOptions.StandardCompression;
        
        string m_outputPath = string.Empty;
        bool m_useDefaultPath = true;

        string m_streamingPath = "Assets/StreamingAssets";
        [SerializeField]
        bool m_advancedSettings;
        [SerializeField]
        Vector2 m_scrollPosition;

        class ToggleData
        {
            public ToggleData(bool s, string title, string tooltip, BuildAssetBundleOptions opt = BuildAssetBundleOptions.None)
            {
                content = new GUIContent(title, tooltip);
                state = EditorPrefs.GetBool(PrefKey, s);
                option = opt;
            }
            public string PrefKey
            { get { return kBuildPrefPrefix + content.text; } }
            public bool state;
            public GUIContent content;
            public BuildAssetBundleOptions option;
        }
        List<ToggleData> m_toggleData;
        ToggleData m_ForceRebuild;
        ToggleData m_CopyToStreaming;
        GUIContent m_TargetContent;
        GUIContent m_CompressionContent;
        public enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression,
            ChunkBasedCompression,
        }
        GUIContent[] m_CompressionOptions =
        {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
        };
        int[] m_CompressionValues = { 0, 1, 2 };


        public AssetBundleBuildTab()
        {
            m_advancedSettings = false;
        }
        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_buildTarget = ABBuilder.Instance.CurBuildPlatform; // (AssetbundleHelper.BuildPlatform)EditorPrefs.GetInt(kBuildPrefPrefix + "BuildTarget", (int)m_buildTarget);
            m_compression = (CompressOptions)EditorPrefs.GetInt(kBuildPrefPrefix + "Compression", (int)m_compression);
            m_toggleData = new List<ToggleData>();
            m_toggleData.Add(new ToggleData(
                false,
                "Exclude Type Information",
                "Do not include type information within the asset bundle (don't write type tree).",
                BuildAssetBundleOptions.DisableWriteTypeTree));
            m_toggleData.Add(new ToggleData(
                false,
                "Force Rebuild",
                "Force rebuild the asset bundles",
                BuildAssetBundleOptions.ForceRebuildAssetBundle));
            m_toggleData.Add(new ToggleData(
                false,
                "Ignore Type Tree Changes",
                "Ignore the type tree changes when doing the incremental build check.",
                BuildAssetBundleOptions.IgnoreTypeTreeChanges));
            m_toggleData.Add(new ToggleData(
                false,
                "Append Hash",
                "Append the hash to the assetBundle name.",
                BuildAssetBundleOptions.AppendHashToAssetBundleName));
            m_toggleData.Add(new ToggleData(
                false,
                "Strict Mode",
                "Do not allow the build to succeed if any errors are reporting during it.",
                BuildAssetBundleOptions.StrictMode));
            m_toggleData.Add(new ToggleData(
                false,
                "Dry Run Build",
                "Do a dry run build.",
                BuildAssetBundleOptions.DryRunBuild));


            m_ForceRebuild = new ToggleData(
                false,
                "Clear Folders",
                "Will wipe out all contents of build directory as well as StreamingAssets/AssetBundles if you are choosing to copy build there.");
            m_CopyToStreaming = new ToggleData(
                false,
                "Copy to StreamingAssets",
                "After build completes, will copy all build content to " + m_streamingPath + " for use in stand-alone player.");

            m_TargetContent = new GUIContent("Build Target", "Choose target platform to build for.");
            m_CompressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");
            
            m_useDefaultPath = EditorPrefs.GetBool(kBuildPrefPrefix + "DefaultOutputBuildPath", m_useDefaultPath);
        }


        public void Update()
        {
        }

        public void OnGUI(Rect pos)
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            bool newState = false;

            //basic options
            EditorGUILayout.Space();
            GUILayout.BeginVertical();

            m_buildTarget = ABBuilder.Instance.CurBuildPlatform;
            EditorGUILayout.TextField(m_TargetContent, m_buildTarget.ToString());
                        
            EditorPrefs.SetInt(kBuildPrefPrefix + "BuildTarget", (int)m_buildTarget);

            m_outputPath = ABBuilder.Instance.GetPathPrefix_Assetbundle();
            EditorUserBuildSettings.SetPlatformSettings(ABBuilder.GetCurrentBuildPlatformName(), "AssetBundleOutputPath", m_outputPath);
            
            //output path
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            var newPath = EditorGUILayout.TextField("Output Path", m_outputPath);
            if (newPath != m_outputPath)
            {
                m_useDefaultPath = false;
                m_outputPath = newPath;
                EditorUserBuildSettings.SetPlatformSettings(ABBuilder.GetCurrentBuildPlatformName(), "AssetBundleOutputPath", m_outputPath);
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (string.IsNullOrEmpty(m_outputPath))
                m_outputPath = EditorUserBuildSettings.GetPlatformSettings(ABBuilder.GetCurrentBuildPlatformName(), "AssetBundleOutputPath");
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            newState = GUILayout.Toggle(
            m_ForceRebuild.state,
            m_ForceRebuild.content);
            if (newState != m_ForceRebuild.state)
            {
                EditorPrefs.SetBool(m_ForceRebuild.PrefKey, newState);
                m_ForceRebuild.state = newState;
            }
            newState = GUILayout.Toggle(
                m_CopyToStreaming.state,
                m_CopyToStreaming.content);
            if (newState != m_CopyToStreaming.state)
            {
                EditorPrefs.SetBool(m_CopyToStreaming.PrefKey, newState);
                m_CopyToStreaming.state = newState;
            }

            // advanced options
            EditorGUILayout.Space();
            m_advancedSettings = EditorGUILayout.Foldout(m_advancedSettings, "Advanced Settings");
            if(m_advancedSettings)
            {
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                CompressOptions cmp = (CompressOptions)EditorGUILayout.IntPopup(
                    m_CompressionContent, 
                    (int)m_compression,
                    m_CompressionOptions,
                    m_CompressionValues);

                if (cmp != m_compression)
                {
                    m_compression = cmp;
                    EditorPrefs.SetInt(kBuildPrefPrefix + "Compression", (int)m_compression);
                }
                foreach (var tog in m_toggleData)
                {
                    newState = EditorGUILayout.ToggleLeft(
                        tog.content,
                        tog.state);
                    if (newState != tog.state)
                    {
                        EditorPrefs.SetBool(tog.PrefKey, newState);
                        tog.state = newState;
                    }
                }
                EditorGUILayout.Space();
                EditorGUI.indentLevel = indent;
            }
            
            // build.
            EditorGUILayout.Space();
            if (GUILayout.Button("Build") )
            {
                ExecuteBuild();
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();           
        }

        private void ExecuteBuild()
        {
            if (string.IsNullOrEmpty(m_outputPath))
                EditorUtility.DisplayDialog("Tips", "AssetBundle output path is null.", "OK");

            if (string.IsNullOrEmpty(m_outputPath))
            {
                Debug.LogError("AssetBundle Build: No valid output path for build.");
                return;
            }

            if (m_ForceRebuild.state)
            {
                string message = "Do you want to delete all files in the directory " + m_outputPath;
                if (m_CopyToStreaming.state)
                    message += " and " + m_streamingPath;
                message += "?";
                if (EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No"))
                {
                    try
                    {
                        if (Directory.Exists(m_outputPath))
                            Directory.Delete(m_outputPath, true);

                        if (m_CopyToStreaming.state)
                            if (Directory.Exists(m_streamingPath))
                                Directory.Delete(m_streamingPath, true);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            if (!Directory.Exists(m_outputPath))
                Directory.CreateDirectory(m_outputPath);
            BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;
            if (m_compression == CompressOptions.Uncompressed)
                opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
            else if (m_compression == CompressOptions.ChunkBasedCompression)
                opt |= BuildAssetBundleOptions.ChunkBasedCompression;
            foreach (var tog in m_toggleData)
            {
                if (tog.state)
                    opt |= tog.option;
            }
            ABBuilder.Instance.BuildAssetbundles(opt);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if (m_CopyToStreaming.state)
                StreamingAssetsSync.SyncAssets();
        }
    }
}