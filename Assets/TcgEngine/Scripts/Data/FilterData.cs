using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 目標過濾器的基類
    /// 讓您可以在已按條件選取目標後但在應用效果之前過濾目標
    /// </summary>

    public class FilterData : ScriptableObject
    {
        public virtual List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            return source; //Override this，過濾目標卡
        }

        public virtual List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            return source; //Override this，過濾目標玩家
        }

        public virtual List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            return source; //Override this，過濾定位槽
        }

        public virtual List<CardData> FilterTargets(Game data, AbilityData ability, Card caster, List<CardData> source, List<CardData> dest)
        {
            return source; //Override this，對於創建新卡片的過濾器
        }
    }
}
