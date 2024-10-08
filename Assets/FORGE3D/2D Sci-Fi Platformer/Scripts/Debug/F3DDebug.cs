using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class F3DDebug : MonoBehaviour
{
    public static F3DDebug Singletone;
    private Text _debugText;
    private static Dictionary<string, string> _cacheDict = new Dictionary<string, string>();

    // Use this for initialization
    private void Awake()
    {
        Singletone = this;
        _debugText = GetComponent<Text>();
        if (_debugText == null) return;
        _debugText.text = string.Empty;
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        if (_debugText == null) return;
        if (_cacheDict == null) return;
        var debugCache = string.Empty;
        foreach (var key in _cacheDict.Keys)
            debugCache += key + _cacheDict[key];
        _debugText.text = debugCache;
    }

    public static void LogBool(string logEntryName, bool logBool)
    {
        AddCache(logEntryName + ": ", logBool + "\n");
    }

    public static void LogFloat(string logEntryName, float logFloat)
    {
        AddCache(logEntryName + ": ", logFloat.ToString("0.000") + "\n");
    }

    public static void LogVector2(string logEntryName, Vector2 logVector2)
    {
        AddCache(logEntryName + ": ", logVector2.x.ToString("0.000") + " " + logVector2.y.ToString("0.000") + "\n");
    }

    public static void LogString(string logEntryName, string logSting)
    {
        AddCache(logEntryName + ": ", logSting + "\n");
    }

    private static void AddCache(string key, string value)
    {
        if (_cacheDict == null) return;
        if (key == string.Empty) return;
        if (value == string.Empty) return;
        if (_cacheDict.ContainsKey(key))
            _cacheDict[key] = value;
        else
            _cacheDict.Add(key, value);
    }

    // GameDev Commands
    private void Update()
    {
        // Reload First Scene
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(0);
    }
}