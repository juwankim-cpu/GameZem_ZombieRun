using UnityEngine;

public class ItemScript : MonoBehaviour
{
    public enum ItemType
    {
        HP,
        Buff
    }

    public ItemType itemType;
    public ItemManager itemManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 충돌했는지 확인
        if (collision.CompareTag("Player"))
        {
            // 아이템 타입에 따라 효과 적용
            switch (itemType)
            {
                case ItemType.HP:
                    ApplyHPEffect();
                    break;
                case ItemType.Buff:
                    ApplyBuffEffect();
                    break;
            }

            // 아이템 파괴
            Destroy(gameObject);
        }
    }

    private void ApplyHPEffect()
    {
        if (itemManager != null)
        {
            // HP 회복 로직 (GameManager 또는 StatusBar를 통해 적용)
            Debug.Log($"HP 아이템 획득! 회복량: {itemManager.hpRecoveryAmount}");

            // 실제 HP 적용은 GameManager의 currentHealth를 사용
            var gameManager = FindObjectOfType<ZombieRun.Adohi.GameSystem.GameManager>();
            if (gameManager != null && gameManager.currentHealth != null)
            {
                gameManager.currentHealth.Value = Mathf.Min(
                    gameManager.currentHealth.Value + itemManager.hpRecoveryAmount,
                    100f // 최대 HP
                );
            }
        }
    }

    private void ApplyBuffEffect()
    {
        if (itemManager != null)
        {
            // 버프 획득 로직
            Debug.Log($"버프 아이템 획득! 버프량: {itemManager.buffGainAmount}");

            // 실제 버프 적용은 GameManager의 currentBoost를 사용
            var gameManager = FindObjectOfType<ZombieRun.Adohi.GameSystem.GameManager>();
            if (gameManager != null && gameManager.currentBoost != null)
            {
                gameManager.currentBoost.Value = Mathf.Min(
                    gameManager.currentBoost.Value + itemManager.buffGainAmount,
                    100f // 최대 Boost
                );
            }
        }
    }
}

