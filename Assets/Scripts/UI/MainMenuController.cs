using System;
using System.Collections.Generic;
using MeWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MeWorld.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public Action<int, GameDifficulty, GamePlayMode> OnStartGameRequested;

        // UI Roots
        private GameObject _canvasObj;
        private Canvas _canvas;
        private RectTransform _mainMenuRoot;
        private RectTransform _soloSetupRoot;
        private RectTransform _settingsModalRoot;
        private RectTransform _multiplayerModalRoot;
        private RectTransform _tutorialModalRoot;

        // Solo Setup State
        private GameDifficulty _selectedDifficulty = GameDifficulty.Normal;
        private GamePlayMode _selectedMode = GamePlayMode.SoloConquest;
        private int _currentSeed = 20261103;

        private readonly List<Button> _difficultyButtons = new List<Button>();
        private readonly List<Image> _difficultyBgs = new List<Image>();
        private readonly List<Button> _modeButtons = new List<Button>();
        private readonly List<Image> _modeBgs = new List<Image>();
        private TextMeshProUGUI _seedValueText;
        private TextMeshProUGUI _nationNameValueText;

        // Settings State
        private GameObject _keybindsTabObj;
        private GameObject _audioDisplayTabObj;
        private Button _tabKeybindsBtn;
        private Button _tabAudioDisplayBtn;
        private Image _tabKeybindsBg;
        private Image _tabAudioDisplayBg;

        private readonly Dictionary<KeyAction, TextMeshProUGUI> _keybindLabelTexts = new Dictionary<KeyAction, TextMeshProUGUI>();
        private readonly Dictionary<KeyAction, Image> _keybindButtonBgs = new Dictionary<KeyAction, Image>();
        private Slider _masterVolumeSlider;
        private TextMeshProUGUI _masterVolumeValueText;
        private Image _muteToggleBg;
        private TextMeshProUGUI _muteToggleLabel;
        private Image _labelsToggleBg;
        private TextMeshProUGUI _labelsToggleLabel;
        private Image _smoothCamToggleBg;
        private TextMeshProUGUI _smoothCamToggleLabel;

        // Palette
        private static readonly Color ColBgDark = new Color32(13, 17, 23, 250);
        private static readonly Color ColPanelDark = new Color32(19, 23, 32, 240);
        private static readonly Color ColPanelInner = new Color32(28, 34, 46, 255);
        private static readonly Color ColButtonHover = new Color32(40, 48, 64, 255);
        private static readonly Color ColGreenBright = new Color32(72, 219, 128, 255);
        private static readonly Color ColGoldBright = new Color32(246, 208, 72, 255);
        private static readonly Color ColOrangeAccent = new Color32(245, 158, 42, 255);
        private static readonly Color ColPinkPlayer = new Color32(233, 59, 129, 255);
        private static readonly Color ColCyanBright = new Color32(56, 189, 248, 255);
        private static readonly Color ColTextMuted = new Color32(156, 168, 188, 255);

        public void Initialize()
        {
            EnsureEventSystem();
            BuildCanvasAndScreens();
            _selectedDifficulty = KeybindManager.Instance.CurrentDifficulty;
            _selectedMode = KeybindManager.Instance.CurrentGameMode;
            _currentSeed = KeybindManager.Instance.CustomSeed;
            UpdateSoloSetupUI();
            RefreshSettingsDisplay();

            KeybindManager.Instance.OnKeybindChanged += (act, key) => RefreshKeybindButton(act);
            KeybindManager.Instance.OnRebindWaiting += (act) =>
            {
                if (_keybindLabelTexts.TryGetValue(act, out TextMeshProUGUI t))
                {
                    t.text = "TUŞA BASIN...";
                    t.color = ColGoldBright;
                }
                if (_keybindButtonBgs.TryGetValue(act, out Image bg))
                {
                    bg.color = ColOrangeAccent;
                }
            };
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(transform, false);
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (KeybindManager.Instance.IsRebinding)
            {
                KeybindManager.Instance.UpdateRebindingInput();
            }
        }

        public void ShowMenu()
        {
            if (_canvasObj != null) _canvasObj.SetActive(true);
            ShowScreen(MainMenuScreen.Main);
        }

        public void HideMenu()
        {
            if (_canvasObj != null) _canvasObj.SetActive(false);
        }

        public void OpenSettingsModalDirectly()
        {
            if (_canvasObj != null) _canvasObj.SetActive(true);
            ShowScreen(MainMenuScreen.Settings);
        }

        private enum MainMenuScreen
        {
            Main,
            SoloSetup,
            Settings,
            MultiplayerModal,
            TutorialModal
        }

        private void ShowScreen(MainMenuScreen screen)
        {
            _mainMenuRoot.gameObject.SetActive(screen == MainMenuScreen.Main);
            _soloSetupRoot.gameObject.SetActive(screen == MainMenuScreen.SoloSetup);
            _settingsModalRoot.gameObject.SetActive(screen == MainMenuScreen.Settings);
            _multiplayerModalRoot.gameObject.SetActive(screen == MainMenuScreen.MultiplayerModal);
            _tutorialModalRoot.gameObject.SetActive(screen == MainMenuScreen.TutorialModal);

            if (screen == MainMenuScreen.Settings)
            {
                RefreshSettingsDisplay();
            }
        }

        private void BuildCanvasAndScreens()
        {
            _canvasObj = new GameObject("RoWorld_MainMenu_Canvas");
            _canvasObj.transform.SetParent(transform, false);

            _canvas = _canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;

            CanvasScaler scaler = _canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform rootRt = _canvasObj.GetComponent<RectTransform>();

            // Fullscreen Background
            RectTransform bg = CreatePanel("MenuBackground", rootRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColBgDark);
            bg.offsetMin = Vector2.zero;
            bg.offsetMax = Vector2.zero;

            BuildMainMenuScreen(rootRt);
            BuildSoloSetupScreen(rootRt);
            BuildSettingsModal(rootRt);
            BuildMultiplayerModal(rootRt);
            BuildTutorialModal(rootRt);

            ShowScreen(MainMenuScreen.Main);
        }

        private void BuildMainMenuScreen(RectTransform root)
        {
            _mainMenuRoot = CreateRect("MainMenu_Root", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _mainMenuRoot.offsetMin = Vector2.zero;
            _mainMenuRoot.offsetMax = Vector2.zero;

            // Ambient Card Center
            RectTransform card = CreatePanel("CenterCard", _mainMenuRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(560, 680), ColPanelDark);

            // Title & Subtitle
            TextMeshProUGUI title = CreateTMP("GameTitle", card, "RoWorld", 46, FontStyles.Bold, ColPinkPlayer, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(0, 52));

            TextMeshProUGUI pvpBadge = CreateTMP("PvPBadge", card, "[ World PvP ]", 18, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(pvpBadge.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -82), new Vector2(0, 26));

            TextMeshProUGUI subDesc = CreateTMP("SubDesc", card, "88 AI Ulusu • 110 Kabile • %80 Dünya Zaferi", 13, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(subDesc.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(0, 22));

            // Buttons Stack
            float startY = -160f;
            float btnH = 62f;
            float spacing = 16f;

            // 1. Solo Play (CTA)
            CreateMainMenuButton(card, "SOLO OYNA", "Tek Oyunculu Fetih & İzleyici Modu", ProceduralSpriteFactory.GetSwordsIcon(), new Vector2(0, startY), ColGreenBright, () =>
            {
                ShowScreen(MainMenuScreen.SoloSetup);
            });

            // 2. Multiplayer (With Coming Soon Tag)
            startY -= (btnH + spacing);
            CreateMainMenuButton(card, "ÇOK OYUNCULU", "88 Kişilik Canlı Dünya PvP [YAKINDA]", ProceduralSpriteFactory.GetGlobeIcon(), new Vector2(0, startY), ColCyanBright, () =>
            {
                ShowScreen(MainMenuScreen.MultiplayerModal);
            });

            // 3. Settings (Ayarlar & Kısayollar)
            startY -= (btnH + spacing);
            CreateMainMenuButton(card, "AYARLAR", "Fabrika / Bina Kısayolları & Ses", ProceduralSpriteFactory.GetSettingsGearIcon(), new Vector2(0, startY), ColOrangeAccent, () =>
            {
                ShowScreen(MainMenuScreen.Settings);
            });

            // 4. How to Play
            startY -= (btnH + spacing);
            CreateMainMenuButton(card, "NASIL OYNANIR?", "Fetih, Lojistik ve Nükleer Silahlar", ProceduralSpriteFactory.GetBookIcon(), new Vector2(0, startY), ColGoldBright, () =>
            {
                ShowScreen(MainMenuScreen.TutorialModal);
            });

            // 5. Quit
            startY -= (btnH + spacing);
            CreateMainMenuButton(card, "ÇIKIŞ", "Oyundan Ayrıl", ProceduralSpriteFactory.GetDoorExitIcon(), new Vector2(0, startY), new Color32(239, 68, 68, 255), () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            // Footer
            TextMeshProUGUI footer = CreateTMP("Footer", card, "OpenFront Engine • RoWorld Remake • v1.2.0", 11, FontStyles.Normal, new Color32(100, 116, 139, 255), TextAlignmentOptions.Center);
            SetRect(footer.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(0, 20));
        }

        private void CreateMainMenuButton(RectTransform parent, string title, string sub, Sprite icon, Vector2 pos, Color accent, Action onClick)
        {
            RectTransform btnRt = CreatePanel("Btn_" + title, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(460, 62), ColPanelInner);
            Image bg = btnRt.GetComponent<Image>();

            Button btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = ColPanelInner;
            cb.highlightedColor = ColButtonHover;
            cb.pressedColor = new Color32(20, 24, 34, 255);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Left Color Accent Strip
            RectTransform strip = CreatePanel("Strip", btnRt, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(3, 0), new Vector2(5, 0), accent);
            strip.offsetMin = new Vector2(3, 6);
            strip.offsetMax = new Vector2(8, -6);

            // Icon
            RectTransform iconRt = CreateRect("Icon", btnRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(28, 28));
            Image iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.color = accent;
            iconImg.raycastTarget = false;

            // Title
            TextMeshProUGUI titleText = CreateTMP("Title", btnRt, title, 16, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(titleText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(68, 8), new Vector2(-20, 20));

            // Subtitle
            TextMeshProUGUI subText = CreateTMP("Sub", btnRt, sub, 11, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(subText.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(68, -12), new Vector2(-20, 16));
        }

        private void BuildSoloSetupScreen(RectTransform root)
        {
            _soloSetupRoot = CreateRect("SoloSetup_Root", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _soloSetupRoot.offsetMin = Vector2.zero;
            _soloSetupRoot.offsetMax = Vector2.zero;

            RectTransform card = CreatePanel("SoloCard", _soloSetupRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 740), ColPanelDark);

            // Header
            TextMeshProUGUI title = CreateTMP("SoloTitle", card, "TEK OYUNCULU AYARLARI", 26, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(0, 36));

            TextMeshProUGUI sub = CreateTMP("SoloSub", card, "Zorluk seviyenizi ve oyun modunuzu belirleyin", 13, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(sub.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -64), new Vector2(0, 22));

            // 1. Difficulty Section
            float curY = -105f;
            TextMeshProUGUI diffHeader = CreateTMP("DiffHeader", card, "ZORLUK SEVİYESİ", 13, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(diffHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(0, 20));

            curY -= 28f;
            string[] diffNames = { "KOLAY", "NORMAL", "ZOR", "KAOS" };
            float btnW = 135f;
            _difficultyButtons.Clear();
            _difficultyBgs.Clear();

            for (int i = 0; i < 4; i++)
            {
                int diffIdx = i;
                RectTransform bRt = CreatePanel("Diff_" + i, card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36 + i * (btnW + 8), curY), new Vector2(btnW, 40), ColPanelInner);
                Image bg = bRt.GetComponent<Image>();
                _difficultyBgs.Add(bg);

                Button btn = bRt.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _selectedDifficulty = (GameDifficulty)diffIdx;
                    KeybindManager.Instance.SetDifficulty(_selectedDifficulty);
                    UpdateSoloSetupUI();
                });
                _difficultyButtons.Add(btn);

                TextMeshProUGUI lbl = CreateTMP("Label", bRt, diffNames[i], 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                SetRect(lbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            // 2. Game Mode Section
            curY -= 65f;
            TextMeshProUGUI modeHeader = CreateTMP("ModeHeader", card, "OYUN MODU", 13, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(modeHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(0, 20));

            curY -= 28f;
            _modeButtons.Clear();
            _modeBgs.Clear();

            string[] modeNames = { "STANDART FETİH (OYUNCU)", "İZLEYİCİ MODU (88 AI)" };
            float modeBtnW = 278f;
            for (int m = 0; m < 2; m++)
            {
                int modeIdx = m;
                RectTransform mRt = CreatePanel("Mode_" + m, card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36 + m * (modeBtnW + 12), curY), new Vector2(modeBtnW, 44), ColPanelInner);
                Image bg = mRt.GetComponent<Image>();
                _modeBgs.Add(bg);

                Button btn = mRt.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    _selectedMode = (GamePlayMode)modeIdx;
                    KeybindManager.Instance.SetGameMode(_selectedMode);
                    UpdateSoloSetupUI();
                });
                _modeButtons.Add(btn);

                TextMeshProUGUI lbl = CreateTMP("Label", mRt, modeNames[m], 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                SetRect(lbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            // 3. Player Nation Name Section
            curY -= 60f;
            TextMeshProUGUI nameHeader = CreateTMP("NameHeader", card, "ÜLKE İSMİNİZ (NATION NAME)", 13, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(nameHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(0, 20));

            curY -= 26f;
            RectTransform nameBox = CreatePanel("NameBox", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(380, 40), ColPanelInner);
            _nationNameValueText = CreateTMP("NameVal", nameBox, KeybindManager.Instance.PlayerNationName, 15, FontStyles.Bold, ColPinkPlayer, TextAlignmentOptions.Left);
            SetRect(_nationNameValueText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(-16, 0));

            RectTransform rndNameBtnRt = CreatePanel("RndNameBtn", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(430, curY), new Vector2(174, 40), ColPanelInner);
            Button rndNameBtn = rndNameBtnRt.gameObject.AddComponent<Button>();
            rndNameBtn.onClick.AddListener(() =>
            {
                string nextName = KeybindManager.Instance.GetRandomPresetNationName();
                KeybindManager.Instance.SetPlayerNationName(nextName);
                if (_nationNameValueText != null) _nationNameValueText.text = nextName;
            });
            TextMeshProUGUI rndNameLbl = CreateTMP("RndNameLbl", rndNameBtnRt, "RASTGELE İSİM", 12, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(rndNameLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 4. World Seed Section
            curY -= 56f;
            TextMeshProUGUI seedHeader = CreateTMP("SeedHeader", card, "DÜNYA HARİTASI TOHUMU (SEED)", 13, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(seedHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(0, 20));

            curY -= 26f;
            RectTransform seedBox = CreatePanel("SeedBox", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(250, 40), ColPanelInner);
            _seedValueText = CreateTMP("SeedVal", seedBox, _currentSeed.ToString(), 15, FontStyles.Bold, ColCyanBright, TextAlignmentOptions.Left);
            SetRect(_seedValueText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(-16, 0));

            RectTransform rndBtnRt = CreatePanel("RandomSeedBtn", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(296, curY), new Vector2(140, 40), ColPanelInner);
            Button rndBtn = rndBtnRt.gameObject.AddComponent<Button>();
            rndBtn.onClick.AddListener(() =>
            {
                _currentSeed = UnityEngine.Random.Range(100000, 9999999);
                KeybindManager.Instance.CustomSeed = _currentSeed;
                _seedValueText.text = _currentSeed.ToString();
            });
            TextMeshProUGUI rndLbl = CreateTMP("RndLbl", rndBtnRt, "YENİ TOHUM", 11, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(rndLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform classicBtnRt = CreatePanel("ClassicSeedBtn", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(446, curY), new Vector2(158, 40), ColPanelInner);
            Button classicBtn = classicBtnRt.gameObject.AddComponent<Button>();
            classicBtn.onClick.AddListener(() =>
            {
                _currentSeed = 20261103;
                KeybindManager.Instance.CustomSeed = _currentSeed;
                _seedValueText.text = "20261103";
            });
            TextMeshProUGUI classicLbl = CreateTMP("ClassicLbl", classicBtnRt, "KLASİK HARİTA", 11, FontStyles.Bold, ColCyanBright, TextAlignmentOptions.Center);
            SetRect(classicLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 5. Mode Description Box
            curY -= 54f;
            RectTransform infoBox = CreatePanel("InfoBox", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, curY), new Vector2(568, 62), new Color32(23, 29, 40, 255));
            TextMeshProUGUI infoText = CreateTMP("InfoText", infoBox, "Standart Fetih: Oyun başladığında 10s süre içinde dünya haritasında dilediğiniz bir karaya tıklayarak doğuş bölgenizi seçin!\nİzleyici Modu: Kamera serbestçe dünyayı gezer, 88 AI ulusunun büyük savaşını izleyin.", 11.5f, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(infoText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12, 4), new Vector2(-12, -4));

            // Bottom Buttons: Back & Start Game
            float btmY = 32f;
            RectTransform backBtnRt = CreatePanel("BackBtn", card, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, btmY), new Vector2(160, 52), ColPanelInner);
            Button backBtn = backBtnRt.gameObject.AddComponent<Button>();
            backBtn.onClick.AddListener(() => ShowScreen(MainMenuScreen.Main));
            TextMeshProUGUI backLbl = CreateTMP("BackLbl", backBtnRt, "< GERİ", 14, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(backLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform startBtnRt = CreatePanel("StartGameBtn", card, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, btmY), new Vector2(390, 52), ColGreenBright);
            Button startBtn = startBtnRt.gameObject.AddComponent<Button>();
            startBtn.onClick.AddListener(() =>
            {
                HideMenu();
                OnStartGameRequested?.Invoke(_currentSeed, _selectedDifficulty, _selectedMode);
            });
            TextMeshProUGUI startLbl = CreateTMP("StartLbl", startBtnRt, "OYUNA BAŞLA", 17, FontStyles.Bold, new Color32(13, 23, 17, 255), TextAlignmentOptions.Center);
            SetRect(startLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void UpdateSoloSetupUI()
        {
            for (int i = 0; i < _difficultyBgs.Count; i++)
            {
                bool isSel = (int)_selectedDifficulty == i;
                _difficultyBgs[i].color = isSel ? ColOrangeAccent : ColPanelInner;
            }

            for (int m = 0; m < _modeBgs.Count; m++)
            {
                bool isSel = (int)_selectedMode == m;
                _modeBgs[m].color = isSel ? ColPinkPlayer : ColPanelInner;
            }

            if (_seedValueText != null)
                _seedValueText.text = _currentSeed.ToString();

            if (_nationNameValueText != null)
                _nationNameValueText.text = KeybindManager.Instance.PlayerNationName;
        }

        private void BuildSettingsModal(RectTransform root)
        {
            _settingsModalRoot = CreateRect("SettingsModal_Root", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _settingsModalRoot.offsetMin = Vector2.zero;
            _settingsModalRoot.offsetMax = Vector2.zero;

            RectTransform card = CreatePanel("SettingsCard", _settingsModalRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 720), ColPanelDark);

            // Title & Close Button
            TextMeshProUGUI title = CreateTMP("SettingsTitle", card, "AYARLAR (SETTINGS)", 24, FontStyles.Bold, ColOrangeAccent, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(0.8f, 1), new Vector2(0, 1), new Vector2(36, -26), new Vector2(0, 32));

            RectTransform closeBtnRt = CreatePanel("CloseBtn", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -26), new Vector2(36, 36), ColPanelInner);
            Button closeBtn = closeBtnRt.gameObject.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => ShowScreen(MainMenuScreen.Main));
            TextMeshProUGUI closeLbl = CreateTMP("CloseLbl", closeBtnRt, "✕", 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(closeLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Tabs Bar
            float tabY = -72f;
            RectTransform tab1Rt = CreatePanel("Tab_Keybinds", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, tabY), new Vector2(338, 40), ColPanelInner);
            _tabKeybindsBg = tab1Rt.GetComponent<Image>();
            _tabKeybindsBtn = tab1Rt.gameObject.AddComponent<Button>();
            _tabKeybindsBtn.onClick.AddListener(() => SwitchSettingsTab(true));
            TextMeshProUGUI tab1Lbl = CreateTMP("Tab1Lbl", tab1Rt, "⌨ KISAYOL TUŞLARI (KEYBINDINGS)", 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(tab1Lbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform tab2Rt = CreatePanel("Tab_AudioDisplay", card, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(386, tabY), new Vector2(338, 40), ColPanelInner);
            _tabAudioDisplayBg = tab2Rt.GetComponent<Image>();
            _tabAudioDisplayBtn = tab2Rt.gameObject.AddComponent<Button>();
            _tabAudioDisplayBtn.onClick.AddListener(() => SwitchSettingsTab(false));
            TextMeshProUGUI tab2Lbl = CreateTMP("Tab2Lbl", tab2Rt, "🔊 SES & GÖRSEL AYARLARI", 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(tab2Lbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Tab 1 Container (Keybindings)
            _keybindsTabObj = new GameObject("TabContent_Keybinds");
            _keybindsTabObj.transform.SetParent(card, false);
            RectTransform kbRt = _keybindsTabObj.AddComponent<RectTransform>();
            SetRect(kbRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(36, 75), new Vector2(-36, -125));

            BuildKeybindsTabContent(kbRt);

            // Tab 2 Container (Audio & Display)
            _audioDisplayTabObj = new GameObject("TabContent_AudioDisplay");
            _audioDisplayTabObj.transform.SetParent(card, false);
            RectTransform adRt = _audioDisplayTabObj.AddComponent<RectTransform>();
            SetRect(adRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(36, 75), new Vector2(-36, -125));

            BuildAudioDisplayTabContent(adRt);

            // Bottom Buttons (Reset Defaults & Save)
            RectTransform resetBtnRt = CreatePanel("ResetBtn", card, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(36, 24), new Vector2(240, 44), ColPanelInner);
            Button resetBtn = resetBtnRt.gameObject.AddComponent<Button>();
            resetBtn.onClick.AddListener(() =>
            {
                KeybindManager.Instance.ResetKeybindingsToDefaults();
                RefreshSettingsDisplay();
            });
            TextMeshProUGUI resetLbl = CreateTMP("ResetLbl", resetBtnRt, "↺ VARSAYILANLARA SIFIRLA", 12, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(resetLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform saveBtnRt = CreatePanel("SaveBtn", card, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 24), new Vector2(200, 44), ColGreenBright);
            Button saveBtn = saveBtnRt.gameObject.AddComponent<Button>();
            saveBtn.onClick.AddListener(() => ShowScreen(MainMenuScreen.Main));
            TextMeshProUGUI saveLbl = CreateTMP("SaveLbl", saveBtnRt, "✓ TAMAM & KAYDET", 13, FontStyles.Bold, new Color32(13, 23, 17, 255), TextAlignmentOptions.Center);
            SetRect(saveLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            SwitchSettingsTab(true);
        }

        private void SwitchSettingsTab(bool showKeybinds)
        {
            _keybindsTabObj.SetActive(showKeybinds);
            _audioDisplayTabObj.SetActive(!showKeybinds);

            _tabKeybindsBg.color = showKeybinds ? ColOrangeAccent : ColPanelInner;
            _tabAudioDisplayBg.color = !showKeybinds ? ColOrangeAccent : ColPanelInner;
        }

        private void BuildKeybindsTabContent(RectTransform container)
        {
            // Info Header
            TextMeshProUGUI info = CreateTMP("KbInfo", container, "Değiştirmek istediğiniz kısayol kutucuğuna tıklayın, ardından atamak istediğiniz yeni tuşa basın.", 12, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(info.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 24));

            KeyAction[] actions = (KeyAction[])Enum.GetValues(typeof(KeyAction));
            float startY = -34f;
            float rowH = 35f;
            float spacing = 4f;

            _keybindLabelTexts.Clear();
            _keybindButtonBgs.Clear();

            for (int i = 0; i < actions.Length; i++)
            {
                KeyAction act = actions[i];
                float y = startY - i * (rowH + spacing);

                RectTransform row = CreatePanel("Row_" + act, container, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(0, rowH), ColPanelInner);
                row.offsetMin = new Vector2(0, y - rowH);
                row.offsetMax = new Vector2(0, y);

                // Action Name
                TextMeshProUGUI actName = CreateTMP("Name", row, KeybindManager.Instance.GetActionDisplayName(act), 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
                SetRect(actName.rectTransform, new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(0, 0));

                // Key Button
                RectTransform keyBtnRt = CreatePanel("KeyBtn", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(130, 28), new Color32(40, 48, 64, 255));
                Image bg = keyBtnRt.GetComponent<Image>();
                _keybindButtonBgs[act] = bg;

                Button btn = keyBtnRt.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    KeybindManager.Instance.StartRebinding(act);
                });

                TextMeshProUGUI keyLbl = CreateTMP("KeyLbl", keyBtnRt, KeybindManager.Instance.GetKeyDisplayName(act), 12, FontStyles.Bold, ColCyanBright, TextAlignmentOptions.Center);
                SetRect(keyLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                _keybindLabelTexts[act] = keyLbl;
            }
        }

        private void BuildAudioDisplayTabContent(RectTransform container)
        {
            float curY = -15f;

            // 1. Master Volume
            TextMeshProUGUI volHeader = CreateTMP("VolHeader", container, "ANA SES DÜZEYİ (MASTER VOLUME)", 13, FontStyles.Bold, ColTextMuted, TextAlignmentOptions.Left);
            SetRect(volHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(12, curY), new Vector2(0, 20));

            curY -= 32f;
            RectTransform sliderTrack = CreatePanel("SliderTrack", container, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, curY), new Vector2(460, 22), ColPanelInner);

            // Unity UI Slider
            _masterVolumeSlider = sliderTrack.gameObject.AddComponent<Slider>();
            _masterVolumeSlider.minValue = 0f;
            _masterVolumeSlider.maxValue = 1f;
            _masterVolumeSlider.value = KeybindManager.Instance.MasterVolume;

            RectTransform fillArea = CreateRect("FillArea", sliderTrack, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform fill = CreatePanel("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, ColGoldBright);
            _masterVolumeSlider.fillRect = fill;

            RectTransform handleArea = CreateRect("HandleArea", sliderTrack, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform handle = CreateRect("Handle", handleArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            Image handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.sprite = ProceduralSpriteFactory.GetSliderKnobSprite();
            _masterVolumeSlider.handleRect = handle;
            _masterVolumeSlider.targetGraphic = handleImg;

            _masterVolumeValueText = CreateTMP("VolVal", container, $"%{Mathf.RoundToInt(_masterVolumeSlider.value * 100f)}", 14, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Left);
            SetRect(_masterVolumeValueText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(490, curY - 2), new Vector2(100, 26));

            _masterVolumeSlider.onValueChanged.AddListener((v) =>
            {
                KeybindManager.Instance.SetMasterVolume(v);
                _masterVolumeValueText.text = $"%{Mathf.RoundToInt(v * 100f)}";
            });

            // 2. Mute Audio Toggle
            curY -= 65f;
            RectTransform muteRow = CreatePanel("MuteRow", container, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, curY), new Vector2(0, 48), ColPanelInner);
            muteRow.offsetMin = new Vector2(12, curY - 48);
            muteRow.offsetMax = new Vector2(-12, curY);

            TextMeshProUGUI muteTitle = CreateTMP("MuteTitle", muteRow, "Tüm Sesleri Kapat (Mute Audio)", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(muteTitle.rectTransform, new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(0, 0));

            RectTransform muteBtnRt = CreatePanel("MuteBtn", muteRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 32), ColPanelDark);
            _muteToggleBg = muteBtnRt.GetComponent<Image>();
            Button muteBtn = muteBtnRt.gameObject.AddComponent<Button>();
            muteBtn.onClick.AddListener(() =>
            {
                bool newState = !KeybindManager.Instance.IsMuted;
                KeybindManager.Instance.SetMuted(newState);
                RefreshSettingsDisplay();
            });
            _muteToggleLabel = CreateTMP("MuteLbl", muteBtnRt, "KAPALI", 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(_muteToggleLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 3. World Country Labels Toggle
            curY -= 65f;
            RectTransform labelRow = CreatePanel("LabelRow", container, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, curY), new Vector2(0, 48), ColPanelInner);
            labelRow.offsetMin = new Vector2(12, curY - 48);
            labelRow.offsetMax = new Vector2(-12, curY);

            TextMeshProUGUI labelTitle = CreateTMP("LabelTitle", labelRow, "Harita Ülke İsimleri & Asker Sayıları", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(labelTitle.rectTransform, new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(0, 0));

            RectTransform labelBtnRt = CreatePanel("LabelBtn", labelRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 32), ColPanelDark);
            _labelsToggleBg = labelBtnRt.GetComponent<Image>();
            Button labelBtn = labelBtnRt.gameObject.AddComponent<Button>();
            labelBtn.onClick.AddListener(() =>
            {
                bool newState = !KeybindManager.Instance.ShowWorldLabels;
                KeybindManager.Instance.SetShowWorldLabels(newState);
                RefreshSettingsDisplay();
            });
            _labelsToggleLabel = CreateTMP("LabelLbl", labelBtnRt, "AÇIK", 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(_labelsToggleLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 4. Smooth Camera Toggle
            curY -= 65f;
            RectTransform camRow = CreatePanel("CamRow", container, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, curY), new Vector2(0, 48), ColPanelInner);
            camRow.offsetMin = new Vector2(12, curY - 48);
            camRow.offsetMax = new Vector2(-12, curY);

            TextMeshProUGUI camTitle = CreateTMP("CamTitle", camRow, "Yumuşak Kamera Geçişi (Smooth Cam)", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetRect(camTitle.rectTransform, new Vector2(0, 0), new Vector2(0.7f, 1), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(0, 0));

            RectTransform camBtnRt = CreatePanel("CamBtn", camRow, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(110, 32), ColPanelDark);
            _smoothCamToggleBg = camBtnRt.GetComponent<Image>();
            Button camBtn = camBtnRt.gameObject.AddComponent<Button>();
            camBtn.onClick.AddListener(() =>
            {
                bool newState = !KeybindManager.Instance.SmoothCamera;
                KeybindManager.Instance.SetSmoothCamera(newState);
                RefreshSettingsDisplay();
            });
            _smoothCamToggleLabel = CreateTMP("CamLbl", camBtnRt, "AÇIK", 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(_smoothCamToggleLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void RefreshSettingsDisplay()
        {
            foreach (KeyAction act in Enum.GetValues(typeof(KeyAction)))
            {
                RefreshKeybindButton(act);
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = KeybindManager.Instance.MasterVolume;
                if (_masterVolumeValueText != null)
                    _masterVolumeValueText.text = $"%{Mathf.RoundToInt(KeybindManager.Instance.MasterVolume * 100f)}";
            }

            if (_muteToggleBg != null && _muteToggleLabel != null)
            {
                bool m = KeybindManager.Instance.IsMuted;
                _muteToggleBg.color = m ? new Color32(239, 68, 68, 255) : ColPanelInner;
                _muteToggleLabel.text = m ? "AÇIK (MUTED)" : "KAPALI";
            }

            if (_labelsToggleBg != null && _labelsToggleLabel != null)
            {
                bool l = KeybindManager.Instance.ShowWorldLabels;
                _labelsToggleBg.color = l ? ColGreenBright : ColPanelInner;
                _labelsToggleLabel.text = l ? "AÇIK" : "KAPALI";
                _labelsToggleLabel.color = l ? new Color32(13, 23, 17, 255) : Color.white;
            }

            if (_smoothCamToggleBg != null && _smoothCamToggleLabel != null)
            {
                bool s = KeybindManager.Instance.SmoothCamera;
                _smoothCamToggleBg.color = s ? ColGreenBright : ColPanelInner;
                _smoothCamToggleLabel.text = s ? "AÇIK" : "KAPALI";
                _smoothCamToggleLabel.color = s ? new Color32(13, 23, 17, 255) : Color.white;
            }
        }

        private void RefreshKeybindButton(KeyAction act)
        {
            if (_keybindLabelTexts.TryGetValue(act, out TextMeshProUGUI text))
            {
                text.text = KeybindManager.Instance.GetKeyDisplayName(act);
                text.color = ColCyanBright;
            }
            if (_keybindButtonBgs.TryGetValue(act, out Image bg))
            {
                bg.color = new Color32(40, 48, 64, 255);
            }
        }

        private void BuildMultiplayerModal(RectTransform root)
        {
            _multiplayerModalRoot = CreateRect("MultiplayerModal_Root", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _multiplayerModalRoot.offsetMin = Vector2.zero;
            _multiplayerModalRoot.offsetMax = Vector2.zero;

            RectTransform card = CreatePanel("MultiCard", _multiplayerModalRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 460), ColPanelDark);

            // Icon & Header
            RectTransform iconRt = CreateRect("GlobeIcon", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -38), new Vector2(48, 48));
            Image icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = ProceduralSpriteFactory.GetGlobeIcon();
            icon.color = ColCyanBright;

            TextMeshProUGUI title = CreateTMP("MultiTitle", card, "ÇOK OYUNCULU MOD (MULTIPLAYER)", 22, FontStyles.Bold, ColCyanBright, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(0, 32));

            // Status Tag
            RectTransform tagRt = CreatePanel("StatusTag", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(240, 26), new Color32(14, 116, 144, 255));
            TextMeshProUGUI tagText = CreateTMP("TagText", tagRt, "● ALTYAPI GELİŞTİRİLİYOR (YAKINDA)", 11, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(tagText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Description
            TextMeshProUGUI desc = CreateTMP("Desc", card,
                "88 Gerçek oyunculu küresel fetih ve ittifak sunucu altyapımız hazırlanmaktadır.\n\n" +
                "Sunucu eşleştirme ve WebSocket ağı tamamlandığında dünya genelindeki oyuncularla eş zamanlı PvP savaşları burada açılacaktır.\n\n" +
                "Şu anda 'Tek Oyunculu' modda 88 gelişmiş yapay zeka ulusuna ve kabilelere karşı stratejinizi geliştirebilirsiniz!",
                13, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(desc.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(36, 95), new Vector2(-36, -180));

            // Close Button
            RectTransform okBtnRt = CreatePanel("OkBtn", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 32), new Vector2(240, 46), ColCyanBright);
            Button okBtn = okBtnRt.gameObject.AddComponent<Button>();
            okBtn.onClick.AddListener(() => ShowScreen(MainMenuScreen.Main));
            TextMeshProUGUI okLbl = CreateTMP("OkLbl", okBtnRt, "ANLAŞILDI, GERİ DÖN", 13, FontStyles.Bold, new Color32(10, 20, 30, 255), TextAlignmentOptions.Center);
            SetRect(okLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private void BuildTutorialModal(RectTransform root)
        {
            _tutorialModalRoot = CreateRect("TutorialModal_Root", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _tutorialModalRoot.offsetMin = Vector2.zero;
            _tutorialModalRoot.offsetMax = Vector2.zero;

            RectTransform card = CreatePanel("TutorialCard", _tutorialModalRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 680), ColPanelDark);

            TextMeshProUGUI title = CreateTMP("TutTitle", card, "NASIL OYNANIR? (REHBER & STRATEJİ)", 24, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(0, 32));

            string[] headings =
            {
                "1. Toprak Fethi & Zafer Şartı (%80)",
                "2. Lojistik Asker Artışı & Altın",
                "3. Binalar (Fabrika, Liman, Savunma)",
                "4. Nükleer Silahlar & SAM Kalkanı"
            };

            string[] bodies =
            {
                "Haritada sınırınız olan komşu topraklara tıklayarak seçtiğiniz oranda (%10-%100) asker gönderin. Dünya topraklarının %80'ini kontrol eden ulus oyunu kazanır!",
                "Asker büyümesi lojistik eğriye bağlıdır; kapasitenin %40-50'sindeyken büyüme hızı zirveye ulaşır. Şehirler asker kapasitesini, Fabrikalar altın gelirini katlar.",
                "Liman kurarak kıyı aşırı çıkarma yapabilir ve ticaret geliri sağlayabilirsiniz. Savunma Karakolları düşman saldırılarına karşı sınırlarınıza 2.5x direnç kazandırır.",
                "Füze Silosu kurarak Atom ve Hidrojen bombaları fırlatabilirsiniz. Düşman füzelerinden korunmak için SAM Hava Savunma Sistemi inşa etmeyi unutmayın!"
            };

            Color[] colors = { ColPinkPlayer, ColGreenBright, ColOrangeAccent, ColGoldBright };

            float curY = -75f;
            for (int i = 0; i < 4; i++)
            {
                RectTransform bRt = CreatePanel("Card_" + i, card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, curY), new Vector2(0, 110), ColPanelInner);
                bRt.offsetMin = new Vector2(36, curY - 110);
                bRt.offsetMax = new Vector2(-36, curY);

                TextMeshProUGUI h = CreateTMP("H", bRt, headings[i], 15, FontStyles.Bold, colors[i], TextAlignmentOptions.Left);
                SetRect(h.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(16, -10), new Vector2(-16, 22));

                TextMeshProUGUI b = CreateTMP("B", bRt, bodies[i], 12, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Left);
                SetRect(b.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(16, 8), new Vector2(-16, -34));

                curY -= 124f;
            }

            // Close Button
            RectTransform closeBtnRt = CreatePanel("CloseBtn", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(220, 44), ColGoldBright);
            Button closeBtn = closeBtnRt.gameObject.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => ShowScreen(MainMenuScreen.Main));
            TextMeshProUGUI closeLbl = CreateTMP("CloseLbl", closeBtnRt, "ANLADIM, MENÜYE DÖN", 13, FontStyles.Bold, new Color32(23, 17, 10, 255), TextAlignmentOptions.Center);
            SetRect(closeLbl.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // Helper Methods for Procedural UI
        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color)
        {
            RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPos, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        private static TextMeshProUGUI CreateTMP(string name, RectTransform parent, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
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
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = piv;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }
    }
}
