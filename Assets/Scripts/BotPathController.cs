using System.Collections.Generic;
using UnityEngine;
using PathBerserker2d;

[RequireComponent(typeof(NavAgent))]
public class BotPathController : MonoBehaviour
{
    [Header("Target Following")]
    [SerializeField] private float targetUpdateInterval = 0.5f;
    [SerializeField] private float targetStopDistance = 1.0f;
    [SerializeField] private float closeEnoughDistance = 3.0f;
    
    [Header("Ally Avoidance")]
    [SerializeField] private float allyDetectionDistance = 1.5f;
    [SerializeField] private float avoidanceOffset = 2.0f;
    [SerializeField] private float avoidanceCheckInterval = 0.3f;
    
    [Header("Path Settings")]
    [SerializeField] private float pathUpdateDistance = 1.5f; // Дистанция, при которой обновляем путь к цели
    
    // Ссылки на другие компоненты
    private NavAgent navAgent;
    private Follower follower;
    private Transform currentTarget;
    public BotManager botManager;
    
    // Таймеры для проверок
    private float targetUpdateTimer = 0f;
    private float allyCheckTimer = 0f;
    
    // Состояние пути
    private bool pathBlocked = false;
    private GameObject blockingAlly;
    private Vector2 lastTargetPosition;
    private bool isAvoidingAlly = false;
    
    // Кэшированные списки
    private List<GameObject> myTeam = new List<GameObject>();
    
    private void Awake()
    {
        navAgent = GetComponent<NavAgent>();
        follower = GetComponent<Follower>();
        
        if (botManager == null)
            botManager = FindObjectOfType<BotManager>();
    }
    
    private void Start()
    {
        // Получаем цель из Follower, если есть
        if (follower != null && follower.target != null)
        {
            currentTarget = follower.target;
            lastTargetPosition = currentTarget.position;
        }
        
        // Определяем, в какой команде находится этот бот
        if (botManager != null)
        {
            if (botManager.IsInTeam1(gameObject))
                myTeam = botManager.GetTeam1Bots();
            else if (botManager.IsInTeam2(gameObject))
                myTeam = botManager.GetTeam2Bots();
        }
        
        // Запускаем начальный путь к цели
        if (currentTarget != null)
            UpdatePathToTarget();
    }
    
    public void SetParameters(float detectionDistance, float updateInterval, float offset)
    {
        allyDetectionDistance = detectionDistance;
        avoidanceCheckInterval = updateInterval;
        avoidanceOffset = offset;
    }
    
    private void Update()
    {
        // Обновляем ссылку на цель, если она изменилась в Follower
        if (follower != null && follower.target != currentTarget)
        {
            currentTarget = follower.target;
            if (currentTarget != null)
                lastTargetPosition = currentTarget.position;
        }
        
        if (currentTarget == null)
            return;
            
        // Проверка и обновление пути к цели
        targetUpdateTimer += Time.deltaTime;
        if (targetUpdateTimer >= targetUpdateInterval)
        {
            targetUpdateTimer = 0f;
            UpdatePathToTarget();
        }
        
        // Проверка на блокировку пути союзниками
        allyCheckTimer += Time.deltaTime;
        if (allyCheckTimer >= avoidanceCheckInterval && navAgent.IsFollowingAPath)
        {
            allyCheckTimer = 0f;
            CheckAndAvoidAllies();
        }
    }
    
    // Обновляем путь к цели с учетом её движения
    private void UpdatePathToTarget()
    {
        if (currentTarget == null || !navAgent.enabled)
            return;
            
        Vector2 targetPos = currentTarget.position;
        float distToTarget = Vector2.Distance(transform.position, targetPos);
        
        // Если цель далеко и переместилась значительно - обновляем путь
        if (distToTarget > targetStopDistance)
        {
            bool needPathUpdate = false;
            
            // Если цель существенно сдвинулась, обновляем путь
            if (Vector2.Distance(targetPos, lastTargetPosition) > pathUpdateDistance)
            {
                needPathUpdate = true;
                lastTargetPosition = targetPos;
            }
            
            // Если у нас нет активного пути, обновляем его
            if (!navAgent.PathGoal.HasValue)
                needPathUpdate = true;
                
            // Если мы находимся в режиме избегания, но путь свободен, возвращаемся на прямой путь
            if (isAvoidingAlly && !IsPathBlockedByAlly(targetPos))
            {
                needPathUpdate = true;
                isAvoidingAlly = false;
            }
            
            if (needPathUpdate && !isAvoidingAlly)
            {
                navAgent.UpdatePath(targetPos);
            }
        }
        else if (distToTarget < targetStopDistance)
        {
            // Если мы достаточно близко к цели, останавливаемся
            navAgent.Stop();
        }
    }
    
