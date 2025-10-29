using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.cyborgAssets.inspectorButtonPro;
using Ami.BroAudio;

public class Tutorials : MonoBehaviour
{
    [Header("팝업 페이지들")]
    public List<Image> popups;

    [Header("네비게이션 이미지")]
    public Image leftArrow;
    public Image rightArrow;

    [Header("설정")]
    [SerializeField] private int currentPage = 0;
    [SerializeField] private bool showOnStart = false;

    [Header("브금 설정")]
    [SerializeField] private float tutorialMusicVolume = 0.3f; // 튜토리얼 중 브금 볼륨
    [SerializeField] private float musicFadeTime = 0.5f; // 브금 페이드 시간

    private int totalPages => popups != null ? popups.Count : 0;

    void Start()
    {
        // 초기화
        currentPage = 0;

        if (showOnStart)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    void Update()
    {
        // 튜토리얼이 활성화되어 있을 때만 키 입력 처리
        if (!gameObject.activeSelf) return;

        // 왼쪽 화살표 키
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPage();
        }

        // 오른쪽 화살표 키
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPage();
        }
    }

    /// <summary>
    /// 튜토리얼 표시
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        currentPage = 0;
        ShowPage(currentPage);

        // 브금 볼륨 줄이기
        BroAudio.SetVolume(BroAudioType.Music, tutorialMusicVolume, musicFadeTime);
    }

    /// <summary>
    /// 튜토리얼 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);

        // 브금 볼륨 원래대로
        BroAudio.SetVolume(BroAudioType.Music, 1f, musicFadeTime);
    }

    /// <summary>
    /// 튜토리얼 보이기/숨기기 토글
    /// </summary>
    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// 다음 페이지
    /// </summary>
    public void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            ShowPage(currentPage);
        }
    }

    /// <summary>
    /// 이전 페이지
    /// </summary>
    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowPage(currentPage);
        }
    }

    /// <summary>
    /// 특정 페이지 표시
    /// </summary>
    void ShowPage(int pageIndex)
    {
        if (popups == null || popups.Count == 0) return;

        // 모든 페이지 숨기기
        for (int i = 0; i < popups.Count; i++)
        {
            if (popups[i] != null)
            {
                popups[i].gameObject.SetActive(i == pageIndex);
            }
        }

        // 화살표 업데이트
        UpdateArrows();
    }

    /// <summary>
    /// 화살표 표시 업데이트
    /// </summary>
    void UpdateArrows()
    {
        // 첫 페이지: 오른쪽 화살표만
        // 마지막 페이지: 왼쪽 화살표만
        // 중간 페이지: 양쪽 다

        bool isFirstPage = currentPage == 0;
        bool isLastPage = currentPage == totalPages - 1;

        if (leftArrow != null)
            leftArrow.gameObject.SetActive(!isFirstPage);

        if (rightArrow != null)
            rightArrow.gameObject.SetActive(!isLastPage);
    }

    // ========== 테스트 버튼 ==========

    [ProButton]
    public void TestShow()
    {
        Show();
        Debug.Log("튜토리얼 표시");
    }

    [ProButton]
    public void TestHide()
    {
        Hide();
        Debug.Log("튜토리얼 숨김");
    }

    [ProButton]
    public void TestToggle()
    {
        Toggle();
        Debug.Log($"튜토리얼 토글: {(gameObject.activeSelf ? "표시" : "숨김")}");
    }

    [ProButton]
    public void TestNextPage()
    {
        NextPage();
        Debug.Log($"다음 페이지: {currentPage + 1}/{totalPages}");
    }

    [ProButton]
    public void TestPreviousPage()
    {
        PreviousPage();
        Debug.Log($"이전 페이지: {currentPage + 1}/{totalPages}");
    }

    [ProButton]
    public void TestPrintCurrentPage()
    {
        Debug.Log($"현재 페이지: {currentPage + 1}/{totalPages}");
    }
}
