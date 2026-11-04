using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.Nations;
using TMPro;
using UnityEngine;

namespace MeWorld.UI
{
    public class WorldLabelsController : MonoBehaviour
    {
        private class LabelEntry
        {
            public NationData Nation;
            public GameObject RootObj;
            public TextMeshPro TextComp;
            public SpriteRenderer CrownIcon;
        }

        private MapRenderer _mapRenderer;
        private TerritoryAndCombatManager _territoryManager;
        private Camera _cam;

        private Transform _labelsRoot;
        private readonly List<LabelEntry> _entries = new List<LabelEntry>(220);

        public void Initialize(MapRenderer mapRenderer, TerritoryAndCombatManager territoryManager, Camera cam)
        {
            _mapRenderer = mapRenderer;
            _territoryManager = territoryManager;
            _cam = cam;

            if (_labelsRoot != null) Destroy(_labelsRoot.gameObject);
            _labelsRoot = new GameObject("WorldLabels_Root").transform;
            _labelsRoot.SetParent(transform, false);
            _entries.Clear();

            IReadOnlyList<NationData> list = _territoryManager.NationList;
            for (int i = 0; i < list.Count; i++)
            {
                NationData n = list[i];
                GameObject labelObj = new GameObject($"Label_{n.Name}");
                labelObj.transform.SetParent(_labelsRoot, false);

                TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
                TMP_FontAsset font = ProceduralSpriteFactory.GetDefaultFont();
                if (font != null) tmp.font = font;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 3.2f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.sortingOrder = 20;

                // Subtle dark outline/color matching RoWorld screenshot
                if (n.IsPlayer)
                {
                    tmp.color = new Color32(25, 22, 30, 255);
                    tmp.fontSize = 3.4f;
                }
                else if (n.Name == "India")
                {
                    tmp.color = new Color32(28, 72, 118, 245);
                    tmp.fontSize = 3.6f;
                }
                else if (n.Name == "Oman")
                {
                    tmp.color = new Color32(142, 74, 24, 245);
                    tmp.fontSize = 3.6f;
                }
                else if (!n.IsTribe)
                {
                    tmp.color = new Color32(42, 52, 60, 235);
                    tmp.fontSize = 2.9f;
                }
                else
                {
                    // Tribes have subtle, elegant muted labels so map stays clean
                    tmp.color = new Color32(95, 105, 95, 195);
                    tmp.fontSize = 2.05f;
                }

                SpriteRenderer crownSr = null;
                if (n.IsPlayer)
                {
                    GameObject crownObj = new GameObject("PlayerCrown");
                    crownObj.transform.SetParent(labelObj.transform, false);
                    crownObj.transform.localPosition = new Vector3(-1.22f, 0.22f, 0f);
                    crownObj.transform.localScale = Vector3.one * 0.40f;
                    crownSr = crownObj.AddComponent<SpriteRenderer>();
                    crownSr.sprite = ProceduralSpriteFactory.GetCrownSprite();
                    crownSr.color = new Color32(246, 205, 65, 255);
                    crownSr.sortingOrder = 21;
                }

                _entries.Add(new LabelEntry
                {
                    Nation = n,
                    RootObj = labelObj,
                    TextComp = tmp,
                    CrownIcon = crownSr
                });
            }

            RefreshLabels();
        }

        private void LateUpdate()
        {
            RefreshLabels();
        }

        public void RefreshLabels()
        {
            if (_mapRenderer == null || _entries.Count == 0) return;
            float ortho = _cam != null ? _cam.orthographicSize : 8.5f;

            for (int i = 0; i < _entries.Count; i++)
            {
                LabelEntry entry = _entries[i];
                NationData n = entry.Nation;

                if (!n.IsAlive || n.TileCount < 10)
                {
                    if (entry.RootObj.activeSelf) entry.RootObj.SetActive(false);
                    continue;
                }

                // Hide tiny tribes when zoomed far out to keep map clean
                if (n.IsTribe && n.TileCount < 85 && ortho > 12.5f)
                {
                    if (entry.RootObj.activeSelf) entry.RootObj.SetActive(false);
                    continue;
                }

                if (!entry.RootObj.activeSelf) entry.RootObj.SetActive(true);

                Vector3 worldPos = _mapRenderer.GridToWorld(n.CenterGridPos.x, n.CenterGridPos.y);
                entry.RootObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.3f);

                // Scale proportionally to territory size (sqrt of TileCount), just like India/Oman vs smaller nations in screenshot
                float radiusEst = Mathf.Sqrt(n.TileCount);
                float scale = Mathf.Clamp(radiusEst * 0.042f, 0.36f, 1.85f);
                if (n.IsPlayer) scale = Mathf.Max(scale, 0.68f);

                entry.RootObj.transform.localScale = new Vector3(scale, scale, 1f);

                string troopStr = GameConfig.FormatNumber(n.Troops);
                entry.TextComp.text = $"{n.Name}\n<size=78%>{troopStr}</size>";
            }
        }
    }
}
