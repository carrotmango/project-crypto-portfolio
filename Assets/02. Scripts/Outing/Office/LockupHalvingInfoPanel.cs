using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LockupHalvingInfoPanel : MonoBehaviour {
    [Header("Main Panel")]
    public GameObject panelGroup;
    public TextMeshProUGUI panelTitleText;

    [Header("Left: Supply Info")]
    public Image circulatingFillImage;
    public TextMeshProUGUI circulatingRatioText;
    public TextMeshProUGUI totalValueText;

    [Header("Right: 3 Cycles (Index 0~2)")]
    public TextMeshProUGUI[] unlockDateTexts;
    public TextMeshProUGUI[] unlockAmountTexts;
    public TextMeshProUGUI[] unlockValueTexts;
    public TextMeshProUGUI[] unlockDaysLeftTexts;

    [Header("Top: Labels")]
    public TextMeshProUGUI labelDate;
    public TextMeshProUGUI labelAmount;
    public TextMeshProUGUI labelValue;

    private bool useUsdMode = false; // USD 모드 캐싱 변수

    public void Setup(CoinData currentCoin, CoinMetaData meta, bool isUsd = false) {
        useUsdMode = isUsd; // 전달받은 토글 상태 저장
        panelGroup.SetActive(true);

        double currentSupply = currentCoin.CirculatingSupply;
        double maxSupply = currentCoin.MaxSupply;

        bool isFullyCirculated = currentSupply >= maxSupply;
        bool hasNoEvent = (meta.HalvingCycleMonths == 0 && meta.UnlockCycleMonths == 0);
        bool isHalving = meta.HalvingCycleMonths > 0;

        // 1. 타이틀 및 라벨 텍스트 설정
        if (isFullyCirculated || hasNoEvent) {
            panelTitleText.text = "유통량 정보";
            if (labelDate != null) labelDate.text = "해제 날짜";
            if (labelAmount != null) labelAmount.text = "수량";
        } else if (isHalving) {
            panelTitleText.text = "반감기 정보";
            if (labelDate != null) labelDate.text = "반감기 날짜";
            if (labelAmount != null) labelAmount.text = "블록 보상";
        } else {
            panelTitleText.text = "락업 해제 정보";
            if (labelDate != null) labelDate.text = "해제 날짜";
            if (labelAmount != null) labelAmount.text = "수량";
        }
        if (labelValue != null) labelValue.text = "가치";

        // 2. 좌측 유통량 게이지 및 텍스트 갱신
        if (circulatingFillImage != null)
            circulatingFillImage.fillAmount = (float)(currentSupply / maxSupply);

        // [수정] 수량 표기 (1096.50만 / 21.13억 또는 10.97M / 2.11B 형태로 변경)
        if (circulatingRatioText != null)
            circulatingRatioText.text = $"{FormatAmount(currentSupply)} / {FormatAmount(maxSupply)}";

        if (totalValueText != null) {
            double currentVal = currentSupply * currentCoin.CurrentPrice;
            double maxVal = maxSupply * currentCoin.CurrentPrice;
            // USD 모드 반영
            totalValueText.text = $"{FormatCurrency(currentVal)} / {FormatCurrency(maxVal)}";
        }

        // 3. 우측 리스트 예외 처리
        if (isFullyCirculated || hasNoEvent) {
            if (unlockDateTexts[0] != null) unlockDateTexts[0].text = "-";
            if (unlockAmountTexts[0] != null) unlockAmountTexts[0].text = isFullyCirculated ? "100%\n유통 완료" : "예정된\n일정 없음";
            if (unlockValueTexts[0] != null) unlockValueTexts[0].text = "-";
            if (unlockDaysLeftTexts[0] != null) unlockDaysLeftTexts[0].text = "-";

            for (int i = 1; i < unlockDateTexts.Length; i++) {
                if (unlockDateTexts[i] != null) unlockDateTexts[i].text = "";
                if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = "";
                if (unlockValueTexts[i] != null) unlockValueTexts[i].text = "";
                if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = "";
            }
            return;
        }

        // 4. 날짜 계산 (실제 게임 시간 연동)
        // 4. 날짜 계산 (실제 게임 시간 연동 및 날짜 분산)
        int cycleMonths = isHalving ? meta.HalvingCycleMonths : meta.UnlockCycleMonths;
        DateTime currentDate = CoinManager.Instance.CurrentDateTime;

        // [개선] 코인 고유 심볼을 시드로 사용하여 코인마다 다른 해제 '일(Day)' 결정
        // GetHashCode는 고정된 값을 반환하므로 코인별로 날짜가 고정됩니다.
        int dateSeed = currentCoin.Symbol.GetHashCode();
        System.Random dateRand = new System.Random(dateSeed);

        // 2월 및 짧은 달을 고려하여 안전하게 1~28일 사이로 랜덤 지정
        int fixedReleaseDay = dateRand.Next(1, 29);

        // 기준 시작일을 해당 코인의 고유 날짜로 설정
        DateTime startDate = new DateTime(2020, 1, fixedReleaseDay);
        DateTime firstEventDate = startDate;

        // 현재 게임 시간보다 미래에 있는 첫 번째 이벤트 날짜 찾기
        while (firstEventDate.Date < currentDate.Date) {
            firstEventDate = firstEventDate.AddMonths(cycleMonths);
        }

        double simulatedSupply = currentSupply;

        for (int i = 0; i < 2; i++) {
            if (i >= unlockDateTexts.Length) break;

            // 해당 코인의 주기에 맞춰 다음 날짜 계산
            DateTime targetDate = firstEventDate.AddMonths(cycleMonths * i);
            TimeSpan diff = targetDate - currentDate;
            int daysLeft = diff.Days;

            if (isHalving) {
                // [반감기 모드] 
                int halvingCount = 0;
                DateTime tempDate = startDate.AddMonths(meta.HalvingCycleMonths);
                while (tempDate <= targetDate) {
                    halvingCount++;
                    tempDate = tempDate.AddMonths(meta.HalvingCycleMonths);
                }

                double baseReward = 50.0; // 비트코인 기준 예시
                double currentReward = baseReward / Math.Pow(2, halvingCount - 1);
                double nextReward = currentReward / 2.0;

                if (unlockDateTexts[i] != null) unlockDateTexts[i].text = targetDate.ToString("MM.dd.yyyy");
                if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = $"{currentReward}\n->\n{nextReward}";
                if (unlockValueTexts[i] != null) unlockValueTexts[i].text = "-";
                if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = $"{daysLeft}일 뒤";
            } else {
                // [락업 해제 모드]
                double remainingSupply = maxSupply - simulatedSupply;

                if (remainingSupply <= 0) {
                    if (unlockDateTexts[i] != null) unlockDateTexts[i].text = "-";
                    if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = "-";
                    if (unlockValueTexts[i] != null) unlockValueTexts[i].text = "-";
                    if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = "-";
                    continue;
                }

                float remainingRatio = (float)(remainingSupply / maxSupply);
                float unlockRatio = 0f;

                // 해당 타겟 날짜에 맞춰 고정된 랜덤 물량 비율 생성
                int amountSeed = currentCoin.Symbol.GetHashCode() + targetDate.DayOfYear + targetDate.Year;
                UnityEngine.Random.InitState(amountSeed);

                if (remainingRatio <= 0.20f && remainingRatio > 0) {
                    unlockRatio = remainingRatio;
                } else {
                    unlockRatio = UnityEngine.Random.Range(0.05f, 0.15f);
                }
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks); // 랜덤 상태 복구

                if (unlockRatio > remainingRatio) unlockRatio = remainingRatio;

                double unlockAmount = maxSupply * unlockRatio;
                double unlockPercent = unlockRatio * 100.0;
                double unlockValue = unlockAmount * currentCoin.CurrentPrice;

                if (unlockDateTexts[i] != null) unlockDateTexts[i].text = targetDate.ToString("MM.dd.yyyy");
                if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = $"{FormatAmount(unlockAmount)}\n({unlockPercent:F1}%)";
                if (unlockValueTexts[i] != null) unlockValueTexts[i].text = FormatCurrency(unlockValue);
                if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = $"{daysLeft}일 뒤";

                simulatedSupply += unlockAmount;
            }
        }
    }

    // ==========================================
    // 통합 포맷팅 엔진 (수량 전용 & 가치 전용)
    // ==========================================

        // [수정] 단순 수량(개수) 표기 (1096.50만, 21.13억 또는 10.97M, 2.11B)
    private string FormatAmount(double amount) {
        if (useUsdMode) {
            // USD 모드일 때 수량 표기 (개수 자체는 달러 환율 영향을 받지 않으므로 단위만 바꿈)
            if (amount >= 1_000_000_000_000d) return $"{(amount / 1_000_000_000_000d):0.##}T";
            if (amount >= 1_000_000_000d) return $"{(amount / 1_000_000_000d):0.##}B";
            if (amount >= 1_000_000d) return $"{(amount / 1_000_000d):0.##}M";
            if (amount >= 1_000d) return $"{(amount / 1_000d):0.##}K";
            return $"{amount:N0}";
        } else {
            // KRW 모드일 때 수량 표기
            if (amount >= 1_0000_0000_0000d) return $"{(amount / 1_0000_0000_0000d):0.##}조";
            if (amount >= 1_0000_0000d) return $"{(amount / 1_0000_0000d):0.##}억";
            if (amount >= 1_0000d) return $"{(amount / 1_0000d):0.##}만";
            return $"{amount:N0}";
        }
    }

    // 화폐 가치 표기 (기존 유지, USD/KRW 환율 적용)
    private string FormatCurrency(double krwAmount) {
        if (useUsdMode) {
            double usdAmount = krwAmount / GlobalEconomyManager.UsdToKrw;
            if (usdAmount >= 1_000_000_000_000) return $"${(usdAmount / 1_000_000_000_000d):F2}T";
            if (usdAmount >= 1_000_000_000) return $"${(usdAmount / 1_000_000_000d):F2}B";
            if (usdAmount >= 1_000_000) return $"${(usdAmount / 1_000_000d):F2}M";
            if (usdAmount >= 1_000) return $"${(usdAmount / 1_000d):F2}K";
            return $"${usdAmount:N2}";
        } else {
            // 원화 가치일 때도 FormatAmount를 활용해 조, 억, 만 단위를 빌려 씁니다.
            return FormatAmount(krwAmount) + " 원";
        }
    }
}