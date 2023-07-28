using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{

    public enum StatusType
    {
        None = 0,

        AttackBonus = 4,      //攻擊狀態可用於攻擊提升，限制 X 回合 
        HPBonus = 5,          //攻擊狀態可用於HP提升，限制X回合 

        Stealth = 10,       //行動之前無法受到攻擊
        Invincibility = 12, //X回合內無法被攻擊
        Shell = 13,         //第一次沒有受到任何傷害
        Protection = 14,    //嘲諷，為其他卡牌提供保護
        Protected = 15,     //受嘲諷保護的卡牌
        Armor = 16,         //受到較少的傷害
        SpellImmunity = 18, //無法被法術瞄準/傷害

        Deathtouch = 20,    //攻擊角色時死亡
        Fury = 22,          //每回合可攻擊兩次
        Flying = 24,         //可以無視嘲諷
        Trample = 26,         //額外傷害分配給玩家

        Silenced = 30,      //所有能力被取消
        Paralysed = 32,     //X 回合內無法執行任何操作
        Poisoned = 34,     //每次回合開始都會失去生命值
        Sleep = 36,         //在回合開始時不重置


    }

    /// <summary>
    /// 定義所有狀態效果數據
    /// 狀態是可以通過能力獲得或失去的效果，並且會影響遊戲玩法
    /// 狀態可以有持續時間
    /// </summary>

    [CreateAssetMenu(fileName = "status", menuName = "TcgEngine/StatusData", order = 7)]
    public class StatusData : ScriptableObject
    {
        public StatusType effect;

        [Header("Display")]
        public string title;
        public Sprite icon;

        [TextArea(3, 5)]
        public string desc;

        [Header("FX")]
        public GameObject status_fx;

        [Header("AI")]
        public int hvalue;

        public static List<StatusData> status_list = new List<StatusData>();

        public string GetTitle()
        {
            return title;
        }

        public string GetDesc()
        {
            return GetDesc(1);
        }

        public string GetDesc(int value)
        {
            string des = desc.Replace("<value>", value.ToString());
            return des;
        }

        public static void Load(string folder = "")
        {
            if (status_list.Count == 0)
                status_list.AddRange(Resources.LoadAll<StatusData>(folder));
        }

        public static StatusData Get(StatusType effect)
        {
            foreach (StatusData status in GetAll())
            {
                if (status.effect == effect)
                    return status;
            }
            return null;
        }

        public static List<StatusData> GetAll()
        {
            return status_list;
        }
    }
}