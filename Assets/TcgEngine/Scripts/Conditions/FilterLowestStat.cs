using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //選擇所有統計數據最低的目標

    [CreateAssetMenu(fileName = "filter", menuName = "TcgEngine/Filter/LowestStat", order = 10)]
    public class FilterLowestStat : FilterData
    {
        public ConditionStatType stat;

        public override List<Card> FilterTargets(Game data, AbilityData ability, Card caster, List<Card> source, List<Card> dest)
        {
            //找到最低的
            int lowest = 99999;
            foreach (Card card in source)
            {
                int stat = GetStat(card);
                if (stat < lowest)
                    lowest = stat;
            }

            //添加所有最低值
            foreach (Card card in source)
            {
                int stat = GetStat(card);
                if (stat == lowest)
                    dest.Add(card);
            }

            return dest;
        }

        private int GetStat(Card card)
        {
            if (stat == ConditionStatType.Attack)
            {
                return card.GetAttack();
            }
            if (stat == ConditionStatType.HP)
            {
                return card.GetHP();
            }
            if (stat == ConditionStatType.Mana)
            {
                return card.GetMana();
            }
            return 0;
        }
    }
}
