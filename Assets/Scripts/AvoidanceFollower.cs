using UnityEngine;
using PathBerserker2d;

namespace BotSystem
{
    [RequireComponent(typeof(NavAgent))]
    public class AvoidanceFollower : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Following Settings (настраиваются из BotManager)")]
        [HideInInspector] public float closeEnoughRadius = 3.0f;
        [HideInInspector] public float travelStopRadius = 1.0f;
        [HideInInspector] public float targetPredictionTime = 0.1f;
        [HideInInspector] public float pathUpdateInterval = 0.3f;

        [Header("Distance Control")]
        [Tooltip("Минимальное расстояние до цели")]
        [HideInInspector] public float minDistanceThreshold = 2.0f;
        [Tooltip("Максимальное расстояние до цели")]
        [HideInInspector] public float maxDistanceThreshold = 5.0f;
        [Tooltip("Оптимальное расстояние до цели")]
        [HideInInspector] public float optimalDistance = 3.5f;

        [Header("Line of Sight")]
        [Tooltip("Максимальная дистанция проверки видимости")]
        [HideInInspector] public float lineOfSightDistance = 10.0f;
        [Tooltip("Слои препятствий для проверки видимости")]
        [HideInInspector] public LayerMask obstacleLayerMask = -1;
        [Tooltip("Интервал проверки видимости")]
        [HideInInspector] public float lineOfSightCheckInterval = 0.2f;

        // NavTag константы - теперь используются как идентификаторы команды
        private const int TEAM1 = 1;
        private const int TEAM2 = 2;

        // Компоненты
        private NavAgent navAgent;
        private NavAreaMarker avoidanceMarker;
        private SimpleBotManager botManager;

        // Состояние следования
        private Vector2 lastTargetPosition;
        private float lastPathUpdateTime;
        private bool isInitialized;

        // Состояние избегания
        private int myTeamNumber;
        private Vector3 lastMarkerPosition;
        private float lastMarkerUpdateTime;
        private float avoidanceRadius = 2f;
        private float avoidanceHeight = 1f;
        private float markerUpdateThreshold = 0.1f;
        private float markerUpdateInterval = 0.1f;

        // Состояние видимости и расстояния
        private bool hasLineOfSight = true;
        private float lastLineOfSightCheck = 0f;
        private Vector2 lastKnownTargetPosition;
        private Vector2 alternativeTargetPosition;
        private bool useAlternativeTarget = false;
        private float distanceToTarget = 0f;

