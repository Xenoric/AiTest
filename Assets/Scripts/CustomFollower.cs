using UnityEngine;
using PathBerserker2d;
using System.Collections.Generic;

/// <summary>
/// Улучшенный компонент следования с интеграцией PathBerserker2d и избеганием союзников/врагов
/// </summary>
[RequireComponent(typeof(NavAgent))]
public class CustomFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    // Компоненты
    private NavAgent navAgent;
    private BotManager.PathFollowingSettings pathSettings;
    private BotManager.AvoidanceSettings avoidanceSettings;

    // Кэшированные списки для оптимизации
    private List<GameObject> teammates;
    private List<GameObject> enemies;

    // Состояние следования
    private Vector2 lastTargetPosition;
    private Vector2 lastValidTargetPosition;
    private bool hasValidTarget;

    // Таймеры для оптимизации производительности
    private float pathUpdateTimer;
    private float allyCheckTimer;
    private float enemyCheckTimer;

    // Состояние избегания
    private bool isAvoidingAlly;
    private bool isAvoidingEnemy;
    private float avoidanceTimer;
    private GameObject currentObstacle;
    private Vector2 avoidanceDestination;

    // Счетчики для обработки ошибок
    private int consecutivePathFailures;

    private void Awake()
    {
        navAgent = GetComponent<NavAgent>();
        InitializeFromBotManager();
    }

    private void Start()
    {
        CacheTeamLists();
        InitializeTargetTracking();
        SubscribeToNavAgentEvents();
    }

    /// <summary>
    /// Инициализация настроек из BotManager
    /// </summary>
    private void InitializeFromBotManager()
    {
        if (BotManager.Instance == null)
        {
            Debug.LogError($"BotManager.Instance is null for {name}. Using default settings.");
            return;
        }

        pathSettings = BotManager.Instance.GetPathSettings(gameObject);
        avoidanceSettings = BotManager.Instance.GetAvoidanceSettings(gameObject);
    }

    /// <summary>
    /// Кэширование списков команд для оптимизации
    /// </summary>
    private void CacheTeamLists()
    {
        if (BotManager.Instance == null) return;

        teammates = BotManager.Instance.GetTeamBots(BotManager.Instance.GetBotTeamID(gameObject));
        enemies = BotManager.Instance.GetEnemyBots(gameObject);
    }

    /// <summary>
    /// Инициализация отслеживания цели
    /// </summary>
    private void InitializeTargetTracking()
    {
        if (target != null)
        {
            lastTargetPosition = target.position;
            lastValidTargetPosition = target.position;
            hasValidTarget = true;

            // Начальный путь к цели с использованием PathBerserker2d
            if (navAgent.HasValidPosition)
            {
                navAgent.PathTo(target.position);
            }
        }
    }

    /// <summary>
    /// Подписка на события NavAgent
    /// </summary>
    private void SubscribeToNavAgentEvents()
    {
        navAgent.OnReachedGoal += OnReachedGoal;
        navAgent.OnFailedToFindPath += OnFailedToFindPath;
    }

    private void OnDestroy()
    {
        UnsubscribeFromNavAgentEvents();
    }

    /// <summary>
    /// Отписка от событий NavAgent
    /// </summary>
    private void UnsubscribeFromNavAgentEvents()
    {
        if (navAgent != null)
        {
            navAgent.OnReachedGoal -= OnReachedGoal;
            navAgent.OnFailedToFindPath -= OnFailedToFindPath;
        }
    }

    /// <summary>
    /// Обработчик достижения цели
    /// </summary>
    private void OnReachedGoal(NavAgent agent)
    {
        ResetAvoidanceState();
        consecutivePathFailures = 0;
    }

    /// <summary>
    /// Обработчик неудачи поиска пути
    /// </summary>
    private void OnFailedToFindPath(NavAgent agent)
    {
        consecutivePathFailures++;

        // При множественных неудачах сбрасываем состояние избегания
        if (consecutivePathFailures >= 3)
        {
            ResetAvoidanceState();
            TryAlternativePathfinding();
        }
    }

    /// <summary>
    /// Сброс состояния избегания
    /// </summary>
    private void ResetAvoidanceState()
    {
        isAvoidingAlly = false;
        isAvoidingEnemy = false;
        avoidanceTimer = 0f;
        currentObstacle = null;
    }

    /// <summary>
    /// Попытка альтернативного поиска пути при неудачах
    /// </summary>
    private void TryAlternativePathfinding()
    {
        if (!hasValidTarget) return;

        // Попробуем путь к последней валидной позиции цели
        if (navAgent.HasValidPosition)
        {
            navAgent.PathTo(lastValidTargetPosition);
        }
    }

    private void Update()
    {
        if (!IsValidForUpdate()) return;

        UpdateTimers();
        HandleAvoidanceTimeout();

        // Основные обновления с оптимизированными интервалами
        if (ShouldUpdatePath())
        {
            UpdatePathToTarget();
        }

        if (ShouldCheckForAllies())
        {
            CheckForBlockingAllies();
        }

        if (ShouldCheckForEnemies())
        {
            CheckForNearbyEnemies();
        }

        // Проверка на слишком близких союзников когда агент простаивает
        if (navAgent.IsIdle && !IsInAvoidanceMode())
        {
            HandleIdleProximityCheck();
        }
    }

    /// <summary>
    /// Проверка валидности для обновления
    /// </summary>
    private bool IsValidForUpdate()
    {
        return target != null && navAgent != null && navAgent.enabled && pathSettings != null;
    }

    /// <summary>
    /// Обновление таймеров
    /// </summary>
    private void UpdateTimers()
    {
        pathUpdateTimer += Time.deltaTime;
        allyCheckTimer += Time.deltaTime;
        enemyCheckTimer += Time.deltaTime;

        if (IsInAvoidanceMode())
        {
            avoidanceTimer += Time.deltaTime;
        }
    }

    /// <summary>
    /// Обработка таймаута избегания
    /// </summary>
    private void HandleAvoidanceTimeout()
    {
        if (IsInAvoidanceMode() && avoidanceTimer > avoidanceSettings.maxAvoidanceTime)
        {
            ResetAvoidanceState();
            TryDirectPathToTarget();
        }
    }

    /// <summary>
    /// Проверка необходимости обновления пути
    /// </summary>
    private bool ShouldUpdatePath()
    {
        return pathUpdateTimer >= pathSettings.updateFrequency;
    }

    /// <summary>
    /// Проверка необходимости проверки союзников
    /// </summary>
    private bool ShouldCheckForAllies()
    {
        return allyCheckTimer >= avoidanceSettings.pathRecalculationInterval
               && navAgent.IsFollowingAPath
               && !IsInAvoidanceMode();
    }

    /// <summary>
    /// Проверка необходимости проверки врагов
    /// </summary>
    private bool ShouldCheckForEnemies()
    {
        return avoidanceSettings.enableEnemyAvoidance
               && enemyCheckTimer >= avoidanceSettings.enemyCheckInterval
               && !isAvoidingEnemy;
    }

    /// <summary>
    /// Проверка режима избегания
    /// </summary>
    private bool IsInAvoidanceMode()
    {
        return isAvoidingAlly || isAvoidingEnemy;
    }

    /// <summary>
    /// Обновление пути к цели с улучшенной логикой
    /// </summary>
    private void UpdatePathToTarget()
    {
        pathUpdateTimer = 0f;

        if (!hasValidTarget) return;

        Vector2 currentTargetPos = target.position;
        Vector2 myPosition = transform.position;
        float distanceToTarget = Vector2.Distance(myPosition, currentTargetPos);

        // Предсказание движения цели
        Vector2 predictedTargetPos = CalculatePredictedTargetPosition(currentTargetPos);

        // Обновляем отслеживание позиции цели
        UpdateTargetPositionTracking(currentTargetPos);

        // Логика дистанции и остановки
        if (HandleProximityLogic(distanceToTarget, predictedTargetPos))
        {
            return; // Обработка завершена в методе
        }

        // Обновление пути если не в режиме избегания
        if (!IsInAvoidanceMode() && distanceToTarget > pathSettings.closeEnoughRadius)
        {
            RequestPathToTarget(predictedTargetPos);
        }
    }

    /// <summary>
    /// Расчет предсказанной позиции цели
    /// </summary>
    private Vector2 CalculatePredictedTargetPosition(Vector2 currentPos)
    {
        if (pathSettings.targetPredictionTime <= 0f) return currentPos;

        Vector2 targetVelocity = (currentPos - lastTargetPosition) / pathSettings.updateFrequency;

        // Применяем предсказание только если цель движется достаточно быстро
        if (targetVelocity.magnitude > 0.1f)
        {
            return currentPos + targetVelocity * pathSettings.targetPredictionTime;
        }

        return currentPos;
    }

    /// <summary>
    /// Обновление отслеживания позиции цели
    /// </summary>
    private void UpdateTargetPositionTracking(Vector2 currentPos)
    {
        lastTargetPosition = currentPos;

        // Обновляем последнюю валидную позицию если агент может туда добраться
        if (navAgent.HasValidPosition)
        {
            lastValidTargetPosition = currentPos;
        }
    }

    /// <summary>
    /// Обработка логики близости к цели
    /// </summary>
    private bool HandleProximityLogic(float distance, Vector2 targetPos)
    {
        // Слишком близко - останавливаемся или отступаем
        if (distance < pathSettings.travelStopRadius)
        {
            navAgent.Stop();

            // Активное отступление при критической близости
            if (distance < pathSettings.travelStopRadius * 0.7f)
            {
                ExecuteRetreatFromTarget(targetPos);
            }
            return true;
        }

        // Зона комфорта - не обновляем путь
        if (distance >= pathSettings.travelStopRadius && distance <= pathSettings.closeEnoughRadius)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Выполнение отступления от цели
    /// </summary>
    private void ExecuteRetreatFromTarget(Vector2 targetPos)
    {
        Vector2 retreatDirection = ((Vector2)transform.position - targetPos).normalized;
        Vector2 retreatPoint = targetPos + retreatDirection * (pathSettings.travelStopRadius * 1.2f);

        if (navAgent.HasValidPosition)
        {
            navAgent.PathTo(retreatPoint);
        }
    }

    /// <summary>
    /// Запрос пути к цели через PathBerserker2d
    /// </summary>
    private void RequestPathToTarget(Vector2 targetPos)
    {
        if (navAgent.HasValidPosition)
        {
            navAgent.UpdatePath(targetPos);
        }
    }

    /// <summary>
    /// Попытка прямого пути к цели
    /// </summary>
    private void TryDirectPathToTarget()
    {
        if (hasValidTarget && navAgent.HasValidPosition)
        {
            navAgent.UpdatePath(target.position);
        }
    }

    /// <summary>
    /// Проверка блокирующих союзников
    /// </summary>
    private void CheckForBlockingAllies()
    {
        allyCheckTimer = 0f;

        if (!hasValidTarget || teammates == null) return;

        GameObject blockingAlly = FindBlockingTeammate();

        if (blockingAlly != null)
        {
            InitiateAllyAvoidance(blockingAlly);
        }
    }

    /// <summary>
    /// Инициация избегания союзника
    /// </summary>
    private void InitiateAllyAvoidance(GameObject ally)
    {
        currentObstacle = ally;
        avoidanceDestination = CalculateAvoidancePoint(ally.transform.position, false);

        isAvoidingAlly = true;
        avoidanceTimer = 0f;

        if (navAgent.HasValidPosition)
        {
            navAgent.UpdatePath(avoidanceDestination);
        }
    }

    /// <summary>
    /// Проверка ближайших врагов для избегания
    /// </summary>
    private void CheckForNearbyEnemies()
    {
        enemyCheckTimer = 0f;

        if (!avoidanceSettings.enableEnemyAvoidance || enemies == null) return;

        GameObject nearestEnemy = FindNearestThreat();

        if (nearestEnemy != null)
        {
            InitiateEnemyAvoidance(nearestEnemy);
        }
    }

    /// <summary>
    /// Поиск ближайшей угрозы среди врагов
    /// </summary>
    private GameObject FindNearestThreat()
    {
        Vector2 myPosition = transform.position;
        GameObject nearestEnemy = null;
        float nearestDistance = avoidanceSettings.enemyDetectionDistance;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector2.Distance(myPosition, enemy.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// Инициация избегания врага
    /// </summary>
    private void InitiateEnemyAvoidance(GameObject enemy)
    {
        currentObstacle = enemy;
        avoidanceDestination = CalculateAvoidancePoint(enemy.transform.position, true);

        isAvoidingEnemy = true;
        avoidanceTimer = 0f;

        if (navAgent.HasValidPosition)
        {
            navAgent.UpdatePath(avoidanceDestination);
        }
    }

    /// <summary>
    /// Поиск блокирующего союзника
    /// </summary>
    private GameObject FindBlockingTeammate()
    {
        if (teammates == null || teammates.Count == 0 || !hasValidTarget)
            return null;

        Vector2 myPosition = transform.position;
        Vector2 goalPosition = target.position;
        Vector2 pathDirection = (goalPosition - myPosition).normalized;

        foreach (GameObject ally in teammates)
        {
            if (ally == null || ally == gameObject) continue;

            if (IsAllyBlockingPath(ally, myPosition, pathDirection))
            {
                return ally;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверка блокирует ли союзник путь
    /// </summary>
    private bool IsAllyBlockingPath(GameObject ally, Vector2 myPosition, Vector2 pathDirection)
    {
        Vector2 allyPosition = ally.transform.position;
        float distanceToAlly = Vector2.Distance(myPosition, allyPosition);

        // Союзник слишком далеко
        if (distanceToAlly > avoidanceSettings.allyDetectionDistance * 2f)
            return false;

        // Проекция союзника на путь
        float projection = Vector2.Dot(allyPosition - myPosition, pathDirection);

        // Союзник позади нас
        if (projection <= 0) return false;

        // Расстояние от союзника до линии пути
        Vector2 projectedPoint = myPosition + pathDirection * projection;
        float distanceFromPath = Vector2.Distance(allyPosition, projectedPoint);

        return distanceFromPath < avoidanceSettings.allyDetectionDistance;
    }

    /// <summary>
    /// Расчет точки обхода препятствия
    /// </summary>
    private Vector2 CalculateAvoidancePoint(Vector2 obstaclePosition, bool isEnemy)
    {
        if (!hasValidTarget) return transform.position;

        Vector2 myPosition = transform.position;
        Vector2 targetPosition = target.position;
        Vector2 directionToTarget = (targetPosition - myPosition).normalized;

        // Выбираем дистанцию избегания в зависимости от типа препятствия
        float avoidanceDistance = isEnemy ?
            avoidanceSettings.enemyAvoidanceDistance :
            avoidanceSettings.avoidanceOffset;

        // Перпендикулярное направление для обхода
        Vector2 perpendicularDirection = new Vector2(-directionToTarget.y, directionToTarget.x);

        // Определяем сторону обхода
        Vector2 toObstacle = obstaclePosition - myPosition;
        if (Vector2.Dot(toObstacle, perpendicularDirection) < 0)
        {
            perpendicularDirection = -perpendicularDirection;
        }

        // Комбинируем направление к цели и перпендикулярное направление
        Vector2 avoidanceDirection = (directionToTarget + perpendicularDirection).normalized;

        // Рассчитываем точку обхода
        Vector2 avoidancePoint = obstaclePosition + avoidanceDirection * avoidanceDistance;

        // Для врагов добавляем дополнительное смещение от цели
        if (isEnemy)
        {
            Vector2 awayFromTarget = (myPosition - targetPosition).normalized;
            avoidancePoint += awayFromTarget * (avoidanceDistance * 0.3f);
        }

        return avoidancePoint;
    }

    /// <summary>
    /// Обработка проверки близости при простое агента
    /// </summary>
    private void HandleIdleProximityCheck()
    {
        GameObject tooCloseAlly = FindTooCloseAlly();

        if (tooCloseAlly != null)
        {
            ExecuteProximityRetreat(tooCloseAlly);
        }
    }

    /// <summary>
    /// Поиск слишком близкого союзника
    /// </summary>
    private GameObject FindTooCloseAlly()
    {
        if (teammates == null || teammates.Count == 0) return null;

        Vector2 myPosition = transform.position;
        GameObject closestAlly = null;
        float closestDistance = avoidanceSettings.allyDetectionDistance * 0.7f;

        foreach (GameObject ally in teammates)
        {
            if (ally == null || ally == gameObject) continue;

            float distance = Vector2.Distance(myPosition, ally.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAlly = ally;
            }
        }

        return closestAlly;
    }

    /// <summary>
    /// Выполнение отступления от близкого союзника
    /// </summary>
    private void ExecuteProximityRetreat(GameObject ally)
    {
        Vector2 myPosition = transform.position;
        Vector2 allyPosition = ally.transform.position;
        Vector2 retreatDirection = (myPosition - allyPosition).normalized;

        // Обработка случая нулевого направления
        if (retreatDirection.magnitude < 0.1f)
        {
            retreatDirection = new Vector2(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;
        }

        Vector2 retreatPoint = myPosition + retreatDirection * (avoidanceSettings.allyDetectionDistance * 1.5f);

        if (navAgent.HasValidPosition)
        {
            navAgent.PathTo(retreatPoint);
        }
    }

    /// <summary>
    /// Обновление настроек из BotManager (вызывается BotManager при создании бота)
    /// </summary>
    public void UpdateSettingsFromBotManager()
    {
        InitializeFromBotManager();
        CacheTeamLists();
    }

    /// <summary>
    /// Устаревшие методы настройки для обратной совместимости
    /// </summary>
    [System.Obsolete("Use BotManager configuration instead")]
    public void SetFollowingParameters(float closeEnough, float stopRadius, float updateRate, float predictionTime)
    {
        // Метод оставлен для обратной совместимости, но настройки теперь берутся из BotManager
    }

    [System.Obsolete("Use BotManager configuration instead")]
    public void SetAvoidanceParameters(float detection, float offset, float interval, float maxAvoidTime)
    {
        // Метод оставлен для обратной совместимости, но настройки теперь берутся из BotManager
    }

    /// <summary>
    /// Визуализация для отладки
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || avoidanceSettings == null || pathSettings == null) return;

        DrawDetectionRadii();
        DrawTargetInfo();
        DrawAvoidanceInfo();
        DrawPathInfo();
    }

    /// <summary>
    /// Отрисовка радиусов обнаружения
    /// </summary>
    private void DrawDetectionRadii()
    {
        Vector3 position = transform.position;

        // Радиус обнаружения союзников
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, avoidanceSettings.allyDetectionDistance);

        // Радиус обнаружения врагов
        if (avoidanceSettings.enableEnemyAvoidance)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, avoidanceSettings.enemyDetectionDistance);
        }
    }

    /// <summary>
    /// Отрисовка информации о цели
    /// </summary>
    private void DrawTargetInfo()
    {
        if (!hasValidTarget) return;

        Vector3 targetPos = target.position;

        // Дистанция остановки
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, pathSettings.travelStopRadius);

        // Дистанция "достаточно близко"
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPos, pathSettings.closeEnoughRadius);

        // Линия к цели
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, targetPos);
    }

    /// <summary>
    /// Отрисовка информации об избегании
    /// </summary>
    private void DrawAvoidanceInfo()
    {
        if (!IsInAvoidanceMode() || currentObstacle == null) return;

        // Линия к препятствию
        Gizmos.color = isAvoidingEnemy ? Color.red : Color.gray;
        Gizmos.DrawLine(transform.position, currentObstacle.transform.position);

        // Точка обхода
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(avoidanceDestination, 0.3f);
        Gizmos.DrawLine(transform.position, avoidanceDestination);
    }

    /// <summary>
    /// Отрисовка информации о пути
    /// </summary>
    private void DrawPathInfo()
    {
        if (navAgent == null || !navAgent.PathGoal.HasValue) return;

        // Текущая цель пути
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(navAgent.PathGoal.Value, 0.2f);
    }
}