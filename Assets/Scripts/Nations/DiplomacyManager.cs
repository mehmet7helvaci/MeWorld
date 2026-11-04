using System;
using System.Collections;
using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.UI;
using TMPro;
using UnityEngine;

namespace MeWorld.Nations
{
    public class WorldFloatingEmote : MonoBehaviour
    {
        private TextMeshPro _textComp;
        private SpriteRenderer _bgRenderer;
        private float _lifeTimer = 2.6f;
        private float _maxLife = 2.6f;
        private Vector3 _velocity = new Vector3(0, 0.85f, 0);

        public void Setup(string emoteText, Color32 badgeColor)
        {
            // Background pill
            _bgRenderer = gameObject.AddComponent<SpriteRenderer>();
            _bgRenderer.sprite = ProceduralSpriteFactory.GetRoundedPanelSprite();
            _bgRenderer.color = new Color32(18, 22, 32, 230);
            _bgRenderer.sortingOrder = 55;
            transform.localScale = new Vector3(0.55f, 0.42f, 1f);

            // Text
            GameObject textObj = new GameObject("EmoteText");
            textObj.transform.SetParent(transform, false);
            textObj.transform.localPosition = new Vector3(0, 0, -0.1f);

            _textComp = textObj.AddComponent<TextMeshPro>();
            TMP_FontAsset font = ProceduralSpriteFactory.GetDefaultFont();
            if (font != null) _textComp.font = font;
            _textComp.text = emoteText;
            _textComp.fontSize = 4.2f;
            _textComp.fontStyle = FontStyles.Bold;
            _textComp.alignment = TextAlignmentOptions.Center;
            _textComp.sortingOrder = 56;
            _textComp.color = badgeColor;
        }

        private void Update()
        {
            _lifeTimer -= Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            float alpha = Mathf.Clamp01(_lifeTimer / (_maxLife * 0.4f));
            if (_bgRenderer != null)
            {
                Color c = _bgRenderer.color;
                c.a = alpha * 0.9f;
                _bgRenderer.color = c;
            }
            if (_textComp != null)
            {
                Color c = _textComp.color;
                c.a = alpha;
                _textComp.color = c;
            }

            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class DiplomacyManager : MonoBehaviour
    {
        private WorldMapGrid _grid;
        private TerritoryAndCombatManager _territoryManager;
        private MapRenderer _mapRenderer;
        private Transform _emotesRoot;

        public Action<ushort, ushort, DiplomacyEmote, string> OnDiplomaticActionExecuted;

        public void Initialize(WorldMapGrid grid, TerritoryAndCombatManager territoryManager, MapRenderer mapRenderer)
        {
            _grid = grid;
            _territoryManager = territoryManager;
            _mapRenderer = mapRenderer;

            if (_emotesRoot != null) Destroy(_emotesRoot.gameObject);
            GameObject rootObj = new GameObject("WorldEmotes_Root");
            rootObj.transform.SetParent(transform, false);
            _emotesRoot = rootObj.transform;
        }

        public bool ProposeOrFormAlliance(ushort nationA, ushort nationB, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(nationA, out NationData nA) ||
                !_territoryManager.Nations.TryGetValue(nationB, out NationData nB))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            if (nA.IsAlliedWith(nationB))
            {
                message = $"{nA.Name} ve {nB.Name} zaten müttefik!";
                return false;
            }

            // Remove any active embargo between them
            nA.Embargoes.Remove(nationB);
            nB.Embargoes.Remove(nationA);

            nA.Allies.Add(nationB);
            nB.Allies.Add(nationA);

            nA.AdjustRelation(nationB, 40f);
            nB.AdjustRelation(nationA, 40f);

            message = $"İttifak kuruldu: {nA.Name} ve {nB.Name} artık resmi müttefik!";
            DispatchWorldEmote(nationA, nationB, DiplomacyEmote.ThumbsUp, "İTTİFAK");
            OnDiplomaticActionExecuted?.Invoke(nationA, nationB, DiplomacyEmote.ThumbsUp, message);
            return true;
        }

        public bool BreakAlliance(ushort breakerId, ushort victimId, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(breakerId, out NationData breaker) ||
                !_territoryManager.Nations.TryGetValue(victimId, out NationData victim))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            if (!breaker.IsAlliedWith(victimId))
            {
                message = "Bu ulusla aktif bir ittifakınız bulunmuyor.";
                return false;
            }

            breaker.Allies.Remove(victimId);
            victim.Allies.Remove(breakerId);

            // Traitor penalty
            breaker.TraitorTimer = 60f;
            breaker.AdjustRelation(victimId, -60f);
            victim.AdjustRelation(breakerId, -75f);

            // Global diplomatic trust penalty with other nations
            foreach (NationData other in _territoryManager.NationList)
            {
                if (other.Id != breakerId && other.Id != victimId)
                {
                    other.AdjustRelation(breakerId, -15f);
                }
            }

            message = $"{breaker.Name}, {victim.Name} ile olan ittifakını bozdu ve HAİN damgası yedi!";
            DispatchWorldEmote(breakerId, victimId, DiplomacyEmote.Betrayal, "İHANET!");
            OnDiplomaticActionExecuted?.Invoke(breakerId, victimId, DiplomacyEmote.Betrayal, message);

            // Victim AI reacts with outrage
            StartCoroutine(DelayedAIReply(victimId, breakerId, DiplomacyEmote.War, "HAİN!", 0.8f));
            return true;
        }

