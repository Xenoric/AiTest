using System.Collections.Generic;
using UnityEngine;
using PathBerserker2d;

public class BotManager : MonoBehaviour
{
    [System.Serializable]
    public class PathFollowingSettings
    {
        [Header("Path Following")]
        public float closeEnoughRadius = 3.0f;
        public float travelStopRadius = 1.0f;
        public float updateFrequency = 0.3f;
        public float targetPredictionTime = 0.1f;
    }

    [System.Serializable]
    public class AvoidanceSettings
    {
        [Header("Ally Avoidance")]
        public float allyDetectionDistance = 1.5f;
        public float pathRecalculationInterval = 0.5f;
        public float avoidanceOffset = 2.0f;
        public float maxAvoidanceTime = 3.0f;
    }

    [System.Serializable]
    public class TeamSettings
    {
        public Transform[] spawnPoints;
        public int botCount = 4;
        public Color teamColor = Color.white;
        
        [Header("Индивидуальные настройки команды (если используются)")]
        public bool useTeamSpecificSettings = false;
        [Tooltip("Настройки пути для этой команды (если useTeamSpecificSettings = true)")]
        public PathFollowingSettings pathSettings = new PathFollowingSettings();
        [Tooltip("Настройки избегания для этой команды (если useTeamSpecificSettings = true)")]
        public AvoidanceSettings avoidanceSettings = new AvoidanceSettings();
    }
    
    // Синглтон для доступа к BotManager
    public static BotManager Instance { get; private set; }

    [Header("Bot Configuration")]
    [SerializeField] private GameObject botPrefab; // Общий префаб для всех ботов
    
    [Header("Team Configuration")]
    [SerializeField] private TeamSettings team1;
    [SerializeField] private TeamSettings team2;
    
    [Header("Общие настройки для обеих команд")]
    [SerializeField] private PathFollowingSettings commonPathSettings = new PathFollowingSettings();
    [SerializeField] private AvoidanceSettings commonAvoidanceSettings = new AvoidanceSettings();
    
    // Списки ботов по командам
    private List<GameObject> team1Bots = new List<GameObject>();
    private List<GameObject> team2Bots = new List<GameObject>();
    
    private void Awake()
    {
        // Простая реализация синглтона
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        SpawnTeams();
        AssignTargets();
    }
    
    private void SpawnTeams()
    {
        // Спавним обе команды с помощью общего метода
        SpawnTeam(team1, team1Bots, 1); // Команда 1
        SpawnTeam(team2, team2Bots, 2); // Команда 2
    }
    
