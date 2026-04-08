using System.Collections.Generic;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// 각인 종류 하나를 정의하는 ScriptableObject.
    /// tiers 리스트에 단계별 보너스를 설정한다.
    /// </summary>
    [CreateAssetMenu(menuName = "2D Roguelike/Inscription Definition", fileName = "NewInscription")]
    public class InscriptionDefinition : ScriptableObject
    {
        public string                        inscriptionId;
        public string                        displayName;
        public Sprite                        icon;
        public List<InscriptionTierDefinition> tiers;
    }
}
