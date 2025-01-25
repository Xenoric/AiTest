using System;
using System.Diagnostics;
using UnityEngine;
using TMPro;

public class PerformanceStats: MonoBehaviour
{
    private float[] fpsSamples = new float[60];
    private int sampleIndex = 0;
    private float minFPS = float.MaxValue;
    private float maxFPS = float.MinValue;
    private float totalFPS = 0f;
    private int sampleCount = 0;

    private float gcMemory = 0f;
    private float totalMemory = 0f;
    private float cpuUsage = 0f;
    private float cpuTime = 0f;
    private float userCpuTime = 0f;
    private float privilegedCpuTime = 0f;
    private float systemMemory = 0f;

    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI minFPSText;
    [SerializeField] private TextMeshProUGUI maxFPSText;
    [SerializeField] private TextMeshProUGUI avgFPSText;
    [SerializeField] private TextMeshProUGUI sampleCountText;
    [SerializeField] private TextMeshProUGUI gcMemoryText;
    [SerializeField] private TextMeshProUGUI totalMemoryText;
    [SerializeField] private TextMeshProUGUI cpuUsageText;
    [SerializeField] private TextMeshProUGUI cpuTimeText;
    [SerializeField] private TextMeshProUGUI userCpuTimeText;
    [SerializeField] private TextMeshProUGUI privilegedCpuTimeText;
    [SerializeField] private TextMeshProUGUI systemMemoryText;

    private Process currentProcess;
    private float updateInterval = 0.5f; // Интервал обновления
    private float timeSinceLastUpdate = 0f;

    void Start()
    {
        currentProcess = Process.GetCurrentProcess();
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            CollectAndUpdatePerformanceStats();
            timeSinceLastUpdate = 0f;
        }
    }

    private void CollectAndUpdatePerformanceStats()
    {
        // Сбор данных о FPS
        float currentFPS = 1f / Time.deltaTime;
        fpsSamples[sampleIndex] = currentFPS;
        sampleIndex = (sampleIndex + 1) % fpsSamples.Length;

        // Обновление минимума, максимума и среднего FPS
        minFPS = Mathf.Min(minFPS, currentFPS);
        maxFPS = Mathf.Max(maxFPS, currentFPS);
        totalFPS += currentFPS;

        if (sampleCount < fpsSamples.Length)
        {
            sampleCount++;
        }
        else
        {
            // Вычитаем старое значение и добавляем новое
            totalFPS = totalFPS - fpsSamples[sampleIndex] + currentFPS;
        }

        float avgFPS = totalFPS / sampleCount;

        // Сбор данных о памяти и CPU
        gcMemory = GC.GetTotalMemory(false) / 1024f / 1024f; // В МБ
        totalMemory = GC.GetTotalMemory(true) / 1024f / 1024f; // В МБ

        cpuTime = (float)currentProcess.TotalProcessorTime.TotalSeconds;
        userCpuTime = (float)currentProcess.UserProcessorTime.TotalSeconds;
        privilegedCpuTime = (float)currentProcess.PrivilegedProcessorTime.TotalSeconds;

        // Расчет использования CPU
        cpuUsage = (float)(currentProcess.TotalProcessorTime.TotalMilliseconds/
            (Environment.ProcessorCount * DateTime.Now.Subtract(currentProcess.StartTime).TotalMilliseconds) * 100f);

        // Системная память
        systemMemory = currentProcess.WorkingSet64 / 1024f / 1024f / 1024f; // В ГБ

        // Обновление текстовых элементов
        UpdateUI(currentFPS, avgFPS);
    }

    private void UpdateUI(float currentFPS, float avgFPS)
    {
        if (fpsText != null) fpsText.text = $"FPS: {currentFPS:F2}";
        if (minFPSText != null) minFPSText.text = $"Min FPS: {minFPS:F2}";
        if (maxFPSText != null) maxFPSText.text = $"Max FPS: {maxFPS:F2}";
        if (avgFPSText != null) avgFPSText.text = $"Avg FPS: {avgFPS:F2}";
        if (sampleCountText != null) sampleCountText.text = $"Sample Count: {sampleCount}";
        if (gcMemoryText != null) gcMemoryText.text = $"GC Memory: {gcMemory:F2} MB";
        if (totalMemoryText != null) totalMemoryText.text = $"Total Memory: {totalMemory:F2} MB";
        if (cpuUsageText != null) cpuUsageText.text = $"CPU Usage: {cpuUsage:F2}%";
        if (cpuTimeText != null) cpuTimeText.text = $"CPU Time: {cpuTime:F2} sec";
        if (userCpuTimeText != null) userCpuTimeText.text = $"User  CPU Time: {userCpuTime:F2} sec";
        if (privilegedCpuTimeText != null) privilegedCpuTimeText.text = $"Privileged CPU Time: {privilegedCpuTime:F2} sec";
        if (systemMemoryText != null) systemMemoryText.text = $"System Memory: {systemMemory:F2} GB";
    }

    void OnDestroy()
    {
        currentProcess?.Dispose();
    }
}