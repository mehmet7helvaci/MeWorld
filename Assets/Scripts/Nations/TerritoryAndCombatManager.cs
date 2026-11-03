using System;
using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using UnityEngine;

namespace MeWorld.Nations
{
    public class TerritoryAndCombatManager : MonoBehaviour
    {
        private WorldMapGrid _grid;
        private MapRenderer _mapRenderer;

        private readonly Dictionary<ushort, NationData> _nations = new Dictionary<ushort, NationData>(256);
        private readonly List<NationData> _nationList = new List<NationData>(256);
        private readonly List<AttackWave> _activeWaves = new List<AttackWave>(128);

        public IReadOnlyDictionary<ushort, NationData> Nations => _nations;
        public IReadOnlyList<NationData> NationList => _nationList;
        public NationData PlayerNation { get; private set; }
        public NationData LeadingNation { get; private set; }
        public int PlayerRank { get; private set; } = 1;

        public float ProtectionTimer { get; private set; } = GameConfig.InitialProtectionSeconds;

        private int[] _sumX;
        private int[] _sumY;
        private int[] _counts;
        private readonly int[] _neighborBuffer = new int[4];
        private readonly List<int> _candidatePixels = new List<int>(512);

        public Func<ushort, int, int, float> DefenseMultiplierProvider;

        public void Initialize(WorldMapGrid grid, MapRenderer mapRenderer, int seed)
        {
            _grid = grid;
            _mapRenderer = mapRenderer;

            int maxId = GameConfig.PlayerNationCount + GameConfig.TribeCount + 5;
            _sumX = new int[maxId];
            _sumY = new int[maxId];
            _counts = new int[maxId];

            SpawnNationsAndTribes(seed);
            RecalculateStatsAndCenters();
            TickEconomy(0.01f);
            _grid.UpdateAllBorders();
            _mapRenderer.MarkDirty();
        }

