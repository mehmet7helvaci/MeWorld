using System;
using System.Collections;
using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.Nations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MeWorld.UI
{
    public class RadialContextMenu : MonoBehaviour
    {
        private DiplomacyManager _diplomacyManager;
        private TerritoryAndCombatManager _territoryManager;
        private RoWorldHUDController _hudController;
        private CameraController2D _cameraController;

        private RectTransform _rootRt;
        private RectTransform _radialContainer;
        private RectTransform _centerHubRt;
        private TextMeshProUGUI _hubTitle;
        private TextMeshProUGUI _hubStatusBadge;
        private TextMeshProUGUI _hubStatsSummary;
        private Image _hubBorderRing;

        // Sub-panels
        private RectTransform _emoteTrayRt;
        private RectTransform _goldTrayRt;
        private RectTransform _intelModalRt;
        private TextMeshProUGUI _intelContentText;

        // Radial Pedals
        private class RadialPedal
        {
            public RectTransform RootRt;
            public Image BgImage;
            public Image IconImage;
            public TextMeshProUGUI TitleText;
            public Button ButtonComp;
        }

        private readonly List<RadialPedal> _pedals = new List<RadialPedal>(6);

        public bool IsOpen { get; private set; }
        public NationData CurrentTarget { get; private set; }

        private static readonly Color ColBgDark = new Color32(18, 22, 32, 246);
        private static readonly Color ColPanelInner = new Color32(28, 35, 48, 255);
        private static readonly Color ColGreenBright = new Color32(72, 219, 128, 255);
        private static readonly Color ColGoldBright = new Color32(246, 208, 72, 255);
        private static readonly Color ColOrangeAccent = new Color32(245, 158, 42, 255);
        private static readonly Color ColCyanBright = new Color32(56, 189, 248, 255);
        private static readonly Color ColRedAlert = new Color32(239, 68, 68, 255);
        private static readonly Color ColPinkPlayer = new Color32(233, 59, 129, 255);
        private static readonly Color ColTextMuted = new Color32(156, 168, 188, 255);

        public void Initialize(DiplomacyManager diplomacyManager, TerritoryAndCombatManager territoryManager, RoWorldHUDController hudController, CameraController2D cameraController, RectTransform canvasRoot)
        {
            _diplomacyManager = diplomacyManager;
            _territoryManager = territoryManager;
            _hudController = hudController;
            _cameraController = cameraController;

            BuildRadialUI(canvasRoot);
            Close();
        }

        private void BuildRadialUI(RectTransform canvasRoot)
        {
            GameObject rootObj = new GameObject("RadialContextMenu_Root", typeof(RectTransform));
            _rootRt = rootObj.GetComponent<RectTransform>();
            _rootRt.SetParent(canvasRoot, false);
            _rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            _rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            _rootRt.pivot = new Vector2(0.5f, 0.5f);
            _rootRt.sizeDelta = new Vector2(400f, 400f);

            GameObject containerObj = new GameObject("RadialContainer", typeof(RectTransform));
            _radialContainer = containerObj.GetComponent<RectTransform>();
            _radialContainer.SetParent(_rootRt, false);
            _radialContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _radialContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _radialContainer.pivot = new Vector2(0.5f, 0.5f);
            _radialContainer.sizeDelta = new Vector2(400f, 400f);

            // 1. Center Hub
            BuildCenterHub(_radialContainer);

            // 2. Build 6 Radial Action Pedals
            float radius = 138f;
            float[] anglesDeg = { 90f, 30f, 330f, 270f, 210f, 150f };

            _pedals.Clear();
            for (int i = 0; i < 6; i++)
            {
                float rad = anglesDeg[i] * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
                RadialPedal pedal = CreatePedal(_radialContainer, $"Pedal_{i}", pos, i);
                _pedals.Add(pedal);
            }

            // 3. Build Emote Tray sub-panel
            BuildEmoteTray(_radialContainer);

            // 4. Build Gold Tray sub-panel
            BuildGoldTray(_radialContainer);

            // 5. Build Intel Modal sub-panel
            BuildIntelModal(_radialContainer);
        }

        private void BuildCenterHub(RectTransform parent)
        {
            GameObject hubObj = new GameObject("CenterHub", typeof(RectTransform), typeof(Image));
            _centerHubRt = hubObj.GetComponent<RectTransform>();
            _centerHubRt.SetParent(parent, false);
            _centerHubRt.anchorMin = new Vector2(0.5f, 0.5f);
            _centerHubRt.anchorMax = new Vector2(0.5f, 0.5f);
            _centerHubRt.pivot = new Vector2(0.5f, 0.5f);
            _centerHubRt.sizeDelta = new Vector2(150f, 150f);

            Image bg = hubObj.GetComponent<Image>();
            bg.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
            bg.color = ColBgDark;

            // Border Ring
            GameObject ringObj = new GameObject("BorderRing", typeof(RectTransform), typeof(Image));
            RectTransform ringRt = ringObj.GetComponent<RectTransform>();
            ringRt.SetParent(_centerHubRt, false);
            StretchFull(ringRt);
            _hubBorderRing = ringObj.GetComponent<Image>();
            _hubBorderRing.sprite = ProceduralSpriteFactory.GetCircleSprite(true);
            _hubBorderRing.color = ColPinkPlayer;

            // Nation Name Title
            _hubTitle = CreateTMP("HubTitle", _centerHubRt, "ULUS ADI", 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(_hubTitle.rectTransform, new Vector2(0, 0.60f), new Vector2(1, 0.90f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Status Badge
            _hubStatusBadge = CreateTMP("HubStatus", _centerHubRt, "[ BARIŞ ]", 10.5f, FontStyles.Bold, ColGreenBright, TextAlignmentOptions.Center);
            SetRect(_hubStatusBadge.rectTransform, new Vector2(0, 0.40f), new Vector2(1, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Stats Summary
            _hubStatsSummary = CreateTMP("HubStats", _centerHubRt, "Asker: 1.2K\nAltın: 15K", 9.5f, FontStyles.Normal, ColTextMuted, TextAlignmentOptions.Center);
            SetRect(_hubStatsSummary.rectTransform, new Vector2(0, 0.08f), new Vector2(1, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Close Button
            GameObject closeObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.SetParent(_centerHubRt, false);
            closeRt.anchorMin = new Vector2(0.85f, 0.85f);
            closeRt.anchorMax = new Vector2(0.85f, 0.85f);
            closeRt.pivot = new Vector2(0.5f, 0.5f);
            closeRt.sizeDelta = new Vector2(24f, 24f);

            Image closeImg = closeObj.GetComponent<Image>();
            closeImg.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
            closeImg.color = new Color32(239, 68, 68, 220);

            Button closeBtn = closeObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            TextMeshProUGUI closeLbl = CreateTMP("Lbl", closeRt, "X", 11, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(closeLbl.rectTransform);
        }

        private RadialPedal CreatePedal(RectTransform parent, string name, Vector2 pos, int index)
        {
            GameObject pedalObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform pedalRt = pedalObj.GetComponent<RectTransform>();
            pedalRt.SetParent(parent, false);
            pedalRt.anchorMin = new Vector2(0.5f, 0.5f);
            pedalRt.anchorMax = new Vector2(0.5f, 0.5f);
            pedalRt.pivot = new Vector2(0.5f, 0.5f);
            pedalRt.anchoredPosition = pos;
            pedalRt.sizeDelta = new Vector2(86f, 62f);

            Image bg = pedalObj.GetComponent<Image>();
            bg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            bg.color = ColPanelInner;

            Button btn = pedalObj.GetComponent<Button>();
            int capturedIdx = index;
            btn.onClick.AddListener(() => OnPedalClicked(capturedIdx));

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.SetParent(pedalRt, false);
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0, -6f);
            iconRt.sizeDelta = new Vector2(26f, 26f);

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.raycastTarget = false;

            // Title
            TextMeshProUGUI title = CreateTMP("Title", pedalRt, "ACTION", 9.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 0), new Vector2(1, 0.44f), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);

            return new RadialPedal
            {
                RootRt = pedalRt,
                BgImage = bg,
                IconImage = iconImg,
                TitleText = title,
                ButtonComp = btn
            };
        }

        private void BuildEmoteTray(RectTransform parent)
        {
            GameObject trayObj = new GameObject("EmoteTray", typeof(RectTransform), typeof(Image));
            _emoteTrayRt = trayObj.GetComponent<RectTransform>();
            _emoteTrayRt.SetParent(parent, false);
            _emoteTrayRt.anchorMin = new Vector2(0.5f, 0.5f);
            _emoteTrayRt.anchorMax = new Vector2(0.5f, 0.5f);
            _emoteTrayRt.pivot = new Vector2(0.5f, 0.5f);
            _emoteTrayRt.anchoredPosition = new Vector2(0, -145f);
            _emoteTrayRt.sizeDelta = new Vector2(360f, 54f);

            Image bg = trayObj.GetComponent<Image>();
            bg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            bg.color = ColBgDark;

            DiplomacyEmote[] emotes = { DiplomacyEmote.Peace, DiplomacyEmote.War, DiplomacyEmote.ThumbsUp, DiplomacyEmote.Skull, DiplomacyEmote.Clown, DiplomacyEmote.SOS };
            string[] emoteLabels = { "BARIŞ", "SAVAŞ", "DOST", "TEHDİT", "ALAY", "YARDIM" };
            Color[] emoteColors = { ColGreenBright, ColRedAlert, ColCyanBright, new Color32(192, 132, 252, 255), ColGoldBright, ColOrangeAccent };

            float startX = -150f;
            float stepX = 60f;

            for (int e = 0; e < 6; e++)
            {
                DiplomacyEmote em = emotes[e];
                GameObject eBtnObj = new GameObject($"EmoteBtn_{e}", typeof(RectTransform), typeof(Image), typeof(Button));
                RectTransform eRt = eBtnObj.GetComponent<RectTransform>();
                eRt.SetParent(_emoteTrayRt, false);
                eRt.anchorMin = new Vector2(0.5f, 0.5f);
                eRt.anchorMax = new Vector2(0.5f, 0.5f);
                eRt.pivot = new Vector2(0.5f, 0.5f);
                eRt.anchoredPosition = new Vector2(startX + e * stepX, 0);
                eRt.sizeDelta = new Vector2(54f, 40f);

                Image eBg = eBtnObj.GetComponent<Image>();
                eBg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
                eBg.color = ColPanelInner;

                Button eBtn = eBtnObj.GetComponent<Button>();
                eBtn.onClick.AddListener(() =>
                {
                    ExecuteSendEmote(em);
                });

                TextMeshProUGUI lbl = CreateTMP("Lbl", eRt, emoteLabels[e], 9.5f, FontStyles.Bold, emoteColors[e], TextAlignmentOptions.Center);
                StretchFull(lbl.rectTransform);
            }

            _emoteTrayRt.gameObject.SetActive(false);
        }

        private void BuildGoldTray(RectTransform parent)
        {
            GameObject trayObj = new GameObject("GoldTray", typeof(RectTransform), typeof(Image));
            _goldTrayRt = trayObj.GetComponent<RectTransform>();
            _goldTrayRt.SetParent(parent, false);
            _goldTrayRt.anchorMin = new Vector2(0.5f, 0.5f);
            _goldTrayRt.anchorMax = new Vector2(0.5f, 0.5f);
            _goldTrayRt.pivot = new Vector2(0.5f, 0.5f);
            _goldTrayRt.anchoredPosition = new Vector2(145f, 0);
            _goldTrayRt.sizeDelta = new Vector2(100f, 175f);

            Image bg = trayObj.GetComponent<Image>();
            bg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            bg.color = ColBgDark;

            double[] amounts = { 10000, 25000, 50000, 100000 };
            string[] labels = { "+10K", "+25K", "+50K", "+100K" };

            float startY = 60f;
            float stepY = -40f;

            for (int g = 0; g < 4; g++)
            {
                double amt = amounts[g];
                GameObject gBtnObj = new GameObject($"GoldBtn_{g}", typeof(RectTransform), typeof(Image), typeof(Button));
                RectTransform gRt = gBtnObj.GetComponent<RectTransform>();
                gRt.SetParent(_goldTrayRt, false);
                gRt.anchorMin = new Vector2(0.5f, 0.5f);
                gRt.anchorMax = new Vector2(0.5f, 0.5f);
                gRt.pivot = new Vector2(0.5f, 0.5f);
                gRt.anchoredPosition = new Vector2(0, startY + g * stepY);
                gRt.sizeDelta = new Vector2(84f, 32f);

                Image gBg = gBtnObj.GetComponent<Image>();
                gBg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
                gBg.color = ColPanelInner;

                Button gBtn = gBtnObj.GetComponent<Button>();
                gBtn.onClick.AddListener(() =>
                {
                    ExecuteDonateGold(amt);
                });

                TextMeshProUGUI lbl = CreateTMP("Lbl", gRt, labels[g], 11, FontStyles.Bold, ColGoldBright, TextAlignmentOptions.Center);
                StretchFull(lbl.rectTransform);
            }

            _goldTrayRt.gameObject.SetActive(false);
        }

        private void BuildIntelModal(RectTransform parent)
        {
            GameObject modalObj = new GameObject("IntelModal", typeof(RectTransform), typeof(Image));
            _intelModalRt = modalObj.GetComponent<RectTransform>();
            _intelModalRt.SetParent(parent, false);
            _intelModalRt.anchorMin = new Vector2(0.5f, 0.5f);
            _intelModalRt.anchorMax = new Vector2(0.5f, 0.5f);
            _intelModalRt.pivot = new Vector2(0.5f, 0.5f);
            _intelModalRt.anchoredPosition = new Vector2(-155f, 0);
            _intelModalRt.sizeDelta = new Vector2(230f, 210f);

            Image bg = modalObj.GetComponent<Image>();
            bg.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            bg.color = ColBgDark;

            TextMeshProUGUI title = CreateTMP("Title", _intelModalRt, "İSTİHBARAT RAPORU", 12, FontStyles.Bold, ColCyanBright, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 20));

            _intelContentText = CreateTMP("Content", _intelModalRt, "Bilgi yükleniyor...", 10, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
            SetRect(_intelContentText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(14, 10), new Vector2(-14, -32));

            _intelModalRt.gameObject.SetActive(false);
        }

        public void OpenForNation(NationData target, Vector2 screenPos)
        {
            if (target == null) return;
            CurrentTarget = target;
            IsOpen = true;
            _rootRt.gameObject.SetActive(true);

            // Convert screen position to local canvas position
            RectTransform canvasRt = _rootRt.parent as RectTransform;
            if (canvasRt != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, null, out Vector2 localPt);
                // Clamp within canvas view
                localPt.x = Mathf.Clamp(localPt.x, -720f, 720f);
                localPt.y = Mathf.Clamp(localPt.y, -360f, 360f);
                _rootRt.anchoredPosition = localPt;
            }

            // Close sub-trays
            if (_emoteTrayRt != null) _emoteTrayRt.gameObject.SetActive(false);
            if (_goldTrayRt != null) _goldTrayRt.gameObject.SetActive(false);
            if (_intelModalRt != null) _intelModalRt.gameObject.SetActive(false);

            RefreshUI();
            StartCoroutine(PopInAnimation());
        }

        private IEnumerator PopInAnimation()
        {
            float t = 0f;
            float dur = 0.12f;
            _radialContainer.localScale = Vector3.one * 0.4f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.4f, 1.0f, Mathf.Sin(p * Mathf.PI * 0.5f));
                _radialContainer.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _radialContainer.localScale = Vector3.one;
        }

        public void Close()
        {
            IsOpen = false;
            CurrentTarget = null;
            if (_rootRt != null) _rootRt.gameObject.SetActive(false);
            if (_emoteTrayRt != null) _emoteTrayRt.gameObject.SetActive(false);
            if (_goldTrayRt != null) _goldTrayRt.gameObject.SetActive(false);
            if (_intelModalRt != null) _intelModalRt.gameObject.SetActive(false);
        }

        public void RefreshUI()
        {
            if (CurrentTarget == null || _territoryManager == null) return;
            NationData player = _territoryManager.PlayerNation;
            if (player == null) return;

            // Update Center Hub
            _hubTitle.text = CurrentTarget.Name;
            _hubBorderRing.color = CurrentTarget.BorderColor;

            bool isAllied = player.IsAlliedWith(CurrentTarget.Id);
            bool isEmbargoed = player.HasEmbargoAgainst(CurrentTarget.Id);
            float rel = player.GetRelationWith(CurrentTarget.Id);

            if (isAllied)
            {
                _hubStatusBadge.text = "[ MÜTTEFİK ]";
                _hubStatusBadge.color = ColGreenBright;
            }
            else if (isEmbargoed)
            {
                _hubStatusBadge.text = "[ AMBARGO ]";
                _hubStatusBadge.color = ColOrangeAccent;
            }
            else if (rel < -25f)
            {
                _hubStatusBadge.text = "[ DÜŞMAN ]";
                _hubStatusBadge.color = ColRedAlert;
            }
            else
            {
                _hubStatusBadge.text = "[ BARIŞ ]";
                _hubStatusBadge.color = ColTextMuted;
            }

            _hubStatsSummary.text = $"Asker: {GameConfig.FormatNumber(CurrentTarget.Troops)}\nAltın: {GameConfig.FormatNumber(CurrentTarget.Gold)}\nToprak: {CurrentTarget.TileCount} ({CurrentTarget.WorldControlPercent * 100f:0.1}%)";

            // Pedal 0: Alliance / Break
            RadialPedal p0 = _pedals[0];
            p0.IconImage.sprite = isAllied ? ProceduralSpriteFactory.GetBrokenHeartIcon() : ProceduralSpriteFactory.GetHandshakeIcon();
            p0.IconImage.color = isAllied ? ColRedAlert : ColGreenBright;
            p0.TitleText.text = isAllied ? "İTTİFAKI BOZ" : "İTTİFAK KUR";
            p0.TitleText.color = isAllied ? ColRedAlert : ColGreenBright;

            // Pedal 1: Embargo / Lift
            RadialPedal p1 = _pedals[1];
            p1.IconImage.sprite = ProceduralSpriteFactory.GetEmbargoIcon();
            p1.IconImage.color = isEmbargoed ? ColCyanBright : ColOrangeAccent;
            p1.TitleText.text = isEmbargoed ? "AMBARGO KALDIR" : "AMBARGO";
            p1.TitleText.color = isEmbargoed ? ColCyanBright : ColOrangeAccent;

            // Pedal 2: Send Gold
            RadialPedal p2 = _pedals[2];
            p2.IconImage.sprite = ProceduralSpriteFactory.GetCoinIcon();
            p2.IconImage.color = ColGoldBright;
            p2.TitleText.text = "ALTIN GÖNDER";
            p2.TitleText.color = ColGoldBright;

            // Pedal 3: Send Troops
            RadialPedal p3 = _pedals[3];
            p3.IconImage.sprite = ProceduralSpriteFactory.GetShieldIcon();
            p3.IconImage.color = ColPinkPlayer;
            p3.TitleText.text = "ASKER GÖNDER";
            p3.TitleText.color = ColPinkPlayer;

            // Pedal 4: Emotes
            RadialPedal p4 = _pedals[4];
            p4.IconImage.sprite = ProceduralSpriteFactory.GetChatBubbleIcon();
            p4.IconImage.color = ColCyanBright;
            p4.TitleText.text = "TEPKİLER";
            p4.TitleText.color = ColCyanBright;

            // Pedal 5: Intel
            RadialPedal p5 = _pedals[5];
            p5.IconImage.sprite = ProceduralSpriteFactory.GetSearchIcon();
            p5.IconImage.color = Color.white;
            p5.TitleText.text = "İSTİHBARAT";
            p5.TitleText.color = Color.white;
        }

        private void OnPedalClicked(int index)
        {
            if (CurrentTarget == null || _territoryManager == null || _diplomacyManager == null) return;
            NationData player = _territoryManager.PlayerNation;
            if (player == null) return;

            switch (index)
            {
                case 0: // Alliance / Break Alliance
                    if (player.IsAlliedWith(CurrentTarget.Id))
                    {
                        if (_diplomacyManager.BreakAlliance(player.Id, CurrentTarget.Id, out string msg))
                        {
                            _hudController.ShowStatusMessage(msg, 3.5f);
                            Close();
                        }
                    }
                    else
                    {
                        if (_diplomacyManager.ProposeOrFormAlliance(player.Id, CurrentTarget.Id, out string msg))
                        {
                            _hudController.ShowStatusMessage(msg, 3.5f);
                            Close();
                        }
                    }
                    break;

                case 1: // Toggle Embargo
                    if (_diplomacyManager.ToggleTradeEmbargo(player.Id, CurrentTarget.Id, out string embMsg))
                    {
                        _hudController.ShowStatusMessage(embMsg, 3.0f);
                        Close();
                    }
                    break;

                case 2: // Toggle Gold Tray
                    if (_goldTrayRt != null)
                    {
                        bool active = !_goldTrayRt.gameObject.activeSelf;
                        _goldTrayRt.gameObject.SetActive(active);
                        if (_emoteTrayRt != null) _emoteTrayRt.gameObject.SetActive(false);
                        if (_intelModalRt != null) _intelModalRt.gameObject.SetActive(false);
                    }
                    break;

                case 3: // Send Troops based on Attack Ratio
                    double sendTroops = player.Troops * _hudController.SelectedAttackRatio;
                    if (_diplomacyManager.DonateTroops(player.Id, CurrentTarget.Id, sendTroops, out string trMsg))
                    {
                        _hudController.ShowStatusMessage(trMsg, 3.0f);
                        _hudController.RefreshHUD();
                        Close();
                    }
                    else
                    {
                        _hudController.ShowStatusMessage(trMsg, 2.5f);
                    }
                    break;

                case 4: // Toggle Emote Tray
                    if (_emoteTrayRt != null)
                    {
                        bool active = !_emoteTrayRt.gameObject.activeSelf;
                        _emoteTrayRt.gameObject.SetActive(active);
                        if (_goldTrayRt != null) _goldTrayRt.gameObject.SetActive(false);
                        if (_intelModalRt != null) _intelModalRt.gameObject.SetActive(false);
                    }
                    break;

                case 5: // Toggle Intel Modal
                    if (_intelModalRt != null)
                    {
                        bool active = !_intelModalRt.gameObject.activeSelf;
                        _intelModalRt.gameObject.SetActive(active);
                        if (_emoteTrayRt != null) _emoteTrayRt.gameObject.SetActive(false);
                        if (_goldTrayRt != null) _goldTrayRt.gameObject.SetActive(false);

                        if (active)
                        {
                            _intelContentText.text =
                                $"Başkent: ({CurrentTarget.CapitalX}, {CurrentTarget.CapitalY})\n" +
                                $"Şehir: {CurrentTarget.CityCount} | Liman: {CurrentTarget.PortCount}\n" +
                                $"Fabrika: {CurrentTarget.FactoryCount} | Silo: {CurrentTarget.SiloCount}\n" +
                                $"Nüfus Büyümesi: +{GameConfig.FormatNumber(CurrentTarget.TroopGrowthPerSec)}/s\n" +
                                $"Gelir Hızı: +{GameConfig.FormatNumber(CurrentTarget.GoldGrowthPerSec)}/s\n" +
                                $"Müttefik Sayısı: {CurrentTarget.Allies.Count}\n" +
                                $"Ambargo Sayısı: {CurrentTarget.Embargoes.Count}";
                        }
                    }
                    break;
            }
        }

        private void ExecuteDonateGold(double amount)
        {
            if (CurrentTarget == null || _territoryManager == null || _diplomacyManager == null) return;
            NationData player = _territoryManager.PlayerNation;
            if (player == null) return;

            if (_diplomacyManager.DonateGold(player.Id, CurrentTarget.Id, amount, out string msg))
            {
                _hudController.ShowStatusMessage(msg, 3.0f);
                _hudController.RefreshHUD();
                Close();
            }
            else
            {
                _hudController.ShowStatusMessage(msg, 2.5f);
            }
        }

        private void ExecuteSendEmote(DiplomacyEmote emote)
        {
            if (CurrentTarget == null || _territoryManager == null || _diplomacyManager == null) return;
            NationData player = _territoryManager.PlayerNation;
            if (player == null) return;

            if (_diplomacyManager.SendEmote(player.Id, CurrentTarget.Id, emote, out string msg))
            {
                _hudController.ShowStatusMessage(msg, 3.0f);
                Close();
            }
        }

        private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float size, FontStyles style, Color col, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ProceduralSpriteFactory.GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = col;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
