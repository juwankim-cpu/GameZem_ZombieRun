using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HPManager : MonoBehaviour
{
    [Header("HP 설정")]
    public float maxHP = 100f; // 최대 HP
    public float currentHP; // 현재 HP
    public float hpDecreaseRate = 1f; // HP 감소율 (초당 %)
    public float hpDecreaseInterval = 1f; // HP 감소 간격 (초)
    
    [Header("UI 설정")]
    public Slider hpSlider; // HP 슬라이더
    public Text hpText; // HP 텍스트
    public string hpTextFormat = "HP: {0:F0}/{1:F0}"; // HP 텍스트 형식
    
    [Header("게임오버 설정")]
    public GameObject gameOverPanel; // 게임오버 패널
    public string gameOverSceneName = "GameOver"; // 게임오버 씬 이름
    public bool useSceneTransition = false; // 씬 전환 사용 여부
    
    [Header("효과 설정")]
    public bool enableLowHPEffect = true; // 낮은 HP 효과
    public float lowHPThreshold = 20f; // 낮은 HP 임계값
    public Color lowHPColor = Color.red; // 낮은 HP 색상
    public Color normalHPColor = Color.white; // 일반 HP 색상
    
    private bool isGameOver = false;
    private bool isHPDecreasing = true;
    private Coroutine hpDecreaseCoroutine;
    private Image hpFillImage; // HP 슬라이더의 Fill 이미지
    
    void Start()
    {
        // 현재 HP를 최대 HP로 초기화
        currentHP = maxHP;
        
        // UI 컴포넌트 찾기
        if (hpSlider == null)
            hpSlider = GetComponentInChildren<Slider>();
            
        if (hpText == null)
            hpText = GetComponentInChildren<Text>();
        
        // HP 슬라이더 설정
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
            hpFillImage = hpSlider.fillRect.GetComponent<Image>();
        }
        
        // HP 감소 코루틴 시작
        if (isHPDecreasing)
        {
            hpDecreaseCoroutine = StartCoroutine(DecreaseHP());
        }
        
        // 초기 UI 업데이트
        UpdateHPUI();
    }

    void Update()
    {
        // 게임오버 상태가 아닐 때만 HP 감소
        if (!isGameOver && isHPDecreasing && hpDecreaseCoroutine == null)
        {
            hpDecreaseCoroutine = StartCoroutine(DecreaseHP());
        }
    }
    
    private IEnumerator DecreaseHP()
    {
        while (!isGameOver && isHPDecreasing && currentHP > 0)
        {
            yield return new WaitForSeconds(hpDecreaseInterval);
            
            if (!isGameOver)
            {
                // HP 감소 (1초에 1%)
                float decreaseAmount = maxHP * (hpDecreaseRate / 100f);
                currentHP = Mathf.Max(0, currentHP - decreaseAmount);
                
                // UI 업데이트
                UpdateHPUI();
                
                // 낮은 HP 효과
                if (enableLowHPEffect && currentHP <= lowHPThreshold)
                {
                    StartCoroutine(LowHPEffect());
                }
                
                // HP가 0이 되면 게임오버
                if (currentHP <= 0)
                {
                    GameOver();
                }
                
                Debug.Log($"HP 감소: {currentHP:F1}/{maxHP:F1} ({currentHP/maxHP*100:F1}%)");
            }
        }
    }
    
    private void UpdateHPUI()
    {
        // HP 슬라이더 업데이트
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }
        
        // HP 텍스트 업데이트
        if (hpText != null)
        {
            hpText.text = string.Format(hpTextFormat, currentHP, maxHP);
        }
    }
    
    private IEnumerator LowHPEffect()
    {
        if (hpFillImage != null)
        {
            Color originalColor = hpFillImage.color;
            
            while (currentHP <= lowHPThreshold && !isGameOver)
            {
                // 빨간색으로 깜빡이기
                hpFillImage.color = lowHPColor;
                yield return new WaitForSeconds(0.2f);
                
                hpFillImage.color = originalColor;
                yield return new WaitForSeconds(0.2f);
            }
            
            // 원래 색상으로 복원
            hpFillImage.color = originalColor;
        }
    }
    
    private void GameOver()
    {
        isGameOver = true;
        isHPDecreasing = false;
        
        // HP 감소 코루틴 중지
        if (hpDecreaseCoroutine != null)
        {
            StopCoroutine(hpDecreaseCoroutine);
            hpDecreaseCoroutine = null;
        }
        
        // 게임오버 패널 표시
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 게임오버 로그
        Debug.Log("게임오버! HP가 0이 되었습니다.");
        
        // 씬 전환 (옵션)
        if (useSceneTransition)
        {
            StartCoroutine(LoadGameOverScene());
        }
    }
    
    private IEnumerator LoadGameOverScene()
    {
        yield return new WaitForSeconds(2f); // 2초 대기
        
        // 씬 전환 (SceneManager 사용)
        #if UNITY_5_3_OR_NEWER
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        #else
        Application.LoadLevel(gameOverSceneName);
        #endif
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void HealHP(float amount)
    {
        if (!isGameOver)
        {
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            UpdateHPUI();
            Debug.Log($"HP 회복: +{amount:F1}, 현재 HP: {currentHP:F1}");
        }
    }
    
    public void DamageHP(float amount)
    {
        if (!isGameOver)
        {
            currentHP = Mathf.Max(0, currentHP - amount);
            UpdateHPUI();
            Debug.Log($"HP 피해: -{amount:F1}, 현재 HP: {currentHP:F1}");
            
            if (currentHP <= 0)
            {
                GameOver();
            }
        }
    }
    
    public void SetHPDecreasing(bool decreasing)
    {
        isHPDecreasing = decreasing;
        
        if (!decreasing && hpDecreaseCoroutine != null)
        {
            StopCoroutine(hpDecreaseCoroutine);
            hpDecreaseCoroutine = null;
        }
    }
    
    public void ResetHP()
    {
        currentHP = maxHP;
        isGameOver = false;
        isHPDecreasing = true;
        UpdateHPUI();
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        Debug.Log("HP가 리셋되었습니다.");
    }
    
    // 현재 HP 비율 반환 (0~1)
    public float GetHPPercentage()
    {
        return currentHP / maxHP;
    }
    
    // 게임오버 상태 확인
    public bool IsGameOver()
    {
        return isGameOver;
    }
}
