using UnityEngine;
using PathBerserker2d;
using System.Collections.Generic;

[RequireComponent(typeof(NavAgent))]
public class CustomFollower : MonoBehaviour
{
    [Header("Following")]
    [SerializeField] public Transform target;
    [SerializeField] private float closeEnoughRadius = 3.0f;
    [SerializeField] private float travelStopRadius = 1.0f;
    [SerializeField] private float updateFrequency = 0.3f;
    [SerializeField] private float targetPredictionTime = 0.1f; // Предсказание движения цели
    
    [Header("Ally Avoidance")]
    [SerializeField] private float allyDetectionDistance = 1.5f;
    [SerializeField] private float avoidanceOffset = 2.0f;
    [SerializeField] private float avoidanceCheckInterval = 0.3f;
    [SerializeField] private float maxAvoidanceTime = 4f; // Максимальное время обхода
    [SerializeField] private bool enableAvoidance = true;
    
    // Компоненты
    private NavAgent navAgent;
    private List<GameObject> teammates = new List<GameObject>();
    
    // Состояние
    private float updateTimer = 0f;
    private float checkTimer = 0f;
    private float avoidanceTimer = 0f;
    private bool isAvoiding = false;
    private GameObject blockingAlly = null;
    private Vector2 lastTargetPosition;
    private int failedPathAttempts = 0;
    
    private void Awake()
    {
        navAgent = GetComponent<NavAgent>();
    }
    
    private void Start()
    {
        // Определяем союзников из нашей команды
        if (BotManager.Instance != null)
        {
            if (BotManager.Instance.IsInTeam1(gameObject))
                teammates = BotManager.Instance.GetTeam1Bots();
            else if (BotManager.Instance.IsInTeam2(gameObject))
                teammates = BotManager.Instance.GetTeam2Bots();
        }
        
        // Начальный путь к цели
        if (target != null)
        {
            lastTargetPosition = target.position;
            navAgent.PathTo(target.position);
        }
        
        // Подписываемся на события
        navAgent.OnReachedGoal += OnReachedGoal;
        navAgent.OnFailedToFindPath += OnFailedToFindPath;
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (navAgent != null)
        {
            navAgent.OnReachedGoal -= OnReachedGoal;
            navAgent.OnFailedToFindPath -= OnFailedToFindPath;
        }
    }
    
    private void OnReachedGoal(NavAgent agent)
    {
        // Сбрасываем состояние избегания
        isAvoiding = false;
        avoidanceTimer = 0f;
        failedPathAttempts = 0;
    }
    
    private void OnFailedToFindPath(NavAgent agent)
    {
        failedPathAttempts++;
        
        // Если много неудачных попыток, сбрасываем состояние избегания
        if (failedPathAttempts >= 3)
        {
            isAvoiding = false;
            avoidanceTimer = 0f;
        }
    }
    
    private void Update()
    {
        if (target == null || !navAgent.enabled)
            return;
        
        // Обновление таймеров
        updateTimer += Time.deltaTime;
        checkTimer += Time.deltaTime;
        
        if (isAvoiding)
        {
            avoidanceTimer += Time.deltaTime;
            
            // Если обход длится слишком долго, принудительно возвращаемся к цели
            if (avoidanceTimer > maxAvoidanceTime)
            {
                isAvoiding = false;
                avoidanceTimer = 0f;
                TryDirectPathToTarget();
            }
        }
        
        // Обновление пути к цели
        if (updateTimer >= updateFrequency)
        {
            updateTimer = 0f;
            UpdatePathToTarget();
        }
        
        // Проверка на блокирующих союзников
        if (checkTimer >= avoidanceCheckInterval && enableAvoidance && navAgent.IsFollowingAPath && !isAvoiding)
        {
            checkTimer = 0f;
            CheckForBlockingAllies();
        }
    }
    
    private void UpdatePathToTarget()
    {
        if (target == null) return;
    
        Vector2 targetPos = target.position;
        float distToTarget = Vector2.Distance(transform.position, targetPos);
    
        // Предсказываем движение цели
        if (targetPredictionTime > 0 && Vector2.Distance(targetPos, lastTargetPosition) > 0.1f)
        {
            Vector2 targetVelocity = (targetPos - lastTargetPosition) / updateFrequency;
            targetPos += targetVelocity * targetPredictionTime;
        }
    
        // Обновляем позицию цели
        lastTargetPosition = target.position;
    
        // ВАЖНОЕ ИЗМЕНЕНИЕ: Улучшенная логика остановки
        if (distToTarget < travelStopRadius)
        {
            // Если мы слишком близко, останавливаемся
            navAgent.Stop();
        
            // Если мы ОЧЕНЬ близко, активно отходим назад
            if (distToTarget < travelStopRadius * 0.7f) 
            {
                Vector2 directionFromTarget = (Vector2)transform.position - targetPos;
                directionFromTarget.Normalize();
            
                // Рассчитываем точку отступления
                Vector2 retreatPoint = targetPos + directionFromTarget * (travelStopRadius * 1.2f);
                navAgent.PathTo(retreatPoint);
            
                Debug.Log($"Бот {name} отступает от цели {target.name}, расстояние: {distToTarget}");
            }
            return;
        }
    
        // Если мы находимся в "зоне комфорта" (между travelStopRadius и closeEnoughRadius)
        // мы не обновляем путь, чтобы предотвратить постоянные перемещения
        if (distToTarget >= travelStopRadius && distToTarget <= closeEnoughRadius)
        {
            // Находимся на нормальном расстоянии, ничего не делаем
            return;
        }
    
        // Если не в режиме избегания и слишком далеко, следуем к цели
        if (!isAvoiding && distToTarget > closeEnoughRadius)
        {
            navAgent.UpdatePath(targetPos);
        }
    }
    
