using TMPro;
using UnityEngine;

namespace _2D_Roguelike
{
    /// <summary>
    /// Shop_Batch에 부착. 활성화 시 랜덤 아이템을 스폰하고 가격을 표시한다.
    /// </summary>
    public class ShopItemSpawner : MonoBehaviour
    {
        [Header("아이템 풀 (상점에 등장할 아이템 목록)")]
        [SerializeField] private ItemDefinition[] _itemPool;

        [Header("레퍼런스")]
        [SerializeField] private GameObject  _shopItemPickupPrefab;
        [SerializeField] private Transform   _itemSpawnPoint;
        [SerializeField] private TMP_Text    _priceText;

        private GameObject _spawnedItem;

        private void OnEnable()
        {
            SpawnItem();
        }

        private void OnDisable()
        {
            if (_spawnedItem != null)
            {
                Destroy(_spawnedItem);
                _spawnedItem = null;
            }

            if (_priceText != null)
                _priceText.text = string.Empty;
        }

        private void SpawnItem()
        {
            if (_itemPool == null || _itemPool.Length == 0) return;
            if (_shopItemPickupPrefab == null || _itemSpawnPoint == null) return;

            ItemDefinition picked = _itemPool[Random.Range(0, _itemPool.Length)];
            if (picked == null) return;

            _spawnedItem = Instantiate(_shopItemPickupPrefab, _itemSpawnPoint.position, Quaternion.identity, _itemSpawnPoint);
            var pickup = _spawnedItem.GetComponent<ShopItemPickup>();
            pickup?.Init(picked);

            if (_priceText != null)
                _priceText.text = picked.price.ToString();
        }
    }
}
