/**
 * Unity Zombie Run - 온라인 랭킹 시스템
 * Google Apps Script Web App
 * 
 * 설정 방법:
 * 1. Google Sheets에서 새 스프레드시트 생성
 * 2. 확장 프로그램 > Apps Script 메뉴 클릭
 * 3. 이 코드를 복사해서 붙여넣기
 * 4. 배포 > 새 배포 클릭
 * 5. 유형 선택 > 웹 앱
 * 6. 다음 사용자로 실행: 나
 * 7. 액세스 권한: 모든 사용자
 * 8. 배포 클릭
 * 9. 생성된 웹 앱 URL을 Unity의 RankingSystem에 입력
 */

// 스크립트 속성 키
const RANKING_DATA_KEY = 'ZOMBIE_RUN_RANKINGS';

/**
 * GET 요청 처리 - 랭킹 데이터 조회
 */
function doGet(e) {
  try {
    const action = e.parameter.action;
    
    if (action === 'get') {
      // 랭킹 데이터 가져오기
      const rankingData = loadRankingData();
      
      return ContentService
        .createTextOutput(JSON.stringify(rankingData))
        .setMimeType(ContentService.MimeType.JSON);
    }
    
    return ContentService
      .createTextOutput(JSON.stringify({ error: 'Invalid action' }))
      .setMimeType(ContentService.MimeType.JSON);
      
  } catch (error) {
    Logger.log('Error in doGet: ' + error);
    return ContentService
      .createTextOutput(JSON.stringify({ error: error.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}

/**
 * POST 요청 처리 - 랭킹 데이터 저장
 */
function doPost(e) {
  try {
    const action = e.parameter.action;
    
    if (action === 'save') {
      // 랭킹 데이터 저장
      const data = e.parameter.data;
      
      if (!data) {
        return ContentService
          .createTextOutput(JSON.stringify({ success: false, error: 'No data provided' }))
          .setMimeType(ContentService.MimeType.JSON);
      }
      
      saveRankingData(data);
      
      // 스프레드시트에도 저장 (선택사항 - 백업 및 가시성)
      updateSpreadsheet(JSON.parse(data));
      
      return ContentService
        .createTextOutput(JSON.stringify({ success: true }))
        .setMimeType(ContentService.MimeType.JSON);
    }
    
    return ContentService
      .createTextOutput(JSON.stringify({ success: false, error: 'Invalid action' }))
      .setMimeType(ContentService.MimeType.JSON);
      
  } catch (error) {
    Logger.log('Error in doPost: ' + error);
    return ContentService
      .createTextOutput(JSON.stringify({ success: false, error: error.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}

/**
 * 랭킹 데이터 로드
 */
function loadRankingData() {
  const scriptProperties = PropertiesService.getScriptProperties();
  const data = scriptProperties.getProperty(RANKING_DATA_KEY);
  
  if (!data) {
    // 데이터가 없으면 빈 랭킹 반환
    return { rankings: [] };
  }
  
  return JSON.parse(data);
}

/**
 * 랭킹 데이터 저장
 */
function saveRankingData(jsonData) {
  const scriptProperties = PropertiesService.getScriptProperties();
  scriptProperties.setProperty(RANKING_DATA_KEY, jsonData);
}

/**
 * 스프레드시트에 랭킹 데이터 업데이트 (선택사항)
 * 시각적으로 랭킹을 확인하고 싶을 때 유용
 */
function updateSpreadsheet(rankingData) {
  try {
    const sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName('Rankings');
    
    // 시트가 없으면 생성
    let targetSheet = sheet;
    if (!targetSheet) {
      targetSheet = SpreadsheetApp.getActiveSpreadsheet().insertSheet('Rankings');
      
      // 헤더 추가
      targetSheet.getRange('A1:D1').setValues([['순위', '플레이어', '점수', '날짜/시간']]);
      targetSheet.getRange('A1:D1').setFontWeight('bold');
      targetSheet.getRange('A1:D1').setBackground('#4285f4');
      targetSheet.getRange('A1:D1').setFontColor('#ffffff');
    }
    
    // 기존 데이터 삭제 (헤더 제외)
    if (targetSheet.getLastRow() > 1) {
      targetSheet.getRange(2, 1, targetSheet.getLastRow() - 1, 4).clearContent();
    }
    
    // 새 데이터 추가
    if (rankingData.rankings && rankingData.rankings.length > 0) {
      const rows = rankingData.rankings.map((entry, index) => [
        index + 1,
        entry.playerName,
        Math.floor(entry.score) + '미터',
        entry.dateTime
      ]);
      
      targetSheet.getRange(2, 1, rows.length, 4).setValues(rows);
      
      // 스타일 적용
      targetSheet.autoResizeColumns(1, 4);
      
      // 1등 강조
      if (rows.length > 0) {
        targetSheet.getRange(2, 1, 1, 4).setBackground('#ffd700').setFontWeight('bold');
      }
      
      // 2등 강조
      if (rows.length > 1) {
        targetSheet.getRange(3, 1, 1, 4).setBackground('#c0c0c0');
      }
      
      // 3등 강조
      if (rows.length > 2) {
        targetSheet.getRange(4, 1, 1, 4).setBackground('#cd7f32');
      }
    }
    
    Logger.log('Spreadsheet updated successfully');
    
  } catch (error) {
    Logger.log('Error updating spreadsheet: ' + error);
    // 스프레드시트 업데이트 실패해도 계속 진행
  }
}

/**
 * 테스트용 함수 - 랭킹 데이터 초기화
 */
function resetRankings() {
  const scriptProperties = PropertiesService.getScriptProperties();
  scriptProperties.deleteProperty(RANKING_DATA_KEY);
  
  // 스프레드시트도 초기화
  const sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName('Rankings');
  if (sheet) {
    if (sheet.getLastRow() > 1) {
      sheet.getRange(2, 1, sheet.getLastRow() - 1, 4).clearContent();
    }
  }
  
  Logger.log('Rankings reset successfully');
}

/**
 * 테스트용 함수 - 현재 저장된 랭킹 출력
 */
function printCurrentRankings() {
  const data = loadRankingData();
  Logger.log('Current Rankings:');
  Logger.log(JSON.stringify(data, null, 2));
}

