using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AssetBundle 打包工具
/// </summary>
public class BuildAssetBundle 
{
    /// <summary>
    /// 打包指定文件夹下的所有资源为AssetBundle
    /// </summary>
    [UnityEditor.MenuItem("AssetBundleTools/BuildSelectedFolderAssetBundle")]
    public static void BuildSelectedFolderAssetBundle() {
        UnityEditor.BuildAssetBundleOptions s_options = UnityEditor.BuildAssetBundleOptions.UncompressedAssetBundle | UnityEditor.BuildAssetBundleOptions.DeterministicAssetBundle | UnityEditor.BuildAssetBundleOptions.IgnoreTypeTreeChanges;
        // 打包AB输出路径
        // string strABOutPathDir = "Assets/AssetBundleIntroduction/Res_Assetbundle";
        string strABOutPathDir = "Assets/ABOut";

        // 获取“StreamingAssets”文件夹路径（不一定这个文件夹，可自定义）
        // strABOutPathDir = Application.streamingAssetsPath;

        // 判断文件夹是否存在，不存在则新建
        if (Directory.Exists(strABOutPathDir) == false)
        {
            Directory.CreateDirectory(strABOutPathDir);
        }

        // 获取要打包的文件夹路径
        // string folderPath = EditorUtility.OpenFolderPanel("选择要打包的文件夹", "Assets", "");
        string folderPath = "Assets/AssetBundleIntroduction/Res_Assetbundle";

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("未选择任何文件夹！");
            return;
        }

        // 获取文件夹下的所有资源
        string[] assetPaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

        List<AssetBundleBuild> bundleBuilds = new List<AssetBundleBuild>();

        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = "myassetbundle";
        build.assetNames = assetPaths;

        bundleBuilds.Add(build);

        // 打包生成AB包 (目标平台根据需要设置即可)
        BuildPipeline.BuildAssetBundles(strABOutPathDir, bundleBuilds.ToArray(), s_options, BuildTarget.StandaloneWindows64);

        Debug.Log("AssetBundle 打包完成！");
    }
}