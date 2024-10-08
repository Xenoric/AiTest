using UnityEngine;
using System.Collections;

public class F3DDebugTime : MonoBehaviour
{
    public bool Log = false;

    // Use this for initialization
    void Start()
    {
        Time.timeScale = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha0))
        {
            Time.timeScale += Time.deltaTime * 3;
            Time.timeScale = Mathf.Clamp01(Time.timeScale);
            if (Log) F3DDebug.LogFloat("TimeScale", Time.timeScale);
        }
        else if (Input.GetKey(KeyCode.Alpha9))
        {
            Time.timeScale -= Time.deltaTime * 3;
            Time.timeScale = Mathf.Clamp01(Time.timeScale);
            if(Log)F3DDebug.LogFloat("TimeScale", Time.timeScale);
        }
    }
}