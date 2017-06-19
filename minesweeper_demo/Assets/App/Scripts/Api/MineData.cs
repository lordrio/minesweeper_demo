using System;
using UnityEngine;

namespace Data
{
    [System.Serializable]
    public class MineData
    {
        public string created_by;
        public string data;
        public int solve_time;

        [System.Serializable]
        public class MineDataList
        {
            public MineData[] data;
        }

        public static MineDataList CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<MineDataList>(jsonString);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", created_by, data, solve_time);
        }
    }
}
   