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

    internal class BundleData
    {
        public AssetBundle Bundle;

        public int Count;

        public BundleData(AssetBundle ab)
        {
            Bundle = ab;
            Count = 1;
        }
    }

    //存放Bundle信息的集合
    private Dictionary<string, BundleInfo> m_BundleInfos = new Dictionary<string, BundleInfo>();

    //存放Bundle资源的集合
    private Dictionary<string, BundleData> m_AssetBundles = new Dictionary<string, BundleData>();

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

        BundleData bundle = GetBundle(bundleName); 
        if (bundle == null)
        {
            UObject obj = Manager.Pool.Spawn("AssetBundld", bundleName);
            if (obj != null)
            {
                AssetBundle ab = obj as AssetBundle;
                bundle = new BundleData(ab);
            }
            else
            {
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
                yield return request;
                bundle = new BundleData(request.assetBundle);
            }
            m_AssetBundles.Add(bundleName, bundle);
        }

        if (dependences != null && dependences.Count > 0)
        {
            for (int i = 0; i < dependences.Count; i++)
            {
                yield return LoadBundleAsync(dependences[i]);
            }
        }

        if(assetName.EndsWith(".unity"))
        {
            action?.Invoke(null);
            yield break;
        }

        if (action == null)
        {
            yield break;
        }

        AssetBundleRequest bundleRequest = bundle.Bundle.LoadAssetAsync(assetName);
        yield return bundleRequest;
        Debug.Log("LoadBundleAsync");
        action?.Invoke(bundleRequest?.asset);
    }

    BundleData GetBundle(string name)
    {
        BundleData bundle = null;
        if (m_AssetBundles.TryGetValue(name, out bundle))
        {
            bundle.Count++;
            return bundle;
        }
        return null;
    }

    //减去一个bundle的引用计数
    private void MinusOneBundleCount(string bundleName)
    {
        if (m_AssetBundles.TryGetValue(bundleName, out BundleData bundle))
        {
            if (bundle.Count > 0)
            {
                bundle.Count--;
                Debug.Log("bundle引用计数 :" + bundleName + " count:" + bundle.Count);
            }
            if (bundle.Count <= 0)
            {
                Debug.Log("放入bundle对象池 :" + bundleName);
                Manager.Pool.UnSpawn("AssetBundle", bundleName, bundle.Bundle);
                m_AssetBundles.Remove(bundleName);

            }
        }
    }

    //减去bundle和依赖的引用计数
    public void MinusBundleCount(string assetName)
    {
        string bundleName = m_BundleInfos[assetName].BundleName;

        MinusOneBundleCount(bundleName);

        //依赖资源
        List<string> dependences = m_BundleInfos[assetName].Dependences;
        if (dependences != null)
        {
            foreach (string dependence in dependences)
            {
                string name = m_BundleInfos[dependence].BundleName;
                MinusOneBundleCount(name);
            }
        }

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

    public void LoadPrefab(string assetNmae, Action<UObject> action = null)
    {
        LoadAsset(assetNmae, action);
    }

    //卸载先不做
    public void UnloadBundle(UObject obj)
    {
        AssetBundle ab = obj as AssetBundle;
        ab.Unload(true);
    }

}
