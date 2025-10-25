using UnityEngine;

public class HPTestController : MonoBehaviour
{
    [Header("테스트 설정")]
    public HPManager hpManager; // HP 매니저 참조
    public KeyCode healKey = KeyCode.H; // HP 회복 키
    public KeyCode damageKey = KeyCode.J; // HP 피해 키
    public KeyCode resetKey = KeyCode.R; // HP 리셋 키
    public KeyCode toggleDecreaseKey = KeyCode.T; // HP 감소 토글 키
    
    [Header("테스트 값")]
    public float healAmount = 10f; // 회복량
    public float damageAmount = 20f; // 피해량
    
    void Start()
    {
        // HP 매니저가 설정되지 않았다면 자동으로 찾기
        if (hpManager == null)
        {
            hpManager = FindObjectOfType<HPManager>();
        }
        
        if (hpManager == null)
        {
            Debug.LogWarning("HPManager를 찾을 수 없습니다!");
        }
    }
    
    void Update()
    {
        if (hpManager == null) return;
        
        // HP 회복
        if (Input.GetKeyDown(healKey))
        {
            hpManager.HealHP(healAmount);
        }
        
        // HP 피해
        if (Input.GetKeyDown(damageKey))
        {
            hpManager.DamageHP(damageAmount);
        }
        
        // HP 리셋
        if (Input.GetKeyDown(resetKey))
        {
            hpManager.ResetHP();
        }
        
        // HP 감소 토글
        if (Input.GetKeyDown(toggleDecreaseKey))
        {
            bool currentState = hpManager.GetComponent<HPManager>().enabled;
            hpManager.SetHPDecreasing(!currentState);
            Debug.Log($"HP 감소: {(currentState ? "비활성화" : "활성화")}");
        }
    }
    
    void OnGUI()
    {
        if (hpManager == null) return;
        
        // 화면에 테스트 키 안내 표시
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("HP 테스트 컨트롤:");
        GUILayout.Label($"{healKey} - HP 회복 (+{healAmount})");
        GUILayout.Label($"{damageKey} - HP 피해 (-{damageAmount})");
        GUILayout.Label($"{resetKey} - HP 리셋");
        GUILayout.Label($"{toggleDecreaseKey} - HP 감소 토글");
        GUILayout.Space(10);
        GUILayout.Label($"현재 HP: {hpManager.GetHPPercentage() * 100:F1}%");
        GUILayout.Label($"게임오버: {(hpManager.IsGameOver() ? "예" : "아니오")}");
        GUILayout.EndArea();
    }
}
