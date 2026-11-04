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
        private int _lastSeed;
        private readonly int[] _neighborBuffer = new int[4];
        private readonly List<int> _candidatePixels = new List<int>(512);

        public Func<ushort, int, int, float> DefenseMultiplierProvider;
        public Action<NationData, NationData> OnNationPlundered;

        public void Initialize(WorldMapGrid grid, MapRenderer mapRenderer, int seed)
        {
            _grid = grid;
            _mapRenderer = mapRenderer;
            _lastSeed = seed;

            int maxId = GameConfig.PlayerNationCount + GameConfig.TribeCount + 10;
            _sumX = new int[maxId];
            _sumY = new int[maxId];
            _counts = new int[maxId];

            _nations.Clear();
            _nationList.Clear();
            _activeWaves.Clear();

            GamePlayMode mode = KeybindManager.Instance.CurrentGameMode;
            if (mode == GamePlayMode.Spectator)
            {
                ProtectionTimer = 0f;
                SpawnSpectatorWorld(seed);
            }
        }

        public Vector2Int FindBestStartingLand()
        {
            if (_grid == null) return new Vector2Int(280, 160);

            if (_lastSeed == 20261103)
            {
                return FindLandNear(
                    Mathf.RoundToInt(_grid.Width * 0.42f),
                    Mathf.RoundToInt(_grid.Height * 0.47f),
                    45
                );
            }

            int bestX = _grid.Width / 2;
            int bestY = _grid.Height / 2;
            int bestScore = -1;

            System.Random rng = new System.Random(_lastSeed + 77);
            for (int t = 0; t < 300; t++)
            {
                int rx = rng.Next(Mathf.RoundToInt(_grid.Width * 0.15f), Mathf.RoundToInt(_grid.Width * 0.85f));
                int ry = rng.Next(Mathf.RoundToInt(_grid.Height * 0.15f), Mathf.RoundToInt(_grid.Height * 0.85f));
                int idx = _grid.GetIndex(rx, ry);
                if (!_grid.IsLand(idx)) continue;

                int landCount = 0;
                for (int dy = -12; dy <= 12; dy += 3)
                {
                    for (int dx = -12; dx <= 12; dx += 3)
                    {
                        int nx = rx + dx;
                        int ny = ry + dy;
                        if (_grid.InBounds(nx, ny) && _grid.IsLand(_grid.GetIndex(nx, ny)))
                            landCount++;
                    }
                }

                if (landCount > bestScore)
                {
                    bestScore = landCount;
                    bestX = rx;
                    bestY = ry;
                    if (bestScore > 50) break;
                }
            }

            return new Vector2Int(bestX, bestY);
        }

        public void SpawnPlayerAndAI(int playerSpawnX, int playerSpawnY, string playerName, int seed)
        {
            _nations.Clear();
            _nationList.Clear();
            _activeWaves.Clear();

            System.Random rng = new System.Random(seed + 101);
            List<Vector2Int> spawnedCenters = new List<Vector2Int>(220);

            GameDifficulty diff = KeybindManager.Instance.CurrentDifficulty;
            switch (diff)
            {
                case GameDifficulty.Easy: ProtectionTimer = 240f; break;
                case GameDifficulty.Hard: ProtectionTimer = 90f; break;
                case GameDifficulty.Chaos: ProtectionTimer = 35f; break;
                default: ProtectionTimer = GameConfig.InitialProtectionSeconds; break;
            }

            // 1. Spawn Player Nation at chosen location
            Vector2Int playerPos = new Vector2Int(playerSpawnX, playerSpawnY);
            if (!_grid.InBounds(playerPos.x, playerPos.y) || !_grid.IsLand(_grid.GetIndex(playerPos.x, playerPos.y)))
            {
                playerPos = FindLandNear(playerSpawnX, playerSpawnY, 35);
            }

            PlayerNation = CreateAndClaimNation(1, playerName, true, false, playerPos.x, playerPos.y, 5, rng);
            switch (diff)
            {
                case GameDifficulty.Easy:
                    PlayerNation.Troops = 1200;
                    PlayerNation.MaxTroops = 3500;
                    PlayerNation.Gold = 35000;
                    break;
                case GameDifficulty.Hard:
                    PlayerNation.Troops = 500;
                    PlayerNation.MaxTroops = 1800;
                    PlayerNation.Gold = 5000;
                    break;
                case GameDifficulty.Chaos:
                    PlayerNation.Troops = 350;
                    PlayerNation.MaxTroops = 1200;
                    PlayerNation.Gold = 0;
                    break;
                default:
                    PlayerNation.Troops = 750;
                    PlayerNation.MaxTroops = 2500;
                    PlayerNation.Gold = 15000;
                    break;
            }
            spawnedCenters.Add(playerPos);

            // 2. Spawn remaining 87 AI Nations and 110 Tribes across the map
            ushort nextId = 2;
            int totalEntities = GameConfig.PlayerNationCount + GameConfig.TribeCount;
            int nameIdx = 0;
            int tribeIdx = 0;

            int attempts = 0;
            while (nextId <= totalEntities && attempts < 16000)
            {
                attempts++;
                int gx = rng.Next(6, _grid.Width - 6);
                int gy = rng.Next(6, _grid.Height - 6);
                int cellIdx = _grid.GetIndex(gx, gy);

                if (!_grid.IsLand(cellIdx) || _grid.OwnerId[cellIdx] != 0)
                    continue;

                float dxPlayer = gx - playerPos.x;
                float dyPlayer = gy - playerPos.y;
                if (dxPlayer * dxPlayer + dyPlayer * dyPlayer < 160f)
                    continue;

                bool isTribe = nextId > GameConfig.PlayerNationCount;
                bool tooClose = false;
                float minDistSq = isTribe ? 85f : 160f;
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

                string nName;
                if (!isTribe)
                {
                    nName = nameIdx < NationNamePool.FeaturedNationNames.Length
                        ? NationNamePool.FeaturedNationNames[nameIdx++]
                        : $"Nation {nextId}";
                }
                else
                {
                    nName = tribeIdx < NationNamePool.TribeNames.Length
                        ? NationNamePool.TribeNames[tribeIdx++]
                        : $"Tribe {nextId - GameConfig.PlayerNationCount}";
                }

                // AI Nations start with radius 4-5 (~35-55 tiles), Tribes start with radius 3-4 (~18-35 tiles)
                int radius = isTribe ? rng.Next(3, 5) : rng.Next(4, 6);
                NationData nd = CreateAndClaimNation(nextId, nName, false, isTribe, gx, gy, radius, rng);
                nd.Troops = isTribe ? rng.Next(180, 420) : rng.Next(450, 800);
                nd.MaxTroops = isTribe ? 1400 : 2200;
                nd.Gold = isTribe ? rng.Next(2000, 6500) : rng.Next(6000, 15000);
                spawnedCenters.Add(new Vector2Int(gx, gy));
                nextId++;
            }

            // ExpandTerritoriesVoronoi() removed: game starts with authentic neutral wilderness (~85%)
            RecalculateStatsAndCenters();
            TickEconomy(0.01f);
            _grid.UpdateAllBorders();
            _mapRenderer.MarkDirty();
        }

        private void SpawnSpectatorWorld(int seed)
        {
            _nations.Clear();
            _nationList.Clear();
            _activeWaves.Clear();
            PlayerNation = null;

            System.Random rng = new System.Random(seed + 101);
            List<Vector2Int> spawnedCenters = new List<Vector2Int>(220);

            ushort nextId = 1;
            int totalEntities = GameConfig.PlayerNationCount + GameConfig.TribeCount;
            int nameIdx = 0;
            int tribeIdx = 0;

            int attempts = 0;
            while (nextId <= totalEntities && attempts < 16000)
            {
                attempts++;
                int gx = rng.Next(6, _grid.Width - 6);
                int gy = rng.Next(6, _grid.Height - 6);
                int cellIdx = _grid.GetIndex(gx, gy);

                if (!_grid.IsLand(cellIdx) || _grid.OwnerId[cellIdx] != 0)
                    continue;

                bool isTribe = nextId > GameConfig.PlayerNationCount;
                bool tooClose = false;
                float minDistSq = isTribe ? 85f : 160f;
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

                string nName;
                if (!isTribe)
                {
                    nName = nameIdx < NationNamePool.FeaturedNationNames.Length
                        ? NationNamePool.FeaturedNationNames[nameIdx++]
                        : $"Nation {nextId}";
                }
                else
                {
                    nName = tribeIdx < NationNamePool.TribeNames.Length
                        ? NationNamePool.TribeNames[tribeIdx++]
                        : $"Tribe {nextId - GameConfig.PlayerNationCount}";
                }

                int radius = isTribe ? rng.Next(3, 5) : rng.Next(4, 6);
                NationData nd = CreateAndClaimNation(nextId, nName, false, isTribe, gx, gy, radius, rng);
                nd.Troops = isTribe ? rng.Next(180, 420) : rng.Next(450, 800);
                nd.MaxTroops = isTribe ? 1400 : 2200;
                nd.Gold = isTribe ? rng.Next(2000, 6500) : rng.Next(6000, 15000);
                spawnedCenters.Add(new Vector2Int(gx, gy));
                nextId++;
            }

            RecalculateStatsAndCenters();
            TickEconomy(0.01f);
            _grid.UpdateAllBorders();
            _mapRenderer.MarkDirty();
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

            int capIdx = _grid.GetIndex(cx, cy);
            if (_grid.InBounds(cx, cy) && _grid.IsLand(capIdx))
            {
                _grid.OwnerId[capIdx] = id;
            }

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

            for (int i = 0; i < _nationList.Count; i++)
            {
                if (_nationList[i].TraitorTimer > 0f)
                {
                    _nationList[i].TraitorTimer = Mathf.Max(0f, _nationList[i].TraitorTimer - dt);
                }
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

                // OpenFront/RoWorld population scaling:
                // Base capacity ~1500, +12 per tile, +25,000 per constructed City
                n.MaxTroops = 1500.0 + (n.TileCount * (n.IsTribe ? 6.0 : 12.0)) + (n.CityCount * 25000.0);
                double fillRatio = n.MaxTroops > 0 ? (n.Troops / n.MaxTroops) : 0.0;

                // The ~42% Rule from OpenFront / RoWorld:
                // Troop regeneration is non-linear and peaks at ~42% capacity.
                // Draining troops to zero causes growth to crawl; near 100% capacity it tapers off.
                double diffFromPeak = fillRatio - 0.42;
                double growthFactor = Math.Max(0.08, 1.0 - (diffFromPeak * diffFromPeak * 4.5));
                double baseGrowth = (n.IsTribe ? 8.0 : 22.0) + (n.TileCount * (n.IsTribe ? 0.03 : 0.095)) + (n.CityCount * 70.0);
                n.TroopGrowthPerSec = baseGrowth * growthFactor;

                n.Troops = Math.Min(n.MaxTroops, n.Troops + n.TroopGrowthPerSec * dt);

                // Early game starts with modest gold income (~120-150/s), scaling with territory and high-tier buildings
                n.GoldGrowthPerSec = (n.IsTribe ? 18.0 : 75.0)
                    + (n.TileCount * (n.IsTribe ? 0.30 : 0.85))
                    + (n.CityCount * 280.0)
                    + (n.PortCount * 500.0)
                    + (n.FactoryCount * 850.0);

                // Trade sanctions / Embargoes reduce mutual gold growth
                if (n.Embargoes.Count > 0)
                {
                    double embargoMult = Math.Max(0.55, 1.0 - n.Embargoes.Count * 0.12);
                    n.GoldGrowthPerSec *= embargoMult;
                }

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

                    double pixelCost = 6.0; // neutral land base cost
                    if (_grid.Terrain[targetCell] == TerrainType.Mountains) pixelCost = 9.5;
                    else if (_grid.Terrain[targetCell] == TerrainType.Highlands) pixelCost = 7.5;

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

                if (wave.TargetOwnerId == 0 && wave.ActiveFrontier.Count > 24)
                {
                    wave.ActiveFrontier.Sort((a, b) =>
                    {
                        _grid.GetXY(a, out int ax, out int ay);
                        _grid.GetXY(b, out int bx, out int by);
                        int da = (ax - wave.SeedGridX) * (ax - wave.SeedGridX) + (ay - wave.SeedGridY) * (ay - wave.SeedGridY);
                        int db = (bx - wave.SeedGridX) * (bx - wave.SeedGridX) + (by - wave.SeedGridY) * (by - wave.SeedGridY);
                        return da.CompareTo(db);
                    });
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
                bool wasAlive = n.IsAlive;
                n.TileCount = c;
                n.WorldControlPercent = c * invTotalLand;
                n.IsAlive = c > 0;

                if (wasAlive && c == 0)
                {
                    // OpenFront plunder mechanic: conqueror plunders victim's gold treasury
                    int capIdx = _grid.GetIndex(n.CapitalX, n.CapitalY);
                    ushort conquerorId = _grid.InBounds(n.CapitalX, n.CapitalY) ? _grid.OwnerId[capIdx] : (ushort)0;
                    if (conquerorId > 0 && _nations.TryGetValue(conquerorId, out NationData conqueror) && conqueror.IsAlive)
                    {
                        conqueror.Gold += n.Gold;
                        conqueror.Troops = Math.Min(conqueror.MaxTroops, conqueror.Troops + n.Troops * 0.35);
                        OnNationPlundered?.Invoke(conqueror, n);
                    }
                }

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
