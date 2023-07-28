using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //從源數組中隨機選取 X 個目標

    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/Random", order = 10)]
    public class FilterRandom : FilterData
    {
        public int amount = 1; //選擇的隨機目標數量

        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        public override List<Player> FilterTargets(Game data, AbilityData ability, Card caster, List<Player> source, List<Player> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        public override List<Slot> FilterTargets(Game data, AbilityData ability, Card caster, List<Slot> source, List<Slot> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }

        public override List<CardData> FilterTargets(Game data, AbilityData ability, Card caster, List<CardData> source, List<CardData> dest)
        {
            return GameTool.PickXRandom(source, dest, amount);
        }
    }
}
