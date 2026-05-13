using UnityEngine;

namespace _2D_Roguelike
{
    public static class EnhanceSaveService
    {
        private const string Prefix = "Enhance_";

        public static int GetLevel(string nodeId) =>
            PlayerPrefs.GetInt(Prefix + nodeId, 0);

        public static void SetLevel(string nodeId, int level)
        {
            PlayerPrefs.SetInt(Prefix + nodeId, level);
            PlayerPrefs.Save();
        }

        public static void SetLevelNoFlush(string nodeId, int level) =>
            PlayerPrefs.SetInt(Prefix + nodeId, level);

        public static void FlushSave() => PlayerPrefs.Save();
    }
}