        private void SpawnNationsAndTribes(int seed)
        {
            _nations.Clear();
            _nationList.Clear();
            _activeWaves.Clear();

            System.Random rng = new System.Random(seed + 101);
            List<Vector2Int> spawnedCenters = new List<Vector2Int>(220);

            // 1. Spawn Player Nation ("SnickTrix") in the central peninsula (like the screenshot!)
            Vector2Int playerPos = FindLandNear(
                Mathf.RoundToInt(_grid.Width * 0.42f),
                Mathf.RoundToInt(_grid.Height * 0.47f),
                45
            );
            PlayerNation = CreateAndClaimNation(1, "SnickTrix", true, false, playerPos.x, playerPos.y, 17, rng);
            PlayerNation.Troops = 7470;
            PlayerNation.MaxTroops = 20500;
            PlayerNation.Gold = 775000;
            spawnedCenters.Add(playerPos);

            // 2. Spawn Featured Neighboring Nations from the screenshot ("India", "Oman", "Fernhold", "Tideborn", "Yak Tribe", "Mammoth Moot", etc.)
            ushort nextId = 2;
            SpawnPinnedNation(ref nextId, "India", false, 0.36f, 0.69f, 24, 6280, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Oman", false, 0.20f, 0.55f, 23, 6430, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Fernhold", false, 0.35f, 0.48f, 11, 2400, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Tideborn", false, 0.30f, 0.63f, 10, 2210, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Mammoth Moot", true, 0.40f, 0.31f, 10, 1510, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Flintcarvers", true, 0.41f, 0.59f, 9, 2500, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Schreckseers", true, 0.45f, 0.64f, 9, 2380, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Frostwardens", false, 0.50f, 0.62f, 11, 2210, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Bramblefire", true, 0.53f, 0.52f, 9, 2400, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Yak Tribe", true, 0.59f, 0.70f, 11, 1870, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Mooncrest", false, 0.26f, 0.72f, 10, 1610, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Pelican Folk", true, 0.55f, 0.66f, 8, 1950, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "The Pinebridlers", true, 0.60f, 0.53f, 10, 2100, spawnedCenters, rng);
            SpawnPinnedNation(ref nextId, "Unrivaled Company", false, 0.21f, 0.68f, 10, 2050, spawnedCenters, rng);

            int totalEntities = GameConfig.PlayerNationCount + GameConfig.TribeCount;
            int nameIdx = 14;
            int tribeIdx = 0;

            int attempts = 0;
            while (nextId <= totalEntities && attempts < 12000)
            {
                attempts++;
                int gx = rng.Next(8, _grid.Width - 8);
                int gy = rng.Next(8, _grid.Height - 8);
                int cellIdx = _grid.GetIndex(gx, gy);

                if (!_grid.IsLand(cellIdx) || _grid.OwnerId[cellIdx] != 0)
                    continue;

                bool tooClose = false;
                float minDistSq = nextId <= GameConfig.PlayerNationCount ? 210f : 115f;
                for (int i = 0; i < spawnedCenters.Count; i++)
                {
                    float dx = spawnedCenters[i].x - gx;
                    float dy = spawnedCenters[i].y - gy;
                    if (dx * dx + dy * dy < minDistSq)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                bool isTribe = nextId > GameConfig.PlayerNationCount;
                string nName;
                if (!isTribe)
                {
                    nName = nameIdx < NationNamePool.FeaturedNationNames.Length
                        ? NationNamePool.FeaturedNationNames[nameIdx++]
                        : $"Empire {nextId}";
                }
                else
                {
                    nName = tribeIdx < NationNamePool.TribeNames.Length
                        ? NationNamePool.TribeNames[tribeIdx++]
                        : $"Tribe {nextId - GameConfig.PlayerNationCount}";
                }

                int radius = isTribe ? rng.Next(5, 8) : rng.Next(7, 12);
                NationData nd = CreateAndClaimNation(nextId, nName, false, isTribe, gx, gy, radius, rng);
                nd.Troops = isTribe ? rng.Next(1100, 2600) : rng.Next(2200, 4800);
                nd.Gold = isTribe ? rng.Next(5000, 15000) : rng.Next(15000, 35000);
                spawnedCenters.Add(new Vector2Int(gx, gy));
                nextId++;
            }

            ExpandTerritoriesVoronoi();
        }

        private void ExpandTerritoriesVoronoi()
        {
            Queue<int> queue = new Queue<int>(_grid.TotalCells / 4);
            int[] dist = new int[_grid.TotalCells];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;

            int[] nBuf = new int[4];
            for (int y = 0; y < _grid.Height; y++)
            {
                int row = y * _grid.Width;
                for (int x = 0; x < _grid.Width; x++)
                {
                    int idx = row + x;
                    if (_grid.IsLand(idx) && _grid.OwnerId[idx] != 0)
                    {
                        int nCount = _grid.GetNeighbors4(x, y, nBuf);
                        for (int n = 0; n < nCount; n++)
                        {
                            int nIdx = nBuf[n];
                            if (_grid.IsLand(nIdx) && _grid.OwnerId[nIdx] == 0)
                            {
                                queue.Enqueue(idx);
                                dist[idx] = 0;
                                break;
                            }
                        }
                    }
                }
            }

            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                ushort owner = _grid.OwnerId[curr];
                int d = dist[curr];

                _grid.GetXY(curr, out int cx, out int cy);
                int nCount = _grid.GetNeighbors4(cx, cy, nBuf);
                for (int n = 0; n < nCount; n++)
                {
                    int next = nBuf[n];
                    if (!_grid.IsLand(next)) continue;

                    if (_grid.OwnerId[next] == 0)
                    {
                        _grid.OwnerId[next] = owner;
                        dist[next] = d + 1;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        private void SpawnPinnedNation(ref ushort nextId, string name, bool isTribe, float normX, float normY, int radius, double startTroops, List<Vector2Int> centers, System.Random rng)
        {
            Vector2Int pos = FindLandNear(
                Mathf.RoundToInt(_grid.Width * normX),
                Mathf.RoundToInt(_grid.Height * normY),
                40
            );
            NationData nd = CreateAndClaimNation(nextId, name, false, isTribe, pos.x, pos.y, radius, rng);
            nd.Troops = startTroops;
            nd.Gold = isTribe ? 12000 : 28000;
            centers.Add(pos);
            nextId++;
        }

        private Vector2Int FindLandNear(int targetX, int targetY, int maxSearchRadius)
        {
            targetX = Mathf.Clamp(targetX, 4, _grid.Width - 5);
            targetY = Mathf.Clamp(targetY, 4, _grid.Height - 5);

            for (int r = 0; r <= maxSearchRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = targetX + dx;
                        int y = targetY + dy;
                        if (!_grid.InBounds(x, y)) continue;
                        int idx = _grid.GetIndex(x, y);
                        if (_grid.IsLand(idx) && _grid.OwnerId[idx] == 0)
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            return new Vector2Int(targetX, targetY);
        }

        private NationData CreateAndClaimNation(ushort id, string name, bool isPlayer, bool isTribe, int cx, int cy, int radius, System.Random rng)
        {
            NationNamePool.GetNationColors(id, isPlayer, isTribe, name, out Color32 fill, out Color32 border);

            NationData nation = new NationData
            {
                Id = id,
                Name = name,
                IsPlayer = isPlayer,
                IsTribe = isTribe,
                IsAlive = true,
                FillColor = fill,
                BorderColor = border,
                CapitalX = cx,
                CapitalY = cy,
                CenterGridPos = new Vector2(cx, cy),
                MaxTroops = 15000
            };

            int rSq = radius * radius;
            for (int dy = -radius - 2; dy <= radius + 2; dy++)
            {
                for (int dx = -radius - 2; dx <= radius + 2; dx++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (!_grid.InBounds(x, y)) continue;
                    int idx = _grid.GetIndex(x, y);
                    if (!_grid.IsLand(idx) || _grid.OwnerId[idx] != 0) continue;

                    float distSq = dx * dx + dy * dy;
                    float jitter = (float)(rng.NextDouble() * 0.35 + 0.82);
                    if (distSq <= rSq * jitter)
                    {
                        _grid.OwnerId[idx] = id;
                    }
                }
            }

            _nations[id] = nation;
            _nationList.Add(nation);
            return nation;
        }

        public void Tick(float dt)
        {
            if (ProtectionTimer > 0f)
            {
                ProtectionTimer = Mathf.Max(0f, ProtectionTimer - dt);
            }

            TickCombat();
            TickEconomy(dt);
        }

        private void TickEconomy(float dt)
        {
            for (int i = 0; i < _nationList.Count; i++)
            {
                NationData n = _nationList[i];
                if (!n.IsAlive) continue;

                // Calibrated to match RoWorld reference:
                // Starting with ~900 tiles: MaxTroops ~23K, Growth: +140/s, Gold: 775K, Gold Growth: +1.26K/s
                n.MaxTroops = 5000.0 + (n.TileCount * (n.IsTribe ? 10.0 : 18.0)) + (n.CityCount * 7500.0);
                double fillRatio = n.MaxTroops > 0 ? (n.Troops / n.MaxTroops) : 1.0;

                // Logistic troop growth curve (fastest around 35-50% capacity, tapers near cap)
                double logisticFactor = Math.Max(0.0, 1.0 - (fillRatio * fillRatio));
                double baseGrowth = (n.IsTribe ? 16.0 : 42.0) + (n.TileCount * (n.IsTribe ? 0.04 : 0.118)) + (n.CityCount * 45.0);
                n.TroopGrowthPerSec = baseGrowth * logisticFactor;

                n.Troops = Math.Min(n.MaxTroops, n.Troops + n.TroopGrowthPerSec * dt);

                n.GoldGrowthPerSec = (n.IsTribe ? 25.0 : 240.0)
                    + (n.TileCount * (n.IsTribe ? 0.45 : 1.15))
                    + (n.CityCount * 160.0)
                    + (n.PortCount * 320.0)
                    + (n.FactoryCount * 650.0);

                n.Gold += n.GoldGrowthPerSec * dt;
            }
        }

        public bool StartAttack(ushort attackerId, ushort targetOwnerId, float ratio, int clickGridX, int clickGridY)
        {
            if (attackerId == targetOwnerId) return false;
            if (!_nations.TryGetValue(attackerId, out NationData attacker) || !attacker.IsAlive) return false;

            if (targetOwnerId != 0 && _nations.TryGetValue(targetOwnerId, out NationData targetNation))
            {
                if (attacker.IsAlliedWith(targetOwnerId)) return false;

                // Protect player during initial timer if AI tries to attack player
                if (targetNation.IsPlayer && ProtectionTimer > 0f && !attacker.IsPlayer)
                {
                    return false;
                }
            }

            ratio = Mathf.Clamp(ratio, 0.05f, 1.0f);
            double sendTroops = attacker.Troops * ratio;
            if (sendTroops < 25.0) return false;

            bool hasLandBorder = HasAdjacentBorder(attackerId, targetOwnerId);
            bool isNaval = false;

            if (!hasLandBorder)
            {
                // Allow naval landing if attacker has a port (or is player clicking coastal tile) and target pixel is near coast
                int clickIdx = _grid.GetIndex(clickGridX, clickGridY);
                if (_grid.InBounds(clickGridX, clickGridY) && _grid.IsLand(clickIdx) && _grid.OwnerId[clickIdx] == targetOwnerId)
                {
                    if (attacker.PortCount > 0 || (attacker.IsPlayer && IsNearCoast(clickGridX, clickGridY, 4)))
                    {
                        isNaval = true;
                    }
                }
                if (!isNaval) return false;
            }

            attacker.Troops -= sendTroops;

            // Check if there is already an active wave from attacker to targetOwnerId
            AttackWave wave = null;
            for (int i = 0; i < _activeWaves.Count; i++)
            {
                AttackWave existing = _activeWaves[i];
                if (existing.AttackerId == attackerId && existing.TargetOwnerId == targetOwnerId)
                {
                    existing.RemainingTroops += sendTroops;
                    wave = existing;
                    break;
                }
            }

            if (wave == null)
            {
                wave = new AttackWave
                {
                    AttackerId = attackerId,
                    TargetOwnerId = targetOwnerId,
                    RemainingTroops = sendTroops,
                    SeedGridX = clickGridX,
                    SeedGridY = clickGridY,
                    IsNavalInvasion = isNaval
                };
                _activeWaves.Add(wave);
            }

            // Populate/refresh wave frontier focused around (clickGridX, clickGridY)
            RefreshWaveFrontier(wave, clickGridX, clickGridY);

            return true;
        }

        private void RefreshWaveFrontier(AttackWave wave, int focusX, int focusY)
        {
            if (wave.ActiveFrontier.Count > 0) return;

            List<int> candidateBorder = new List<int>(64);
            int w = _grid.Width;
            int h = _grid.Height;

            // Find border cells of TargetOwnerId adjacent to AttackerId
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    if (_grid.OwnerId[idx] != wave.TargetOwnerId || !_grid.IsLand(idx)) continue;

                    if ((x > 0 && _grid.OwnerId[idx - 1] == wave.AttackerId) ||
                        (x < w - 1 && _grid.OwnerId[idx + 1] == wave.AttackerId) ||
                        (y > 0 && _grid.OwnerId[idx - w] == wave.AttackerId) ||
                        (y < h - 1 && _grid.OwnerId[idx + w] == wave.AttackerId))
                    {
                        candidateBorder.Add(idx);
                    }
                }
            }

            if (candidateBorder.Count == 0 && wave.IsNavalInvasion)
            {
                int seedIdx = _grid.GetIndex(wave.SeedGridX, wave.SeedGridY);
                if (_grid.InBounds(wave.SeedGridX, wave.SeedGridY) && _grid.IsLand(seedIdx) && _grid.OwnerId[seedIdx] == wave.TargetOwnerId)
                {
                    candidateBorder.Add(seedIdx);
                }
                wave.IsNavalInvasion = false;
            }

            // Sort so frontier starts exactly at the clicked front!
            candidateBorder.Sort((a, b) =>
            {
                _grid.GetXY(a, out int ax, out int ay);
                _grid.GetXY(b, out int bx, out int by);
                int da = (ax - focusX) * (ax - focusX) + (ay - focusY) * (ay - focusY);
                int db = (bx - focusX) * (bx - focusX) + (by - focusY) * (by - focusY);
                return da.CompareTo(db);
            });

            for (int i = 0; i < candidateBorder.Count; i++)
            {
                int cell = candidateBorder[i];
                if (wave.InFrontier.Add(cell))
                {
                    wave.ActiveFrontier.Add(cell);
                }
            }
        }

        public bool HasAdjacentBorder(ushort ownerA, ushort ownerB)
        {
            int w = _grid.Width;
            int h = _grid.Height;

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    if (_grid.OwnerId[idx] != ownerA || !_grid.IsBorder[idx]) continue;

                    int nCount = _grid.GetNeighbors4(x, y, _neighborBuffer);
                    for (int n = 0; n < nCount; n++)
                    {
                        int nIdx = _neighborBuffer[n];
                        if (_grid.IsLand(nIdx) && _grid.OwnerId[nIdx] == ownerB)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsNearCoast(int gx, int gy, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = gx + dx;
                    int y = gy + dy;
                    if (_grid.InBounds(x, y) && _grid.IsCoast[_grid.GetIndex(x, y)])
                        return true;
                }
            }
            return false;
        }

        private void TickCombat()
        {
            if (_activeWaves.Count == 0) return;

            bool anyPixelChanged = false;
            int minChangedX = _grid.Width;
            int minChangedY = _grid.Height;
            int maxChangedX = 0;
            int maxChangedY = 0;

            for (int wIdx = _activeWaves.Count - 1; wIdx >= 0; wIdx--)
            {
                AttackWave wave = _activeWaves[wIdx];
                if (!_nations.TryGetValue(wave.AttackerId, out NationData attacker) || !attacker.IsAlive || wave.RemainingTroops <= 5.0)
                {
                    _activeWaves.RemoveAt(wIdx);
                    continue;
                }

                if (wave.ActiveFrontier.Count == 0)
                {
                    RefreshWaveFrontier(wave, wave.SeedGridX, wave.SeedGridY);
                }

                if (wave.ActiveFrontier.Count == 0)
                {
                    // Return remaining unused troops back to attacker
                    attacker.Troops = Math.Min(attacker.MaxTroops, attacker.Troops + wave.RemainingTroops);
                    _activeWaves.RemoveAt(wIdx);
                    continue;
                }

                NationData defender = null;
                if (wave.TargetOwnerId != 0)
                {
                    _nations.TryGetValue(wave.TargetOwnerId, out defender);
                }

                int maxPixelsThisTick = Mathf.Clamp(wave.ActiveFrontier.Count / 3 + 8, 8, 35);
                int conquered = 0;

                while (wave.ActiveFrontier.Count > 0 && conquered < maxPixelsThisTick)
                {
                    int targetCell = wave.ActiveFrontier[0];
                    wave.ActiveFrontier.RemoveAt(0);
                    wave.InFrontier.Remove(targetCell);

                    if (_grid.OwnerId[targetCell] != wave.TargetOwnerId) continue;

                    _grid.GetXY(targetCell, out int cx, out int cy);

                    double pixelCost = 6.5; // neutral land cost
                    if (defender != null && defender.IsAlive)
                    {
                        double defenderDensity = defender.Troops / Math.Max(10, defender.TileCount);
                        float defMult = DefenseMultiplierProvider != null
                            ? DefenseMultiplierProvider(defender.Id, cx, cy)
                            : 1.0f;

                        if (_grid.Terrain[targetCell] == TerrainType.Mountains) defMult *= 1.4f;
                        else if (_grid.Terrain[targetCell] == TerrainType.Highlands) defMult *= 1.15f;

                        pixelCost = Math.Max(7.0, (8.5 + defenderDensity * 0.65) * defMult);
                    }

                    if (wave.RemainingTroops < pixelCost)
                    {
                        wave.RemainingTroops = 0;
                        break;
                    }

                    wave.RemainingTroops -= pixelCost;
                    if (defender != null && defender.IsAlive)
                    {
                        defender.Troops = Math.Max(0.0, defender.Troops - pixelCost * 0.65);
                    }

                    _grid.OwnerId[targetCell] = wave.AttackerId;
                    conquered++;
                    anyPixelChanged = true;

                    if (cx < minChangedX) minChangedX = cx;
                    if (cy < minChangedY) minChangedY = cy;
                    if (cx > maxChangedX) maxChangedX = cx;
                    if (cy > maxChangedY) maxChangedY = cy;

                    // Expand frontier with adjacent enemy cells
                    int nCount = _grid.GetNeighbors4(cx, cy, _neighborBuffer);
                    for (int n = 0; n < nCount; n++)
                    {
                        int nIdx = _neighborBuffer[n];
                        if (_grid.IsLand(nIdx) && _grid.OwnerId[nIdx] == wave.TargetOwnerId)
                        {
                            if (wave.InFrontier.Add(nIdx))
                            {
                                wave.ActiveFrontier.Add(nIdx);
                            }
                        }
                    }
                }

                if (wave.RemainingTroops <= 5.0)
                {
                    _activeWaves.RemoveAt(wIdx);
                }
            }

            if (anyPixelChanged)
            {
                _grid.UpdateBordersInRect(minChangedX, minChangedY, maxChangedX, maxChangedY);
                RecalculateStatsAndCenters();
                _mapRenderer.MarkDirty();
            }
        }

        public void RecalculateStatsAndCenters()
        {
            Array.Clear(_sumX, 0, _sumX.Length);
            Array.Clear(_sumY, 0, _sumY.Length);
            Array.Clear(_counts, 0, _counts.Length);

            int w = _grid.Width;
            int h = _grid.Height;

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    ushort owner = _grid.OwnerId[row + x];
                    if (owner > 0 && owner < _counts.Length)
                    {
                        _counts[owner]++;
                        _sumX[owner] += x;
                        _sumY[owner] += y;
                    }
                }
            }

            float invTotalLand = 1f / Mathf.Max(1, _grid.TotalLandCells);
            NationData topNation = null;
            int bestTiles = -1;

            for (int i = 0; i < _nationList.Count; i++)
            {
                NationData n = _nationList[i];
                int c = n.Id < _counts.Length ? _counts[n.Id] : 0;
                n.TileCount = c;
                n.WorldControlPercent = c * invTotalLand;
                n.IsAlive = c > 0;

                if (c > 0)
                {
                    n.CenterGridPos = new Vector2((float)_sumX[n.Id] / c, (float)_sumY[n.Id] / c);
                }

                if (c > bestTiles)
                {
                    bestTiles = c;
                    topNation = n;
                }
            }

            LeadingNation = topNation;

            // Compute Player Rank
            if (PlayerNation != null)
            {
                int rank = 1;
                int playerTiles = PlayerNation.TileCount;
                for (int i = 0; i < _nationList.Count; i++)
                {
                    if (_nationList[i].Id != PlayerNation.Id && _nationList[i].TileCount > playerTiles)
                    {
                        rank++;
                    }
                }
                PlayerRank = rank;
            }
        }
    }
}