        public bool ToggleTradeEmbargo(ushort senderId, ushort targetId, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(senderId, out NationData sender) ||
                !_territoryManager.Nations.TryGetValue(targetId, out NationData target))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            if (sender.HasEmbargoAgainst(targetId))
            {
                // Lift Embargo
                sender.Embargoes.Remove(targetId);
                sender.AdjustRelation(targetId, 15f);
                target.AdjustRelation(senderId, 15f);
                message = $"{sender.Name}, {target.Name} üzerindeki ticari ambargoyu kaldırdı.";
                DispatchWorldEmote(senderId, targetId, DiplomacyEmote.Peace, "AMBARGO KALKTI");
                OnDiplomaticActionExecuted?.Invoke(senderId, targetId, DiplomacyEmote.Peace, message);
                return true;
            }
            else
            {
                // Declare Embargo (Breaks alliance if existing)
                if (sender.IsAlliedWith(targetId))
                {
                    BreakAlliance(senderId, targetId, out _);
                }

                sender.Embargoes.Add(targetId);
                sender.AdjustRelation(targetId, -35f);
                target.AdjustRelation(senderId, -40f);
                message = $"{sender.Name}, {target.Name} devletine ticari ambargo ve yaptırım ilan etti!";
                DispatchWorldEmote(senderId, targetId, DiplomacyEmote.War, "AMBARGO!");
                OnDiplomaticActionExecuted?.Invoke(senderId, targetId, DiplomacyEmote.War, message);

                // AI responds with irritation
                StartCoroutine(DelayedAIReply(targetId, senderId, DiplomacyEmote.Skull, "YAPTIRIM!", 0.7f));
                return true;
            }
        }

        public bool DonateGold(ushort senderId, ushort receiverId, double amount, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(senderId, out NationData sender) ||
                !_territoryManager.Nations.TryGetValue(receiverId, out NationData receiver))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            if (sender.Gold < amount)
            {
                message = $"Yetersiz altın! En az {GameConfig.FormatNumber(amount)} altın gerekiyor.";
                return false;
            }

            sender.Gold -= amount;
            receiver.Gold += amount;
            receiver.AdjustRelation(senderId, 35f);

            message = $"{receiver.Name} devletine {GameConfig.FormatNumber(amount)} altın yardım gönderildi!";
            DispatchWorldEmote(senderId, receiverId, DiplomacyEmote.Gold, $"+{GameConfig.FormatNumber(amount)} ALTIN");
            OnDiplomaticActionExecuted?.Invoke(senderId, receiverId, DiplomacyEmote.Gold, message);

