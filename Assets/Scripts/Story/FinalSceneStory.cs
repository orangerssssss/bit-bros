using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 最终场景故事线：
/// 进入场景 -> 开场自动对话 -> 任务：击败洛特王 -> 黑屏提示 -> 点击继续 -> 传送玩家 -> 激活 Merlin 并添加最终对话
/// </summary>
public class FinalSceneStory : MonoBehaviour
{
    private enum EndingType
    {
        DrawSword,
        RefuseSword
    }

    private static readonly string[] DrawSwordEndingPages =
    {
        "他最终握住了那柄剑。\n于是，沉睡在王权之中的古老之物，再一次睁开了眼。",
        "亚瑟曾以为自己将终结乱世，\n却在血与低语之中，成为了新的王。\n他的意志被一点点吞没，\n他的躯壳，则被奉上神座。",
        "自此，石中剑再度执掌大地。\n战火复燃，瘟疫蔓延，\n旧日的苦难披上新的冠冕，\n世界也重新坠入漫长而无尽的混沌。",
        "王仍坐在王座之上。\n只是那已不再是人。"
    };

    private static readonly string[] RefuseSwordEndingPages =
    {
        "在命运与谎言的尽头，\n他拒绝了那柄剑，也拒绝了被赐予的王位。",
        "亚瑟以自己的身躯为封印，\n拖着梅林，连同石中剑深埋于长夜之下。\n古老的低语终于沉寂，\n延续数百年的诅咒，也在此刻断绝。",
        "再没有被选中的王，\n再没有轮回不止的献祭。\n破碎的大地，终于迎来了真正的安宁。",
        "人们未必记得他的名字，\n但从那以后，晨光重新照在不列颠的土地上。\n而世界，终于开始属于活着的人。"
    };

    private const string EndingCreditsPage = "《弃王之躯》\n\n感谢您的游玩\n\n制作：陆相岑  张容宾";

    private const string DefaultDialog40Path = "Assets/ScriptableObjects/Dialog/MainStory/DialogConfig_4_0.asset";
    private const string DefaultDialog41Path = "Assets/ScriptableObjects/Dialog/MainStory/DialogConfig_4_1.asset";
    private const string DefaultDialog42Path = "Assets/ScriptableObjects/Dialog/MainStory/DialogConfig_4_2.asset";

    private static FinalSceneStory instance;

    public static FinalSceneStory Instance
    {
        get
        {
            if (instance == null) instance = GameObject.FindObjectOfType<FinalSceneStory>();
            return instance;
        }
    }

    public int storyProcess = 0;

    [Header("过场")]
    public Image blackImage;

    [Header("对话文件")]
    public DialogConfig dialog_4_0;
    public DialogConfig dialog_4_1;
    public DialogConfig dialog_4_2;

    [Header("对话物")]
    public DialogObject kingNPC;
    public DialogObject merlinNPC;

    [Header("战斗角色")]
    public FightAttributes kingFightAttributes;

    [Header("物体")]
    public GameObject king;
    public GameObject merlin;

    [Header("位置")]
    public Transform postBossPlayerSpawnPoint;
    public Transform merlinAppearPoint;

    [Header("标识")]
    public Transform mark_king;
    public Transform mark_merlin;

    public bool autoPickupStoryDialog = false;

    [SerializeField] private float autoDialogDelay = 1.0f;
    [SerializeField] private float dialogSystemWaitTimeout = 0f;
    [SerializeField] private float dialogSystemPollInterval = 0.2f;
    [SerializeField] private float blackFadeSpeed = 2.2f;
    [SerializeField] private float bossHealthMultiplier = 4.0f;
    [SerializeField] private float overlayTextFadeDuration = 1.1f;
    [SerializeField] private string postBossPromptMessage = "勇敢的战士，做出你最终的选择吧";
    private float dialogDelayTimer = 0f;
    private bool dialogStarted = false;
    private bool introDialogQueued = false;
    private bool merlinDialogQueued = false;
    private bool bossDeathHandled = false;
    private bool bossHealthBoostApplied = false;
    private bool merlinChoiceFinished = false;
    private bool endingSequenceRunning = false;
    private bool overlayAdvanceRequested = false;
    private bool overlayInputEnabled = false;
    private bool overlayHintBlinking = false;
    private Coroutine openDialogRetryCoroutine = null;