    // Проверяем и избегаем союзников
    private void CheckAndAvoidAllies()
    {
        if (!navAgent.PathGoal.HasValue || currentTarget == null)
            return;
            
        // Проверяем, блокируют ли союзники путь
        pathBlocked = FindBlockingAlly(out blockingAlly);
        
        if (pathBlocked && blockingAlly != null)
        {
            isAvoidingAlly = true;
            
            // Генерируем точку обхода
            Vector2 avoidanceTarget = CalculateAvoidancePoint(blockingAlly.transform, currentTarget.position);
            
            // Обновляем путь
            navAgent.UpdatePath(avoidanceTarget);
        }
    }
    
    // Находит союзника, блокирующего путь
    private bool FindBlockingAlly(out GameObject blockingAlly)
    {
        blockingAlly = null;
        
        if (!navAgent.PathGoal.HasValue || myTeam.Count == 0 || currentTarget == null)
            return false;
            
        Vector2 targetPosition = currentTarget.position;
        Vector2 myPosition = transform.position;
        Vector2 directionToTarget = targetPosition - myPosition;
        float distanceToTarget = directionToTarget.magnitude;
        
        if (distanceToTarget < 0.1f)
            return false;
            
        directionToTarget /= distanceToTarget; // Нормализация
        
        foreach (GameObject ally in myTeam)
        {
            // Пропускаем себя
            if (ally == gameObject)
                continue;
                
            // Проверяем положение союзника
            Vector2 allyPosition = ally.transform.position;
            
            // Вычисляем проекцию союзника на линию пути
            float projection = Vector2.Dot(allyPosition - myPosition, directionToTarget);
            
            // Игнорируем союзников позади нас
            if (projection <= 0)
                continue;
                
            // Вычисляем точку проекции на линии пути
            Vector2 projectedPoint = myPosition + directionToTarget * projection;
            
            // Расстояние от союзника до линии пути
            float distanceFromPath = Vector2.Distance(allyPosition, projectedPoint);
            
            // Если союзник близко к пути и находится между нами и целью
            if (distanceFromPath < allyDetectionDistance && projection < distanceToTarget)
            {
                blockingAlly = ally;
                return true;
            }
        }
        
        return false;
    }
    
    // Проверяет, блокирует ли какой-либо союзник прямой путь к цели
    private bool IsPathBlockedByAlly(Vector2 targetPos)
    {
        Vector2 myPosition = transform.position;
        Vector2 directionToTarget = targetPos - myPosition;
        float distanceToTarget = directionToTarget.magnitude;
        
        if (distanceToTarget < 0.1f)
            return false;
            
        directionToTarget /= distanceToTarget;
        
        foreach (GameObject ally in myTeam)
        {
            if (ally == gameObject)
                continue;
                
            Vector2 allyPosition = ally.transform.position;
            float projection = Vector2.Dot(allyPosition - myPosition, directionToTarget);
            
            if (projection <= 0)
                continue;
                
            Vector2 projectedPoint = myPosition + directionToTarget * projection;
            float distanceFromPath = Vector2.Distance(allyPosition, projectedPoint);
            
            if (distanceFromPath < allyDetectionDistance && projection < distanceToTarget)
                return true;
        }
        
        return false;
    }
    
    // Вычисляет точку обхода для блокирующего союзника
    private Vector2 CalculateAvoidancePoint(Transform blockingTransform, Vector2 finalGoal)
    {
        if (blockingTransform == null)
            return finalGoal;
            
        // Получаем направление к цели
        Vector2 directionToTarget = finalGoal - (Vector2)transform.position;
        directionToTarget.Normalize();
        
        // Создаем перпендикулярный вектор
        Vector2 perpendicularDir = new Vector2(-directionToTarget.y, directionToTarget.x);
        
        // Определяем, с какой стороны лучше обойти союзника
        Vector2 directionToAlly = blockingTransform.position - transform.position;
        
        // Выбираем сторону обхода
        if (Vector2.Dot(directionToAlly, perpendicularDir) < 0)
            perpendicularDir = -perpendicularDir;
            
        // Вычисляем промежуточную точку обхода
        Vector2 avoidancePoint = (Vector2)blockingTransform.position + perpendicularDir * avoidanceOffset;
        
        return avoidancePoint;
    }
    
    // Для отладки
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
            
        // Показываем зону обнаружения союзников
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, allyDetectionDistance);
        
        // Показываем текущую цель
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, targetStopDistance);
        }
        
        // Показываем блокирующего союзника
        if (pathBlocked && blockingAlly != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, blockingAlly.transform.position);
        }
        
        // Показываем путь
        if (navAgent != null && navAgent.PathGoal.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(navAgent.PathGoal.Value, 0.3f);
        }
    }
}