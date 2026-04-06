namespace _2D_Roguelike
{
    public enum EffectTrigger
    {
        /// <summary>장착 즉시 적용, 해제 시 제거되는 패시브</summary>
        OnEquip,

        /// <summary>해제 시 처리 (현재는 OnEquip의 Remove로 대응, 확장용)</summary>
        OnUnequip
    }
}
