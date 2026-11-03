using System;
using System.Collections.Generic;
using System.Globalization;
using MeWorld.Core;
using MeWorld.Nations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MeWorld.UI
{
    public class RoWorldHUDController : MonoBehaviour
    {
        private TerritoryAndCombatManager _territoryManager;

        // UI State exposed to GameManager
        public float SelectedAttackRatio { get; private set; } = 0.20f;
        public StructureType SelectedAction { get; private set; } = StructureType.None;
        public bool IsPaused { get; private set; } = false;

        public Action OnFocusPlayerRequested;
        public Action OnRestartRequested;

        // Top HUD text references
        private TextMeshProUGUI _protectedTimerText;
        private TextMeshProUGUI _youControlText;
        private TextMeshProUGUI _leaderControlText;
        private RectTransform _youProgressFill;
        private RectTransform _leaderProgressFill;
        private TextMeshProUGUI _topRightRankText;

        // Bottom HUD text references
        private TextMeshProUGUI _pauseBtnText;
        private TextMeshProUGUI _playerWorldSubText;
        private TextMeshProUGUI _troopsGrowthText;
        private TextMeshProUGUI _troopsValueText;
        private RectTransform _troopsBarFill;
        private TextMeshProUGUI _goldGrowthText;
        private TextMeshProUGUI _goldValueText;
        private TextMeshProUGUI _attackPctHeaderText;
        private TextMeshProUGUI _attackSendsSubText;
        private TextMeshProUGUI _statusBannerText;
        private float _statusBannerTimer;

        // Preset buttons & Action buttons
        private readonly List<Image> _ratioButtonBgs = new List<Image>(7);
        private readonly List<TextMeshProUGUI> _ratioButtonLabels = new List<TextMeshProUGUI>(7);

        private class ActionBtnUI
        {
            public StructureType Type;
            public Image BgImage;
            public Image BorderImage;
            public TextMeshProUGUI CostText;
        }
        private readonly List<ActionBtnUI> _actionButtons = new List<ActionBtnUI>(10);

        // Victory / Defeat modal
        private GameObject _endGameModal;
        private TextMeshProUGUI _endGameTitle;
        private TextMeshProUGUI _endGameSubtitle;

        // Color Palette matching RoWorld dark HUD
        private static readonly Color ColPanelDark = new Color32(19, 23, 32, 238);
        private static readonly Color ColPanelInner = new Color32(28, 34, 46, 255);
        private static readonly Color ColGreenBright = new Color32(72, 219, 128, 255);
        private static readonly Color ColGoldBright = new Color32(246, 208, 72, 255);
        private static readonly Color ColOrangeAccent = new Color32(245, 158, 42, 255);
        private static readonly Color ColTextMuted = new Color32(156, 168, 188, 255);

        public void Initialize(TerritoryAndCombatManager territoryManager)
        {
            _territoryManager = territoryManager;
            EnsureEventSystem();
            BuildCanvasAndHUD();
            SelectAttackRatioIndex(1); // Default 20% like screenshot
            RefreshHUD();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(transform, false);
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        public void ClearSelectedAction()
        {
            SelectedAction = StructureType.None;
            UpdateActionButtonsVisual();
        }

        public void ShowStatusMessage(string msg, float duration = 3.0f)
        {
            if (_statusBannerText == null) return;
            _statusBannerText.text = msg;
            _statusBannerText.transform.parent.gameObject.SetActive(true);
            _statusBannerTimer = duration;
        }

        public void ShowEndGameModal(bool victory, string reason)
        {
            if (_endGameModal == null) return;
            _endGameModal.SetActive(true);
            _endGameTitle.text = victory ? "WORLD CONQUERED!" : "NATION DEFEATED";
            _endGameTitle.color = victory ? ColGoldBright : new Color32(245, 85, 85, 255);
            _endGameSubtitle.text = reason;
        }

        private void BuildCanvasAndHUD()
        {
            GameObject canvasObj = new GameObject("RoWorld_HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform root = canvasObj.GetComponent<RectTransform>();

            BuildTopLeftButtons(root);
            BuildTopCenterWorldControlPill(root);
            BuildTopRightRankPill(root);
            BuildBottomControlBar(root);
            BuildStatusNotificationBanner(root);
            BuildEndGameOverlay(root);
        }

        private void BuildTopLeftButtons(RectTransform root)
        {
            RectTransform container = CreateRect("TopLeftCircularBar", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -18), new Vector2(280, 44));

            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                RectTransform btnRt = CreateRect($"QuickBtn_{i}", container, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 44f, 0), new Vector2(38, 38));
                Image bg = btnRt.gameObject.AddComponent<Image>();
                bg.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
                bg.color = ColPanelDark;

                Button btn = btnRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() =>
                {
                    if (idx == 1) OnFocusPlayerRequested?.Invoke();
                    else if (idx == 4) OnRestartRequested?.Invoke();
                    else ShowStatusMessage(idx == 0 ? "Audio Toggled" : "RoWorld PvP Active");
                });

                RectTransform iconRt = CreateRect($"Icon_{i}", btnRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
                Image icon = iconRt.gameObject.AddComponent<Image>();
                icon.sprite = ProceduralSpriteFactory.GetTopBarIcon(idx);
                icon.color = Color.white;
                icon.raycastTarget = false;
            }
        }

        private void BuildTopCenterWorldControlPill(RectTransform root)
        {
            RectTransform pill = CreatePanel("TopWorldControlPill", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-50f, -14f), new Vector2(540f, 66f), ColPanelDark);

            // Left Protected Box
            RectTransform protBox = CreatePanel("ProtectedBox", pill, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(128f, 50f), ColPanelInner);

            TextMeshProUGUI protLabel = CreateTMP("ProtHeader", protBox, "PROTECTED", 11, FontStyles.Bold, ColGreenBright, TextAlignmentOptions.Center);
            SetRect(protLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(0, 16));

            _protectedTimerText = CreateTMP("ProtTimer", protBox, "2:37", 21, FontStyles.Bold, ColGreenBright, TextAlignmentOptions.Center);
            SetRect(_protectedTimerText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(0, 26));

            // Right World Control Section
            RectTransform wcBox = CreateRect("WorldControlArea", pill, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            wcBox.offsetMin = new Vector2(148f, 6f);
            wcBox.offsetMax = new Vector2(-14f, -6f);

            TextMeshProUGUI wcTitle = CreateTMP("WCTitle", wcBox, "WORLD CONTROL", 11, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(wcTitle.rectTransform, new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(0, 1), new Vector2(0, -2), new Vector2(0, 16));

            TextMeshProUGUI goalTitle = CreateTMP("GoalTitle", wcBox, "GOAL 80%", 11, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Right);
            SetRect(goalTitle.rectTransform, new Vector2(0.6f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -2), new Vector2(0, 16));

            // Progress Bar Track
            RectTransform barTrack = CreatePanel("BarTrack", wcBox, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 2f), new Vector2(0, 10f), ColPanelInner);

            _youProgressFill = CreateRect("YouFill", barTrack, new Vector2(0, 0), new Vector2(0.05f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            Image youImg = _youProgressFill.gameObject.AddComponent<Image>();
            youImg.color = new Color32(236, 72, 153, 255); // Pink player bar

            _leaderProgressFill = CreateRect("LeaderFill", barTrack, new Vector2(0.05f, 0), new Vector2(0.14f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            Image leadImg = _leaderProgressFill.gameObject.AddComponent<Image>();
            leadImg.color = new Color32(59, 130, 246, 255); // Blue leader bar

            // Bottom labels: "You 0.18%" and "#1 India 0.35%"
            _youControlText = CreateTMP("YouControlTxt", wcBox, "<color=#EC4899>[*]</color> You  <b>0.18%</b>", 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            SetRect(_youControlText.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 18));

            _leaderControlText = CreateTMP("LeaderControlTxt", wcBox, "<color=#3B82F6>[*]</color> #1 India  <b>0.35%</b>", 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Right);
            SetRect(_leaderControlText.rectTransform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, 18));
        }

        private void BuildTopRightRankPill(RectTransform root)
        {
            RectTransform rankPill = CreatePanel("TopRightRankPill", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(150f, 36f), ColPanelDark);
            _topRightRankText = CreateTMP("RankTxt", rankPill, "#210 • 0.18%", 13f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(_topRightRankText.rectTransform);
        }

        private void BuildBottomControlBar(RectTransform root)
        {
            RectTransform bar = CreatePanel("BottomDockBar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1520f, 84f), ColPanelDark);

            // 1. Pause Button & Player Identity (x: 12 .. 225)
            RectTransform pauseBtnRt = CreatePanel("PauseBtn", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 44f), ColPanelInner);
            Button pauseBtn = pauseBtnRt.gameObject.AddComponent<Button>();
            _pauseBtnText = CreateTMP("PauseSymbol", pauseBtnRt, "||", 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(_pauseBtnText.rectTransform);
            pauseBtn.onClick.AddListener(() =>
            {
                IsPaused = !IsPaused;
                _pauseBtnText.text = IsPaused ? ">" : "||";
            });

            RectTransform playerInfoRt = CreateRect("PlayerIdentity", bar, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(64f, 0f), new Vector2(165f, 0f));

            RectTransform crownIconRt = CreateRect("CrownIcon", playerInfoRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2f, 11f), new Vector2(16f, 16f));
            Image crownImg = crownIconRt.gameObject.AddComponent<Image>();
            crownImg.sprite = ProceduralSpriteFactory.GetCrownSprite();
            crownImg.color = ColGoldBright;

            TextMeshProUGUI pName = CreateTMP("PlayerName", playerInfoRt, "SnickTrix", 16, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(pName.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(22f, 11f), new Vector2(-22f, 22f));

            _playerWorldSubText = CreateTMP("PlayerWorldSub", playerInfoRt, "0.18% of the world", 12, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(_playerWorldSubText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(2f, -11f), new Vector2(-4f, 18f));

            // 2. TROOPS Box (x: 235 .. 435)
            RectTransform troopsBox = CreatePanel("TroopsBox", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(235f, 0f), new Vector2(198f, 66f), ColPanelInner);
            TextMeshProUGUI trHeader = CreateTMP("TrHeader", troopsBox, "TROOPS", 11, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(trHeader.rectTransform, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(0, 16));

            _troopsGrowthText = CreateTMP("TrGrowth", troopsBox, "+140/s", 11, FontStyles.Bold, ColGreenBright, TextAlignmentOptions.Right);
            SetRect(_troopsGrowthText.rectTransform, new Vector2(0.55f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -6), new Vector2(0, 16));

            _troopsValueText = CreateTMP("TrValue", troopsBox, "7.47K <size=72%><color=#8E9BAE>/ 20.5K</color></size>", 19, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(_troopsValueText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(10, -3), new Vector2(-20, 26));

            RectTransform trBarTrack = CreatePanel("TrBarTrack", troopsBox, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(-20, 6), new Color32(15, 18, 25, 255));
            _troopsBarFill = CreateRect("TrBarFill", trBarTrack, new Vector2(0, 0), new Vector2(0.36f, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            Image trFillImg = _troopsBarFill.gameObject.AddComponent<Image>();
            trFillImg.color = new Color32(56, 189, 248, 255);

            // 3. GOLD Box (x: 442 .. 600)
            RectTransform goldBox = CreatePanel("GoldBox", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(442f, 0f), new Vector2(155f, 66f), ColPanelInner);
            TextMeshProUGUI gHeader = CreateTMP("GHeader", goldBox, "GOLD", 11, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(gHeader.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(0, 16));

            _goldGrowthText = CreateTMP("GGrowth", goldBox, "+1.26K/s", 11, FontStyles.Bold, ColGreenBright, TextAlignmentOptions.Right);
            SetRect(_goldGrowthText.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -6), new Vector2(0, 16));

            _goldValueText = CreateTMP("GValue", goldBox, "775K", 21, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Left);
            SetRect(_goldValueText.rectTransform, new Vector2(0, 0), new Vector2(1, 0.65f), new Vector2(0, 0), new Vector2(10, 6), new Vector2(-20, 0));

            // 4. ATTACK Ratio Selector Box (x: 606 .. 892)
            RectTransform atkBox = CreatePanel("AttackBox", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(606f, 0f), new Vector2(284f, 66f), ColPanelInner);
            TextMeshProUGUI atkTitle = CreateTMP("AtkTitle", atkBox, "ATTACK", 11, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(atkTitle.rectTransform, new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(0, 1), new Vector2(10, -4), new Vector2(0, 15));

            _attackPctHeaderText = CreateTMP("AtkPctHead", atkBox, "20%", 12, FontStyles.Bold, ColOrangeAccent, TextAlignmentOptions.Right);
            SetRect(_attackPctHeaderText.rectTransform, new Vector2(0.6f, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -4), new Vector2(0, 15));

            // 7 preset buttons: 10% 20% 35% 50% 65% 80% 100%
            _ratioButtonBgs.Clear();
            _ratioButtonLabels.Clear();
            float btnW = 36f;
            for (int i = 0; i < GameConfig.AttackPresets.Length; i++)
            {
                int presetIndex = i;
                int pctInt = Mathf.RoundToInt(GameConfig.AttackPresets[i] * 100f);
                RectTransform pBtnRt = CreatePanel($"Preset_{pctInt}", atkBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8f + i * 38.5f, -1f), new Vector2(btnW, 22f), new Color32(18, 22, 30, 255));
                Image pBg = pBtnRt.GetComponent<Image>();
                Button b = pBtnRt.gameObject.AddComponent<Button>();
                b.targetGraphic = pBg;
                b.onClick.AddListener(() => SelectAttackRatioIndex(presetIndex));

                TextMeshProUGUI lbl = CreateTMP("Lbl", pBtnRt, $"{pctInt}%", 10.5f, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Center);
                StretchFull(lbl.rectTransform);

                _ratioButtonBgs.Add(pBg);
                _ratioButtonLabels.Add(lbl);
            }

            _attackSendsSubText = CreateTMP("AtkSendsSub", atkBox, "Sends 1.49K troops", 10.5f, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(_attackSendsSubText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(10, 3), new Vector2(-20, 14));

            // 5. Right 10 Building & Nuclear Action Buttons (x: 900 .. 1505)
            _actionButtons.Clear();
            StructureType[] types =
            {
                StructureType.City,
                StructureType.Port,
                StructureType.Factory,
                StructureType.DefensePost,
                StructureType.SAMLauncher,
                StructureType.MissileSilo,
                StructureType.AtomBomb,
                StructureType.HydrogenBomb,
                StructureType.Warship,
                StructureType.Alliance
            };

            for (int i = 0; i < types.Length; i++)
            {
                StructureType st = types[i];
                RectTransform actRt = CreatePanel($"ActionBtn_{st}", bar, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(900f + i * 60f, 0f), new Vector2(55f, 66f), ColPanelInner);
                Image bgImg = actRt.GetComponent<Image>();

                // Highlight border when selected
                RectTransform borderRt = CreatePanel("SelBorder", actRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color32(246, 208, 72, 0));
                Image borderImg = borderRt.GetComponent<Image>();
                borderImg.raycastTarget = false;

                // Procedural Icon
                RectTransform iconRt = CreateRect("Icon", actRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8f), new Vector2(30f, 30f));
                Image iconImg = iconRt.gameObject.AddComponent<Image>();
                iconImg.sprite = ProceduralSpriteFactory.GetActionIcon(st);
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;

                // Cost label at bottom
                string costStr = GameConfig.GetActionCostLabel(st);
                TextMeshProUGUI costTxt = CreateTMP("Cost", actRt, costStr, 11.5f, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
                SetRect(costTxt.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 4f), new Vector2(0, 16f));
                costTxt.raycastTarget = false;

                Button actBtn = actRt.gameObject.AddComponent<Button>();
                actBtn.targetGraphic = bgImg;
                actBtn.onClick.AddListener(() => ToggleSelectAction(st));

                _actionButtons.Add(new ActionBtnUI
                {
                    Type = st,
                    BgImage = bgImg,
                    BorderImage = borderImg,
                    CostText = costTxt
                });
            }
        }

        private void BuildStatusNotificationBanner(RectTransform root)
        {
            RectTransform banner = CreatePanel("StatusBanner", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 108f), new Vector2(560f, 34f), ColPanelDark);
            _statusBannerText = CreateTMP("BannerTxt", banner, "", 14, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            StretchFull(_statusBannerText.rectTransform);
            banner.gameObject.SetActive(false);
        }

        private void BuildEndGameOverlay(RectTransform root)
        {
            _endGameModal = new GameObject("EndGameModal", typeof(RectTransform), typeof(Image));
            RectTransform rt = _endGameModal.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            StretchFull(rt);
            _endGameModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.72f);

            RectTransform box = CreatePanel("ModalBox", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 250f), ColPanelDark);

            _endGameTitle = CreateTMP("Title", box, "WORLD CONQUERED!", 32, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(_endGameTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(-30, 44));

            _endGameSubtitle = CreateTMP("Sub", box, "You reached 80% World Control!", 17, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
            SetRect(_endGameSubtitle.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(-40, 50));

            RectTransform btnRt = CreatePanel("PlayAgainBtn", box, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(200, 48), new Color32(37, 99, 235, 255));
            Button btn = btnRt.gameObject.AddComponent<Button>();
            TextMeshProUGUI btnTxt = CreateTMP("BtnTxt", btnRt, "PLAY AGAIN", 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(btnTxt.rectTransform);
            btn.onClick.AddListener(() =>
            {
                _endGameModal.SetActive(false);
                OnRestartRequested?.Invoke();
            });

            _endGameModal.SetActive(false);
        }

        private void SelectAttackRatioIndex(int index)
        {
            index = Mathf.Clamp(index, 0, GameConfig.AttackPresets.Length - 1);
            SelectedAttackRatio = GameConfig.AttackPresets[index];

            for (int i = 0; i < _ratioButtonBgs.Count; i++)
            {
                bool active = (i == index);
                _ratioButtonBgs[i].color = active ? new Color32(180, 108, 24, 255) : new Color32(18, 22, 30, 255);
                _ratioButtonLabels[i].color = active ? Color.white : ColTextMuted;
            }
            RefreshHUD();
        }

        private void ToggleSelectAction(StructureType type)
        {
            if (SelectedAction == type)
            {
                SelectedAction = StructureType.None;
                ShowStatusMessage("Action cancelled. Click any neighbor to attack.", 1.8f);
            }
            else
            {
                SelectedAction = type;
                ShowStatusMessage($"Selected: {GameConfig.GetActionDisplayName(type)} — Click on map to place/fire (Right-click to cancel)", 4.0f);
            }
            UpdateActionButtonsVisual();
        }

        private void UpdateActionButtonsVisual()
        {
            NationData player = _territoryManager != null ? _territoryManager.PlayerNation : null;
            double playerGold = player != null ? player.Gold : 0.0;

            for (int i = 0; i < _actionButtons.Count; i++)
            {
                ActionBtnUI ab = _actionButtons[i];
                bool isSelected = (ab.Type == SelectedAction);
                double cost = GameConfig.GetActionCost(ab.Type);
                bool canAfford = playerGold >= cost;

                ab.BgImage.color = isSelected ? new Color32(48, 62, 92, 255) : ColPanelInner;
                ab.BorderImage.color = isSelected ? new Color32(246, 208, 72, 90) : new Color32(0, 0, 0, 0);
                ab.CostText.color = canAfford ? ColGoldBright : new Color32(160, 110, 110, 200);
            }
        }

        private void Update()
        {
            if (_statusBannerTimer > 0f)
            {
                _statusBannerTimer -= Time.unscaledDeltaTime;
                if (_statusBannerTimer <= 0f && _statusBannerText != null)
                {
                    _statusBannerText.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        public void RefreshHUD()
        {
            if (_territoryManager == null || _territoryManager.PlayerNation == null) return;

            NationData player = _territoryManager.PlayerNation;
            NationData leader = _territoryManager.LeadingNation ?? player;

            // Protected timer
            _protectedTimerText.text = GameConfig.FormatTime(_territoryManager.ProtectionTimer);

            // World control percentages
            float playerPct = player.WorldControlPercent * 100f;
            float leaderPct = leader.WorldControlPercent * 100f;
            string pPctStr = playerPct.ToString("0.00", CultureInfo.InvariantCulture) + "%";
            string lPctStr = leaderPct.ToString("0.00", CultureInfo.InvariantCulture) + "%";

            _youControlText.text = $"<color=#EC4899>[*]</color> You  <b>{pPctStr}</b>";
            _leaderControlText.text = $"<color=#3B82F6>[*]</color> #1 {leader.Name}  <b>{lPctStr}</b>";

            float barNormYou = Mathf.Clamp01(player.WorldControlPercent / GameConfig.WinControlPercentage);
            float barNormLead = Mathf.Clamp01(leader.WorldControlPercent / GameConfig.WinControlPercentage);
            _youProgressFill.anchorMax = new Vector2(Mathf.Max(0.02f, barNormYou), 1f);
            _leaderProgressFill.anchorMin = new Vector2(Mathf.Max(0.02f, barNormYou), 0f);
            _leaderProgressFill.anchorMax = new Vector2(Mathf.Clamp(barNormYou + barNormLead, 0.05f, 1f), 1f);

            // Top Right Rank Pill - matching reference screenshot #210 • 0.18%
            _topRightRankText.text = $"#{_territoryManager.PlayerRank} • {pPctStr}";

            // Bottom Player World Subtext
            _playerWorldSubText.text = $"{pPctStr} of the world";

            // Troops Section
            _troopsGrowthText.text = $"+{GameConfig.FormatNumber(player.TroopGrowthPerSec)}/s";
            _troopsValueText.text = $"{GameConfig.FormatNumber(player.Troops)} <size=72%><color=#8E9BAE>/ {GameConfig.FormatNumber(player.MaxTroops)}</color></size>";
            float troopFill = player.MaxTroops > 0 ? Mathf.Clamp01((float)(player.Troops / player.MaxTroops)) : 0f;
            _troopsBarFill.anchorMax = new Vector2(troopFill, 1f);

            // Gold Section
            _goldGrowthText.text = $"+{GameConfig.FormatNumber(player.GoldGrowthPerSec)}/s";
            _goldValueText.text = GameConfig.FormatNumber(player.Gold);

            // Attack Section
            int atkPctInt = Mathf.RoundToInt(SelectedAttackRatio * 100f);
            _attackPctHeaderText.text = $"{atkPctInt}%";
            double sendsAmount = player.Troops * SelectedAttackRatio;
            _attackSendsSubText.text = $"Sends {GameConfig.FormatNumber(sendsAmount)} troops";

            UpdateActionButtonsVisual();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ProceduralSpriteFactory.GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
