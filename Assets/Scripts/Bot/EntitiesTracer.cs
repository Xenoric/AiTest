using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Bot
{
    public class EntitiesTracer : MonoBehaviour
    {
        public Dictionary<BotMovement, Vector2Int> Bots { get; } = new();
        [SerializeField] private Grid.Grid _grid;

        public void Trace(BotMovement bot)
        {
            var gridPosition = _grid.WorldToGridPoint(bot.transform.position);
            Bots[bot] = gridPosition;
        }
    }
}