using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathBerserker2d;

namespace BotSystem
{
    /// <summary>
    /// Простой менеджер ботов для создания двух команд и назначения целей.
    /// Использует стандартные компоненты PathBerserker2d: TransformBasedMovement, NavAgent, Follower
    /// </summary>
    public class SimpleBotManager : MonoBehaviour
    {
        [System.Serializable]
        public class TeamConfiguration
        {
            [Header("Настройки команды")]
            public Transform[] spawnPoints = new Transform[4];
            public Color teamColor = Color.white;

        }

        [Header("Конфигурация ботов")]
        [SerializeField] private GameObject botPrefab;
        [Tooltip("Количество ботов в каждой команде")]
        [SerializeField] private int botsPerTeam = 4;
        [Tooltip("Использовать AvoidanceFollower вместо стандартного Follower")]
        [SerializeField] private bool useAvoidanceFollower = true;

        [Header("Команды")]
        [SerializeField] private TeamConfiguration team1;
        [SerializeField] private TeamConfiguration team2;

        [Header("Общие настройки следования")]
        [Tooltip("Радиус, при котором бот начинает движение к цели")]
        [SerializeField] private float closeEnoughRadius = 3.0f;
        [Tooltip("Радиус, при котором бот останавливается")]
        [SerializeField] private float travelStopRadius = 1.0f;
        [Tooltip("Время предсказания позиции цели")]
        [SerializeField] private float targetPredictionTime = 0.1f;

        [Header("Общие настройки избегания NavTag")]
        [Tooltip("Радиус зоны избегания NavTag")]
        [SerializeField] private float navTagAvoidanceRadius = 2.0f;
        [Tooltip("Высота зоны избегания NavTag")]
        [SerializeField] private float navTagAvoidanceHeight = 1.0f;
        [Tooltip("Стоимость прохождения через зону врага")]
        [SerializeField] private float enemyAvoidanceCost = 1000f;
        [Tooltip("Стоимость прохождения через зону союзника")]
        [SerializeField] private float allyAvoidanceCost = 100f;
        [Tooltip("Интервал обновления пути")]
        [SerializeField] private float pathUpdateInterval = 0.3f;

        [Header("Контроль расстояния")]
        [Tooltip("Минимальное расстояние до цели")]
        [SerializeField] private float minDistanceThreshold = 2.0f;
        [Tooltip("Максимальное расстояние до цели")]
        [SerializeField] private float maxDistanceThreshold = 5.0f;
        [Tooltip("Оптимальное расстояние до цели")]
        [SerializeField] private float optimalDistance = 3.5f;

        [Header("Проверка видимости")]
        [Tooltip("Максимальная дистанция проверки видимости")]
        [SerializeField] private float lineOfSightDistance = 10.0f;
        [Tooltip("Слои препятствий (стены, другие боты)")]
        [SerializeField] private LayerMask obstacleLayerMask = -1;
        [Tooltip("Интервал проверки видимости")]
        [SerializeField] private float lineOfSightCheckInterval = 0.2f;

        // Списки ботов по командам
        private List<GameObject> team1Bots = new List<GameObject>();
        private List<GameObject> team2Bots = new List<GameObject>();

        // Синглтон для удобного доступа
        public static SimpleBotManager Instance { get; private set; }

        private void Awake()
        {
            // Простая реализация синглтона
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Проверяем корректность настроек
            if (!ValidateConfiguration())
            {
                Debug.LogError("SimpleBotManager: Некорректная конфигурация! Проверьте настройки.");
                return;
            }

            // Создаем команды и назначаем цели
            SpawnBothTeams();
            AssignTargetsToAllBots();
        }

        /// <summary>
        /// Проверка корректности конфигурации
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (botPrefab == null)
            {
                Debug.LogError("SimpleBotManager: Не назначен префаб бота!");
                return false;
            }

            if (team1.spawnPoints.Length < botsPerTeam)
            {
                Debug.LogError($"SimpleBotManager: Недостаточно точек спавна для команды 1. Нужно: {botsPerTeam}, есть: {team1.spawnPoints.Length}");
                return false;
            }