        private void Awake()
        {
            navAgent = GetComponent<NavAgent>();
            if (navAgent == null)
            {
                Debug.LogError($"AvoidanceFollower: NavAgent не найден на {gameObject.name}!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            InitializeComponents();
        }

        private void OnDestroy()
        {
            // Отписываемся от событий NavAgent
            if (navAgent != null)
            {
                navAgent.OnFailedToFindPath -= OnFailedToFindPath;
            }

            // Очищаем маркер избегания
            if (avoidanceMarker != null)
            {
                DestroyImmediate(avoidanceMarker.gameObject);
            }
        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        private void InitializeComponents()
        {
            botManager = SimpleBotManager.Instance;
            if (botManager == null)
            {
                Debug.LogError($"AvoidanceFollower: SimpleBotManager не найден для {gameObject.name}!");
                return;
            }

            // Определяем команду бота
            myTeamNumber = botManager.GetBotTeamNumber(gameObject);
            if (myTeamNumber == 0)
            {
                Debug.LogError($"AvoidanceFollower: Бот {gameObject.name} не принадлежит ни к одной команде!");
                return;
            }

            // Создаем маркер избегания
            CreateAvoidanceMarker();

            // Подписываемся на события NavAgent
            navAgent.OnFailedToFindPath += OnFailedToFindPath;

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || target == null || navAgent == null)
            {
                if (!isInitialized) Debug.LogWarning($"AvoidanceFollower {gameObject.name}: Не инициализирован");
                if (target == null) Debug.LogWarning($"AvoidanceFollower {gameObject.name}: Нет цели");
                if (navAgent == null) Debug.LogWarning($"AvoidanceFollower {gameObject.name}: Нет NavAgent");
                return;
            }

            // Обновляем расстояние до цели
            distanceToTarget = Vector2.Distance(navAgent.Position, target.position);

            // Проверяем прямую видимость с заданным интервалом
            if (Time.time - lastLineOfSightCheck >= lineOfSightCheckInterval)
            {
                CheckLineOfSight();
                lastLineOfSightCheck = Time.time;
            }

            // Обновляем маркер избегания при движении
            UpdateAvoidanceMarker();

            // Обновляем следование к цели с заданным интервалом
            if (Time.time - lastPathUpdateTime >= pathUpdateInterval)
            {
                UpdateTargetFollowing();
                lastPathUpdateTime = Time.time;
            }
        }



        /// <summary>
        /// Обновление следования к цели с NavTag избеганием, контролем расстояния и видимости
        /// </summary>
        private void UpdateTargetFollowing()
        {
            if (target == null)
                return;

            Vector2 targetPos = GetOptimalTargetPosition();

            // Проверяем, нужно ли обновить путь на основе расстояния и видимости
            if (ShouldUpdatePath(targetPos))
            {
                // NavAgent автоматически учтет NavTag стоимости при построении пути
                navAgent.UpdatePath(targetPos);
                lastTargetPosition = targetPos;
            }
            else if (IsWithinOptimalRange() && hasLineOfSight)
            {
                // Останавливаемся, если находимся в оптимальном диапазоне и есть видимость
                navAgent.Stop();
            }
        }

        /// <summary>
        /// Определяет оптимальную позицию для движения с учетом расстояния и видимости
        /// </summary>
        private Vector2 GetOptimalTargetPosition()
        {
            if (target == null)
                return transform.position;

            Vector2 targetPos = GetTargetPosition();

            // Если нет прямой видимости, используем альтернативную позицию
            if (!hasLineOfSight && useAlternativeTarget)
            {
                return alternativeTargetPosition;
            }

            // Если слишком близко к цели, отступаем
            if (distanceToTarget < minDistanceThreshold && hasLineOfSight)
            {
                Vector2 directionAway = (navAgent.Position - (Vector2)target.position).normalized;
                return (Vector2)target.position + directionAway * optimalDistance;
            }

            // Если слишком далеко, приближаемся до оптимального расстояния
            if (distanceToTarget > maxDistanceThreshold)
            {
                Vector2 directionToTarget = ((Vector2)target.position - navAgent.Position).normalized;
                return (Vector2)target.position - directionToTarget * optimalDistance;
            }

            return targetPos;
        }

        /// <summary>
        /// Проверяет, нужно ли обновлять путь
        /// </summary>
        private bool ShouldUpdatePath(Vector2 targetPos)
        {
            // Обновляем путь если:
            // 1. Цель значительно изменила позицию
            // 2. Агент не следует пути
            // 3. Не находимся в оптимальном диапазоне
            // 4. Нет прямой видимости и нужно искать обходной путь

            bool targetMoved = Vector2.Distance(lastTargetPosition, targetPos) > 1.0f;
            bool notFollowingPath = !navAgent.IsFollowingAPath;
            bool notInOptimalRange = !IsWithinOptimalRange();
            bool needAlternativePath = !hasLineOfSight;

            return targetMoved || notFollowingPath || notInOptimalRange || needAlternativePath;
        }

        /// <summary>
        /// Проверяет, находится ли бот в оптимальном диапазоне расстояний
        /// </summary>
        private bool IsWithinOptimalRange()
        {
            return distanceToTarget >= minDistanceThreshold && distanceToTarget <= maxDistanceThreshold;
        }

        /// <summary>
        /// Получение позиции цели с предсказанием
        /// </summary>
        private Vector2 GetTargetPosition()
        {
            if (targetPredictionTime <= 0 || target == null)
                return target.position;

            // Простое предсказание на основе скорости цели
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                Vector2 predictedPos = (Vector2)target.position + targetRb.velocity * targetPredictionTime;
                return predictedPos;
            }

            return target.position;
        }

        /// <summary>
        /// Проверка прямой видимости до цели
        /// </summary>
        private void CheckLineOfSight()
        {
            if (target == null)
            {
                hasLineOfSight = false;
                return;
            }

            Vector2 startPos = navAgent.Position;
            Vector2 targetPos = target.position;
            Vector2 direction = (targetPos - startPos).normalized;
            float distance = Vector2.Distance(startPos, targetPos);

            // Ограничиваем дистанцию проверки
            distance = Mathf.Min(distance, lineOfSightDistance);

            // Выполняем raycast для проверки препятствий
            RaycastHit2D hit = Physics2D.Raycast(startPos, direction, distance, obstacleLayerMask);

            if (hit.collider != null)
            {
                // Проверяем, не является ли препятствие самой целью
                if (hit.collider.transform != target)
                {
                    hasLineOfSight = false;
                    // Ищем альтернативный путь
                    FindAlternativeTarget(hit.point, targetPos);
                }
                else
                {
                    hasLineOfSight = true;
                    useAlternativeTarget = false;
                }
            }
            else
            {
                hasLineOfSight = true;
                useAlternativeTarget = false;
            }

            // Сохраняем последнюю известную позицию цели
            if (hasLineOfSight)
            {
                lastKnownTargetPosition = targetPos;
            }
        }

        /// <summary>
        /// Поиск альтернативной цели для обхода препятствий
        /// </summary>
        private void FindAlternativeTarget(Vector2 obstaclePoint, Vector2 originalTarget)
        {
            Vector2 startPos = navAgent.Position;

            // Вычисляем направление к препятствию и перпендикулярные направления
            Vector2 toObstacle = (obstaclePoint - startPos).normalized;
            Vector2 perpendicular1 = new Vector2(-toObstacle.y, toObstacle.x);
            Vector2 perpendicular2 = new Vector2(toObstacle.y, -toObstacle.x);

            // Расстояние для поиска обходного пути
            float searchDistance = 3.0f;

            // Пробуем найти свободный путь в перпендикулярных направлениях
            Vector2[] candidatePositions = {
                obstaclePoint + perpendicular1 * searchDistance,
                obstaclePoint + perpendicular2 * searchDistance,
                obstaclePoint + (perpendicular1 + toObstacle).normalized * searchDistance,
                obstaclePoint + (perpendicular2 + toObstacle).normalized * searchDistance
            };

            Vector2 bestPosition = lastKnownTargetPosition;
            float bestScore = float.MaxValue;

            foreach (Vector2 candidatePos in candidatePositions)
            {
                // Проверяем, свободен ли путь к кандидату
                if (!Physics2D.Raycast(startPos, (candidatePos - startPos).normalized,
                    Vector2.Distance(startPos, candidatePos), obstacleLayerMask))
                {
                    // Вычисляем оценку позиции (расстояние до оригинальной цели)
                    float score = Vector2.Distance(candidatePos, originalTarget);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPosition = candidatePos;
                    }
                }
            }

            alternativeTargetPosition = bestPosition;
            useAlternativeTarget = true;
        }

