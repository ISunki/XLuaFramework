using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class HotUpdate : MonoBehaviour
{
    internal class DownloadFileInfo
    {
        public string url;
        public string FileName;
        public DownloadHandler fileData;
    }

    private byte[] m_ReadOnlyPathFileListData;
    private byte[] m_ServerFileListData;
    /// <summary>
    /// 下载单个文件
    /// </summary>
    /// <param name="info"></param>
    /// <param name="Complete"></param>
    /// <returns></returns>
    IEnumerator DownloadFile(DownloadFileInfo info, Action<DownloadFileInfo> Complete)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(info.url);
        yield return webRequest.SendWebRequest();
        if (webRequest.isHttpError || webRequest.isNetworkError)
        {
            Debug.LogError("下载文件出错：" + info.url);
            yield break;
            //重试
        }
        info.fileData = webRequest.downloadHandler;
        Complete?.Invoke(info);
        webRequest.Dispose();
    }

    /// <summary>
    /// 下载多个文件
    /// </summary>
    /// <param name="infos"></param>
    /// <param name="Complete"></param>
    /// <param name="DownloadAllComplete"></param>
    /// <returns></returns>
    IEnumerator DownloadFile(List<DownloadFileInfo> infos, Action<DownloadFileInfo> Complete, Action DownloadAllComplete)
    {
        foreach (DownloadFileInfo info in infos)
        {
            yield return DownloadFile(info, Complete);
        }
        DownloadAllComplete?.Invoke();
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileData"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private List<DownloadFileInfo> GetFileList(string fileData, string path)
    {
        string content = fileData.Trim().Replace("\r", "");
        string[] files = content.Split('\n');
        List<DownloadFileInfo> dLFileInfos = new List<DownloadFileInfo>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            string[] info = files[i].Split('|');
            DownloadFileInfo fileInfo = new DownloadFileInfo();
            fileInfo.FileName = info[1];
            fileInfo.url = Path.Combine(path, info[1]);
            dLFileInfos.Add(fileInfo);
        }
        return dLFileInfos;
    }

    private void Start()
    {
        if (IsFirstInstall())
        {
            ReleaseResources();
        }
        else
        {
            CheckUpdate();
        }
    }
    private bool IsFirstInstall()
    {
        //判断只读目录是否存在版本文件
        bool isExistsReadOnlyPath = FileUtil.IsExists(Path.Combine(PathUtil.ReadOnlyPath, AppConst.FileListName));

        //判断可读写目录是否存在版本文件
        bool isExistsReadWritePath = FileUtil.IsExists(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName));

        return isExistsReadOnlyPath && !isExistsReadWritePath;

    }

    private void ReleaseResources()
    {
        string url = Path.Combine(PathUtil.ReadOnlyPath, AppConst.FileListName);
        DownloadFileInfo info = new DownloadFileInfo();
        info.url = url;
        StartCoroutine(DownloadFile(info, OnDownloadReadOnlyPathFileListComplete));
    }

    private void OnDownloadReadOnlyPathFileListComplete(DownloadFileInfo file)
    {
        m_ReadOnlyPathFileListData = file.fileData.data;
        List<DownloadFileInfo> fileInfos = GetFileList(file.fileData.text, PathUtil.ReadOnlyPath);
        StartCoroutine(DownloadFile(fileInfos, OnReleaseFileComplete, OnReleaseAllFileComplete));
    }

    private void OnReleaseAllFileComplete()
    {
        FileUtil.WriteFile(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName), m_ReadOnlyPathFileListData);
        CheckUpdate();
    }

    private void OnReleaseFileComplete(DownloadFileInfo fileInfo)
    {
        Debug.Log("OnReleaseFileComplete:" + fileInfo.url);
        string writeFile = Path.Combine(PathUtil.ReadWritePath, fileInfo.FileName);
        FileUtil.WriteFile(writeFile, fileInfo.fileData.data);
    }

    private void CheckUpdate()
    {
        string url = Path.Combine(AppConst.ResourcesUrl, AppConst.FileListName);
        DownloadFileInfo info = new DownloadFileInfo();
        info.url = url;
        StartCoroutine(DownloadFile(info, OnDownloadServerFileListComplete));
    }

    private void OnDownloadServerFileListComplete(DownloadFileInfo file)
    {
        m_ServerFileListData = file.fileData.data;
        List<DownloadFileInfo> fileInfos = GetFileList(file.fileData.text, AppConst.ResourcesUrl);
        List<DownloadFileInfo> downloadListFiles = new List<DownloadFileInfo>();

        for (int i = 0; i < fileInfos.Count; i++)
        {
            string localFile = Path.Combine(PathUtil.ReadWritePath, fileInfos[i].FileName);
            if (!FileUtil.IsExists(localFile))
            {
                fileInfos[i].url = Path.Combine(AppConst.ResourcesUrl, fileInfos[i].FileName);
                downloadListFiles.Add(fileInfos[i]);
            }
        }
        if (downloadListFiles.Count > 0)
            StartCoroutine(DownloadFile(fileInfos, OnUpdateFileComplete, OnUpdateAllFileComplete));
        else
            EnterGame();
    }


    private void OnUpdateAllFileComplete()
    {
        FileUtil.WriteFile(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName), m_ServerFileListData);
        EnterGame();
    }

    private void OnUpdateFileComplete(DownloadFileInfo file)
    {
        Debug.Log("OnUpdateFileComplete:" + file.url);
        string writeFile = Path.Combine(PathUtil.ReadWritePath, file.FileName);
        FileUtil.WriteFile(writeFile, file.fileData.data);
    }

    private void EnterGame()
    {
        Manager.Resource.ParseVersionFile();
        Manager.Resource.LoadUI("UITest", OnComplete);
    }

    private void OnComplete(UnityEngine.Object obj)
    {
        GameObject go = Instantiate(obj) as GameObject;
        go.transform.SetParent(this.transform);
        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;
    }
}