    private GameObject continueOverlay;
    private Text continueTitleText;
    private Text continueHintText;
    private Button continueButton;
    private CursorLockMode cachedCursorLockMode;
    private bool cachedCursorVisible;
    private Canvas overlayCanvas;

    private DialogConfig runtimeMerlinChoiceDialog;
    private DialogConfig runtimeMerlinChoiceYesDialog;
    private DialogConfig runtimeMerlinChoiceNoDialog;

    private void Start()
    {
        storyProcess = 0;
        ResolveSceneReferences();
        EnsureBlackImage();
        RestoreDialogDefaultsInEditor();
        ApplyBossHealthMultiplier();
        UpdateStory();

        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.dialogConfigEndEvent.AddListener(OnDialogFinished);
            GameEventManager.Instance.characterBeforeDeathEvent.AddListener(OnCharacterBeforeDeath);
        }
    }

    private void OnDestroy()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.dialogConfigEndEvent.RemoveListener(OnDialogFinished);
            GameEventManager.Instance.characterBeforeDeathEvent.RemoveListener(OnCharacterBeforeDeath);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePromptClicked);
        }

    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bossHealthMultiplier = Mathf.Max(1.0f, bossHealthMultiplier);
        autoDialogDelay = Mathf.Max(0f, autoDialogDelay);
        dialogSystemPollInterval = Mathf.Max(0.05f, dialogSystemPollInterval);
        blackFadeSpeed = Mathf.Max(0.1f, blackFadeSpeed);
        RestoreDialogDefaultsInEditor();
    }
