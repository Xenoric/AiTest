using UnityEngine;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    public List<BotMovement> teamOneBots; // Список ботов первой команды
    public List<BotMovement> teamTwoBots; // Список ботов второй команды

    private int frameCounter = 0; // Счетчик кадров
    public int updateTargetEveryNFrames = 2; // Обновлять цель каждые N кадров

    void Update()
    {
        frameCounter++;

        // Обновляем цели только когда счетчик кадров достигает заданного значения
        if (frameCounter >= updateTargetEveryNFrames)
        {
            UpdateBotsTargets();
            frameCounter = 0; // Сбрасываем счетчик
        }

        // Обновляем движение ботов каждый кадр
        UpdateBotsMovement();
    }

    private void UpdateBotsTargets()
    {
        // Обновляем цели для ботов первой команды
        foreach (var bot in teamOneBots)
        {
            Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamTwoBots);
            
            // Проверяем, изменилась ли цель
            if (bot.targetPosition != nearestBotPosition)
            {
                bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
            }
        }
        // Обновляем цели для ботов второй команды
        foreach (var bot in teamTwoBots)
        {
            Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamOneBots);
            
            // Проверяем, изменилась ли цель
            if (bot.targetPosition != nearestBotPosition)
            {
                bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
            }
        }
    }

    private void UpdateBotsMovement()
    {
        // Обновляем движение всех ботов
        foreach (var bot in teamOneBots)
        {
            bot.UpdateBot();
        }
        foreach (var bot in teamTwoBots)
        {
            bot.UpdateBot();
        }
    }

    private Vector2 FindNearestOpposingBot(BotMovement bot, List<BotMovement> opposingBots)
    {
        Vector2 nearestPosition = Vector2.zero;
        float nearestDistanceSquared = float.MaxValue;

        foreach (var opposingBot in opposingBots)
        {
            float distanceSquared = (bot.transform.position.x - opposingBot.transform.position.x) * (bot.transform.position.x - opposingBot.transform.position.x) +
                                    (bot.transform.position.y - opposingBot.transform.position.y) * (bot.transform.position.y - opposingBot.transform.position.y);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPosition = opposingBot.transform.position;
            }
        }
        return nearestPosition; // Возвращаем позицию ближайшего противника
    }
}

