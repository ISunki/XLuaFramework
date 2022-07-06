using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    //缓存UI
    Dictionary<string, GameObject> m_UI = new Dictionary<string, GameObject>();

    //UI分组
    Dictionary<string, Transform> m_UIGroups = new Dictionary<string, Transform>();

    private Transform m_UIParent;

    private void Awake()
    {
        m_UIParent = this.transform.parent.Find("UI");
    }

    public void SetUIGroup(List<string> gruop)
    {
        for (int i = 0; i < gruop.Count; i++)
        {
            GameObject go = new GameObject("Group-" + gruop[i]);
            go.transform.SetParent(m_UIParent, false);
            m_UIGroups.Add(gruop[i], go.transform);
        }
    }

    Transform GetUIGroup(string group)
    {
        if (!m_UIGroups.ContainsKey(group))
            Debug.LogError("cannot found group");
        return m_UIGroups[group];
    }


    public void OpenUI(string uiName, string group, string luaName)
    {
        GameObject ui = null;
        if (m_UI.TryGetValue(uiName, out ui))
        {
            UILogic uILogic = ui.GetComponent<UILogic>();
            uILogic.OnOpen();
            return;
        }

        Manager.Resource.LoadUI(uiName, (System.Action<Object>)((UnityEngine.Object obj) =>
        {
            ui = Instantiate(obj) as GameObject;
            m_UI.Add(uiName, ui);

            Transform parent = GetUIGroup(group);
            ui.transform.SetParent(parent, false);

            UILogic uILogic = ui.AddComponent<UILogic>();
            uILogic.Init(luaName);
            uILogic.OnOpen();
        }));
    }

}
