using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 定義所有稀有度數據（普通、稀有、史詩、傳說）
    /// </summary>

    [CreateAssetMenu(fileName = "RarityData", menuName = "TcgEngine/RarityData", order = 1)]
    public class RarityData : ScriptableObject
    {
        public string id;
        public string title;
        public Sprite icon;
        public int rank;        //稀有度指數，應從 1（常見）開始並依次增加

        public static List<RarityData> rarity_list = new List<RarityData>();

        public static void Load(string folder = "")
        {
            if (rarity_list.Count == 0)
                rarity_list.AddRange(Resources.LoadAll<RarityData>(folder));
        }

        public static RarityData GetFirst()
        {
            int lowest = 99999;
            RarityData first = null;
            foreach (RarityData rarity in GetAll())
            {
                if (rarity.rank < lowest)
                {
                    first = rarity;
                    lowest = rarity.rank;
                }
            }
            return first;
        }

        public static RarityData Get(string id)
        {
            foreach (RarityData rarity in GetAll())
            {
                if (rarity.id == id)
                    return rarity;
            }
            return null;
        }

        public static List<RarityData> GetAll()
        {
            return rarity_list;
        }
    }
}