            if (team2.spawnPoints.Length < botsPerTeam)
            {
                Debug.LogError($"SimpleBotManager: Недостаточно точек спавна для команды 2. Нужно: {botsPerTeam}, есть: {team2.spawnPoints.Length}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Создание обеих команд
        /// </summary>
        private void SpawnBothTeams()
        {
            Debug.Log("SimpleBotManager: Создание команд...");
            
            SpawnTeam(team1, team1Bots, "Team1");
            SpawnTeam(team2, team2Bots, "Team2");
            
            Debug.Log($"SimpleBotManager: Создано {team1Bots.Count} ботов в команде 1 и {team2Bots.Count} ботов в команде 2");
        }

        /// <summary>
        /// Создание одной команды
        /// </summary>
        private void SpawnTeam(TeamConfiguration teamConfig, List<GameObject> teamBots, string teamName)
        {
            for (int i = 0; i < botsPerTeam; i++)
            {
                // Создаем бота в точке спавна
                Vector3 spawnPosition = teamConfig.spawnPoints[i].position;
                GameObject bot = Instantiate(botPrefab, spawnPosition, Quaternion.identity, transform);
                bot.name = $"{teamName}_Bot_{i + 1}";

                // Настраиваем цвет команды
                SetBotTeamColor(bot, teamConfig.teamColor);

                // Настраиваем компоненты бота
                ConfigureBotComponents(bot, teamConfig);

                // Добавляем в список команды
                teamBots.Add(bot);

                Debug.Log($"SimpleBotManager: Создан бот {bot.name} в позиции {spawnPosition}");
            }
        }

        /// <summary>
        /// Установка цвета команды для бота
        /// </summary>
        private void SetBotTeamColor(GameObject bot, Color teamColor)
        {
            SpriteRenderer[] renderers = bot.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.color = teamColor;
            }
        }

        /// <summary>
        /// Настройка компонентов бота
        /// </summary>
        private void ConfigureBotComponents(GameObject bot, TeamConfiguration teamConfig)
        {
            // Проверяем наличие необходимых компонентов
            NavAgent navAgent = bot.GetComponent<NavAgent>();
            if (navAgent == null)
            {
                Debug.LogError($"SimpleBotManager: У префаба {bot.name} отсутствует компонент NavAgent!");
                return;
            }

            TransformBasedMovement movement = bot.GetComponent<TransformBasedMovement>();
            if (movement == null)
            {
                Debug.LogError($"SimpleBotManager: У префаба {bot.name} отсутствует компонент TransformBasedMovement!");
                return;
            }

            if (useAvoidanceFollower)
            {
                // Используем AvoidanceFollower с NavTag системой
                ConfigureAvoidanceFollower(bot, navAgent);
            }
            else
            {
                // Используем стандартный Follower
                Follower follower = bot.GetComponent<Follower>();
                if (follower == null)
                {
                    Debug.LogError($"SimpleBotManager: У префаба {bot.name} отсутствует компонент Follower!");
                    return;
                }

                // Настраиваем параметры следования (общие для всех команд)
                follower.closeEnoughRadius = closeEnoughRadius;
                follower.travelStopRadius = travelStopRadius;
                follower.targetPredictionTime = targetPredictionTime;

                // Убеждаемся, что NavAgent правильно связан
                if (follower.navAgent == null)
                {
                    follower.navAgent = navAgent;
                }
            }
        }

        /// <summary>
        /// Настройка AvoidanceFollower с NavTag системой избегания
        /// </summary>
        private void ConfigureAvoidanceFollower(GameObject bot, NavAgent navAgent)
        {
            // Удаляем стандартный Follower, если есть
            Follower oldFollower = bot.GetComponent<Follower>();
            if (oldFollower != null)
            {
                DestroyImmediate(oldFollower);
            }

            // Добавляем AvoidanceFollower
            AvoidanceFollower avoidanceFollower = bot.GetComponent<AvoidanceFollower>();
            if (avoidanceFollower == null)
            {
                avoidanceFollower = bot.AddComponent<AvoidanceFollower>();
            }

            // Настраиваем параметры следования
            avoidanceFollower.ConfigureFollowing(
                closeEnoughRadius,
                travelStopRadius,
                targetPredictionTime,
                pathUpdateInterval
            );

            // Настраиваем параметры избегания
            avoidanceFollower.ConfigureAvoidance(
                navTagAvoidanceRadius,
                navTagAvoidanceHeight,
                0.1f, // updateThreshold
                0.1f  // updateInterval
            );

            // Настраиваем параметры контроля расстояния
            avoidanceFollower.ConfigureDistanceControl(
                minDistanceThreshold,
                maxDistanceThreshold,
                optimalDistance
            );

            // Настраиваем параметры проверки видимости
            avoidanceFollower.ConfigureLineOfSight(
                lineOfSightDistance,
                obstacleLayerMask,
                lineOfSightCheckInterval
            );

            // Настраиваем NavTag стоимости для избегания
            ConfigureNavTagCosts(navAgent);

            // Настраиваем NavTag маркера в зависимости от команды
            ConfigureTeamBasedNavTag(avoidanceFollower);
        }

        /// <summary>
        /// Настройка стоимости NavTag для агента
        /// </summary>
        private void ConfigureNavTagCosts(NavAgent navAgent)
        {
            // Получаем текущие множители или создаем новые
            var multipliers = navAgent.GetType()
                .GetField("navTagTraversalCostMultipliers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(navAgent) as float[];

            if (multipliers == null || multipliers.Length < 3)
            {
                multipliers = new float[3];
                multipliers[0] = 1.0f; // default
                multipliers[1] = enemyAvoidanceCost; // enemy zones
                multipliers[2] = allyAvoidanceCost;  // ally zones

                // Устанавливаем множители обратно
                navAgent.GetType()
                    .GetField("navTagTraversalCostMultipliers",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(navAgent, multipliers);
            }
            else
            {
                multipliers[1] = enemyAvoidanceCost;
                multipliers[2] = allyAvoidanceCost;
            }
        }

        /// <summary>
        /// Настройка NavTag маркера в зависимости от команды
        /// </summary>
        private void ConfigureTeamBasedNavTag(AvoidanceFollower avoidanceFollower)
        {
            // Пока используем простую логику - все боты помечаются как враги
            // В будущем можно добавить более сложную логику:
            // - ENEMY_TAG для врагов
            // - ALLY_TAG для союзников
            // - Динамическое переключение в зависимости от ситуации

            avoidanceFollower.SetNavTag(1); // ENEMY_TAG - все боты избегают друг друга
        }

        /// <summary>
        /// Назначение целей всем ботам
        /// </summary>
        private void AssignTargetsToAllBots()
        {
            Debug.Log("SimpleBotManager: Назначение целей...");
            
            // Команде 1 назначаем цели из команды 2
            AssignTargetsToTeam(team1Bots, team2Bots, "Team1", "Team2");
            
            // Команде 2 назначаем цели из команды 1
            AssignTargetsToTeam(team2Bots, team1Bots, "Team2", "Team1");
        }

        /// <summary>
        /// Назначение целей одной команде без повторов
        /// </summary>
        private void AssignTargetsToTeam(List<GameObject> attackingTeam, List<GameObject> targetTeam, string attackingTeamName, string targetTeamName)
        {
            if (targetTeam.Count == 0)
            {
                Debug.LogWarning($"SimpleBotManager: Нет доступных целей для команды {attackingTeamName}");
                return;
            }

            // Создаем копию списка целей для перемешивания
            List<GameObject> availableTargets = new List<GameObject>(targetTeam);

            // Перемешиваем список целей для случайного распределения
            ShuffleList(availableTargets);

            // Определяем максимальное количество ботов, которые могут получить цели
            int maxAssignments = Mathf.Min(attackingTeam.Count, availableTargets.Count);

            // Назначаем цели без повторов
            for (int i = 0; i < attackingTeam.Count; i++)
            {
                GameObject bot = attackingTeam[i];

                if (i < maxAssignments)
                {
                    // Назначаем уникальную цель
                    GameObject target = availableTargets[i];

                    // Назначаем цель в зависимости от системы избегания
                    AssignTargetToBot(bot, target, attackingTeamName, targetTeamName);
                }
                else
                {
                    // Если целей не хватает, оставляем бота без цели
                    AssignTargetToBot(bot, null, attackingTeamName, targetTeamName);
                    Debug.LogWarning($"SimpleBotManager: Бот {bot.name} ({attackingTeamName}) остался без цели - недостаточно целей в команде {targetTeamName}");
                }
            }

            Debug.Log($"SimpleBotManager: Команде {attackingTeamName} назначено {maxAssignments} уникальных целей из {attackingTeam.Count} ботов");
        }

        /// <summary>
        /// Назначение цели конкретному боту
        /// </summary>
        private void AssignTargetToBot(GameObject bot, GameObject target, string attackingTeamName, string targetTeamName)
        {
            Transform targetTransform = target?.transform;

            if (useAvoidanceFollower)
            {
                AvoidanceFollower avoidanceFollower = bot.GetComponent<AvoidanceFollower>();
                if (avoidanceFollower != null)
                {
                    avoidanceFollower.target = targetTransform;

                    // Маркер избегания уже настроен в ConfigureAvoidanceFollower

                    Debug.Log($"SimpleBotManager: Боту {bot.name} ({attackingTeamName}) назначена цель {target?.name ?? "null"} ({targetTeamName}) [AvoidanceFollower + NavTag]");
                }
                else
                {
                    Debug.LogError($"SimpleBotManager: У бота {bot.name} отсутствует компонент AvoidanceFollower!");
                }
            }
            else
            {
                Follower follower = bot.GetComponent<Follower>();
                if (follower != null)
                {
                    follower.target = targetTransform;
                    Debug.Log($"SimpleBotManager: Боту {bot.name} ({attackingTeamName}) назначена цель {target?.name ?? "null"} ({targetTeamName}) [Стандартный Follower]");
                }
                else
                {
                    Debug.LogError($"SimpleBotManager: У бота {bot.name} отсутствует компонент Follower!");
                }
            }
        }



        /// <summary>
        /// Перемешивание списка (алгоритм Фишера-Йейтса)
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Переназначение целей (можно вызывать во время игры)
        /// </summary>
        public void ReassignAllTargets()
        {
            Debug.Log("SimpleBotManager: Переназначение всех целей...");
            AssignTargetsToAllBots();
        }

        /// <summary>
        /// Получение списка ботов команды
        /// </summary>
        public List<GameObject> GetTeamBots(int teamNumber)
        {
            switch (teamNumber)
            {
                case 1: return new List<GameObject>(team1Bots);
                case 2: return new List<GameObject>(team2Bots);
                default: 
                    Debug.LogWarning($"SimpleBotManager: Неизвестный номер команды: {teamNumber}");
                    return new List<GameObject>();
            }
        }

        /// <summary>
        /// Определение номера команды бота
        /// </summary>
        public int GetBotTeamNumber(GameObject bot)
        {
            if (team1Bots.Contains(bot)) return 1;
            if (team2Bots.Contains(bot)) return 2;
            return 0; // Бот не принадлежит ни к одной команде
        }

        /// <summary>
        /// Удаление бота из списков (при уничтожении)
        /// </summary>
        public void RemoveBot(GameObject bot)
        {
            if (team1Bots.Remove(bot))
            {
                Debug.Log($"SimpleBotManager: Бот {bot.name} удален из команды 1");
            }
            else if (team2Bots.Remove(bot))
            {
                Debug.Log($"SimpleBotManager: Бот {bot.name} удален из команды 2");
            }
        }

        /// <summary>
        /// Получение общих настроек следования
        /// </summary>
        public (float closeEnough, float stopRadius, float predictionTime) GetFollowingSettings()
        {
            return (closeEnoughRadius, travelStopRadius, targetPredictionTime);
        }

        /// <summary>
        /// Получение общих настроек NavTag избегания
        /// </summary>
        public (float avoidanceRadius, float avoidanceHeight, float enemyCost, float allyCost, float updateInterval) GetNavTagAvoidanceSettings()
        {
            return (navTagAvoidanceRadius, navTagAvoidanceHeight, enemyAvoidanceCost, allyAvoidanceCost, pathUpdateInterval);
        }

        /// <summary>
        /// Получение настроек контроля расстояния
        /// </summary>
        public (float minDistance, float maxDistance, float optimal) GetDistanceControlSettings()
        {
            return (minDistanceThreshold, maxDistanceThreshold, optimalDistance);
        }

        /// <summary>
        /// Получение настроек проверки видимости
        /// </summary>
        public (float maxDistance, LayerMask obstacles, float checkInterval) GetLineOfSightSettings()
        {
            return (lineOfSightDistance, obstacleLayerMask, lineOfSightCheckInterval);
        }

        /// <summary>
        /// Отладочная информация
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Рисуем связи между ботами и их целями
            DrawTargetConnections(team1Bots, Color.red);
            DrawTargetConnections(team2Bots, Color.blue);
        }

        private void DrawTargetConnections(List<GameObject> bots, Color color)
        {
            Gizmos.color = color;
            foreach (GameObject bot in bots)
            {
                if (bot == null) continue;

                Transform targetTransform = null;

                if (useAvoidanceFollower)
                {
                    AvoidanceFollower avoidanceFollower = bot.GetComponent<AvoidanceFollower>();
                    if (avoidanceFollower != null)
                    {
                        targetTransform = avoidanceFollower.target;
                    }
                }
                else
                {
                    Follower follower = bot.GetComponent<Follower>();
                    if (follower != null)
                    {
                        targetTransform = follower.target;
                    }
                }

                if (targetTransform != null)
                {
                    Gizmos.DrawLine(bot.transform.position, targetTransform.position);
                }
            }
        }
    }
}
