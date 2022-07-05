using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UObject = UnityEngine.Object;

public class ResourceManager : MonoBehaviour
{
    internal class BundleInfo
    {
        public string AssetName;
        public string BundleName;
        public List<string> Dependences;
    }

    //存放Bundle信息的集合
    private Dictionary<string, BundleInfo> m_BundleInfos = new Dictionary<string, BundleInfo>();

    /// <summary>
    /// 解析版本文件
    /// </summary>
    public void ParseVersionFile()
    {
        //版本文件的路径
        string url = Path.Combine(PathUtil.BundleResourcePath, AppConst.FileListName);
        string[] data = File.ReadAllLines(url);

        //解析文件信息
        for (int i = 0; i < data.Length; i++)
        {
            BundleInfo bundleInfo = new BundleInfo();
            string[] info = data[i].Split('|');
            bundleInfo.AssetName = info[0];
            bundleInfo.BundleName = info[1];
            //list特性：本质是数组，但可以动态扩容
            bundleInfo.Dependences = new List<string>(info.Length - 2);
            for (int j = 2; j < info.Length; j++)
            {
                bundleInfo.Dependences.Add(info[j]);
            }
            m_BundleInfos.Add(bundleInfo.AssetName, bundleInfo);

            if (info[0].IndexOf("LuaScripts") > 0)
                Manager.Lua.LuaNames.Add(info[0]);
        }
    }

    IEnumerator LoadBundleAsync(string assetName, Action<UObject> action = null)
    {
        string bundleName = m_BundleInfos[assetName].BundleName;
        string bundlePath = Path.Combine(PathUtil.BundleResourcePath, bundleName);
        List<string> dependences = m_BundleInfos[assetName].Dependences;
        if (dependences != null && dependences.Count > 0)
        {
            for (int i = 0; i < dependences.Count; i++)
            {
                yield return LoadBundleAsync(dependences[i]);
            }
        }

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return request;

        AssetBundleRequest bundleRequest = request.assetBundle.LoadAssetAsync(assetName);
        yield return bundleRequest;
        Debug.Log("LoadBundleAsync");
        action?.Invoke(bundleRequest?.asset);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器环境加载资源
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="action"></param>
    void EditorLoadAsset(string assetName, Action<UObject> action = null)
    {
        Debug.Log("EditorLoadAsset");
        UObject obj = UnityEditor.AssetDatabase.LoadAssetAtPath(assetName, typeof(UObject));
        if (obj == null)
            Debug.LogError("asset not exists: " + assetName);
        action?.Invoke(obj);
    }
#endif


    private void LoadAsset(string assetName, Action<UObject> action)
    {
#if UNITY_EDITOR
        if (AppConst.GameMode == GameMode.EditorMode)
            EditorLoadAsset(assetName, action);
        else
#endif 
            StartCoroutine(LoadBundleAsync(assetName, action));
    }

    public void LoadUI(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(PathUtil.GetUIPath(assetNmae), action);
    }

    public void LoadMusic(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(PathUtil.GetMusicPath(assetNmae), action);
    }

    public void LoadSound(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(PathUtil.GetSoundPath(assetNmae), action);
    }

    public void LoadEffect(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(PathUtil.GetEffectPath(assetNmae), action);
    }

    public void LoadScene(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(PathUtil.GetScenePath(assetNmae), action);
    }

    public void LoadLua(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(assetNmae, action);
    }

    //卸载先不做


}
