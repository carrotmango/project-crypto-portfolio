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

        // 1. 타이틀 및 라벨 텍스트 설정 (현지화 적용)
        if (isFullyCirculated || hasNoEvent) {
            panelTitleText.text = LocalizationManager.GetText("LBL_INFO_CIRCULATION");
            if (labelDate != null) labelDate.text = LocalizationManager.GetText("LBL_DATE_UNLOCK");
            if (labelAmount != null) labelAmount.text = LocalizationManager.GetText("LBL_AMOUNT_QTY");
        } else if (isHalving) {
            panelTitleText.text = LocalizationManager.GetText("LBL_INFO_HALVING");
            if (labelDate != null) labelDate.text = LocalizationManager.GetText("LBL_DATE_HALVING");
            if (labelAmount != null) labelAmount.text = LocalizationManager.GetText("LBL_AMOUNT_REWARD");
        } else {
            panelTitleText.text = LocalizationManager.GetText("LBL_INFO_UNLOCK");
            if (labelDate != null) labelDate.text = LocalizationManager.GetText("LBL_DATE_UNLOCK");
            if (labelAmount != null) labelAmount.text = LocalizationManager.GetText("LBL_AMOUNT_QTY");
        }
        if (labelValue != null) labelValue.text = LocalizationManager.GetText("LBL_VALUE_LABEL");

        // 2. 좌측 유통량 게이지 및 텍스트 갱신
        if (circulatingFillImage != null)
            circulatingFillImage.fillAmount = (float)(currentSupply / maxSupply);

        if (circulatingRatioText != null)
            circulatingRatioText.text = $"{FormatAmount(currentSupply)} / {FormatAmount(maxSupply)}";

        if (totalValueText != null) {
            double currentVal = currentSupply * currentCoin.CurrentPrice;
            double maxVal = maxSupply * currentCoin.CurrentPrice;
            totalValueText.text = $"{FormatCurrency(currentVal)} / {FormatCurrency(maxVal)}";
        }

        // 3. 우측 리스트 예외 처리
        if (isFullyCirculated || hasNoEvent) {
            if (unlockDateTexts[0] != null) unlockDateTexts[0].text = "-";
            if (unlockAmountTexts[0] != null)
                unlockAmountTexts[0].text = isFullyCirculated ? LocalizationManager.GetText("LBL_FULLY_CIRCULATED") : LocalizationManager.GetText("LBL_NO_SCHEDULE");
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

        // 4. 날짜 계산 (실제 게임 시간 연동 및 날짜 분산)
        int cycleMonths = isHalving ? meta.HalvingCycleMonths : meta.UnlockCycleMonths;
        DateTime currentDate = CoinManager.Instance.CurrentDateTime;

        int dateSeed = currentCoin.Symbol.GetHashCode();
        System.Random dateRand = new System.Random(dateSeed);
        int fixedReleaseDay = dateRand.Next(1, 29);

        DateTime startDate = new DateTime(2020, 1, fixedReleaseDay);
        DateTime firstEventDate = startDate;

        while (firstEventDate.Date < currentDate.Date) {
            firstEventDate = firstEventDate.AddMonths(cycleMonths);
        }

        double simulatedSupply = currentSupply;
        string daysLeftFormat = LocalizationManager.GetText("LBL_DAYS_LEFT"); // "{0}일 뒤" 포맷

        for (int i = 0; i < 2; i++) {
            if (i >= unlockDateTexts.Length) break;

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

                double baseReward = 50.0;
                double currentReward = baseReward / Math.Pow(2, halvingCount - 1);
                double nextReward = currentReward / 2.0;

                if (unlockDateTexts[i] != null) unlockDateTexts[i].text = targetDate.ToString("MM.dd.yyyy");
                if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = $"{currentReward}\n->\n{nextReward}";
                if (unlockValueTexts[i] != null) unlockValueTexts[i].text = "-";
                if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = string.Format(daysLeftFormat, daysLeft);
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

                int amountSeed = currentCoin.Symbol.GetHashCode() + targetDate.DayOfYear + targetDate.Year;
                UnityEngine.Random.InitState(amountSeed);

                if (remainingRatio <= 0.20f && remainingRatio > 0) {
                    unlockRatio = remainingRatio;
                } else {
                    unlockRatio = UnityEngine.Random.Range(0.05f, 0.15f);
                }
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

                if (unlockRatio > remainingRatio) unlockRatio = remainingRatio;

                double unlockAmount = maxSupply * unlockRatio;
                double unlockPercent = unlockRatio * 100.0;
                double unlockValue = unlockAmount * currentCoin.CurrentPrice;

                if (unlockDateTexts[i] != null) unlockDateTexts[i].text = targetDate.ToString("MM.dd.yyyy");
                if (unlockAmountTexts[i] != null) unlockAmountTexts[i].text = $"{FormatAmount(unlockAmount)}\n({unlockPercent:F1}%)";
                if (unlockValueTexts[i] != null) unlockValueTexts[i].text = FormatCurrency(unlockValue);
                if (unlockDaysLeftTexts[i] != null) unlockDaysLeftTexts[i].text = string.Format(daysLeftFormat, daysLeft);

                simulatedSupply += unlockAmount;
            }
        }
    }

    // ==========================================
    // 통합 포맷팅 엔진 (수량 전용 & 가치 전용)
    // ==========================================

    // [수정] 단순 수량(개수) 표기 (1096.50만, 21.13억 또는 10.97M, 2.11B)
    private string FormatAmount(double amount) {
        string formatStyle = LocalizationManager.GetText("FORMAT_STYLE");

        // USD 모드이거나 현재 언어가 영문일 때는 서양식(K/M/B/T) 단위 사용
        if (useUsdMode || formatStyle == "EN") {
            if (amount >= 1_000_000_000_000d) return $"{(amount / 1_000_000_000_000d):0.##}T";
            if (amount >= 1_000_000_000d) return $"{(amount / 1_000_000_000d):0.##}B";
            if (amount >= 1_000_000d) return $"{(amount / 1_000_000d):0.##}M";
            if (amount >= 1_000d) return $"{(amount / 1_000d):0.##}K";
            return $"{amount:N0}";
        } else {
            // KRW 모드 + 한국어일 때는 동양식(만/억/조) 단위 사용
            if (amount >= 1_0000_0000_0000d) return $"{(amount / 1_0000_0000_0000d):0.##}조";
            if (amount >= 1_0000_0000d) return $"{(amount / 1_0000_0000d):0.##}억";
            if (amount >= 1_0000d) return $"{(amount / 1_0000d):0.##}만";
            return $"{amount:N0}";
        }
    }

    // 화폐 가치 표기 (기존 유지, USD/KRW 환율 적용)
    private string FormatCurrency(double krwAmount) {
        string formatStyle = LocalizationManager.GetText("FORMAT_STYLE");
        string unit = LocalizationManager.GetText("UNIT_CURRENCY"); // "원" 또는 " Won"

        if (useUsdMode) {
            // 1. 달러 모드: 환율 적용 후 무조건 $ 기호와 K/M/B/T
            double usdAmount = krwAmount / GlobalEconomyManager.UsdToKrw;
            if (usdAmount >= 1_000_000_000_000) return $"${(usdAmount / 1_000_000_000_000d):F2}T";
            if (usdAmount >= 1_000_000_000) return $"${(usdAmount / 1_000_000_000d):F2}B";
            if (usdAmount >= 1_000_000) return $"${(usdAmount / 1_000_000d):F2}M";
            if (usdAmount >= 1_000) return $"${(usdAmount / 1_000d):F2}K";
            return $"${usdAmount:N2}";
        } else if (formatStyle == "EN") {
            // 2. 영문 + 원화 모드: 외국인 헷갈림 방지를 위해 ₩ 기호 추가 및 서양식 단위
            if (krwAmount >= 1_000_000_000_000) return $"₩{(krwAmount / 1_000_000_000_000d):F2}T";
            if (krwAmount >= 1_000_000_000) return $"₩{(krwAmount / 1_000_000_000d):F2}B";
            if (krwAmount >= 1_000_000) return $"₩{(krwAmount / 1_000_000d):F2}M";
            if (krwAmount >= 1_000) return $"₩{(krwAmount / 1_000d):F2}K";
            return $"₩{krwAmount:N0}";
        } else {
            // 3. 한글 + 원화 모드: 한국인에게 편한 조/억/만 단위 적용
            return FormatAmount(krwAmount) + unit;
        }
    }
}