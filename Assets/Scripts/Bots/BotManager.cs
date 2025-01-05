using UnityEngine;
using System.Collections.Generic;

public class BotManager : MonoBehaviour
{
    public List<BotMovement> teamOneBots; // Список ботов первой команды
    public List<BotMovement> teamTwoBots; // Список ботов второй команды

   void Update()
{
    // Обновляем всех ботов первой команды
    foreach (var bot in teamOneBots)
    {
        Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamTwoBots);
        
        // Проверяем, изменилась ли цель
        if (bot.targetPosition != nearestBotPosition)
        {
            bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
        }
        
        bot.UpdateBot(); // Обновляем движение бота
    }
    // Обновляем всех ботов второй команды
    foreach (var bot in teamTwoBots)
    {
        Vector2 nearestBotPosition = FindNearestOpposingBot(bot, teamOneBots);
        
        // Проверяем, изменилась ли цель
        if (bot.targetPosition != nearestBotPosition)
        {
            bot.SetTarget(nearestBotPosition); // Устанавливаем цель на ближайшего противника
        }
        
        bot.UpdateBot(); // Обновляем движение бота
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

    // Метод для установки новой цели для первой команды
    public void SetTargetsForTeamOne(Vector2 newTarget)
    {
        foreach (var bot in teamOneBots)
        {
            bot.SetTarget(newTarget);
        }
    }

    // Метод для установки новой цели для второй команды
    public void SetTargetsForTeamTwo(Vector2 newTarget)
    {
        foreach (var bot in teamTwoBots)
        {
            bot.SetTarget(newTarget);
        }
    }

    // Метод для остановки всех ботов первой команды
    public void StopAllTeamOneBots()
    {
        foreach (var bot in teamOneBots)
        {
            bot.SetTarget(bot.transform.position); // Устанавливаем текущую позицию как цель
        }
    }

    // Метод для остановки всех ботов второй команды
    public void StopAllTeamTwoBots()
    {
        foreach (var bot in teamTwoBots)
        {
            bot.SetTarget(bot.transform.position); // Устанавливаем текущую позицию как цель
        }
    }
}