        /// <summary>
        /// Обработка неудачи поиска пути
        /// </summary>
        private void OnFailedToFindPath(NavAgent agent)
        {
            // Пытаемся повторить поиск пути через некоторое время
            if (target != null)
            {
                Invoke(nameof(RetryPathfinding), 1.0f);
            }
        }

        /// <summary>
        /// Повторная попытка поиска пути
        /// </summary>
        private void RetryPathfinding()
        {
            if (target != null && navAgent != null)
            {
                navAgent.PathTo(target.position);
            }
        }

        /// <summary>
        /// Создание маркера избегания
        /// </summary>
        private void CreateAvoidanceMarker()
        {
            // Создаем дочерний объект для маркера
            GameObject markerObj = new GameObject($"AvoidanceMarker_Team{myTeamNumber}");
            markerObj.transform.SetParent(transform, false);
            markerObj.transform.localPosition = Vector3.zero;

            // Добавляем NavAreaMarker
            avoidanceMarker = markerObj.AddComponent<NavAreaMarker>();

            // Настраиваем NavTag в зависимости от команды
            avoidanceMarker.NavTag = myTeamNumber; // TEAM1 или TEAM2

            // Настраиваем размер зоны
            RectTransform rect = markerObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(avoidanceRadius * 2, avoidanceHeight * 2);
            rect.localRotation = Quaternion.identity;

            // Настраиваем параметры обновления
            avoidanceMarker.updateAfterTimeOfNoMovement = markerUpdateInterval;

            lastMarkerPosition = transform.position;
            lastMarkerUpdateTime = Time.time;
        }

        /// <summary>
        /// Обновление маркера избегания при движении
        /// </summary>
        private void UpdateAvoidanceMarker()
        {
            if (avoidanceMarker == null)
                return;

            // Проверяем, нужно ли обновить маркер
            float timeSinceUpdate = Time.time - lastMarkerUpdateTime;
            float distanceMoved = Vector3.Distance(transform.position, lastMarkerPosition);

            if (distanceMoved > markerUpdateThreshold && timeSinceUpdate >= markerUpdateInterval)
            {
                lastMarkerPosition = transform.position;
                lastMarkerUpdateTime = Time.time;

                // Обновляем тег в зависимости от команды
                avoidanceMarker.NavTag = myTeamNumber;
                
                // NavAreaMarker автоматически обновится благодаря своей внутренней логике
                avoidanceMarker.transform.hasChanged = true;
            }
        }

