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

    [HideInInspector] public List<TextMeshProUGUI> portfolioCoinSymbolTexts = new List<TextMeshProUGUI>();
    [HideInInspector] public List<TextMeshProUGUI> portfolioCoinLabelTexts = new List<TextMeshProUGUI>();


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


    [Header("UI 표시용")]
    public Image arrowImage;
    private Coroutine arrowCoroutine;
    private Dictionary<TextMeshProUGUI, Coroutine> textHighlightCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    private bool timePausedOnce = false;


    //void Update() {
    //    if (!tutorialFinished && Time.timeScale != 0f)
    //        Time.timeScale = 0f;
    //}


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
        var status = FindObjectOfType<StatusPanelController>();
        if (status != null && status.playerNameText != null)
            playerName = status.playerNameText.text;

        // 노드 생성
        DialogueNode n1 = new DialogueNode { text = $"당신이 이번 우리 회사 신입 트레이더 '{playerName}' 인가요?" };
        DialogueNode n2 = new DialogueNode { text = "굉장히 허접하게 생기셨네요. 이런 사람이 트레이더라니..." };
        DialogueNode n3 = new DialogueNode { text = "어쨌든... 저는 대표님의 비서 'Carrot' 이라고 해요" };
        DialogueNode n4 = new DialogueNode { text = "혹시 트레이딩 경험이 있으신가요?", isChoice = true };

        DialogueNode y1 = new DialogueNode { text = "그럼 뭐, 대충 알려드려도 잘하시겠네요." };
        DialogueNode y2 = new DialogueNode { text = "그래도 혹시 모르니 거래를 하는 방법부터 알려드릴께요." };

        DialogueNode n5 = new DialogueNode { text = "없다구요? 대표님은 어떤 생각으로 이 사람을 뽑으신거지?" };
        DialogueNode n6 = new DialogueNode { text = "후... 일단 거래를 하는 방법부터 알려드릴께요." };

        DialogueNode n7 = new DialogueNode { text = "일단 하단에 있는 '앱' 버튼을 눌러보세요", action = DialogueAction.HighlightAppButton };
        DialogueNode n8 = new DialogueNode { text = "이곳에 여러 앱들이 있는데, 간단하게 설명 드리자면" };
        DialogueNode n9 = new DialogueNode { text = "여기 있는 불비트가 우리 거래 앱이에요.", highlightTarget = HighlightTarget.Bullbit };
        DialogueNode n10 = new DialogueNode { text = "이곳에서 코인을 사고 팔 수 있어요", highlightTarget = HighlightTarget.Bullbit };
        DialogueNode n11 = new DialogueNode { text = "그리고 이건 은행앱이에요.", highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n12 = new DialogueNode { text = "사토시 은행을 통해, 불비트로 돈을 옮기고", highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n13 = new DialogueNode { text = "불비트에서 은행으로 출금을하고, 부동산을 사는 등", highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n14 = new DialogueNode { text = "돈을 움직일땐 은행을 필수로 이용해야 하죠.", highlightTarget = HighlightTarget.Satoshi };
        //DialogueNode n15 = new DialogueNode { text = "그리고 이건 아르바이트 앱 이에요.", highlightTarget = HighlightTarget.PartTimeJob };
        //DialogueNode n16 = new DialogueNode { text = "급전이 필요할때, 하루에 한번 아르바이트를 진행할 수 있어요", highlightTarget = HighlightTarget.PartTimeJob };
        //DialogueNode n17 = new DialogueNode { text = "근데 트레이더라는 사람이..", highlightTarget = HighlightTarget.PartTimeJob };
        //DialogueNode n18 = new DialogueNode { text = "몰래 아르바이트 뛰고 그런거 아니죠...?", highlightTarget = HighlightTarget.PartTimeJob };
        //DialogueNode n19 = new DialogueNode { text = "그럴일은 없길 바랄께요.", highlightTarget = HighlightTarget.PartTimeJob };
        DialogueNode n20 = new DialogueNode { text = "이건 부동산 매매할때 쓰는 앱이에요", highlightTarget = HighlightTarget.Estate };
        DialogueNode n21 = new DialogueNode { text = "나중에 한번 둘러보세요.", highlightTarget = HighlightTarget.Estate };
        DialogueNode n22 = new DialogueNode { text = "이 앱은 Xbird라는 앱인데,", highlightTarget = HighlightTarget.Xbird };
        DialogueNode n23 = new DialogueNode { text = "이 앱을 이용해서 누구보다 빠르게 정보를 얻을 수 있어요.", highlightTarget = HighlightTarget.Xbird };
        DialogueNode n24 = new DialogueNode { text = "하지만 가짜정보도 판을치니, 판단을 잘하셔야 할거에요", highlightTarget = HighlightTarget.Xbird };
        DialogueNode n25 = new DialogueNode { text = "그러나 그런 가짜 정보로도 돈을 벌 수 있어야", highlightTarget = HighlightTarget.Xbird };
        DialogueNode n26 = new DialogueNode { text = "진짜 트레이더라고 불릴 수 있다고 저는 생각해요.", highlightTarget = HighlightTarget.Xbird };
        DialogueNode n27 = new DialogueNode { text = "하단에 뉴스 탭에서도 진입할 수 있어요.", highlightTarget = HighlightTarget.Xbird };
        //DialogueNode n28 = new DialogueNode { text = "당신이 진정한 도파민 중독자라면 추천드릴께요.", highlightTarget = HighlightTarget.Gamble };
        DialogueNode n29 = new DialogueNode { text = "일단 그러면 저희 불비트 거래소에 입금을 먼저 해볼까요?", };
        DialogueNode n30 = new DialogueNode { text = "좀 전에 알려드린 사토시 은행앱을 눌러서", highlightTarget = HighlightTarget.Satoshi };
        DialogueNode n31 = new DialogueNode { text = "입금을 한번 해보도록 하죠.", highlightTarget = HighlightTarget.SatoshiDimWithButton, action = DialogueAction.WaitForSatoshiDepositButton };
        DialogueNode n32 = new DialogueNode { text = "좋아요 여기가 은행 화면이에요.", blockPanelAlpha = 0.0f, highlightTarget = HighlightTarget.None };
        DialogueNode n33 = new DialogueNode { text = $"회사 명의로 '{playerName}님 계좌에 200만원을 넣어드렸어요.", };
        DialogueNode n34 = new DialogueNode { text = "이 돈을 한번 불비트 거래소로 옮겨보죠.", };
        DialogueNode n35 = new DialogueNode { text = "화면 중앙에 있는 이체 버튼을 한번 눌러보세요.", highlightTarget = HighlightTarget.WithdrawButton, action = DialogueAction.WaitForWithdrawButton };
        DialogueNode n36 = new DialogueNode { text = "잘 하셨어요. 일단 이체할 플랫폼을 먼저 골라야해요.", };
        DialogueNode n37 = new DialogueNode { text = "저희는 거래를 해야하니까, 불비트 거래소로 송금 해야겠죠?", };
        DialogueNode n38 = new DialogueNode { text = "지금은 송금할 수 있는 플랫폼이 몇개 없겠지만", };
        DialogueNode n39 = new DialogueNode { text = "보유한 자금이 늘어나시다보면, 새로운 플랫폼이 해금될거에요.", };
        DialogueNode n40 = new DialogueNode { text = "일단 이체를 진행해 볼까요?", };
        DialogueNode n41 = new DialogueNode { text = "전액'버튼을 누르고 이체 버튼을 눌러 모든금액을 송금해보죠.", action = DialogueAction.WaitForTransfer, highlightTarget = HighlightTarget.TransferDim };
        DialogueNode n42 = new DialogueNode { text = "좋아요. 이렇게하면 정상적으로 송금이 됐을거에요.", };
        DialogueNode n43 = new DialogueNode { text = "그럼 저희는 불비트 거래소로 이동해 볼까요?", action = DialogueAction.HighlightBullbitButton };
        DialogueNode n44 = new DialogueNode { text = $"좋아요 왼쪽 상단을 보시면 입금된 잔액을 볼 수 있어요." };
        DialogueNode n45 = new DialogueNode { text = "잔액이 들어온것은 확인했고, 그러면 코인을 한번 매수해볼까요?" };
        DialogueNode n46 = new DialogueNode { text = "그러면 지금 절반의 비트코인과 이더리움을 매수해보죠." };
        DialogueNode n47 = new DialogueNode { text = "일단 비트코인을 먼저 매수해봐요.", action = DialogueAction.HighlightBitcoinBuy };
        DialogueNode n48 = new DialogueNode { text = "좋아요, 생각보다 능숙하시네요?" };
        DialogueNode n49 = new DialogueNode { text = "그러면 남은 잔액 전부로 이더리움을 매수해보죠", action = DialogueAction.HighlightEthBuy };
        DialogueNode n50 = new DialogueNode { text = "음, 망설임 없이 매수하시는걸 보니, 트레이더의 성향이 보이긴 하네요.", };
        DialogueNode n51 = new DialogueNode { text = "이제 매수를 했다면, 자신의 포트폴리오를 확인해야겠죠?", };
        DialogueNode n52 = new DialogueNode { text = "하단 자산관리 메뉴 버튼을 누르면, 포트폴리오를 볼 수 있어요.", };
        DialogueNode n53 = new DialogueNode { text = "지금 바로 확인해볼까요?", action = DialogueAction.HighlightPortfolioButton };
        DialogueNode n54 = new DialogueNode { text = "좋아요, 여기서 불비트를 선택해 보세요.", action = DialogueAction.WaitForClickBullPort };
        DialogueNode n55 = new DialogueNode { text = $"여기서 {playerName}님의 포트폴리오를 볼 수 있어요." };
        DialogueNode n56 = new DialogueNode { text = $"실시간으로 잔고가 요동치는 걸 볼 수 있죠.", action = DialogueAction.HighlightSymbolLabel_ON };
        DialogueNode n57 = new DialogueNode { text = $"코인의 이름을 또는 심볼명을 누르면 ", };
        DialogueNode n58 = new DialogueNode { text = $"이 화면에서 바로 거래도 가능해요.", action = DialogueAction.HighlightSymbolLabel_OFF };
        DialogueNode n59 = new DialogueNode { text = $"좋아요, 매수 매도같이 기본적인 것은 여기까지에요.", };
        DialogueNode n60 = new DialogueNode { text = $"여기서 매수한 코인은 능력껏 잘 팔아보도록 해요.", };
        DialogueNode n61 = new DialogueNode { text = $"자산을 늘리다보면, 추가로 할 수 있는 것들도 열릴거에요.", };
        DialogueNode n62 = new DialogueNode { text = $"그때까지 시장에서 살아남을 수 있을진 모르겠지만...", };
        DialogueNode n63 = new DialogueNode { text = $"그럼 행운을 빌게요.", };

        // 기본 연결
        n1.next = n2; n2.next = n3; n3.next = n4;
        n4.yesNext = y1; n4.noNext = n5;
        y1.next = y2; y2.next = n7;
        n5.next = n6; n6.next = n7;
        n7.next = n8; n8.next = n9; n9.next = n10;
        n10.next = n11; n11.next = n12; n12.next = n13;
        n13.next = n14; n14.next = n20;

        //n15.next = n16; n16.next = n17;
        //n17.next = n18; n18.next = n19; n19.next = n20;

        n20.next = n21;
        n21.next = n22; n22.next = n23; n23.next = n24; n24.next = n25;
        n25.next = n26; n26.next = n27; n27.next = n29; 
        n29.next = n30; n30.next = n31; n31.next = n32; n32.next = n33;
        n33.next = n34; n34.next = n35; n35.next = n36; n36.next = n37;
        n37.next = n38; n38.next = n39; n39.next = n40; n40.next = n41;
        n41.next = n42; n42.next = n43; n43.next = n44; n44.next = n45;
        n45.next = n46; n46.next = n47; n47.next = n48; n48.next = n49;
        n49.next = n50; n50.next = n51; n51.next = n52; n52.next = n53;
        n53.next = n54; n54.next = n55; n55.next = n56; n56.next = n57;
        n57.next = n58; n58.next = n59; n59.next = n60; n60.next = n61;
        n61.next = n62; n62.next = n63; n63.next = null;

        currentNode = n1;
    }

    public void ApplyHighlight(HighlightTarget target) {
        // 전체 Dim 꺼두기
        bullbitDim.SetActive(false);
        satoshiDim.SetActive(false);
        satoshiDimWithButton.SetActive(false);
        partTimeDim.SetActive(false);
        estateDim.SetActive(false);
        xbirdDim.SetActive(false);
        gambleDim.SetActive(false);
        withdrawButtonDim.SetActive(false);
        transferDim.SetActive(false);


        switch (target) {
            case HighlightTarget.Bullbit: bullbitDim.SetActive(true); break;
            case HighlightTarget.Satoshi: satoshiDim.SetActive(true); break;
            case HighlightTarget.SatoshiDimWithButton: satoshiDimWithButton.SetActive(true); break;
            case HighlightTarget.PartTimeJob: partTimeDim.SetActive(true); break;
            case HighlightTarget.Estate: estateDim.SetActive(true); break;
            case HighlightTarget.Xbird: xbirdDim.SetActive(true); break;
            case HighlightTarget.Gamble: gambleDim.SetActive(true); break;
            case HighlightTarget.WithdrawButton: withdrawButtonDim.SetActive(true); break;
            case HighlightTarget.TransferDim: transferDim.SetActive(true); break;
        }
    }


    private void ShowDialogue() {


        if (currentNode == null) {
            EndTutorial();   // 마지막에 도달했으면 튜토리얼 종료 처리
            return;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentNode.text));

        // 노드 진입 시점에서 추가 처리
        if (currentNode.action == DialogueAction.WaitForSatoshiDepositButton) {
            // 여기서는 그냥 대기만 — 실제 버튼 클릭까지 진행 막기
            return;
        }

        if (currentNode.action == DialogueAction.WaitForWithdrawButton) {
            return;
        }


        if (currentNode.blockPanelAlpha >= 0f) {
            SetBlockPanelAlpha(currentNode.blockPanelAlpha);
        }
    }


    private IEnumerator TypeText(string message) {
        dialogueText.text = "";
        isTyping = true;
        textCompleted = false;

        foreach (char c in message) {
            dialogueText.text += c;
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
            // 혹시 필요하면 DIM 전부 끄기
            ApplyHighlight(HighlightTarget.None);
        }
        if (currentNode.blockPanelAlpha >= 0f) {
            SetBlockPanelAlpha(currentNode.blockPanelAlpha);
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

        if (currentNode.action == DialogueAction.WaitForTransfer) {
            tutorialPanel.SetActive(false);

            //TransferButton.onClick.RemoveAllListeners();
            TransferButton.onClick.AddListener(() => {

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

        tutorialPanel.SetActive(false);
        tutorialFinished = true;
        Time.timeScale = 1f;
        CoinManager.Instance.SetTimeSpeed(TimeSpeed.Normal);
    }

    private Coroutine highlightCoroutine;
    private void HighlightButton(Button target) {
        if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
        highlightCoroutine = StartCoroutine(HighlightEffect(target));
    }

    private IEnumerator HighlightEffect(Button target) {
        if (target == null) yield break;
        var img = target.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        Color highlight = Color.green;
        float t = 0f;

        while (true) {
            t += Time.unscaledDeltaTime * 2f;
            img.color = Color.Lerp(original, highlight, Mathf.PingPong(t, 1));
            yield return null;
        }
    }

    private void StopHighlight(Button target) {
        if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
        highlightCoroutine = null;
        if (target != null) {
            var img = target.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    private IEnumerator WaitForBuyPanelAndGuide() {

        if (tutorialFinished) yield break;

        // BuyPanelController가 뜰 때까지 대기
        BuyPanelController buyPanel = null;
        while (buyPanel == null || !buyPanel.panel.activeSelf) {
            buyPanel = FindObjectOfType<BuyPanelController>();
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
            buyPanel = FindObjectOfType<BuyPanelController>();
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

}
