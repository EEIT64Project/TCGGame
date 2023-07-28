using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 將目標卡牌的擁有者更改為施法者（或對手玩家）的擁有者
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ChangeOwner", order = 10)]
    public class EffectChangeOwner : EffectData
    {
        public bool owner_opponent; //換自己還是換對手？

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();
            Player tplayer = owner_opponent ? game.GetOpponentPlayer(caster.player_id) : game.GetPlayer(caster.player_id);
            logic.ChangeOwner(target, tplayer);
        }
    }
}