            // AI appreciates the aid
            StartCoroutine(DelayedAIReply(receiverId, senderId, DiplomacyEmote.Heart, "TEŞEKKÜRLER!", 0.6f));
            return true;
        }

        public bool DonateTroops(ushort senderId, ushort receiverId, double amount, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(senderId, out NationData sender) ||
                !_territoryManager.Nations.TryGetValue(receiverId, out NationData receiver))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            if (sender.Troops < amount + 80)
            {
                message = "Yetersiz asker rezervi! Kendi savunmanız için asker gerekiyor.";
                return false;
            }

            sender.Troops -= amount;
            receiver.Troops = Math.Min(receiver.MaxTroops, receiver.Troops + amount);
            receiver.AdjustRelation(senderId, 45f);

            message = $"{receiver.Name} müttefikine {GameConfig.FormatNumber(amount)} asker takviyesi ulaştırıldı!";
            DispatchWorldEmote(senderId, receiverId, DiplomacyEmote.ThumbsUp, $"+{GameConfig.FormatNumber(amount)} ASKER");
            OnDiplomaticActionExecuted?.Invoke(senderId, receiverId, DiplomacyEmote.ThumbsUp, message);

            StartCoroutine(DelayedAIReply(receiverId, senderId, DiplomacyEmote.ThumbsUp, "TAKVİYE ALINDI", 0.6f));
            return true;
        }

        public bool SendEmote(ushort senderId, ushort receiverId, DiplomacyEmote emote, out string message)
        {
            message = "";
            if (!_territoryManager.Nations.TryGetValue(senderId, out NationData sender) ||
                !_territoryManager.Nations.TryGetValue(receiverId, out NationData receiver))
            {
                message = "Ulus verisi bulunamadi.";
                return false;
            }

            string label;
            switch (emote)
            {
                case DiplomacyEmote.Peace:
                    label = "BARIŞ";
                    receiver.AdjustRelation(senderId, 25f);
                    message = $"{receiver.Name} devletine barış ve dostluk mesajı iletildi.";
                    break;
                case DiplomacyEmote.War:
                    label = "SAVAŞ!";
                    receiver.AdjustRelation(senderId, -50f);
                    message = $"{receiver.Name} devletine savaş ilanı ve tehdit gönderildi!";
                    break;
                case DiplomacyEmote.ThumbsUp:
                    label = "DOSTLUK";
                    receiver.AdjustRelation(senderId, 20f);
                    message = $"{receiver.Name} devletine takdir ve onay mesajı gönderildi.";
                    break;
                case DiplomacyEmote.Skull:
                    label = "TEHDİT!";
                    receiver.AdjustRelation(senderId, -40f);
                    message = $"{receiver.Name} devletine ölüm ve yıkım tehdidi savruldu!";
                    break;
                case DiplomacyEmote.Clown:
                    label = "ALAY";
                    receiver.AdjustRelation(senderId, -30f);
                    message = $"{receiver.Name} devletiyle dalga geçildi (Palyaço)!";
                    break;
                case DiplomacyEmote.SOS:
                    label = "YARDIM!";
                    message = $"{receiver.Name} devletine acil askeri yardım çağrısı yapıldı!";
                    break;
                default:
                    label = "MESAJ";
                    break;
            }

            DispatchWorldEmote(senderId, receiverId, emote, label);
            OnDiplomaticActionExecuted?.Invoke(senderId, receiverId, emote, message);

            // Reactive AI replies
            EvaluateAIReply(receiver, sender, emote);
            return true;
        }

        private void EvaluateAIReply(NationData ai, NationData sender, DiplomacyEmote playerEmote)
        {
            if (ai.IsPlayer || !ai.IsAlive) return;

            switch (playerEmote)
            {
                case DiplomacyEmote.Peace:
                    if (ai.GetRelationWith(sender.Id) >= 10f)
                    {
                        StartCoroutine(DelayedAIReply(ai.Id, sender.Id, DiplomacyEmote.Peace, "BARIŞ KABUL", 0.9f));
                    }
                    break;

                case DiplomacyEmote.War:
                case DiplomacyEmote.Skull:
                    StartCoroutine(DelayedAIReply(ai.Id, sender.Id, DiplomacyEmote.War, "MEYDAN OKUMA!", 0.7f));
                    break;

                case DiplomacyEmote.Clown:
                    DiplomacyEmote reply = UnityEngine.Random.value < 0.5f ? DiplomacyEmote.Clown : DiplomacyEmote.Skull;
                    StartCoroutine(DelayedAIReply(ai.Id, sender.Id, reply, "ÖDEŞECEĞİZ!", 0.8f));
                    break;

                case DiplomacyEmote.ThumbsUp:
                    StartCoroutine(DelayedAIReply(ai.Id, sender.Id, DiplomacyEmote.ThumbsUp, "DOSTUZ", 0.8f));
                    break;

                case DiplomacyEmote.SOS:
                    if (ai.IsAlliedWith(sender.Id))
                    {
                        double helpAmount = Math.Max(200, ai.Troops * 0.25);
                        DonateTroops(ai.Id, sender.Id, helpAmount, out _);
                    }
                    break;
            }
        }

        private IEnumerator DelayedAIReply(ushort aiId, ushort targetId, DiplomacyEmote replyEmote, string text, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_territoryManager.Nations.TryGetValue(aiId, out NationData ai) && ai.IsAlive)
            {
                DispatchWorldEmote(aiId, targetId, replyEmote, text);
            }
        }

        public void DispatchWorldEmote(ushort senderId, ushort targetId, DiplomacyEmote emote, string customLabel = "")
        {
            if (_mapRenderer == null) return;
            if (!_territoryManager.Nations.TryGetValue(targetId, out NationData target) || !target.IsAlive) return;

            Vector3 worldPos = _mapRenderer.GridToWorld(target.CenterGridPos.x, target.CenterGridPos.y);

            GameObject emoteObj = new GameObject($"FloatingEmote_{target.Name}");
            if (_emotesRoot != null) emoteObj.transform.SetParent(_emotesRoot, false);
            emoteObj.transform.position = new Vector3(worldPos.x, worldPos.y + 1.2f, -1.2f);

            WorldFloatingEmote comp = emoteObj.AddComponent<WorldFloatingEmote>();

            Color32 col = new Color32(245, 158, 42, 255);
            switch (emote)
            {
                case DiplomacyEmote.Peace:
                case DiplomacyEmote.ThumbsUp:
                    col = new Color32(72, 219, 128, 255);
                    break;
                case DiplomacyEmote.War:
                case DiplomacyEmote.Betrayal:
                case DiplomacyEmote.Skull:
                    col = new Color32(239, 68, 68, 255);
                    break;
                case DiplomacyEmote.Gold:
                    col = new Color32(246, 208, 72, 255);
                    break;
                case DiplomacyEmote.Heart:
                    col = new Color32(236, 72, 153, 255);
                    break;
                case DiplomacyEmote.Clown:
                    col = new Color32(245, 158, 42, 255);
                    break;
            }

            comp.Setup(customLabel, col);
        }
    }
}
