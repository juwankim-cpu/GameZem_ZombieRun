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
    [Header("버프 게이지 설정")]
    public float maxBuff = 100f;
    public float currentBuff;

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

    public CharactorMove charactorMove;

    
    
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
        // isHPDecreasing이 true이고 코루틴이 null일 때 코루틴을 다시 시작합니다.
        // FeverMode가 끝났을 때 (currentBuff == 0) isHPDecreasing이 true로 설정되므로 여기서 재시작됩니다.
        if (!isGameOver && isHPDecreasing && hpDecreaseCoroutine == null)
        {
            hpDecreaseCoroutine = StartCoroutine(DecreaseHP());
        }

        if (currentBuff >= maxBuff) // maxBuff와 같거나 커지면 (아이템 획득 등으로 maxBuff를 초과할 수도 있으므로 >= 사용)
        {
            // 최대치 초과 방지
            currentBuff = maxBuff; 
            FeverMode();
        }
    }
    
    public void FeverMode()
    {
        // 1. DecreaseHP 코루틴 일시 정지 및 HP 감소 비활성화
        if (hpDecreaseCoroutine != null)
        {
            StopCoroutine(hpDecreaseCoroutine);
            hpDecreaseCoroutine = null;
        }
        isHPDecreasing = false;
        
        // 현재 버프가 0이 아니면 버프 효과 적용
        if (currentBuff > 0)
        {
            // 버프 감소
            currentBuff -= 10f;
            
            // 이동 속도 증가 (임시 로직: charactorMove가 null이 아닐 때만)
            if (charactorMove != null)
            {
                charactorMove.moveSpeed *= 5;
            }
        }

        // 버프가 0 이하로 떨어지면 Fever Mode 종료 및 HP 감소 재시작
        if (currentBuff <= 0f)
        {
            currentBuff = 0f; // 0 이하로 내려가지 않도록 보정

            // 이동 속도 원상 복구 (임시 로직)
            if (charactorMove != null)
            {
                // 이전 로직을 기반으로 원상 복구 로직이 필요합니다. 
                // 여기서는 FeverMode 시작 전의 기본 속도를 0.15f라고 가정하고 복구합니다.
                charactorMove.moveSpeed = 0.15f; 
            }

            // 2. HP 감소 재개
            isHPDecreasing = true;
            Debug.Log("Fever Mode 종료. HP 감소 재개.");

            // Update()에서 hpDecreaseCoroutine == null 이므로 코루틴이 자동으로 재시작됩니다.
        }
        else
        {
            // 버프가 남아있을 때의 로직
            Debug.Log($"Fever Mode 활성화 중. 현재 Buff: {currentBuff:F1}");
        }
    }
    
    private IEnumerator DecreaseHP()
    {
        while (!isGameOver && isHPDecreasing && currentHP > 0)
        {
            yield return new WaitForSeconds(hpDecreaseInterval);
            
            if (!isGameOver && isHPDecreasing) // FeverMode 중에는 isHPDecreasing이 false가 되어 여기서 멈춥니다.
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

        // 코루틴이 while 루프를 빠져나오면 Coroutine 참조를 null로 설정합니다.
        // HP가 0이 되거나 isHPDecreasing이 false가 되었을 때 발생합니다.
        hpDecreaseCoroutine = null; 
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
                
                // LowHPEffect가 중단될 때 원래 색상 복원을 위해 originalColor 대신 normalHPColor를 사용하는 것이 좋습니다.
                hpFillImage.color = normalHPColor; 
                yield return new WaitForSeconds(0.2f);
            }
            
            // 원래 색상으로 복원
            hpFillImage.color = normalHPColor; // 정상 HP 색상으로 복원
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
        else if (decreasing && hpDecreaseCoroutine == null)
        {
            // 감소를 재개할 때 코루틴이 null이면 다시 시작
            hpDecreaseCoroutine = StartCoroutine(DecreaseHP());
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
        
        // 코루틴이 중지 상태일 수 있으므로 다시 시작
        if (hpDecreaseCoroutine == null)
        {
             hpDecreaseCoroutine = StartCoroutine(DecreaseHP());
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
    
    public void GainBuff(float amount)
    {
        if (!isGameOver)
        {
            currentBuff = Mathf.Min(maxBuff, currentBuff + amount);
            
            Debug.Log($"버프 획득: +{amount:F1}, 현재 Buff: {currentBuff:F1}");
        }
    }
}