#endif

    private void Update()
    {
        if (storyProcess == 0 && !dialogStarted)
        {
            dialogDelayTimer += Time.deltaTime;
            if (dialogDelayTimer >= autoDialogDelay)
            {
                dialogStarted = true;
                TryPlayOpeningDialog();
            }
        }

        if (overlayInputEnabled)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                overlayAdvanceRequested = true;
            }
        }

        if (overlayHintBlinking && continueHintText != null && continueHintText.gameObject.activeSelf)
        {
            Color color = continueHintText.color;
            color.a = 0.35f + Mathf.PingPong(Time.unscaledTime * 1.35f, 0.65f);
            continueHintText.color = color;
        }
    }

    private void ResolveSceneReferences()
    {
        if (kingFightAttributes == null && king != null)
        {
            kingFightAttributes = king.GetComponent<FightAttributes>();
            if (kingFightAttributes == null) kingFightAttributes = king.GetComponentInChildren<FightAttributes>(true);
        }

        if (king == null && kingFightAttributes != null)
        {
            king = kingFightAttributes.gameObject;
        }

        if (merlin == null && merlinNPC != null)
        {
            merlin = merlinNPC.gameObject;
        }
    }

    private void EnsureBlackImage()
    {
        if (blackImage != null)
        {
            Canvas existingCanvas = blackImage.GetComponentInParent<Canvas>();
            if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                StretchRectToFullscreen(blackImage.rectTransform);
                overlayCanvas = existingCanvas;
                blackImage.transform.SetAsFirstSibling();
                if (continueOverlay != null)
                {
                    continueOverlay.transform.SetAsLastSibling();
                }
                return;
            }
        }

        Canvas targetCanvas = GetOrCreateOverlayCanvas();
        if (targetCanvas == null) return;

        GameObject overlay = new GameObject("FinalSceneBlackImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlay.transform.SetParent(targetCanvas.transform, false);
        overlay.transform.SetAsLastSibling();

        RectTransform rectTransform = overlay.GetComponent<RectTransform>();
        StretchRectToFullscreen(rectTransform);

        blackImage = overlay.GetComponent<Image>();
        blackImage.color = new Color(0f, 0f, 0f, 0f);
        blackImage.raycastTarget = true;
        overlayCanvas = targetCanvas;
        blackImage.transform.SetAsFirstSibling();

        Debug.Log("FinalSceneStory: auto-created fallback blackImage overlay.");
    }

    private Canvas GetOrCreateOverlayCanvas()
    {
        if (overlayCanvas != null)
        {
            return overlayCanvas;
        }

        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "FinalSceneOverlayCanvas")
            {
                overlayCanvas = canvases[i];
                return overlayCanvas;
            }
        }

        GameObject canvasObject = new GameObject("FinalSceneOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        StretchRectToFullscreen(rectTransform);

        overlayCanvas = canvas;
        return overlayCanvas;
    }

    private void StretchRectToFullscreen(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private void UpdateStory()
    {
        switch (storyProcess)
        {
            case 0:
                if (king != null) king.SetActive(true);
                if (merlin != null) merlin.SetActive(false);

                if (kingNPC != null && dialog_4_0 != null && !introDialogQueued)
                {
                    kingNPC.AddSpecialDialog(dialog_4_0);
                    introDialogQueued = true;
                }
                break;

            case 1:
                UpdateMainTask("和洛特王最终对决", "击败洛特王，结束这场最终之战。");
                SetDestinationTarget(ResolveKingTarget(), 5.0f);
                break;

            case 2:
                HideDestinationMark();
                break;

            case 3:
                UpdateMainTask("与梅林对话", "前往梅林身边，听取他最后的指引。");
                PrepareMerlinPhase();
                SetDestinationTarget(ResolveMerlinTarget(), 4.0f);
                break;

            case 4:
                HideDestinationMark();
                UpdateMainTask("最终的选择", "你已经作出决定，命运将继续向前。");
                break;
        }
    }

    private void TryPlayOpeningDialog()
    {
        if (DialogDisplayer.Instance != null && GameUIManager.Instance != null && kingNPC != null)
        {
            Chat(kingNPC);
            return;
        }

        if (openDialogRetryCoroutine != null)
        {
            StopCoroutine(openDialogRetryCoroutine);
        }
        openDialogRetryCoroutine = StartCoroutine(WaitAndPlayOpeningDialog());
    }

    private IEnumerator WaitAndPlayOpeningDialog()
    {
        float startTime = Time.time;

        while (DialogDisplayer.Instance == null || GameUIManager.Instance == null || kingNPC == null)
        {
            if (dialogSystemWaitTimeout > 0f && Time.time - startTime >= dialogSystemWaitTimeout)
            {
                yield break;
            }

            yield return new WaitForSeconds(dialogSystemPollInterval);
        }

        Chat(kingNPC);
        openDialogRetryCoroutine = null;
    }

    private void OnDialogFinished(DialogConfig dialog)
    {
        if (storyProcess == 0 && dialog == dialog_4_0)
        {
            storyProcess = 1;
            UpdateStory();
            return;
        }

        if (storyProcess == 3 && !merlinChoiceFinished
            && (dialog == runtimeMerlinChoiceYesDialog || dialog == runtimeMerlinChoiceNoDialog))
        {
            merlinChoiceFinished = true;
            storyProcess = 4;
            UpdateStory();
            StartEndingSequence(dialog == runtimeMerlinChoiceYesDialog ? EndingType.DrawSword : EndingType.RefuseSword);
        }
    }

    private void OnCharacterBeforeDeath(CharacterAttributes character)
    {
        if (bossDeathHandled || storyProcess != 1 || character == null)
        {
            return;
        }

        if (!IsKingCharacter(character))
        {
            return;
        }

        bossDeathHandled = true;
        StartCoroutine(HandleBossDefeatedSequence());
    }

    private bool IsKingCharacter(CharacterAttributes character)
    {
        if (kingFightAttributes == null)
        {
            ResolveSceneReferences();
        }

        if (character == kingFightAttributes)
        {
            return true;
        }

        if (kingFightAttributes != null && character.transform == kingFightAttributes.transform)
        {
            return true;
        }

        if (king != null && (character.transform == king.transform || character.transform.IsChildOf(king.transform)))
        {
            return true;
        }

        return false;
    }

    private IEnumerator HandleBossDefeatedSequence()
    {
        storyProcess = 2;
        UpdateStory();
        SetPlayerControlEnabled(false, true);

        yield return FadeBlackTo(1.0f);
        ShowContinuePrompt();

        yield return WaitForOverlayAdvance();
        HideContinuePrompt();
        TeleportPlayerTo(postBossPlayerSpawnPoint);

        storyProcess = 3;
        UpdateStory();

        yield return null;
        yield return FadeBlackTo(0f);
        SetPlayerControlEnabled(true, false);
    }

    private IEnumerator FadeBlackTo(float targetAlpha)
    {
        EnsureBlackImage();

        if (blackImage == null)
        {
            Debug.LogWarning("FinalSceneStory: blackImage is not assigned. Skipping fade.");
            yield break;
        }

        if (!blackImage.gameObject.activeSelf)
        {
            blackImage.gameObject.SetActive(true);
        }

        Color color = blackImage.color;
        color.r = 0f;
        color.g = 0f;
        color.b = 0f;

        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(color.a, targetAlpha, Time.deltaTime * blackFadeSpeed);
            blackImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        blackImage.color = color;
    }

    private void PrepareMerlinPhase()
    {
        if (merlin != null)
        {
            if (merlinAppearPoint != null)
            {
                merlin.transform.SetPositionAndRotation(merlinAppearPoint.position, merlinAppearPoint.rotation);
            }
            merlin.SetActive(true);
        }

        if (merlinNPC != null && !merlinDialogQueued)
        {
            EnsureMerlinChoiceDialogBuilt();
            if (runtimeMerlinChoiceDialog != null)
            {
                merlinNPC.AddSpecialDialog(runtimeMerlinChoiceDialog);
                merlinDialogQueued = true;
            }
        }
    }

    private void EnsureMerlinChoiceDialogBuilt()
    {
        if (runtimeMerlinChoiceDialog != null)
        {
            return;
        }

        runtimeMerlinChoiceYesDialog = CreateRuntimeDialog(new DialogContent[0]);
        runtimeMerlinChoiceNoDialog = CreateRuntimeDialog(new DialogContent[0]);

        List<DialogContent> mergedContents = new List<DialogContent>();
        AppendDialogContents(mergedContents, dialog_4_1);
        AppendDialogContents(mergedContents, dialog_4_2);

        if (mergedContents.Count == 0)
        {
            mergedContents.Add(CreateDialogContent("梅林", "洛特王已经倒下，但王国真正的命运才刚刚开始。"));
            mergedContents.Add(CreateDialogContent("梅林", "石中剑沉睡已久。勇敢的战士，你是否拔出石中剑？"));
        }

        runtimeMerlinChoiceDialog = CreateRuntimeDialog(mergedContents);
        runtimeMerlinChoiceDialog.nextDialog = new List<DialogSelection>
        {
            new DialogSelection { optionName = "是", dialog = runtimeMerlinChoiceYesDialog },
            new DialogSelection { optionName = "否", dialog = runtimeMerlinChoiceNoDialog }
        };
    }

    private void ShowContinuePrompt()
    {
        EnsureContinueOverlay();

        ConfigureOverlayForPrompt();
        SetOverlayText(postBossPromptMessage, "点击继续");
        continueOverlay.SetActive(true);
        overlayAdvanceRequested = false;
        overlayInputEnabled = true;
        overlayHintBlinking = true;

        cachedCursorVisible = Cursor.visible;
        cachedCursorLockMode = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideContinuePrompt()
    {
        overlayAdvanceRequested = false;
        overlayInputEnabled = false;
        overlayHintBlinking = false;

        if (continueOverlay != null)
        {
            continueOverlay.SetActive(false);
        }

        Cursor.visible = cachedCursorVisible;
        Cursor.lockState = cachedCursorLockMode;
    }

    private void EnsureContinueOverlay()
    {
        if (continueOverlay != null)
        {
            return;
        }

        EnsureBlackImage();
        Transform parent = blackImage != null ? blackImage.transform.parent : GetOrCreateOverlayCanvas().transform;
        GameObject overlay = new GameObject("FinalSceneContinueOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        StretchRectToFullscreen(overlayRect);

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.01f);
        overlayImage.raycastTarget = true;

        continueButton = overlay.GetComponent<Button>();
        continueButton.targetGraphic = overlayImage;
        continueButton.onClick.AddListener(OnContinuePromptClicked);

        continueTitleText = CreateOverlayText("PromptText", overlay.transform, 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = continueTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.12f, 0.30f);
        titleRect.anchorMax = new Vector2(0.88f, 0.74f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        continueHintText = CreateOverlayText("PromptHint", overlay.transform, 28, FontStyle.Normal, TextAnchor.MiddleCenter);
        RectTransform hintRect = continueHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.26f, 0.12f);
        hintRect.anchorMax = new Vector2(0.74f, 0.20f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        continueOverlay = overlay;
        continueOverlay.transform.SetAsLastSibling();
        continueOverlay.SetActive(false);
    }

    private Text CreateOverlayText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = LoadBuiltinOverlayFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.25f;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(18, fontSize - 12);
        text.resizeTextMaxSize = fontSize;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        return text;
    }

    private Font LoadBuiltinOverlayFont()
    {
        Font font = null;

        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"FinalSceneStory: failed to load built-in font 'LegacyRuntime.ttf'. {e.Message}");
        }

        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
            }
        }

        return font;
    }

    private void OnContinuePromptClicked()
    {
        overlayAdvanceRequested = true;
    }

    private IEnumerator WaitForOverlayAdvance()
    {
        overlayAdvanceRequested = false;
        while (!overlayAdvanceRequested)
        {
            yield return null;
        }
        overlayAdvanceRequested = false;
    }

    private void ConfigureOverlayForPrompt()
    {
        if (continueTitleText == null || continueHintText == null)
        {
            return;
        }

        continueTitleText.fontSize = 54;
        continueTitleText.fontStyle = FontStyle.Bold;
        continueTitleText.alignment = TextAnchor.MiddleCenter;
        continueTitleText.resizeTextMinSize = 28;
        continueTitleText.resizeTextMaxSize = 54;

        RectTransform titleRect = continueTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.12f, 0.30f);
        titleRect.anchorMax = new Vector2(0.88f, 0.74f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        continueHintText.fontSize = 28;
        continueHintText.alignment = TextAnchor.MiddleCenter;

        RectTransform hintRect = continueHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.26f, 0.12f);
        hintRect.anchorMax = new Vector2(0.74f, 0.20f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
    }

    private void ConfigureOverlayForEnding(bool isCreditsPage)
    {
        if (continueTitleText == null || continueHintText == null)
        {
            return;
        }

        continueTitleText.fontSize = isCreditsPage ? 48 : 42;
        continueTitleText.fontStyle = isCreditsPage ? FontStyle.Bold : FontStyle.Normal;
        continueTitleText.alignment = TextAnchor.MiddleCenter;
        continueTitleText.resizeTextMinSize = isCreditsPage ? 28 : 24;
        continueTitleText.resizeTextMaxSize = isCreditsPage ? 48 : 42;

        RectTransform titleRect = continueTitleText.rectTransform;
        titleRect.anchorMin = isCreditsPage ? new Vector2(0.14f, 0.24f) : new Vector2(0.10f, 0.20f);
        titleRect.anchorMax = isCreditsPage ? new Vector2(0.86f, 0.76f) : new Vector2(0.90f, 0.78f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        continueHintText.fontSize = 26;
        continueHintText.alignment = TextAnchor.MiddleCenter;

        RectTransform hintRect = continueHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.20f, 0.08f);
        hintRect.anchorMax = new Vector2(0.80f, 0.15f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
    }

    private void SetOverlayText(string body, string hint)
    {
        if (continueTitleText != null)
        {
            continueTitleText.text = body;
            Color titleColor = continueTitleText.color;
            titleColor.a = 1f;
            continueTitleText.color = titleColor;
        }

        if (continueHintText != null)
        {
            continueHintText.text = hint;
            Color hintColor = continueHintText.color;
            hintColor.a = 1f;
            continueHintText.color = hintColor;
        }
    }

    private IEnumerator FadeOverlayTextIn(string body, string hint)
    {
        SetOverlayText(body, hint);

        if (continueTitleText == null || continueHintText == null)
        {
            yield break;
        }

        Color titleColor = continueTitleText.color;
        Color hintColor = continueHintText.color;
        titleColor.a = 0f;
        hintColor.a = 0f;
        continueTitleText.color = titleColor;
        continueHintText.color = hintColor;

        float duration = Mathf.Max(0.2f, overlayTextFadeDuration);
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            titleColor.a = eased;
            hintColor.a = eased;
            continueTitleText.color = titleColor;
            continueHintText.color = hintColor;
            yield return null;
        }

        titleColor.a = 1f;
        hintColor.a = 1f;
        continueTitleText.color = titleColor;
        continueHintText.color = hintColor;
    }

    private void StartEndingSequence(EndingType endingType)
    {
        if (endingSequenceRunning)
        {
            return;
        }

        StartCoroutine(PlayEndingSequence(endingType));
    }

    private IEnumerator PlayEndingSequence(EndingType endingType)
    {
        endingSequenceRunning = true;
        SetPlayerControlEnabled(false, true);
        yield return FadeBlackTo(1.0f);

        EnsureContinueOverlay();
        continueOverlay.SetActive(true);
        overlayInputEnabled = true;
        overlayHintBlinking = true;

        string[] pages = endingType == EndingType.DrawSword ? DrawSwordEndingPages : RefuseSwordEndingPages;
        for (int i = 0; i < pages.Length; i++)
        {
            ConfigureOverlayForEnding(false);
            string hint = "点击继续";
            yield return FadeOverlayTextIn(pages[i], hint);
            yield return WaitForOverlayAdvance();
        }

        ConfigureOverlayForEnding(true);
        overlayHintBlinking = false;
        yield return FadeOverlayTextIn(EndingCreditsPage, string.Empty);

        overlayInputEnabled = false;
        overlayHintBlinking = false;
    }

    private void AppendDialogContents(List<DialogContent> target, DialogConfig source)
    {
        if (source == null || source.contents == null)
        {
            return;
        }

        for (int i = 0; i < source.contents.Count; i++)
        {
            DialogContent content = source.contents[i];
            if (content == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(content.name) && string.IsNullOrWhiteSpace(content.content))
            {
                continue;
            }

            target.Add(CreateDialogContent(content.name, content.content));
        }
    }

    private DialogConfig CreateRuntimeDialog(IEnumerable<DialogContent> contents)
    {
        DialogConfig dialog = ScriptableObject.CreateInstance<DialogConfig>();
        dialog.contents = new List<DialogContent>(contents);
        dialog.nextDialog = new List<DialogSelection>();
        return dialog;
    }

    private DialogContent CreateDialogContent(string speakerName, string content)
    {
        return new DialogContent
        {
            name = speakerName,
            content = content
        };
    }

    private void TeleportPlayerTo(Transform targetPoint)
    {
        if (targetPoint == null)
        {
            return;
        }

        if (PlayerInputManager.Instance != null && PlayerInputManager.Instance.moveController != null)
        {
            PlayerInputManager.Instance.moveController.SetPositionAndRotation(targetPoint);
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerObject.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
        }
    }

    private void ApplyBossHealthMultiplier()
    {
        if (bossHealthBoostApplied || kingFightAttributes == null || bossHealthMultiplier <= 1.0f)
        {
            return;
        }

        int currentMaxHealth = Mathf.Max(1, kingFightAttributes.MaxHealth);
        int targetMaxHealth = Mathf.Max(currentMaxHealth + 1, Mathf.RoundToInt(currentMaxHealth * bossHealthMultiplier));
        int requiredConstitution = Mathf.Max(
            kingFightAttributes.Constitution,
            Mathf.CeilToInt((targetMaxHealth - kingFightAttributes.Strength * 2f) / 16f));

        kingFightAttributes.Constitution = requiredConstitution;
        kingFightAttributes.InitAttributes();
        bossHealthBoostApplied = true;

        Debug.Log($"FinalSceneStory: boosted king max health from {currentMaxHealth} to {kingFightAttributes.MaxHealth}.");
    }

    private Transform ResolveKingTarget()
    {
        if (mark_king != null) return mark_king;
        if (kingFightAttributes != null) return kingFightAttributes.transform;
        if (king != null) return king.transform;
        if (kingNPC != null) return kingNPC.transform;
        return null;
    }

    private Transform ResolveMerlinTarget()
    {
        if (mark_merlin != null) return mark_merlin;
        if (merlinNPC != null) return merlinNPC.transform;
        if (merlin != null) return merlin.transform;
        return null;
    }

    private void SetDestinationTarget(Transform target, float hideRange)
    {
        if (GameUIManager.Instance == null || GameUIManager.Instance.destinationMark == null || target == null)
        {
            return;
        }

        GameUIManager.Instance.destinationMark.SetTarget(target, hideRange);
    }

    private void HideDestinationMark()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.destinationMark != null)
        {
            GameUIManager.Instance.destinationMark.HideMark();
        }
    }

    private void UpdateMainTask(string title, string description)
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.mainTaskTip != null)
        {
            GameUIManager.Instance.mainTaskTip.UpdateTask(title, description);
        }
    }

    public void DriveProcess()
    {
        storyProcess++;
        UpdateStory();
    }

    public void Chat(DialogObject dialogObject)
    {
        if (dialogObject == null) return;

        if (!dialogObject.gameObject.activeSelf) dialogObject.gameObject.SetActive(true);
        dialogObject.Interact();
    }

    public void PlayerInputActive(bool active)
    {
        SetPlayerControlEnabled(active, false);
    }

    private void SetPlayerControlEnabled(bool active, bool cursorVisible)
    {
        if (!active)
        {
            if (GameMenu.Instance != null) GameMenu.Instance.CloseMenu();
            if (InventoryManager.Instance != null) InventoryManager.Instance.CloseInventory();
        }

        if (PlayerInputManager.Instance == null)
        {
            return;
        }

        if (active)
        {
            PlayerInputManager.Instance.OpenAllInput();
        }
        else
        {
            PlayerInputManager.Instance.CloseControllInput(cursorVisible);
            if (InventoryManager.Instance != null) InventoryManager.Instance.packageCanOpen = false;
            if (GameMenu.Instance != null) GameMenu.Instance.menuCanOpen = false;
        }
    }

    public void SetPlayerPosition(Transform pos)
    {
        TeleportPlayerTo(pos);
    }

    private void RestoreDialogDefaultsInEditor()
    {
#if UNITY_EDITOR
        if (dialog_4_0 == null) dialog_4_0 = AssetDatabase.LoadAssetAtPath<DialogConfig>(DefaultDialog40Path);
        if (dialog_4_1 == null) dialog_4_1 = AssetDatabase.LoadAssetAtPath<DialogConfig>(DefaultDialog41Path);
        if (dialog_4_2 == null) dialog_4_2 = AssetDatabase.LoadAssetAtPath<DialogConfig>(DefaultDialog42Path);
#endif
    }
}
