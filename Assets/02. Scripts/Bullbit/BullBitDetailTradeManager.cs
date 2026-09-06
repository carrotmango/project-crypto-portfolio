using UnityEngine;
using TMPro;

public class BullBitDetailTradeManager : MonoBehaviour {
    [Header("Detail Panel & Chart")]
    public GameObject detailPanelRoot;    // 상세 패널(차트 포함) 부모 객체
    public SpotChartRenderer spotChartRenderer; // 상세 패널 안에 있는 차트 렌더러

    public TradePanelController tradePanelController;
    private CoinData lastOpenedCoin;

    void Update() {
        // 패널이 켜져 있는 상태에서만 작동하도록 체크
        if (detailPanelRoot != null && detailPanelRoot.activeSelf) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                CloseDetailPanel();
            }
        }
    }

    public bool HasLastCoin => lastOpenedCoin != null;

    public void OpenDetailPanel(CoinData coin) {
        if (coin == null) 
            return;

        lastOpenedCoin = coin;

        // 1. 상세 패널 껍데기 켜기
        if (detailPanelRoot != null) {
            detailPanelRoot.SetActive(true);
        }

        // 2. 차트 렌더러 세팅
        if (spotChartRenderer != null) {
            spotChartRenderer.SelectCoin(coin);
        }

        if (tradePanelController != null) {
            tradePanelController.OpenPanel(coin);
        } else {
            Debug.LogError("행님! BullBitDetailTradeManager에 TradePanelController가 인스펙터에 연결 안 됐습니다!");
        }
    }

    public void ReopenLastCoin() {
        if (lastOpenedCoin != null) {
            OpenDetailPanel(lastOpenedCoin);
        }
    }

    public void CloseDetailPanel() {
        if (detailPanelRoot != null) {
            detailPanelRoot.SetActive(false);
        }
    }
}