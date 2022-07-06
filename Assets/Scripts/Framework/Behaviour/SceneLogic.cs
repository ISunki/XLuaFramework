using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLogic : LuaBehaviour
{
    public string SceneName;

    Action m_LuaOnActive;
    Action m_LuaOnInactive;
    Action m_LuaOnEnter;
    Action m_LuaOnQuit;

    public override void Init(string luaName)
    {
        base.Init(luaName);
        m_ScriptEnv.Get("OnActice", out m_LuaOnActive);
        m_ScriptEnv.Get("OnInactive", out m_LuaOnInactive);
        m_ScriptEnv.Get("OnEnter", out m_LuaOnEnter);
        m_ScriptEnv.Get("OnQuit", out m_LuaOnQuit);
    }

    public void OnActive()
    {
        m_LuaOnActive?.Invoke();
    }

    public void OnInactive()
    {
        m_LuaOnInactive?.Invoke();
    }

    public void OnEnter()
    {
        m_LuaOnEnter?.Invoke();
    }
    public void OnQuit()
    {
        m_LuaOnQuit?.Invoke();
    }

    protected override void Clear()
    {
        base.Clear();
        m_LuaOnActive = null;
        m_LuaOnInactive = null;
        m_LuaOnEnter = null;
        m_LuaOnQuit = null;
    }
}