    private void TryDirectPathToTarget()
    {
        if (target != null)
        {
            navAgent.UpdatePath(target.position);
            Debug.Log($"Бот {name} возвращается напрямую к цели {target.name}");
        }
    }
    
    private void CheckForBlockingAllies()
    {
        if (target == null) return;
        
        // Находим блокирующего союзника
        GameObject ally = FindBlockingTeammate();
        
        if (ally != null)
        {
            // Союзник найден, переходим в режим обхода
            blockingAlly = ally;
            Vector2 avoidancePoint = CalculateAvoidancePoint(blockingAlly.transform.position);
            
            isAvoiding = true;
            avoidanceTimer = 0f;
            navAgent.UpdatePath(avoidancePoint);
            
            Debug.Log($"Бот {name} избегает {blockingAlly.name}");
        }
    }
    
    private GameObject FindBlockingTeammate()
    {
        if (teammates.Count == 0 || target == null) 
            return null;
        
        Vector2 myPosition = transform.position;
        Vector2 goalPosition = target.position;
        Vector2 directionToGoal = goalPosition - myPosition;
        float distanceToGoal = directionToGoal.magnitude;
        
        // Если цель очень близко, не нужно избегать
        if (distanceToGoal < travelStopRadius * 2f) 
            return null;
            
        directionToGoal /= distanceToGoal; // Нормализация
        
        foreach (GameObject ally in teammates)
        {
            if (ally == null || ally == gameObject) 
                continue;
                
            Vector2 allyPosition = ally.transform.position;
            
            // Расстояние до союзника
            float distanceToAlly = Vector2.Distance(myPosition, allyPosition);
            
            // Если союзник слишком далеко, пропускаем
            if (distanceToAlly > allyDetectionDistance * 2f)
                continue;
                
            // Вычисляем проекцию союзника на линию пути
            float projection = Vector2.Dot(allyPosition - myPosition, directionToGoal);
            
            // Игнорируем союзников позади нас или за целью
            if (projection <= 0 || projection >= distanceToGoal * 0.95f) 
                continue;
                
            // Вычисляем точку проекции на линии пути
            Vector2 projectedPoint = myPosition + directionToGoal * projection;
            
            // Расстояние от союзника до линии пути
            float distanceFromPath = Vector2.Distance(allyPosition, projectedPoint);
            
            // Если союзник достаточно близко к пути
            if (distanceFromPath < allyDetectionDistance)
            {
                return ally;
            }
        }
        
        return null;
    }
    
    private Vector2 CalculateAvoidancePoint(Vector2 allyPosition)
    {
        if (target == null)
            return transform.position;
            
        Vector2 myPosition = transform.position;
        Vector2 targetPosition = target.position;
        
        // Направление к цели
        Vector2 dirToTarget = (targetPosition - myPosition).normalized;
        
        // Расстояние между мной и целью
        float distanceToTarget = Vector2.Distance(myPosition, targetPosition);
        
        // Перпендикулярный вектор для обхода
        Vector2 perpendicularDir = new Vector2(-dirToTarget.y, dirToTarget.x);
        
        // Определяем, с какой стороны обходить
        Vector2 toAlly = allyPosition - myPosition;
        if (Vector2.Dot(toAlly, perpendicularDir) < 0)
            perpendicularDir = -perpendicularDir;
        
        // Точка обхода: около 45 градусов от прямого направления
        // Создаем гибрид между боковым и вперед направлениями
        Vector2 avoidDir = (dirToTarget + perpendicularDir).normalized;
        
        // Рассчитываем точку обхода на расстоянии, пропорциональном от союзника до цели
        float avoidDistance = Mathf.Min(avoidanceOffset, distanceToTarget * 0.5f);
        Vector2 avoidancePoint = allyPosition + avoidDir * avoidDistance;
        
        return avoidancePoint;
    }
    
    // Методы настройки
    public void SetFollowingParameters(float closeEnough, float stopRadius, float updateRate, float predictionTime)
    {
        closeEnoughRadius = closeEnough;
        travelStopRadius = stopRadius;
        updateFrequency = updateRate;
        targetPredictionTime = predictionTime;
    }
    
    public void SetAvoidanceParameters(float detection, float offset, float interval, float maxAvoidTime)
    {
        allyDetectionDistance = detection;
        avoidanceOffset = offset;
        avoidanceCheckInterval = interval;
        maxAvoidanceTime = maxAvoidTime;
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Радиус обнаружения союзников
        Gizmos.color = enableAvoidance ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, allyDetectionDistance);
        
        // Дистанция остановки
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, travelStopRadius);
        }
        
        // Отображаем блокирующего союзника и путь обхода
        if (isAvoiding && blockingAlly != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, blockingAlly.transform.position);
            
            // Точка обхода
            if (navAgent.PathGoal.HasValue)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(navAgent.PathGoal.Value, 0.3f);
                Gizmos.DrawLine(transform.position, navAgent.PathGoal.Value);
            }
        }
    }
}