        /// <summary>
        /// Настройка параметров следования (вызывается из SimpleBotManager)
        /// </summary>
        public void ConfigureFollowing(float closeEnough, float stopRadius, float predictionTime, float updateInterval)
        {
            closeEnoughRadius = closeEnough;
            travelStopRadius = stopRadius;
            targetPredictionTime = predictionTime;
            pathUpdateInterval = updateInterval;
        }

        /// <summary>
        /// Настройка параметров избегания (вызывается из SimpleBotManager)
        /// </summary>
        public void ConfigureAvoidance(float radius, float height, float threshold, float interval)
        {
            avoidanceRadius = radius;
            avoidanceHeight = height;
            markerUpdateThreshold = threshold;
            markerUpdateInterval = interval;

            // Обновляем размер маркера, если он уже создан
            if (avoidanceMarker != null)
            {
                RectTransform rect = avoidanceMarker.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(avoidanceRadius * 2, avoidanceHeight * 2);
                avoidanceMarker.updateAfterTimeOfNoMovement = markerUpdateInterval;
            }
        }

        /// <summary>
        /// Настройка параметров контроля расстояния (вызывается из SimpleBotManager)
        /// </summary>
        public void ConfigureDistanceControl(float minDistance, float maxDistance, float optimal)
        {
            minDistanceThreshold = minDistance;
            maxDistanceThreshold = maxDistance;
            optimalDistance = optimal;
        }

        /// <summary>
        /// Настройка параметров проверки видимости (вызывается из SimpleBotManager)
        /// </summary>
        public void ConfigureLineOfSight(float maxDistance, LayerMask obstacles, float checkInterval)
        {
            lineOfSightDistance = maxDistance;
            obstacleLayerMask = obstacles;
            lineOfSightCheckInterval = checkInterval;
        }

        /// <summary>
        /// Получение NavAreaMarker компонента
        /// </summary>
        public NavAreaMarker GetAvoidanceMarker()
        {
            return avoidanceMarker;
        }

        /// <summary>
        /// Получение текущего NavTag маркера
        /// </summary>
        public int GetNavTag()
        {
            return avoidanceMarker != null ? avoidanceMarker.NavTag : 0;
        }

        /// <summary>
        /// Включение/выключение маркера
        /// </summary>
        public void SetMarkerEnabled(bool enabled)
        {
            if (avoidanceMarker != null)
            {
                avoidanceMarker.gameObject.SetActive(enabled);
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
        /// Установка NavTag для маркера (для динамического изменения поведения)
        /// </summary>
        public void SetNavTag(int navTag)
        {
            if (avoidanceMarker != null)
            {
                avoidanceMarker.NavTag = navTag; // Directly set the NavTag
            }
        }

        /// <summary>
        /// Принудительное обновление пути к цели
        /// </summary>
        public void ForceUpdatePath()
        {
            if (target != null && navAgent != null)
            {
                navAgent.UpdatePath(GetTargetPosition());
                lastTargetPosition = GetTargetPosition();
                lastPathUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Получение информации о состоянии видимости
        /// </summary>
        public bool HasLineOfSight => hasLineOfSight;

        /// <summary>
        /// Получение текущего расстояния до цели
        /// </summary>
        public float DistanceToTarget => distanceToTarget;

        /// <summary>
        /// Проверка, находится ли в оптимальном диапазоне
        /// </summary>
        public bool IsInOptimalRange => IsWithinOptimalRange();

        /// <summary>
        /// Отладочная визуализация
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Vector3 myPos = transform.position;
            Vector3 targetPos = target.position;

            // Рисуем линию видимости
            if (hasLineOfSight)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawLine(myPos, targetPos);

            // Рисуем диапазоны расстояний
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Красный для минимального
            Gizmos.DrawWireSphere(myPos, minDistanceThreshold);

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // Зеленый для оптимального
            Gizmos.DrawWireSphere(myPos, optimalDistance);

            Gizmos.color = new Color(0f, 0f, 1f, 0.2f); // Синий для максимального
            Gizmos.DrawWireSphere(myPos, maxDistanceThreshold);

            // Рисуем альтернативную цель, если используется
            if (useAlternativeTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(alternativeTargetPosition, 0.5f);
                Gizmos.DrawLine(myPos, alternativeTargetPosition);
            }

            // Рисуем текущее расстояние
            Gizmos.color = IsWithinOptimalRange() ? Color.green : new Color(1f, 0.5f, 0f); // Оранжевый
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }
    }
}