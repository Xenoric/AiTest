using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text statsText;
    [SerializeField] private PerformanceGraph fpsGraph;
    [SerializeField] private PerformanceGraph cpuGraph;
    [SerializeField] private PerformanceGraph memoryGraph;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    private float fps;
    private float frameCount;
    private float deltaTime;
    private float gcMemory;
    private float allMemory;

    private float minFps = float.MaxValue;
    private float maxFps = float.MinValue;
    private float totalFps;
    private int fpsUpdateCount;
    private float avgFps;

    private Process currentProcess;
    private TimeSpan previousCpuTime;
    private float cpuUsage;

    private void Start()
    {
        if (statsText == null)
        {
            Debug.LogError("Stats Text reference not set! Please assign a UI Text component.");
            enabled = false;
            return;
        }

        currentProcess = Process.GetCurrentProcess();
        previousCpuTime = currentProcess.TotalProcessorTime;

        StartCoroutine(UpdateStats());
    }

    private void Update()
    {
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        
        if (deltaTime > 1.0f)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0;

            minFps = Mathf.Min(minFps, fps);
            maxFps = Mathf.Max(maxFps, fps);

            totalFps += fps;
            fpsUpdateCount++;
            avgFps = totalFps / fpsUpdateCount;
        }
    }

    private IEnumerator UpdateStats()
    {
        WaitForSeconds wait = new WaitForSeconds(updateInterval);
        
        while (true)
        {
            UpdateMemoryStats();
            UpdateCPUStats();
            UpdateUIText();
            yield return wait;
        }
    }

    private void UpdateMemoryStats()
    {
        gcMemory = System.GC.GetTotalMemory(false) / 1024f / 1024f; // MB
        allMemory = System.Convert.ToSingle(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()) / 1024f / 1024f; // MB
    }

    private void UpdateCPUStats()
    {
        TimeSpan currentCpuTime = currentProcess.TotalProcessorTime;
        TimeSpan timeDiff = currentCpuTime - previousCpuTime;
        
        cpuUsage = (float)(timeDiff.TotalMilliseconds / (updateInterval * 1000.0) / System.Environment.ProcessorCount * 100.0);
        
        previousCpuTime = currentCpuTime;
    }

    private void UpdateUIText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendFormat("FPS: {0:F1}\n", fps);
        sb.AppendFormat("Min FPS: {0:F1}\n", minFps);
        sb.AppendFormat("Max FPS: {0:F1}\n", maxFps);
        sb.AppendFormat("Avg FPS: {0:F1}\n", avgFps);
        sb.AppendFormat("Sample Count: {0}\n", fpsUpdateCount);
        
        sb.AppendFormat("GC Memory: {0:F1} MB\n", gcMemory);
        sb.AppendFormat("Total Memory: {0:F1} MB\n", allMemory);
        
        sb.AppendFormat("CPU Usage: {0:F1}%\n", cpuUsage);
        sb.AppendFormat("CPU Time: {0:F1} sec\n", currentProcess.TotalProcessorTime.TotalSeconds);
        sb.AppendFormat("User CPU Time: {0:F1} sec\n", currentProcess.UserProcessorTime.TotalSeconds);
        sb.AppendFormat("Privileged CPU Time: {0:F1} sec\n", currentProcess.PrivilegedProcessorTime.TotalSeconds);
        
        sb.AppendFormat("System Memory: {0:F1} GB\n", SystemInfo.systemMemorySize / 1024f);
        sb.AppendFormat("OS: {0}\n", SystemInfo.operatingSystem);
        sb.AppendFormat("CPU: {0}\n", SystemInfo.processorType);
        sb.AppendFormat("GPU: {0}\n", SystemInfo.graphicsDeviceName);

        statsText.text = sb.ToString();

        // Обновляем графики
        fpsGraph.AddDataPoint(fps);
        cpuGraph.AddDataPoint(cpuUsage);
        memoryGraph.AddDataPoint(allMemory);
    }

    public void ResetFPSStats()
    {
        minFps = float.MaxValue;
        maxFps = float.MinValue;
        totalFps = 0;
        fpsUpdateCount = 0;
        avgFps = 0;
    }

    private void OnDestroy()
    {
        if (currentProcess != null)
        {
            currentProcess.Dispose();
        }
    }
}