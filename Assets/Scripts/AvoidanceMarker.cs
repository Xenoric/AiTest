using UnityEngine;
using PathBerserker2d;

namespace BotSystem
{
    /// <summary>
    /// Создает динамические NavAreaMarker для избегания ботов через систему NavTag
    /// Интегрируется с SimpleBotManager для автоматического определения команд
    /// </summary>
    public class AvoidanceMarker : MonoBehaviour
    {
        [Header("Avoidance Settings")]
        [Tooltip("Радиус зоны избегания")]
        [SerializeField] private float avoidanceRadius = 2f;
        [Tooltip("Высота зоны избегания")]
        [SerializeField] private float avoidanceHeight = 1f;
        [Tooltip("Минимальное расстояние для обновления маркера")]
        [SerializeField] private float updateThreshold = 0.1f;
        [Tooltip("Интервал обновления маркера")]
        [SerializeField] private float updateInterval = 0.1f;

        // NavTag константы
        private const int ENEMY_TAG = 1;
        private const int ALLY_TAG = 2;

        // Компоненты
        private NavAreaMarker marker;
        private SimpleBotManager botManager;

        // Состояние
        private Vector3 lastPosition;
        private float lastUpdateTime;
        private int myTeamNumber;
        private bool isInitialized;

        private void Start()
        {
            InitializeMarker();
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Обновляем маркер только при необходимости
            if (ShouldUpdateMarker())
            {
                UpdateMarkerPosition();
            }
        }

        private void OnDestroy()
        {
            // Очищаем маркер при уничтожении объекта
            if (marker != null)
            {
                DestroyImmediate(marker.gameObject);
            }
        }

        /// <summary>
        /// Инициализация маркера избегания
        /// </summary>
        private void InitializeMarker()
        {
            botManager = SimpleBotManager.Instance;
            if (botManager == null)
            {
                Debug.LogWarning($"AvoidanceMarker: SimpleBotManager не найден для {gameObject.name}");
                return;
            }

            // Определяем команду бота
            myTeamNumber = botManager.GetBotTeamNumber(gameObject);
            if (myTeamNumber == 0)
            {
                Debug.LogWarning($"AvoidanceMarker: Бот {gameObject.name} не принадлежит ни к одной команде");
                return;
            }

            CreateNavAreaMarker();
            lastPosition = transform.position;
            lastUpdateTime = Time.time;
            isInitialized = true;
        }

        /// <summary>
        /// Создание NavAreaMarker
        /// </summary>
        private void CreateNavAreaMarker()
        {
            // Создаем дочерний объект для маркера
            GameObject markerObj = new GameObject($"AvoidanceMarker_Team{myTeamNumber}");
            markerObj.transform.SetParent(transform, false);
            markerObj.transform.localPosition = Vector3.zero;

            // Добавляем NavAreaMarker
            marker = markerObj.AddComponent<NavAreaMarker>();

            // Настраиваем NavTag (враги для других команд)
            marker.NavTag = ENEMY_TAG;

            // Настраиваем размер зоны
            RectTransform rect = markerObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(avoidanceRadius * 2, avoidanceHeight * 2);
            rect.localRotation = Quaternion.identity;

            // Настраиваем параметры обновления
            marker.updateAfterTimeOfNoMovement = updateInterval;
        }

        /// <summary>
        /// Проверяет, нужно ли обновлять маркер
        /// </summary>
        private bool ShouldUpdateMarker()
        {
            float timeSinceUpdate = Time.time - lastUpdateTime;
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            return distanceMoved > updateThreshold && timeSinceUpdate >= updateInterval;
        }

        /// <summary>
        /// Обновляет позицию маркера
        /// </summary>
        private void UpdateMarkerPosition()
        {
            lastPosition = transform.position;
            lastUpdateTime = Time.time;

            // NavAreaMarker автоматически обновится благодаря своей внутренней логике
            // Мы просто отмечаем, что transform изменился
            if (marker != null)
            {
                marker.transform.hasChanged = true;
            }
        }

        /// <summary>
        /// Настройка параметров избегания (вызывается из SimpleBotManager)
        /// </summary>
        public void ConfigureAvoidance(float radius, float height, float threshold, float interval)
        {
            avoidanceRadius = radius;
            avoidanceHeight = height;
            updateThreshold = threshold;
            updateInterval = interval;

            // Обновляем размер маркера, если он уже создан
            if (marker != null)
            {
                RectTransform rect = marker.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(avoidanceRadius * 2, avoidanceHeight * 2);
                marker.updateAfterTimeOfNoMovement = updateInterval;
            }
        }

        /// <summary>
        /// Получение текущего NavTag маркера
        /// </summary>
        public int GetNavTag()
        {
            return marker != null ? marker.NavTag : 0;
        }

        /// <summary>
        /// Включение/выключение маркера
        /// </summary>
        public void SetMarkerEnabled(bool enabled)
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(enabled);
            }
        }

        /// <summary>
        /// Получение радиуса избегания
        /// </summary>
        public float GetAvoidanceRadius()
        {
            return avoidanceRadius;
        }

        /// <summary>
        /// Отладочная визуализация
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Рисуем зону избегания
            Gizmos.color = myTeamNumber == 1 ? Color.red : Color.blue;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawCube(transform.position, new Vector3(avoidanceRadius * 2, avoidanceHeight * 2, 0.1f));

            // Рисуем границу
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 1f);
            Gizmos.DrawWireCube(transform.position, new Vector3(avoidanceRadius * 2, avoidanceHeight * 2, 0.1f));
        }
    }
}