# 🎮 Zombie Run - 온라인 랭킹 시스템 설정 가이드

## 📋 개요

RankingSystem에 온라인 랭킹 기능이 추가되었습니다. 이제 구글 시트를 이용해서 전세계 플레이어들과 점수를 공유할 수 있습니다!

### 주요 기능
- ✅ 에디터에서 온라인/오프라인 모드 토글 가능
- ✅ 네트워크 문제 시 자동으로 오프라인 모드로 전환
- ✅ 구글 시트를 통한 실시간 랭킹 공유
- ✅ 기존 오프라인 기능 완전 호환

---

## 🚀 구글 시트 설정 방법

### 1단계: 구글 스프레드시트 생성

1. [Google Sheets](https://sheets.google.com)에 접속
2. 새 스프레드시트 생성
3. 원하는 이름으로 저장 (예: "ZombieRun_Rankings")

### 2단계: Apps Script 설정

1. 스프레드시트에서 **확장 프로그램 > Apps Script** 메뉴 클릭
2. 기본 코드를 모두 삭제
3. 프로젝트에 포함된 `GoogleAppsScript_RankingSystem.js` 파일의 내용을 복사해서 붙여넣기
4. 프로젝트 이름 설정 (예: "ZombieRun Ranking API")
5. **저장** 버튼 클릭 (💾 아이콘)

### 3단계: 웹 앱으로 배포

1. Apps Script 편집기에서 **배포 > 새 배포** 클릭
2. **유형 선택** 옆의 톱니바퀴 아이콘 클릭 > **웹 앱** 선택
3. 다음 설정 입력:
   - **새 설명**: "ZombieRun Ranking System v1.0" (선택사항)
   - **다음 사용자로 실행**: **나**
   - **액세스 권한**: **모든 사용자**
4. **배포** 버튼 클릭
5. 권한 승인 팝업이 나타나면:
   - **권한 검토** 클릭
   - 본인의 Google 계정 선택
   - **고급** 클릭
   - **[프로젝트 이름](으)로 이동** 클릭
   - **허용** 클릭
6. 배포 완료 후 **웹 앱 URL** 복사 (이 URL이 중요합니다!)

### 4단계: Unity 설정

1. Unity 에디터에서 `RankingSystem` 컴포넌트 선택
2. **Online Ranking Settings** 섹션에서:
   - ✅ **Use Online Ranking** 체크
   - **Google Sheet Web App Url** 필드에 복사한 URL 붙여넣기
   - **Request Timeout**: 10초 (기본값, 필요시 조정)
   - ✅ **Auto Fallback To Offline**: 체크 (권장)

---

## ⚙️ Unity 설정 상세

### Inspector 설정 항목

#### Online Ranking Settings

| 항목 | 설명 | 권장값 |
|------|------|--------|
| **Use Online Ranking** | 온라인 랭킹 사용 여부 | ✅ 체크 |
| **Google Sheet Web App Url** | Apps Script 배포 URL | (3단계에서 복사한 URL) |
| **Request Timeout** | 네트워크 요청 타임아웃 (초) | 10 |
| **Auto Fallback To Offline** | 실패 시 자동 오프라인 전환 | ✅ 체크 (권장) |

---

## 🧪 테스트 방법

### 오프라인 모드 테스트
1. `Use Online Ranking` 체크 해제
2. 게임 플레이
3. 점수가 PlayerPrefs에 로컬 저장됨

### 온라인 모드 테스트
1. `Use Online Ranking` 체크
2. `Google Sheet Web App Url` 입력
3. 게임 플레이
4. 콘솔에서 다음 로그 확인:
   - `[RankingSystem] 온라인 랭킹 모드로 시작합니다.`
   - `[RankingSystem] 온라인 랭킹 로드 성공: X개 항목`
   - `[RankingSystem] 온라인 랭킹 저장 성공`
5. 구글 시트로 돌아가서 **Rankings** 시트 확인
   - 자동으로 시트가 생성되고 랭킹이 표시됨
   - 1등은 금색, 2등은 은색, 3등은 동색으로 강조됨

### 네트워크 오류 테스트
1. 잘못된 URL 입력 또는 인터넷 연결 끊기
2. 게임 플레이
3. 콘솔에서 자동 폴백 로그 확인:
   - `[RankingSystem] 온라인 랭킹 로드 실패. 오프라인 모드로 전환합니다.`
4. 점수가 로컬에 저장됨

---

## 🔧 문제 해결

### "온라인 랭킹 로드 실패" 오류

**원인:**
- 잘못된 Web App URL
- 인터넷 연결 문제
- Apps Script 권한 문제

**해결 방법:**
1. URL이 정확한지 확인 (끝에 `/exec`가 있어야 함)
2. Apps Script 배포 설정에서 "액세스 권한"이 "모든 사용자"인지 확인
3. 새로 배포해서 새 URL 받기
4. 인터넷 연결 확인

### "권한이 거부되었습니다" 오류

**해결 방법:**
1. Apps Script에서 **배포 관리** 클릭
2. 현재 배포 항목의 수정 아이콘 클릭
3. "다음 사용자로 실행"이 **나**로 설정되어 있는지 확인
4. "액세스 권한"이 **모든 사용자**로 설정되어 있는지 확인
5. 새 버전으로 배포

### 데이터가 저장되지 않음

**해결 방법:**
1. Apps Script 편집기에서 **실행 > printCurrentRankings** 실행
2. **보기 > 로그** 에서 현재 데이터 확인
3. 문제가 있으면 **실행 > resetRankings** 실행
4. Unity에서 다시 테스트

---

## 📊 구글 시트 확인

온라인 랭킹이 활성화되면 스프레드시트에 자동으로 **Rankings** 시트가 생성됩니다.

### Rankings 시트 구조

| 순위 | 플레이어 | 점수 | 날짜/시간 |
|------|---------|------|-----------|
| 1 | Player | 1500미터 | 2025-10-28 12:34:56 |
| 2 | Player | 1200미터 | 2025-10-28 12:30:00 |
| 3 | Player | 1000미터 | 2025-10-28 12:25:00 |

- **1등**: 금색 배경 (🥇)
- **2등**: 은색 배경 (🥈)
- **3등**: 동색 배경 (🥉)

---

## 🎯 고급 사용법

### 플레이어 이름 변경
```csharp
// RankingSystem.cs의 Inspector에서
defaultPlayerName = "YourName";
```

### 랭킹 개수 변경
```csharp
// RankingSystem.cs 코드에서
private const int MAX_RANKINGS = 10; // 5에서 10으로 변경
```

### Apps Script 커스터마이징
- `GoogleAppsScript_RankingSystem.js` 파일 수정
- 데이터 구조 변경 가능
- 추가 검증 로직 추가 가능
- 이메일 알림 추가 가능

---

## 📝 참고사항

### 보안
- Apps Script는 Google 계정으로 보호됨
- URL을 알아도 악의적인 데이터 삽입은 어려움
- 필요시 Apps Script에 추가 검증 로직 구현 가능

### 성능
- 구글 시트는 초당 약 60회 요청 제한
- 대규모 동시 접속자에는 부적합
- 소규모 게임이나 테스트용으로 적합

### 비용
- 구글 시트 무료 (일반 사용 범위 내)
- Apps Script 무료 (일일 할당량 내)
- 추가 비용 없음

### 대안
- 대규모 게임: Firebase, PlayFab, 자체 서버 사용 권장
- 이 시스템은 프로토타입/소규모 게임에 최적

---

## 📞 지원

문제가 있으면:
1. Unity 콘솔 로그 확인
2. Apps Script 로그 확인 (보기 > 로그)
3. 이 가이드의 문제 해결 섹션 참고

---

## ✅ 체크리스트

설정 완료 확인:
- [ ] 구글 스프레드시트 생성
- [ ] Apps Script 코드 복사 및 저장
- [ ] 웹 앱으로 배포
- [ ] 권한 승인 완료
- [ ] URL 복사
- [ ] Unity에 URL 입력
- [ ] Use Online Ranking 체크
- [ ] 테스트 플레이
- [ ] 구글 시트에서 데이터 확인

모두 완료되면 온라인 랭킹 시스템 사용 가능! 🎉

