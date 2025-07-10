using UnityEngine;
using BotSystem;

namespace BotSystem
{
    /// <summary>
    /// Тестовый скрипт для демонстрации улучшенного поведения ботов
    /// с контролем расстояния и проверкой видимости
    /// </summary>
    public class BotBehaviorTester : MonoBehaviour
    {
        [Header("Тестирование")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showGUI = true;
        
        [Header("Динамические настройки")]
        [SerializeField] private float testMinDistance = 2.0f;
        [SerializeField] private float testMaxDistance = 5.0f;
        [SerializeField] private float testOptimalDistance = 3.5f;
        
        private SimpleBotManager botManager;
        private AvoidanceFollower[] allFollowers;
        
        private void Start()
        {
            botManager = SimpleBotManager.Instance;
            if (botManager == null)
            {
                Debug.LogError("BotBehaviorTester: SimpleBotManager не найден!");
                return;
            }
            
            // Получаем всех AvoidanceFollower в сцене
            allFollowers = FindObjectsOfType<AvoidanceFollower>();
            Debug.Log($"BotBehaviorTester: Найдено {allFollowers.Length} ботов с AvoidanceFollower");
        }
        
        private void Update()
        {
            if (showDebugInfo && allFollowers != null)
            {
                LogBotStates();
            }
        }
        
        /// <summary>
        /// Логирование состояний ботов для отладки
        /// </summary>
        private void LogBotStates()
        {
            foreach (var follower in allFollowers)
            {
                if (follower == null || follower.target == null) continue;
                
                string botName = follower.gameObject.name;
                bool hasLOS = follower.HasLineOfSight;
                float distance = follower.DistanceToTarget;
                bool inRange = follower.IsInOptimalRange;
                
                Debug.Log($"{botName}: LOS={hasLOS}, Dist={distance:F1}, InRange={inRange}");
            }
        }
        
        /// <summary>
        /// Применение новых настроек расстояния ко всем ботам
        /// </summary>
        public void ApplyDistanceSettings()
        {
            if (allFollowers == null) return;
            
            foreach (var follower in allFollowers)
            {
                if (follower != null)
                {
                    follower.ConfigureDistanceControl(testMinDistance, testMaxDistance, testOptimalDistance);
                }
            }
            
            Debug.Log($"BotBehaviorTester: Применены новые настройки расстояния - Min:{testMinDistance}, Max:{testMaxDistance}, Optimal:{testOptimalDistance}");
        }
        
        /// <summary>
        /// Переназначение всех целей
        /// </summary>
        public void ReassignTargets()
        {
            if (botManager != null)
            {
                botManager.ReassignAllTargets();
                Debug.Log("BotBehaviorTester: Цели переназначены");
            }
        }
        
        /// <summary>
        /// Принудительное обновление путей всех ботов
        /// </summary>
        public void ForceUpdateAllPaths()
        {
            if (allFollowers == null) return;
            
            foreach (var follower in allFollowers)
            {
                if (follower != null)
                {
                    follower.ForceUpdatePath();
                }
            }
            
            Debug.Log("BotBehaviorTester: Принудительно обновлены пути всех ботов");
        }
        
        private void OnGUI()
        {
            if (!showGUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== Bot Behavior Tester ===");
            
            // Показываем общую информацию
            if (botManager != null)
            {
                var team1Count = botManager.GetTeamBots(1).Count;
                var team2Count = botManager.GetTeamBots(2).Count;
                GUILayout.Label($"Team 1: {team1Count} bots");
                GUILayout.Label($"Team 2: {team2Count} bots");
            }
            
            GUILayout.Space(10);
            
            // Настройки расстояния
            GUILayout.Label("Distance Settings:");
            testMinDistance = GUILayout.HorizontalSlider(testMinDistance, 0.5f, 5.0f);
            GUILayout.Label($"Min Distance: {testMinDistance:F1}");
            
            testMaxDistance = GUILayout.HorizontalSlider(testMaxDistance, 3.0f, 10.0f);
            GUILayout.Label($"Max Distance: {testMaxDistance:F1}");
            
            testOptimalDistance = GUILayout.HorizontalSlider(testOptimalDistance, testMinDistance, testMaxDistance);
            GUILayout.Label($"Optimal Distance: {testOptimalDistance:F1}");
            
            if (GUILayout.Button("Apply Distance Settings"))
            {
                ApplyDistanceSettings();
            }
            
            GUILayout.Space(10);
            
            // Кнопки управления
            if (GUILayout.Button("Reassign Targets"))
            {
                ReassignTargets();
            }
            
            if (GUILayout.Button("Force Update Paths"))
            {
                ForceUpdateAllPaths();
            }
            
            GUILayout.Space(10);
            
            // Показываем состояние ботов
            if (allFollowers != null)
            {
                GUILayout.Label("Bot States:");
                int botsWithLOS = 0;
                int botsInRange = 0;
                
                foreach (var follower in allFollowers)
                {
                    if (follower != null && follower.target != null)
                    {
                        if (follower.HasLineOfSight) botsWithLOS++;
                        if (follower.IsInOptimalRange) botsInRange++;
                    }
                }
                
                GUILayout.Label($"Bots with Line of Sight: {botsWithLOS}/{allFollowers.Length}");
                GUILayout.Label($"Bots in Optimal Range: {botsInRange}/{allFollowers.Length}");
            }
            
            GUILayout.EndArea();
        }
    }
}
