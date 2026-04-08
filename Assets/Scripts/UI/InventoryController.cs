using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace _2D_Roguelike
{
    /// <summary>
    /// Tab 키로 열리는 인벤토리 UI를 관리한다.
    /// 열리는 동안 Time.timeScale = 0 으로 게임을 일시정지한다.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        private const int ItemSlotCount  = 9;
        private const int SynergySlotMax = 6;
        private const int DetailSynCount = 2;
        private const string SlotSelected = "inv-item-slot--selected";

        [SerializeField] private Texture2D _jobPortrait;

        // ── UI 참조 ───────────────────────────────────────────────────
        private VisualElement _overlay;

        // 왼쪽: 시너지
        private VisualElement[] _synSlots;
        private VisualElement[] _synIcons;
        private Label[]         _synCurCounts; // 현재 보유 개수 (큰 숫자)
        private Label[]         _synNames;
        private Label[]         _synCounts;    // 티어 텍스트 "2 → 4"

        // 가운데: 아이템 슬롯
        private VisualElement[] _itemSlots;

        // 오른쪽: 상세 정보
        private VisualElement _detailEmpty;
        private VisualElement _detailContent;
        private VisualElement _detailIcon;
        private Label         _detailRarity;
        private Label         _detailName;
        private Label         _detailDescription;
        private VisualElement[] _detailSynCards;
        private VisualElement[] _detailSynIcons;
        private Label[]         _detailSynNames;
        private Label[]         _detailSynCounts;

        // ── 상태 ─────────────────────────────────────────────────────
        private PlayerStatController _stat;
        private int _selectedSlot = -1;

        public bool IsOpen { get; private set; }

        // ── 생명주기 ─────────────────────────────────────────────────

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _overlay = root.Q<VisualElement>("inventory-overlay");
            _overlay.style.display = DisplayStyle.None;

            // 왼쪽 시너지 슬롯 캐싱
            _synSlots     = new VisualElement[SynergySlotMax];
            _synIcons     = new VisualElement[SynergySlotMax];
            _synCurCounts = new Label[SynergySlotMax];
            _synNames     = new Label[SynergySlotMax];
            _synCounts    = new Label[SynergySlotMax];
            for (int i = 0; i < SynergySlotMax; i++)
            {
                _synSlots[i]     = root.Q<VisualElement>($"synergy-slot-{i}");
                _synIcons[i]     = root.Q<VisualElement>($"synergy-icon-{i}");
                _synCurCounts[i] = root.Q<Label>($"synergy-cur-{i}");
                _synNames[i]     = root.Q<Label>($"synergy-name-{i}");
                _synCounts[i]    = root.Q<Label>($"synergy-count-{i}");
            }

            // 가운데 아이템 슬롯 캐싱
            _itemSlots = new VisualElement[ItemSlotCount];
            for (int i = 0; i < ItemSlotCount; i++)
                _itemSlots[i] = root.Q<VisualElement>($"item-slot-{i}");

            // 직업 슬롯 초상화 설정
            var jobSlot = root.Q<VisualElement>("job-slot");
            if (_jobPortrait != null)
                jobSlot.style.backgroundImage = new StyleBackground(_jobPortrait);

            // 오른쪽 상세 정보 캐싱
            _detailEmpty       = root.Q<VisualElement>("detail-empty");
            _detailContent     = root.Q<VisualElement>("detail-content");
            _detailIcon        = root.Q<VisualElement>("detail-icon");
            _detailRarity      = root.Q<Label>("detail-rarity");
            _detailName        = root.Q<Label>("detail-name");
            _detailDescription = root.Q<Label>("detail-description");

            _detailSynCards  = new VisualElement[DetailSynCount];
            _detailSynIcons  = new VisualElement[DetailSynCount];
            _detailSynNames  = new Label[DetailSynCount];
            _detailSynCounts = new Label[DetailSynCount];
            for (int i = 0; i < DetailSynCount; i++)
            {
                _detailSynCards[i]  = root.Q<VisualElement>($"detail-synergy-{i}");
                _detailSynIcons[i]  = root.Q<VisualElement>($"detail-syn-icon-{i}");
                _detailSynNames[i]  = root.Q<Label>($"detail-syn-name-{i}");
                _detailSynCounts[i] = root.Q<Label>($"detail-syn-count-{i}");
            }

            // 플레이어 스탯 컨트롤러 탐색
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                _stat = player.GetComponent<PlayerStatController>();

            if (_stat != null)
            {
                _stat.EquipmentService.OnItemEquipped   += OnEquipmentChanged;
                _stat.EquipmentService.OnItemUnequipped += OnEquipmentChanged;
            }
        }

        private void OnDestroy()
        {
            if (_stat != null)
            {
                _stat.EquipmentService.OnItemEquipped   -= OnEquipmentChanged;
                _stat.EquipmentService.OnItemUnequipped -= OnEquipmentChanged;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (!IsOpen) return;

            HandleMouseInput();
        }

        // ── 마우스 입력 ───────────────────────────────────────────────

        private void HandleMouseInput()
        {
            if (Mouse.current == null) return;

            Vector2 sp       = Mouse.current.position.ReadValue();
            Vector2 panelPos = new Vector2(sp.x, Screen.height - sp.y);
            bool clicked     = Mouse.current.leftButton.wasPressedThisFrame;

            if (!clicked) return;

            var items = _stat?.InventoryService.Items;
            if (items == null) return;

            for (int i = 0; i < ItemSlotCount; i++)
            {
                if (!_itemSlots[i].worldBound.Contains(panelPos)) continue;

                // 아이템이 없는 슬롯 클릭은 무시
                if (i >= items.Count) break;

                OnItemSlotClicked(i);
                break;
            }
        }

        // ── 열기 / 닫기 ──────────────────────────────────────────────

        public void Open()
        {
            IsOpen = true;
            Time.timeScale = 0f;
            _selectedSlot = -1;
            _overlay.style.display = DisplayStyle.Flex;
            RefreshAll();
        }

        public void Close()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            _overlay.style.display = DisplayStyle.None;
        }

        // ── 갱신 ─────────────────────────────────────────────────────

        private void OnEquipmentChanged(ItemInstance _)
        {
            if (IsOpen) RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshItemSlots();
            RefreshSynergyPanel();
            ShowDetail(_selectedSlot);
        }

        // ── 아이템 슬롯 ──────────────────────────────────────────────

        private void RefreshItemSlots()
        {
            var items = _stat?.InventoryService.Items;

            for (int i = 0; i < ItemSlotCount; i++)
            {
                var slot = _itemSlots[i];
                slot.Clear();

                if (items != null && i < items.Count)
                {
                    var icon = items[i].Definition.icon;
                    if (icon != null)
                    {
                        var img = new VisualElement();
                        img.AddToClassList("inv-item-icon");
                        img.style.backgroundImage = new StyleBackground(icon);
                        slot.Add(img);
                    }
                }
            }
        }

        private void OnItemSlotClicked(int index)
        {
            // 같은 슬롯 재클릭 → 선택 해제
            if (_selectedSlot == index)
            {
                _itemSlots[index].RemoveFromClassList(SlotSelected);
                _selectedSlot = -1;
                ShowDetail(-1);
                return;
            }

            // 이전 선택 해제
            if (_selectedSlot >= 0)
                _itemSlots[_selectedSlot].RemoveFromClassList(SlotSelected);

            _selectedSlot = index;
            _itemSlots[index].AddToClassList(SlotSelected);
            ShowDetail(index);
        }

        // ── 상세 정보 ────────────────────────────────────────────────

        private void ShowDetail(int slotIndex)
        {
            var items    = _stat?.InventoryService.Items;
            bool hasItem = items != null && slotIndex >= 0 && slotIndex < items.Count;

            _detailEmpty.style.display   = hasItem ? DisplayStyle.None : DisplayStyle.Flex;
            _detailContent.style.display = hasItem ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasItem) return;

            var def = items[slotIndex].Definition;

            // 아이콘
            if (def.icon != null)
                _detailIcon.style.backgroundImage = new StyleBackground(def.icon);

            // 레어도
            _detailRarity.text        = RarityToString(def.rarity);
            _detailRarity.style.color = RarityColor(def.rarity);

            // 이름 / 설명
            _detailName.text        = def.displayName;
            _detailDescription.text = def.description;

            // 시너지 (최대 2개)
            int synCount = def.inscriptions != null
                ? Mathf.Min(def.inscriptions.Count, DetailSynCount)
                : 0;

            for (int i = 0; i < DetailSynCount; i++)
            {
                bool show = i < synCount;
                _detailSynCards[i].style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show) continue;

                var entry = def.inscriptions[i];
                var insc  = entry.inscription;

                if (insc.icon != null)
                    _detailSynIcons[i].style.backgroundImage = new StyleBackground(insc.icon);

                _detailSynNames[i].text  = insc.displayName;
                _detailSynCounts[i].text = BuildTierCountText(insc);
            }
        }

        // ── 시너지 패널 (왼쪽) ───────────────────────────────────────

        private void RefreshSynergyPanel()
        {
            if (_stat == null) { HideAllSynergySlots(); return; }

            var equippedItems = _stat.EquipmentService.EquippedItems;

            // 장착 아이템에서 각인 목록 수집 (중복 제거, 순서 유지)
            var seen     = new HashSet<string>();
            var inscList = new List<InscriptionDefinition>();

            foreach (var item in equippedItems)
            {
                if (item.Definition.inscriptions == null) continue;
                foreach (var entry in item.Definition.inscriptions)
                {
                    if (entry.inscription == null) continue;
                    if (seen.Add(entry.inscription.inscriptionId))
                        inscList.Add(entry.inscription);
                }
            }

            for (int i = 0; i < SynergySlotMax; i++)
            {
                if (i >= inscList.Count) { _synSlots[i].style.display = DisplayStyle.None; continue; }

                var insc  = inscList[i];
                int count = _stat.InscriptionService.GetCount(insc);

                // 1개 이상 활성화된 각인만 표시
                if (count <= 0) { _synSlots[i].style.display = DisplayStyle.None; continue; }

                _synSlots[i].style.display = DisplayStyle.Flex;

                if (insc.icon != null)
                    _synIcons[i].style.backgroundImage = new StyleBackground(insc.icon);

                _synCurCounts[i].text = count.ToString(); // 현재 개수 (큰 숫자)
                _synNames[i].text     = insc.displayName;
                _synCounts[i].text    = BuildTierCountText(insc);
            }

            // 슬롯 최대치 초과분 숨김
            for (int i = inscList.Count; i < SynergySlotMax; i++)
                _synSlots[i].style.display = DisplayStyle.None;
        }

        private void HideAllSynergySlots()
        {
            for (int i = 0; i < SynergySlotMax; i++)
                _synSlots[i].style.display = DisplayStyle.None;
        }

        // ── 유틸 ─────────────────────────────────────────────────────

        /// <summary>각인의 첫 두 티어 요구 개수를 "2 → 4" 형식으로 반환한다.</summary>
        private static string BuildTierCountText(InscriptionDefinition insc)
        {
            var tiers = insc.tiers;
            if (tiers == null || tiers.Count == 0) return "";
            if (tiers.Count == 1) return tiers[0].requiredCount.ToString();
            return $"{tiers[0].requiredCount} → {tiers[1].requiredCount}";
        }

        private static string RarityToString(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => "일반",
            ItemRarity.Rare      => "희귀",
            ItemRarity.Unique    => "고유",
            ItemRarity.Legendary => "전설",
            _                    => "일반"
        };

        private static StyleColor RarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new StyleColor(new Color(0.85f, 0.85f, 0.85f)),
            ItemRarity.Rare      => new StyleColor(new Color(0.40f, 0.62f, 1.00f)),
            ItemRarity.Unique    => new StyleColor(new Color(1.00f, 0.60f, 0.20f)),
            ItemRarity.Legendary => new StyleColor(new Color(1.00f, 0.85f, 0.10f)),
            _                    => new StyleColor(Color.white)
        };
    }
}
