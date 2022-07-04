using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    IEnumerator Start()
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/prefabs/uitest.prefab.ab");
        yield return request;

        AssetBundleCreateRequest request1 = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/res/miku.jpg.ab");
        yield return request1;

        AssetBundleCreateRequest request2 = AssetBundle.LoadFromFileAsync(Application.streamingAssetsPath + "/ui/res/miku2.jpg.ab");
        yield return request2;

        AssetBundleRequest bundleRequest = request.assetBundle.LoadAssetAsync("Assets/BuildResources/UI/Prefabs/UITest.prefab");
        yield return bundleRequest;

        GameObject go = Instantiate(bundleRequest.asset) as GameObject;
        go.transform.SetParent(this.transform);
        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;

    }

    void Update()
    {
        
    }
}
