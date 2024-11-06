using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PerformanceGraph : MonoBehaviour
{
    [SerializeField] private RectTransform graphContainer;
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Color lineColor = Color.green;
    [SerializeField] private int maxVisibleDataPoints = 100;

    private List<float> dataPoints = new List<float>();
    private List<GameObject> dataObjects = new List<GameObject>();

    public void AddDataPoint(float value)
    {
        dataPoints.Add(value);

        if (dataPoints.Count > maxVisibleDataPoints)
        {
            dataPoints.RemoveAt(0);
        }

        UpdateGraph();
    }

    private void UpdateGraph()
    {
        foreach (var obj in dataObjects)
        {
            Destroy(obj);
        }
        dataObjects.Clear();

        float graphHeight = graphContainer.sizeDelta.y;
        float graphWidth = graphContainer.sizeDelta.x;
        float yMaximum = dataPoints.Max();
        float xSize = graphWidth / (maxVisibleDataPoints - 1);

        for (int i = 0; i < dataPoints.Count; i++)
        {
            float xPosition = i * xSize;
            float yPosition = (dataPoints[i] / yMaximum) * graphHeight;

            GameObject dataObject = CreateDataObject(new Vector2(xPosition, yPosition));
            dataObjects.Add(dataObject);

            if (i > 0)
            {
                GameObject previousDataObject = dataObjects[i - 1];
                ConnectDataPoints(previousDataObject.GetComponent<RectTransform>(), dataObject.GetComponent<RectTransform>());
            }
        }
    }

    private GameObject CreateDataObject(Vector2 position)
    {
        GameObject gameObject = new GameObject("data_point", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().sprite = dotSprite;
        gameObject.GetComponent<Image>().color = lineColor;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform. sizeDelta = new Vector2(5, 5);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        return gameObject;
    }

    private void ConnectDataPoints(RectTransform start, RectTransform end)
    {
        GameObject gameObject = new GameObject("data_line", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().color = lineColor;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 direction = (end.anchoredPosition - start.anchoredPosition).normalized;
        float distance = Vector2.Distance(start.anchoredPosition, end.anchoredPosition);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(distance, 2f);
        rectTransform.anchoredPosition = start.anchoredPosition + direction * distance * 0.5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, GetAngleFromVectorFloat(direction));
        dataObjects.Add(gameObject);
    }

    private float GetAngleFromVectorFloat(Vector2 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (n < 0) n += 360;
        return n;
    }
}