    // Общий метод для спавна команды
    private void SpawnTeam(TeamSettings teamSettings, List<GameObject> teamBots, int teamID)
    {
        if (teamSettings.spawnPoints.Length == 0) return;
        
        // Создаем список доступных точек спавна для последовательного использования
        List<Transform> availableSpawnPoints = new List<Transform>(teamSettings.spawnPoints);
        
        // Определяем, какие настройки использовать для этой команды
        PathFollowingSettings pathSettings = teamSettings.useTeamSpecificSettings 
            ? teamSettings.pathSettings 
            : commonPathSettings;
            
        AvoidanceSettings avoidanceSettings = teamSettings.useTeamSpecificSettings 
            ? teamSettings.avoidanceSettings 
            : commonAvoidanceSettings;
        
        for (int i = 0; i < teamSettings.botCount; i++)
        {
            // Если все точки спавна использованы, но нужно спавнить ещё ботов,
            // восстанавливаем список доступных точек
            if (availableSpawnPoints.Count == 0)
            {
                availableSpawnPoints = new List<Transform>(teamSettings.spawnPoints);
            }
            
            // Выбираем точку спавна из доступных
            int spawnPointIndex = Random.Range(0, availableSpawnPoints.Count);
            Transform spawnPoint = availableSpawnPoints[spawnPointIndex];
            
            // Удаляем использованную точку из списка доступных
            availableSpawnPoints.RemoveAt(spawnPointIndex);
            
            // Создание бота
            GameObject bot = Instantiate(botPrefab, spawnPoint.position, Quaternion.identity, transform);
            bot.name = "Bot_Team" + teamID + "_" + (i+1);
            
            // Получаем все SpriteRenderer в иерархии объекта
            SpriteRenderer[] renderers = bot.GetComponentsInChildren<SpriteRenderer>();
            
            // Устанавливаем цвет для всех спрайтов
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.color = teamSettings.teamColor;
            }
            
            // Удаляем стандартный Follower, если он есть
            Follower oldFollower = bot.GetComponent<Follower>();
            if (oldFollower != null)
            {
                Destroy(oldFollower);
            }
           
            // Добавляем наш улучшенный фоловер с избеганием
            CustomFollower follower = bot.AddComponent<CustomFollower>();
            
            // Настраиваем параметры следования
            follower.SetFollowingParameters(
                pathSettings.closeEnoughRadius,
                pathSettings.travelStopRadius, 
                pathSettings.updateFrequency,
                pathSettings.targetPredictionTime
            );
            
            // Настраиваем параметры избегания
            follower.SetAvoidanceParameters(
                avoidanceSettings.allyDetectionDistance,
                avoidanceSettings.avoidanceOffset,
                avoidanceSettings.pathRecalculationInterval,
                avoidanceSettings.maxAvoidanceTime
            );
            
            // Добавление в список команды
            teamBots.Add(bot);
            
            Debug.Log($"Создан бот {bot.name} для команды {teamID}");
        }
    }

    private void AssignTargets()
    {
        // Используем вспомогательный метод для назначения целей обеим командам
        AssignTargetsToTeam(team1Bots, team2Bots);
        AssignTargetsToTeam(team2Bots, team1Bots);
    }
    
    // Вспомогательный метод для назначения целей одной команде
    private void AssignTargetsToTeam(List<GameObject> team, List<GameObject> targetTeam)
    {
        foreach (GameObject bot in team)
        {
            if (targetTeam.Count == 0) break;
            
            // Используем CustomFollower вместо Follower
            CustomFollower follower = bot.GetComponent<CustomFollower>();
            if (follower != null)
            {
                // Выбираем случайного бота из целевой команды
                GameObject target = targetTeam[Random.Range(0, targetTeam.Count)];
                follower.target = target.transform;
                
                Debug.Log($"Боту {bot.name} назначена цель {target.name}");
            }
        }
    }
    
    // Метод для переназначения целей (можно вызывать при необходимости)
    public void ReassignTargets()
    {
        Debug.Log("Переназначение целей...");
        AssignTargets();
    }
    
    // Методы для проверки принадлежности бота к команде
    public bool IsInTeam1(GameObject bot)
    {
        return team1Bots.Contains(bot);
    }
    
    public bool IsInTeam2(GameObject bot)
    {
        return team2Bots.Contains(bot);
    }
    
    // Методы для получения списков ботов команд
    public List<GameObject> GetTeam1Bots()
    {
        return team1Bots;
    }
    
    public List<GameObject> GetTeam2Bots()
    {
        return team2Bots;
    }
    
    // Получение ботов по ID команды
    public List<GameObject> GetTeamBots(int teamID)
    {
        if (teamID == 1)
            return team1Bots;
        else if (teamID == 2)
            return team2Bots;
            
        return new List<GameObject>();
    }
    
    // Определение ID команды бота
    public int GetBotTeamID(GameObject bot)
    {
        if (team1Bots.Contains(bot))
            return 1;
        else if (team2Bots.Contains(bot))
            return 2;
            
        return 0; // Бот не принадлежит ни к одной команде
    }
    
    // При уничтожении бота, удаляем его из списков
    public void RemoveBot(GameObject bot)
    {
        if (team1Bots.Contains(bot))
        {
            team1Bots.Remove(bot);
            Debug.Log($"Бот {bot.name} удален из команды 1");
        }
        else if (team2Bots.Contains(bot))
        {
            team2Bots.Remove(bot);
            Debug.Log($"Бот {bot.name} удален из команды 2");
        }
    }
}