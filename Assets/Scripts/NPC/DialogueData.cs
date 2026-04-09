using UnityEngine;

namespace _2D_Roguelike
{
    [CreateAssetMenu(fileName = "DialogueData", menuName = "2D Roguelike/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] private string _npcName = "NPC";
        [SerializeField, TextArea(2, 6)] private string[] _lines;

        public string   NpcName => _npcName;
        public string[] Lines   => _lines;
    }
}
