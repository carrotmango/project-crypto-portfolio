using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour {
    public static TutorialManager Instance;

    [Header("UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI dialogueText;
    public Button dialoguePanelButton;
    public Button yesButton;
    public Button noButton;

    [Header("튜토리얼 대상 버튼")]
    public Button appButton;
    public Button satoshiDimButton;
    public Button withdrawButton;
    public Button TransferButton;
    public Button bullbitButton;
    [HideInInspector] public Button btcBuyButton;
    [HideInInspector] public Button ethBuyButton;
    [HideInInspector] public Button bullbitPortfolioRowButton;
    public Button portfolioButton;
    public Button transferMaxButton;

    [HideInInspector] public Button btcRowButton;

    [Header("상세 패널 튜토리얼용")]
    public Button detailTenPercentButton; //  10% 버튼
    public Button detailBuyButton;        //  매수(Buy) 버튼
    public GameObject detailTenPercentDim; //  10% 영역만 뚫어놓은 투명 딤
    public GameObject detailBuyDim;        //  매수 영역만 뚫어놓은 투명 딤

    [Header("업무 패널 튜토리얼용")]
    public Button companyButton;      // 
    public GameObject companyDim;

    [Header("매도 튜토리얼용")]
    public Button detailSellTabButton;        //  매도 탭 여는 버튼
    public Button detailHundredPercentButton; //  100% 버튼
    public Button detailExecuteSellButton;    //  최종 매도하기 버튼

    public GameObject detailSellTabDim;        //  매도 탭 딤
    public GameObject detailHundredPercentDim; //  100% 딤
    public GameObject detailExecuteSellDim;    //  최종 매도하기 딤

    [Header("업무 패널 좌측 메뉴 튜토리얼용")]
    public GameObject depositDim;
    public GameObject skillDim;
    public GameObject questDim;
    public GameObject researchDim;
    public GameObject timeDim;

    [HideInInspector] public List<TextMeshProUGUI> portfolioCoinSymbolTexts = new List<TextMeshProUGUI>();
    [HideInInspector] public List<TextMeshProUGUI> portfolioCoinLabelTexts = new List<TextMeshProUGUI>();
    private Dictionary<Button, Coroutine> buttonHighlightCoroutines = new Dictionary<Button, Coroutine>();
    private Dictionary<Button, Color> buttonOriginalColors = new Dictionary<Button, Color>(); //  


    [Header("옵션")]
    public Toggle tutorialToggle;   // LobbyManager에서 켜져 있으면 실행

    [Header("설정")]
    public float textSpeed = 0.03f;

    //패널 이미지
    public Image blockPanelImage;

    [Header("Dim Targets")]
    public GameObject bullbitDim;
    public GameObject satoshiDim;
    public GameObject satoshiDimWithButton;
    public GameObject perpDim;
    public GameObject spotButtonDim;
    public GameObject partTimeDim;
    public GameObject estateDim;
    public GameObject xbirdDim;
    public GameObject gambleDim;
    public GameObject withdrawButtonDim;
    public GameObject transferDim;
    public GameObject BullbitButtonDim;
    public GameObject btcBuyDim;
    public GameObject ethBuyDim;
    public GameObject fifyDim;
    public GameObject AllDim;
    public GameObject ConfirmDim;
    public GameObject appDim;
    public GameObject portfolioDim;
    public GameObject porfolioInnerDim;
    public GameObject perpAndEstateDim;
    public GameObject bankExplainDim;
    public GameObject AmountDim;
    public GameObject BorderDim;
    public GameObject BorderDim2;
    public GameObject actualDetailPanel;
    public GameObject OfficeBorder;

    [Header("UI 표시용")]
    public Image arrowImage;
    private Coroutine arrowCoroutine;
    private Dictionary<TextMeshProUGUI, Coroutine> textHighlightCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    private bool timePausedOnce = false;

    [Header("Typing Sound")]
    [SerializeField] private AudioClip typingClip;


    public bool IsTutorialPlaying { get; private set; } = false;


    void Update() {
        if (IsTutorialPlaying) {
            Input.ResetInputAxes();
        }
    }


    private IEnumerator ArrowBlink(Image img) {
        Color c = img.color;
        while (true) {
            float a = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            img.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
    }

    private void ShowArrow() {
        if (arrowImage == null) return;
        arrowImage.gameObject.SetActive(true);
        if (arrowCoroutine != null) StopCoroutine(arrowCoroutine);
        arrowCoroutine = StartCoroutine(ArrowBlink(arrowImage));
    }

    private void HideArrow() {
        if (arrowImage == null) return;
        arrowImage.gameObject.SetActive(false);
        if (arrowCoroutine != null) StopCoroutine(arrowCoroutine);
        arrowCoroutine = null;
    }


    private DialogueNode currentNode;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool textCompleted = false;

    private enum State { Idle, Dialogue, WaitingForAction, Finished }
    private State state = State.Idle;

    private bool tutorialFinished = false;

    private void SetBlockPanelAlpha(float alpha01) {
        if (blockPanelImage == null) return;
        var c = blockPanelImage.color;
        c.a = Mathf.Clamp01(alpha01);
        blockPanelImage.color = c;
    }

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        tutorialPanel.SetActive(false);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        appDim.SetActive(false);
    }

    public void StartTutorial() {

        if (tutorialToggle != null && !tutorialToggle.isOn) {
            Time.timeScale = 1f;
            return;
        }

        IsTutorialPlaying = true;

        //
        if (!timePausedOnce) {
            Time.timeScale = 0f;
            timePausedOnce = true;
            CoinManager.Instance.SetTimeSpeed(TimeSpeed.Paused);
        }

        tutorialPanel.SetActive(true);
        state = State.Dialogue;

        // 버튼 이벤트 연결
        dialoguePanelButton.onClick.RemoveAllListeners();
        dialoguePanelButton.onClick.AddListener(OnDialogueClick);

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(OnYesClick);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(OnNoClick);

        // 노드 연결
        SetupNodes();

        ShowDialogue();

        satoshiDimButton.onClick.RemoveAllListeners();
        satoshiDimButton.onClick.AddListener(() => {
            // 하이라이트 해제
            ApplyHighlight(HighlightTarget.None);
            SetBlockPanelAlpha(0f);

            // 다음 대화로 진행
            currentNode = currentNode.next;
            ShowDialogue();
        });

        withdrawButton.onClick.RemoveAllListeners();
        withdrawButton.onClick.AddListener(() => {
            ApplyHighlight(HighlightTarget.None);
            SetBlockPanelAlpha(0f);

            currentNode = currentNode.next;
            ShowDialogue();
        });

    }

    private void SetupNodes() {
        string playerName = "Player";
        var status = FindAnyObjectByType<StatusPanelController>();
        if (status != null && status.playerNameText != null)
            playerName = status.playerNameText.text;

        // 노드 생성 (다국어 매니저에서 텍스트 불러오기)
        DialogueNode n1 = new DialogueNode { text = string.Format(LocalizationManager.GetText("TUTORIAL_N1"), playerName) };
        DialogueNode n2 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N2") };
        DialogueNode n3 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N3") };
        DialogueNode n4 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N4"), isChoice = true };

        DialogueNode y1 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_Y1") };
        DialogueNode y2 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_Y2") };
        DialogueNode y3 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_Y3") };

        DialogueNode n5 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N5") };
        DialogueNode n6 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N6") };
        DialogueNode n7 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N7") };
        DialogueNode n8 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N8") };
        DialogueNode n9 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N9"), action = DialogueAction.HighlightAppButton };
        DialogueNode n10 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N10") };
        DialogueNode n11 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N11"), highlightTarget = HighlightTarget.Bullbit };
        DialogueNode n12 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N12"), highlightTarget = HighlightTarget.Bullbit };
        DialogueNode n13 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N13"), highlightTarget = HighlightTarget.spotButton };
        DialogueNode n14 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N14"), highlightTarget = HighlightTarget.spotButton };
        DialogueNode n15 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N15"), highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n16 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N16"), highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n17 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N17"), highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n18 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N18"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n19 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N19"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n20 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N20"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n21 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N21"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n22 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N22"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n23 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N23"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n24 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N24"), highlightTarget = HighlightTarget.perpAndEstateDim };
        DialogueNode n25 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N25"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n26 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N26"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n27 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N27"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n28 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N28"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n29 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N29"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n30 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N30"), highlightTarget = HighlightTarget.Xbird };
        DialogueNode n31 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N31") };
        DialogueNode n32 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N32"), highlightTarget = HighlightTarget.SatoshiDimWithButton, action = DialogueAction.WaitForSatoshiDepositButton };
        DialogueNode n33 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N33") };
        DialogueNode n34 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N34"), highlightTarget = HighlightTarget.bankExplainDim };
        DialogueNode n35 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N35"), highlightTarget = HighlightTarget.bankExplainDim };
        DialogueNode n36 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N36") };
        DialogueNode n37 = new DialogueNode { text = string.Format(LocalizationManager.GetText("TUTORIAL_N37"), playerName) };
        DialogueNode n38 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N38"), highlightTarget = HighlightTarget.WithdrawButton, action = DialogueAction.WaitForWithdrawButton };
        DialogueNode n39 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N39") };
        DialogueNode n40 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N40") };
        DialogueNode n41 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N41") };
        DialogueNode n42 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N42") };
        DialogueNode n43 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N43") };
        DialogueNode n44 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N44"), action = DialogueAction.WaitForTransfer, highlightTarget = HighlightTarget.TransferDim };
        DialogueNode n45 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N45") };
        DialogueNode n46 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N46"), action = DialogueAction.HighlightBullbitButton };
        DialogueNode n47 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N47"), highlightTarget = HighlightTarget.AmountDim };
        DialogueNode n48 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N48"), highlightTarget = HighlightTarget.AmountDim };
        DialogueNode n49 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N49") };
        DialogueNode n50 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N50") };
        DialogueNode n51 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N51") };
        DialogueNode n52 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N52"), highlightTarget = HighlightTarget.BorderDim };
        DialogueNode n53 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N53"), highlightTarget = HighlightTarget.BorderDim2, action = DialogueAction.WaitForBTCDetailPanel };
        DialogueNode n54 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N54") };
        DialogueNode n55 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N55") };
        DialogueNode n56 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N56") };
        DialogueNode n57 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N57") };
        DialogueNode n58 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N58") };
        DialogueNode n59 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N59"), action = DialogueAction.WaitForDetailBuy };
        DialogueNode n60 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N60") };
        DialogueNode n61 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N61") };
        DialogueNode n62 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N62"), action = DialogueAction.WaitForDetailSell };
        DialogueNode n63 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N63") };
        DialogueNode n64 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N64") };
        DialogueNode n65 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N65"), action = DialogueAction.HighlightCompanyButton };
        DialogueNode n66 = new DialogueNode { text = string.Format(LocalizationManager.GetText("TUTORIAL_N66"), playerName) };
        DialogueNode n67 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N67"), highlightTarget = HighlightTarget.OfficeBorder };
        DialogueNode n68 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N68"), highlightTarget = HighlightTarget.OfficeBorder };
        DialogueNode n69 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N69"), highlightTarget = HighlightTarget.None };

        DialogueNode n70 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N70"), highlightTarget = HighlightTarget.depositDim };
        DialogueNode n71 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N71"), highlightTarget = HighlightTarget.depositDim };
        DialogueNode n72 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N72"), highlightTarget = HighlightTarget.depositDim };
        DialogueNode n73 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N73"), highlightTarget = HighlightTarget.skillDim };
        DialogueNode n74 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N74"), highlightTarget = HighlightTarget.skillDim };
        DialogueNode n75 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N75"), highlightTarget = HighlightTarget.questDim };
        DialogueNode n76 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N76"), highlightTarget = HighlightTarget.questDim };
        DialogueNode n77 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N77"), highlightTarget = HighlightTarget.researchDim };
        DialogueNode n78 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N78"), highlightTarget = HighlightTarget.researchDim };
        DialogueNode n79 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N79"), highlightTarget = HighlightTarget.None };
        DialogueNode n80 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N80") };
        DialogueNode n81 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N81") };
        DialogueNode n82 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N82"), highlightTarget = HighlightTarget.timeDim };
        DialogueNode n83 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N83") , highlightTarget = HighlightTarget.timeDim };
        DialogueNode n84 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N84"), highlightTarget = HighlightTarget.timeDim };
        DialogueNode n85 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N85") };
        DialogueNode n86 = new DialogueNode { text = LocalizationManager.GetText("TUTORIAL_N86") };

        // 기본 연결
        n1.next = n2;
        n2.next = n3;
        n3.next = n4;
        n4.yesNext = y1;
        n4.noNext = n5;

        y1.next = y2;
        y2.next = y3;
        y3.next = n7;

        n5.next = n6;
        n6.next = n7;

        n7.next = n8;
        n8.next = n9;
        n9.next = n10;
        n10.next = n11;
        n11.next = n12;
        n12.next = n13;
        n13.next = n14;
        n14.next = n15;
        n15.next = n16;
        n16.next = n17;
        n17.next = n18;
        n18.next = n19;
        n19.next = n20;
        n20.next = n21;
        n21.next = n22;
        n22.next = n23;
        n23.next = n24;
        n24.next = n25;
        n25.next = n26;
        n26.next = n27;
        n27.next = n28;
        n28.next = n29;
        n29.next = n30;
        n30.next = n31;
        n31.next = n32;
        n32.next = n33;
        n33.next = n34;
        n34.next = n35;
        n35.next = n36;
        n36.next = n37;
        n37.next = n38;
        n38.next = n39;
        n39.next = n40;
        n40.next = n41;
        n41.next = n42;
        n42.next = n43;
        n43.next = n44;
        n44.next = n45;
        n45.next = n46;
        n46.next = n47;
        n47.next = n48;
        n48.next = n49;
        n49.next = n50;
        n50.next = n51;
        n51.next = n52;
        n52.next = n53;
        n53.next = n54;
        n54.next = n55;
        n55.next = n56;
        n56.next = n57;
        n57.next = n58;
        n58.next = n59;
        n59.next = n60;
        n60.next = n61;
        n61.next = n62;
        n62.next = n63;
        n63.next = n64;
        n64.next = n65;
        n65.next = n66;
        n66.next = n67;
        n67.next = n68;
        n68.next = n69;
        n69.next = n70;
        n70.next = n71;
        n71.next = n72;
        n72.next = n73;
        n73.next = n74;
        n74.next = n75;
        n75.next = n76;
        n76.next = n77;
        n77.next = n78;
        n78.next = n79;
        n79.next = n80;
        n80.next = n81;
        n81.next = n82;
        n82.next = n83;
        n83.next = n84;
        n84.next = n85;
        n85.next = n86;
        n86.next = null;

        currentNode = n1;
    }

    public void ApplyHighlight(HighlightTarget target) {
        // 전체 Dim 꺼두기
        if (bullbitDim != null) bullbitDim.SetActive(false);
        if (satoshiDim != null) satoshiDim.SetActive(false);
        if (perpDim != null) perpDim.SetActive(false);
        if (spotButtonDim != null) spotButtonDim.SetActive(false);
        if (satoshiDimWithButton != null) satoshiDimWithButton.SetActive(false);
        if (partTimeDim != null) partTimeDim.SetActive(false);
        if (estateDim != null) estateDim.SetActive(false);
        if (xbirdDim != null) xbirdDim.SetActive(false);
        if (gambleDim != null) gambleDim.SetActive(false);
        if (withdrawButtonDim != null) withdrawButtonDim.SetActive(false);
        if (transferDim != null) transferDim.SetActive(false);
        if (perpAndEstateDim != null) perpAndEstateDim.SetActive(false);
        if (bankExplainDim != null) bankExplainDim.SetActive(false);
        if (AmountDim != null) AmountDim.SetActive(false);
        if (BorderDim != null) BorderDim.SetActive(false);
        if (BorderDim2 != null) BorderDim2.SetActive(false);
        if (OfficeBorder != null) OfficeBorder.SetActive(false); 
        if (depositDim != null) depositDim.SetActive(false);
        if (skillDim != null) skillDim.SetActive(false);
        if (questDim != null) questDim.SetActive(false);
        if (researchDim != null) researchDim.SetActive(false);
        if (timeDim != null) timeDim.SetActive(false);

        switch (target) {
            case HighlightTarget.Bullbit: bullbitDim.SetActive(true); break;
            case HighlightTarget.Satoshi: satoshiDim.SetActive(true); break;
            case HighlightTarget.perpDim: perpDim.SetActive(true); break;
            case HighlightTarget.spotButton: spotButtonDim.SetActive(true); break;
            case HighlightTarget.SatoshiDimWithButton: satoshiDimWithButton.SetActive(true); break;
            case HighlightTarget.PartTimeJob: partTimeDim.SetActive(true); break;
            case HighlightTarget.Estate: estateDim.SetActive(true); break;
            case HighlightTarget.Xbird: xbirdDim.SetActive(true); break;
            case HighlightTarget.Gamble: gambleDim.SetActive(true); break;
            case HighlightTarget.WithdrawButton: withdrawButtonDim.SetActive(true); break;
            case HighlightTarget.TransferDim: transferDim.SetActive(true); break;
            case HighlightTarget.perpAndEstateDim: perpAndEstateDim.SetActive(true); break;
            case HighlightTarget.bankExplainDim: bankExplainDim.SetActive(true); break;
            case HighlightTarget.AmountDim: AmountDim.SetActive(true); break;
            case HighlightTarget.BorderDim: BorderDim.SetActive(true); break;
            case HighlightTarget.BorderDim2: BorderDim2.SetActive(true); break;
            case HighlightTarget.OfficeBorder: OfficeBorder.SetActive(true); break;
            case HighlightTarget.depositDim: if (depositDim != null) depositDim.SetActive(true); break;
            case HighlightTarget.skillDim: if (skillDim != null) skillDim.SetActive(true); break;
            case HighlightTarget.questDim: if (questDim != null) questDim.SetActive(true); break;
            case HighlightTarget.researchDim: if (researchDim != null) researchDim.SetActive(true); break;
            case HighlightTarget.timeDim: if(timeDim != null) timeDim.SetActive(true); break;
        }
    }


    private void ShowDialogue() {

        if (currentNode == null) {
            EndTutorial();   // 마지막에 도달했으면 튜토리얼 종료 처리
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.text));

        if (currentNode.action == DialogueAction.WaitForWithdrawButton) {
            return;
        }

        if (currentNode.blockPanelAlpha >= 0f) {
            SetBlockPanelAlpha(currentNode.blockPanelAlpha);
        }
    }

    // 나중에 Typing Text Player꺼이식하기
    private IEnumerator TypeText(string message) {
        dialogueText.text = "";
        isTyping = true;
        textCompleted = false;

        if (currentNode == null) yield break;

        foreach (char c in message) {
            dialogueText.text += c;

            if (!char.IsWhiteSpace(c) && typingClip != null) {
                SfxPlayer.Instance?.Play(typingClip);
            }

            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;
        textCompleted = true;

        if (currentNode.isChoice) {
            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);
        }

        if (currentNode.highlightTarget != HighlightTarget.None) {
            ApplyHighlight(currentNode.highlightTarget);
        } else {
            ApplyHighlight(HighlightTarget.None);
        }

        if (currentNode.blockPanelAlpha >= 0f) {
            SetBlockPanelAlpha(currentNode.blockPanelAlpha);
        }

 
        if (currentNode.action == DialogueAction.WaitForBTCDetailPanel) {
            tutorialPanel.SetActive(false);
            BorderDim.SetActive(false);
            BorderDim2.SetActive(true);
            StartCoroutine(CheckDetailPanelOpened());
            yield break;
        }

        ShowArrow();
    }
    public void OnDialogueClick() {
        HideArrow();
        if (state != State.Dialogue) return;
        if (tutorialFinished) return;

        if (currentNode.action == DialogueAction.WaitForSatoshiDepositButton) {
            return;
        }

        if (currentNode.action == DialogueAction.WaitForWithdrawButton) {
            return;
        }

        if (currentNode.action == DialogueAction.WaitForTransfer) {
            tutorialPanel.SetActive(false);
            transferDim.SetActive(true); // 딤 켜기

            // 1단계: '전액' 버튼 깜빡임 시작
            HighlightButton(transferMaxButton);

            transferMaxButton.onClick.AddListener(() => {

                // 전액 버튼을 누르면 멈추고
                StopHighlight(transferMaxButton);

                // 2단계: '이체' 버튼 깜빡임 시작
                HighlightButton(TransferButton);

                TransferButton.onClick.AddListener(() => {

                    // 이체 버튼을 누르면 깜빡임 멈추고 딤 해제
                    StopHighlight(TransferButton);
                    transferDim.SetActive(false);

                    // 튜토리얼 다음 단계로 진행!
                    if (!tutorialFinished) {
                        tutorialPanel.SetActive(true);
                        currentNode = currentNode.next;
                        ShowDialogue();
                    }
                });
            });
            return;
        }

        if (currentNode.action == DialogueAction.WaitForBTCDetailPanel && !tutorialFinished) {

            if (isTyping) {
                StopCoroutine(typingCoroutine);
                isTyping = false;
                textCompleted = true;

                tutorialPanel.SetActive(false); // 대화창 즉시 삭제
                BorderDim.SetActive(false);     // n52의 딤 삭제
                BorderDim2.SetActive(true);     // n53의 새 딤 켜기

                StartCoroutine(CheckDetailPanelOpened());
            }
            return;
        }

        if (currentNode.action == DialogueAction.WaitForDetailBuy && !tutorialFinished) {

            if (isTyping) {
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentNode.text;
                isTyping = false;
                textCompleted = true;
                ShowArrow();
                return;
            }

            if (textCompleted) {
                tutorialPanel.SetActive(false);
                detailTenPercentDim.SetActive(true);
                HighlightButton(detailTenPercentButton);

                detailTenPercentButton.onClick.AddListener(() => {
            
                    if (tutorialFinished) return;

                    StopHighlight(detailTenPercentButton);
                    detailTenPercentDim.SetActive(false);

                    detailBuyDim.SetActive(true);
                    HighlightButton(detailBuyButton);

                    detailBuyButton.onClick.AddListener(() => {
                     
                        if (tutorialFinished) return;

                        StopHighlight(detailBuyButton, true);
                        detailBuyDim.SetActive(false);

                        if (!tutorialFinished) {
                            tutorialPanel.SetActive(true);
                            currentNode = currentNode.next;
                            ShowDialogue();
                        }
                    });
                });
            }
            return;
        }

        if (currentNode.action == DialogueAction.WaitForDetailSell && !tutorialFinished) {

            if (isTyping) {
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentNode.text;
                isTyping = false;
                textCompleted = true;
                ShowArrow();
                return;
            }

            if (textCompleted) {
                tutorialPanel.SetActive(false);
                detailSellTabDim.SetActive(true);
                HighlightButton(detailSellTabButton);

                detailSellTabButton.onClick.AddListener(() => {
     
                    if (tutorialFinished) return;

                    StopHighlight(detailSellTabButton);

                    ColorUtility.TryParseHtmlString("#C86464", out Color sellRed);
                    detailSellTabButton.GetComponent<Image>().color = sellRed;

                    detailSellTabDim.SetActive(false);
                    detailHundredPercentDim.SetActive(true);

                    HighlightButton(detailHundredPercentButton);

                    detailHundredPercentButton.onClick.AddListener(() => {
        
                        if (tutorialFinished) return;

                        StopHighlight(detailHundredPercentButton);
                        detailHundredPercentDim.SetActive(false);

                        detailExecuteSellDim.SetActive(true);

                        HighlightButton(detailExecuteSellButton);

                        detailExecuteSellButton.onClick.AddListener(() => {
          
                            if (tutorialFinished) return;

                            StopHighlight(detailExecuteSellButton, true);
                            detailExecuteSellDim.SetActive(false);

                            if (!tutorialFinished) {
                                tutorialPanel.SetActive(true);
                                currentNode = currentNode.next;
                                ShowDialogue();
                            }
                        });
                    });
                });
            }
            return;
        }

        if (currentNode.action == DialogueAction.HighlightCompanyButton && !tutorialFinished) {

            // 1. 타이핑 중 클릭 시 스킵 처리
            if (isTyping) {
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentNode.text;
                isTyping = false;
                textCompleted = true;
                ShowArrow();
                return;
            }

            // 2. 타이핑 완료 후 1스텝 시작
            if (textCompleted) {
                tutorialPanel.SetActive(false); // 대화창 끄기
                companyDim.SetActive(true);     // 💡 업무 버튼 딤 켜기

                // 깜빡임 시작
                HighlightButton(companyButton);

                companyButton.onClick.AddListener(() => {
                    // 💡 [방어막] 튜토리얼 끝났으면 눈치껏 빠지기
                    if (tutorialFinished) return;

                    // 깜빡임 멈추고 딤 끄기 (색상 원래대로)
                    StopHighlight(companyButton);
                    companyDim.SetActive(false);

                    // 튜토리얼 다음 단계("이곳이 업무 패널이에요~")로 진행
                    if (!tutorialFinished) {
                        tutorialPanel.SetActive(true);
                        currentNode = currentNode.next;
                        ShowDialogue();
                    }
                });
            }
            return;
        }

        if (isTyping) {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentNode.text;
            isTyping = false;
            textCompleted = true;

            // 스킵했을 때도 버튼 보이게
            if (currentNode.isChoice) {
                yesButton.gameObject.SetActive(true);
                noButton.gameObject.SetActive(true);

            }
            // 스킵했을 때도 하이라이트 적용
            if (currentNode.highlightTarget != HighlightTarget.None) {
                ApplyHighlight(currentNode.highlightTarget);
            } else {
                // 혹시 필요하면 DIM 전부 끄기
                ApplyHighlight(HighlightTarget.None);
            }

            ShowArrow();
            return;
        }


        if (!textCompleted) return;
        if (currentNode.isChoice) return;

        // 특수 액션 처리
        if (currentNode.action == DialogueAction.HighlightAppButton) {
            tutorialPanel.SetActive(false);
            appDim.SetActive(true);

            HighlightButton(appButton);
            appButton.onClick.RemoveAllListeners();
            appButton.onClick.AddListener(() => {
                StopHighlight(appButton);
                appDim.SetActive(false);

                // 튜토리얼이 아직 안 끝났을 때만 패널 다시 켬
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
            return;
        }

        if (currentNode.action == DialogueAction.HighlightBullbitButton) {
            tutorialPanel.SetActive(false);

            //쉴드걸기
            BullbitButtonDim.SetActive(true);
            HighlightButton(bullbitButton);
            bullbitButton.onClick.AddListener(() => {
                StopHighlight(bullbitButton);
                BullbitButtonDim.SetActive(false);

                // 튜토리얼이 아직 안 끝났을 때만 패널 다시 켬
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
            return;
        }

        if (currentNode.action == DialogueAction.HighlightBitcoinBuy && !tutorialFinished) {
            Time.timeScale = 0f; // 항상 정지 상태 유지
            CoinManager.Instance.SetTimeSpeed(TimeSpeed.Paused);

            tutorialPanel.SetActive(false);
            btcBuyDim.SetActive(true);
            HighlightButton(btcBuyButton);

            btcBuyButton.onClick.AddListener(() => {
                StopHighlight(btcBuyButton);
                btcBuyDim.SetActive(false);

                Time.timeScale = 0f; // 버튼 클릭 후에도 다시 멈춤
                StartCoroutine(WaitForBuyPanelAndGuide());
            });
            return;
        }


        if (currentNode.action == DialogueAction.HighlightEthBuy && !tutorialFinished) {
            Time.timeScale = 0f;
            CoinManager.Instance.SetTimeSpeed(TimeSpeed.Paused);

            tutorialPanel.SetActive(false);
            ethBuyDim.SetActive(true);
            HighlightButton(ethBuyButton);

            ethBuyButton.onClick.AddListener(() => {
                StopHighlight(ethBuyButton);
                ethBuyDim.SetActive(false);

                Time.timeScale = 0f;
                StartCoroutine(WaitForBuyPanelAndGuide_Eth());
            });
            return;
        }


        if (currentNode.action == DialogueAction.HighlightPortfolioButton) {
            tutorialPanel.SetActive(false);
            portfolioDim.SetActive(true);

            HighlightButton(portfolioButton);
            portfolioButton.onClick.RemoveAllListeners();
            portfolioButton.onClick.AddListener(() => {
                StopHighlight(portfolioButton);
                portfolioDim.SetActive(false);

                // 튜토리얼이 아직 안 끝났을 때만 패널 다시 켬
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
            return;
        }

        if (currentNode.action == DialogueAction.WaitForClickBullPort) {
            tutorialPanel.SetActive(false);
            porfolioInnerDim.SetActive(true);

            bullbitPortfolioRowButton.onClick.AddListener(() => {

                porfolioInnerDim.SetActive(false);

                // 튜토리얼이 아직 안 끝났을 때만 패널 다시 켬
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
            return;
        }
        if (currentNode.action == DialogueAction.HighlightSymbolLabel_ON) {
            HighlightSymbolLabel_ON();
        } else if (currentNode.action == DialogueAction.HighlightSymbolLabel_OFF) {
            HighlightSymbolLabel_OFF();
        }

        // 일반 진행
        currentNode = currentNode.next;
        if (currentNode == null) {
            EndTutorial();
        } else {
            ShowDialogue();
        }
    }

    public void OnYesClick() {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        currentNode = currentNode.yesNext;
        ShowDialogue();
    }

    public void OnNoClick() {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        currentNode = currentNode.noNext;
        ShowDialogue();
    }

    private void EndTutorial() {
        state = State.Finished;

        IsTutorialPlaying = false;

        tutorialPanel.SetActive(false);
        tutorialFinished = true;
        Time.timeScale = 1f;
        CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
    }

    private void HighlightButton(Button target) {
        if (target == null) return;

        // [중요] 이미 이 버튼이 반짝이고 있다면, 이전 코루틴을 먼저 확실히 끄고 새로 시작합니다.
        if (buttonHighlightCoroutines.TryGetValue(target, out Coroutine existingCoroutine)) {
            StopCoroutine(existingCoroutine);
            buttonHighlightCoroutines.Remove(target);
        }

        // 새 코루틴 시작하고 딕셔너리에 '꼭' 저장합니다.
        Coroutine c = StartCoroutine(HighlightEffect(target));
        buttonHighlightCoroutines[target] = c;
    }

    private IEnumerator HighlightEffect(Button target) {
        if (target == null) yield break;

        var img = target.GetComponent<Image>();
        var txt = target.GetComponent<TextMeshProUGUI>();

        if (img == null && txt == null) yield break;

        // 원래 색상 기억 (이미지가 있으면 이미지, 없으면 텍스트)
        Color original = img != null ? img.color : txt.color;

        // [중요] StopHighlight에서 써먹어야 하니 원래 색상을 저장해둡니다.
        if (!buttonOriginalColors.ContainsKey(target)) {
            buttonOriginalColors[target] = original;
        }

        Color highlight = Color.green;
        float t = 0f;

        while (true) {
            // UnscaledDeltaTime을 써야 정지 상태(Time.timeScale = 0)에서도 반짝입니다.
            t += Time.unscaledDeltaTime * 3f;
            Color lerpColor = Color.Lerp(original, highlight, Mathf.PingPong(t, 1));

            if (img != null) img.color = lerpColor;
            if (txt != null) txt.color = lerpColor;

            yield return null;
        }
    }

    // 💡 [수정됨] 복구 여부를 결정하는 bool 파라미터 추가! (기본값은 true)
    private void StopHighlight(Button target, bool restoreColor = true) {
        if (target == null) return;

        // 1. 돌아가고 있는 반짝이 코루틴을 멈춥니다.
        if (buttonHighlightCoroutines.TryGetValue(target, out Coroutine c)) {
            StopCoroutine(c);
            buttonHighlightCoroutines.Remove(target);
        }

        // 2. 색상 복구를 허락했을 때만 원래 색으로 되돌립니다.
        if (restoreColor) {
            if (buttonOriginalColors.TryGetValue(target, out Color originalColor)) {
                var img = target.GetComponent<Image>();
                if (img != null) img.color = originalColor;

                var txt = target.GetComponent<TextMeshProUGUI>();
                if (txt != null) txt.color = originalColor;
            } else {
                // 혹시라도 기록이 없으면 최후의 수단으로만 흰색
                var img = target.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
        }

        // 색상 복구를 하든 안 하든 메모리 정리는 해줍니다.
        if (buttonOriginalColors.ContainsKey(target)) {
            buttonOriginalColors.Remove(target);
        }
    }

    private IEnumerator WaitForBuyPanelAndGuide() {

        if (tutorialFinished) yield break;

        // BuyPanelController가 뜰 때까지 대기
        BuyPanelController buyPanel = null;
        while (buyPanel == null || !buyPanel.panel.activeSelf) {
            buyPanel = FindAnyObjectByType<BuyPanelController>();
            yield return null;
        }

        // 패널이 열림 → 50% 버튼 찾기
        Transform ratioBtn = buyPanel.panel.transform.Find("50_Button"); //
        if (ratioBtn == null) {
            Debug.LogError("50_Button 오브젝트를 찾을 수 없습니다.");
            yield break;
        }

        Button halfBtn = ratioBtn.GetComponent<Button>();
        if (halfBtn == null) yield break;

        // 50% 버튼 강조
        HighlightButton(halfBtn);
        fifyDim.SetActive(true);
        halfBtn.onClick.AddListener(() => {
            if (tutorialFinished) return;
            StopHighlight(halfBtn);
            fifyDim.SetActive(false);

            // 매수 확정 버튼 찾기
            Button confirmBtn = buyPanel.confirmButton;
            if (confirmBtn == null) return;

            // 매수 확정 버튼 강조
            HighlightButton(confirmBtn);
            ConfirmDim.SetActive(true);
            confirmBtn.onClick.AddListener(() => {
                StopHighlight(confirmBtn);
                ConfirmDim.SetActive(false);

                // 패널 닫히고 다음 튜토리얼 진행
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
        });
    }

    private IEnumerator WaitForBuyPanelAndGuide_Eth() {

        if (tutorialFinished) yield break;

        // BuyPanelController가 뜰 때까지 대기
        BuyPanelController buyPanel = null;
        while (buyPanel == null || !buyPanel.panel.activeSelf) {
            buyPanel = FindAnyObjectByType<BuyPanelController>();
            yield return null;
        }

        // 패널이 열림 → 50% 버튼 찾기
        Transform ratioBtn = buyPanel.panel.transform.Find("100_Button");
        if (ratioBtn == null) {
            Debug.LogError("100_Button 오브젝트를 찾을 수 없습니다.");
            yield break;
        }

        Button allBtn = ratioBtn.GetComponent<Button>();
        if (allBtn == null) yield break;

        // 100% 버튼 강조
        HighlightButton(allBtn);
        AllDim.SetActive(true);
        allBtn.onClick.AddListener(() => {
            if (tutorialFinished) return;
            StopHighlight(allBtn);
            AllDim.SetActive(false);

            // 매수 확정 버튼 찾기
            Button confirmBtn = buyPanel.confirmButton;
            if (confirmBtn == null) return;

            // 매수 확정 버튼 강조
            HighlightButton(confirmBtn);
            ConfirmDim.SetActive(true);
            confirmBtn.onClick.AddListener(() => {
                StopHighlight(confirmBtn);
                ConfirmDim.SetActive(false);

                // 패널 닫히고 튜토리얼 종료
                if (!tutorialFinished) {
                    tutorialPanel.SetActive(true);
                    currentNode = currentNode.next;
                    ShowDialogue();
                }
            });
        });
    }
    public void HighlightText(TextMeshProUGUI target) {
        if (target == null) return;

        // 이미 돌고 있으면 중복 방지
        if (textHighlightCoroutines.ContainsKey(target))
            return;

        Coroutine c = StartCoroutine(HighlightTextEffect(target));
        textHighlightCoroutines[target] = c;
    }

    public void StopHighlightText(TextMeshProUGUI target) {
        if (target == null) return;

        if (textHighlightCoroutines.TryGetValue(target, out Coroutine c)) {
            StopCoroutine(c);
            textHighlightCoroutines.Remove(target);
        }

        target.color = Color.white;
    }

    private IEnumerator HighlightTextEffect(TextMeshProUGUI target) {
        Color original = target.color;
        Color highlight = Color.green;
        float t = 0f;

        while (true) {
            t += Time.unscaledDeltaTime * 2f;
            target.color = Color.Lerp(original, highlight, Mathf.PingPong(t, 1));
            yield return null;
        }
    }
    public void HighlightSymbolLabel_ON() {
        foreach (var text in portfolioCoinSymbolTexts)
            HighlightText(text);
        foreach (var text in portfolioCoinLabelTexts)
            HighlightText(text);
    }

    public void HighlightSymbolLabel_OFF() {
        foreach (var text in portfolioCoinSymbolTexts)
            StopHighlightText(text);
        foreach (var text in portfolioCoinLabelTexts)
            StopHighlightText(text);
    }

    private IEnumerator CheckDetailPanelOpened() {
        if (tutorialFinished) yield break;

        while (actualDetailPanel == null || !actualDetailPanel.activeSelf) {
            yield return null;
        }


        BorderDim2.SetActive(false); // 쳐뒀던 딤 끄기

        // 튜토리얼 대화창 다시 띄우고 다음으로 진행
        if (!tutorialFinished) {
            tutorialPanel.SetActive(true);
            currentNode = currentNode.next;
            ShowDialogue();
        }
    }
}