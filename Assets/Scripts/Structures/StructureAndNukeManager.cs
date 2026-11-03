using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.Nations;
using MeWorld.UI;
using UnityEngine;

namespace MeWorld.Structures
{
    public class StructureInstance
    {
        public int Id;
        public StructureType Type;
        public ushort OwnerId;
        public int GridX;
        public int GridY;
        public GameObject RootObj;
        public float Cooldown;
    }

    public class MissileInFlight
    {
        public StructureType BombType;
        public ushort SenderId;
        public Vector3 StartWorld;
        public Vector3 TargetWorld;
        public int TargetGridX;
        public int TargetGridY;
        public float Progress;
        public float Speed;
        public LineRenderer TrajectoryLine;
        public Transform MissileHead;
        public bool Intercepted;
    }

    public class WarshipUnit
    {
        public ushort OwnerId;
        public int GridX;
        public int GridY;
        public Transform Visual;
        public float MoveTimer;
        public float FireCooldown;
    }

    public class StructureAndNukeManager : MonoBehaviour
    {
        private WorldMapGrid _grid;
        private MapRenderer _mapRenderer;
        private TerritoryAndCombatManager _territoryManager;

        private readonly List<StructureInstance> _structures = new List<StructureInstance>(256);
        private readonly List<MissileInFlight> _missiles = new List<MissileInFlight>(32);
        private readonly List<WarshipUnit> _warships = new List<WarshipUnit>(32);

        private Transform _structuresRoot;
        private int _nextStructureId = 1;
        private Material _lineMaterial;

        public IReadOnlyList<StructureInstance> Structures => _structures;

        public void Initialize(WorldMapGrid grid, MapRenderer mapRenderer, TerritoryAndCombatManager territoryManager)
        {
            _grid = grid;
            _mapRenderer = mapRenderer;
            _territoryManager = territoryManager;
            _territoryManager.DefenseMultiplierProvider = GetDefenseMultiplierAt;

            if (_structuresRoot != null) Destroy(_structuresRoot.gameObject);
            _structuresRoot = new GameObject("WorldStructures_Root").transform;
            _structuresRoot.SetParent(transform, false);

            Shader lineShader = Shader.Find("Sprites/Default");
            _lineMaterial = new Material(lineShader);

            // Give the player a starting City badge inside SnickTrix territory (just like the screenshot!)
            NationData player = _territoryManager.PlayerNation;
            if (player != null)
            {
                int bx = Mathf.Clamp(player.CapitalX - 8, 2, _grid.Width - 3);
                int by = Mathf.Clamp(player.CapitalY + 6, 2, _grid.Height - 3);
                if (_grid.OwnerId[_grid.GetIndex(bx, by)] != player.Id)
                {
                    bx = Mathf.Clamp(player.CapitalX - 5, 2, _grid.Width - 3);
                    by = Mathf.Clamp(player.CapitalY + 4, 2, _grid.Height - 3);
                }
                SpawnStructureInternal(player, StructureType.City, bx, by);
            }
        }

        public float GetDefenseMultiplierAt(ushort defenderId, int gx, int gy)
        {
            float mult = 1.0f;
            const int defRadiusSq = 18 * 18;

            for (int i = 0; i < _structures.Count; i++)
            {
                StructureInstance s = _structures[i];
                if (s.OwnerId == defenderId && s.Type == StructureType.DefensePost)
                {
                    int dx = s.GridX - gx;
                    int dy = s.GridY - gy;
                    if (dx * dx + dy * dy <= defRadiusSq)
                    {
                        return 2.5f;
                    }
                }
            }
            return mult;
        }

        public bool TryBuildOrExecuteAction(ushort nationId, StructureType actionType, int gx, int gy, out string feedbackMessage)
        {
            feedbackMessage = string.Empty;
            if (!_grid.InBounds(gx, gy))
            {
                feedbackMessage = "Out of map bounds.";
                return false;
            }

            if (!_territoryManager.Nations.TryGetValue(nationId, out NationData nation) || !nation.IsAlive)
            {
                feedbackMessage = "Nation is not active.";
                return false;
            }

            double cost = GameConfig.GetActionCost(actionType);
            if (nation.Gold < cost)
            {
                feedbackMessage = $"Not enough Gold! Need {GameConfig.FormatNumber(cost)}.";
                return false;
            }

            int cellIdx = _grid.GetIndex(gx, gy);

            // Handle Diplomacy / Alliance
            if (actionType == StructureType.Alliance)
            {
                ushort targetOwner = _grid.OwnerId[cellIdx];
                if (targetOwner == 0 || targetOwner == nationId)
                {
                    feedbackMessage = "Click on another nation's territory to forge an alliance.";
                    return false;
                }
                if (_territoryManager.Nations.TryGetValue(targetOwner, out NationData otherNation))
                {
                    nation.Allies.Add(targetOwner);
                    otherNation.Allies.Add(nationId);
                    feedbackMessage = $"Alliance forged with {otherNation.Name}!";
                    return true;
                }
                return false;
            }

            // Handle Nuclear Bombs
            if (actionType == StructureType.AtomBomb || actionType == StructureType.HydrogenBomb)
            {
                if (nation.SiloCount <= 0)
                {
                    feedbackMessage = "Cannot launch nukes! You need an active Missile Silo first.";
                    return false;
                }
                nation.Gold -= cost;
                LaunchNuclearMissile(nation, actionType, gx, gy);
                feedbackMessage = $"{actionType} launched at ({gx}, {gy})!";
                return true;
            }

            // Handle Warship
            if (actionType == StructureType.Warship)
            {
                if (!FindNearestWaterCell(gx, gy, 8, out int wx, out int wy))
                {
                    feedbackMessage = "Warships must be deployed on or near ocean/water tiles!";
                    return false;
                }
                nation.Gold -= cost;
                SpawnWarship(nation, wx, wy);
                feedbackMessage = "Warship deployed!";
                return true;
            }

            // Standard Structures (City, Port, Factory, DefensePost, SAMLauncher, MissileSilo)
            if (_grid.OwnerId[cellIdx] != nationId)
            {
                feedbackMessage = "Structures must be built inside your own territory!";
                return false;
            }

            if (actionType == StructureType.Port && !IsWithinCoastDistance(gx, gy, 4))
            {
                feedbackMessage = "Ports must be built near a coastline!";
                return false;
            }

            nation.Gold -= cost;
            SpawnStructureInternal(nation, actionType, gx, gy);
            feedbackMessage = $"{actionType} constructed!";
            return true;
        }

        private void SpawnStructureInternal(NationData nation, StructureType type, int gx, int gy)
        {
            switch (type)
            {
                case StructureType.City: nation.CityCount++; break;
                case StructureType.Port: nation.PortCount++; break;
                case StructureType.Factory: nation.FactoryCount++; break;
                case StructureType.DefensePost: nation.DefensePostCount++; break;
                case StructureType.SAMLauncher: nation.SamCount++; break;
                case StructureType.MissileSilo: nation.SiloCount++; break;
            }

            Vector3 worldPos = _mapRenderer.GridToWorld(gx, gy);
            GameObject badgeObj = new GameObject($"Struct_{type}_{nation.Name}");
            badgeObj.transform.SetParent(_structuresRoot, false);
            badgeObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f);
            badgeObj.transform.localScale = Vector3.one * 0.44f;

            // Dark circular badge background (like the building icon inside SnickTrix in the screenshot)
            SpriteRenderer bgSr = badgeObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
            bgSr.color = new Color32(32, 36, 46, 235);
            bgSr.sortingOrder = 10;

            // Thin white ring border
            GameObject ringObj = new GameObject("Ring");
            ringObj.transform.SetParent(badgeObj.transform, false);
            SpriteRenderer ringSr = ringObj.AddComponent<SpriteRenderer>();
            ringSr.sprite = ProceduralSpriteFactory.GetCircleSprite(true);
            ringSr.color = nation.IsPlayer ? new Color32(255, 215, 235, 240) : new Color32(210, 220, 235, 210);
            ringSr.sortingOrder = 11;

            // White procedural building icon inside
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(badgeObj.transform, false);
            iconObj.transform.localScale = Vector3.one * 0.56f;
            SpriteRenderer iconSr = iconObj.AddComponent<SpriteRenderer>();
            iconSr.sprite = ProceduralSpriteFactory.GetActionIcon(type);
            iconSr.color = Color.white;
            iconSr.sortingOrder = 12;

            _structures.Add(new StructureInstance
            {
                Id = _nextStructureId++,
                Type = type,
                OwnerId = nation.Id,
                GridX = gx,
                GridY = gy,
                RootObj = badgeObj,
                Cooldown = 0f
            });
        }

        private void SpawnWarship(NationData owner, int wx, int wy)
        {
            Vector3 worldPos = _mapRenderer.GridToWorld(wx, wy);
            GameObject shipObj = new GameObject($"Warship_{owner.Name}");
            shipObj.transform.SetParent(_structuresRoot, false);
            shipObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.6f);
            shipObj.transform.localScale = Vector3.one * 0.6f;

            SpriteRenderer bgSr = shipObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
            bgSr.color = owner.BorderColor;
            bgSr.sortingOrder = 14;

            GameObject iconObj = new GameObject("ShipIcon");
            iconObj.transform.SetParent(shipObj.transform, false);
            iconObj.transform.localScale = Vector3.one * 0.75f;
            SpriteRenderer iconSr = iconObj.AddComponent<SpriteRenderer>();
            iconSr.sprite = ProceduralSpriteFactory.GetActionIcon(StructureType.Warship);
            iconSr.color = Color.white;
            iconSr.sortingOrder = 15;

            _warships.Add(new WarshipUnit
            {
                OwnerId = owner.Id,
                GridX = wx,
                GridY = wy,
                Visual = shipObj.transform,
                MoveTimer = 0f,
                FireCooldown = 0f
            });
        }

        private void LaunchNuclearMissile(NationData sender, StructureType bombType, int targetGx, int targetGy)
        {
            int startGx = Mathf.RoundToInt(sender.CenterGridPos.x);
            int startGy = Mathf.RoundToInt(sender.CenterGridPos.y);

            // Launch from nearest owned Missile Silo if one exists
            for (int i = 0; i < _structures.Count; i++)
            {
                if (_structures[i].OwnerId == sender.Id && _structures[i].Type == StructureType.MissileSilo)
                {
                    startGx = _structures[i].GridX;
                    startGy = _structures[i].GridY;
                    break;
                }
            }

            Vector3 startW = _mapRenderer.GridToWorld(startGx, startGy);
            Vector3 targetW = _mapRenderer.GridToWorld(targetGx, targetGy);

            GameObject missileObj = new GameObject($"Missile_{bombType}");
            missileObj.transform.SetParent(_structuresRoot, false);

            LineRenderer lr = missileObj.AddComponent<LineRenderer>();
            lr.material = _lineMaterial;
            lr.startWidth = 0.09f;
            lr.endWidth = 0.04f;
            lr.positionCount = 2;
            Color trailCol = bombType == StructureType.HydrogenBomb
                ? new Color(1f, 0.25f, 0.1f, 0.9f)
                : new Color(1f, 0.75f, 0.15f, 0.9f);
            lr.startColor = trailCol;
            lr.endColor = Color.red;
            lr.sortingOrder = 30;
            lr.SetPosition(0, startW);
            lr.SetPosition(1, startW);

            SpriteRenderer headSr = missileObj.AddComponent<SpriteRenderer>();
            headSr.sprite = ProceduralSpriteFactory.GetActionIcon(bombType);
            headSr.color = Color.yellow;
            headSr.sortingOrder = 31;
            missileObj.transform.localScale = Vector3.one * 0.55f;

            _missiles.Add(new MissileInFlight
            {
                BombType = bombType,
                SenderId = sender.Id,
                StartWorld = startW,
                TargetWorld = targetW,
                TargetGridX = targetGx,
                TargetGridY = targetGy,
                Progress = 0f,
                Speed = 0.45f,
                TrajectoryLine = lr,
                MissileHead = missileObj.transform,
                Intercepted = false
            });
        }

        private void Update()
        {
            if (_grid == null) return;
            float dt = Time.deltaTime;

            UpdateStructureOwnershipAndCooldowns(dt);
            UpdateMissiles(dt);
            UpdateWarships(dt);
        }

        private void UpdateStructureOwnershipAndCooldowns(float dt)
        {
            for (int i = _structures.Count - 1; i >= 0; i--)
            {
                StructureInstance s = _structures[i];
                if (s.Cooldown > 0f) s.Cooldown -= dt;

                int idx = _grid.GetIndex(s.GridX, s.GridY);
                ushort currentCellOwner = _grid.OwnerId[idx];
                if (currentCellOwner != s.OwnerId)
                {
                    // Territory captured or scorched -> transfer or destroy structure
                    if (_territoryManager.Nations.TryGetValue(s.OwnerId, out NationData oldOwner))
                    {
                        DecrementStructureCount(oldOwner, s.Type);
                    }

                    if (currentCellOwner != 0 && _territoryManager.Nations.TryGetValue(currentCellOwner, out NationData newOwner))
                    {
                        s.OwnerId = currentCellOwner;
                        IncrementStructureCount(newOwner, s.Type);
                    }
                    else
                    {
                        if (s.RootObj != null) Destroy(s.RootObj);
                        _structures.RemoveAt(i);
                    }
                }
            }
        }

        private void UpdateMissiles(float dt)
        {
            for (int i = _missiles.Count - 1; i >= 0; i--)
            {
                MissileInFlight m = _missiles[i];
                m.Progress += m.Speed * dt;

                // Arc interpolation
                Vector3 flatPos = Vector3.Lerp(m.StartWorld, m.TargetWorld, Mathf.Clamp01(m.Progress));
                float arcHeight = Mathf.Sin(Mathf.Clamp01(m.Progress) * Mathf.PI) * 2.2f;
                Vector3 currentPos = new Vector3(flatPos.x, flatPos.y + arcHeight, -1f);

                if (m.MissileHead != null)
                {
                    m.MissileHead.position = currentPos;
                    m.TrajectoryLine.SetPosition(0, m.StartWorld);
                    m.TrajectoryLine.SetPosition(1, currentPos);
                }

                // Check SAM Launcher interception when missile is descending (Progress > 0.65f)
                if (!m.Intercepted && m.Progress >= 0.65f && m.Progress < 0.95f)
                {
                    for (int sIdx = 0; sIdx < _structures.Count; sIdx++)
                    {
                        StructureInstance sam = _structures[sIdx];
                        if (sam.Type == StructureType.SAMLauncher && sam.OwnerId != m.SenderId && sam.Cooldown <= 0f)
                        {
                            int targetIdx = _grid.GetIndex(m.TargetGridX, m.TargetGridY);
                            ushort targetCellOwner = _grid.OwnerId[targetIdx];

                            bool protectsTarget = (targetCellOwner == sam.OwnerId) ||
                                (_territoryManager.Nations.TryGetValue(sam.OwnerId, out NationData samNation) && samNation.IsAlliedWith(targetCellOwner));

                            if (!protectsTarget) continue;

                            int dx = sam.GridX - m.TargetGridX;
                            int dy = sam.GridY - m.TargetGridY;
                            if (dx * dx + dy * dy <= 42 * 42)
                            {
                                sam.Cooldown = 8.0f;
                                m.Intercepted = true;
                                SpawnBlastVisual(currentPos, 0.8f, Color.cyan);
                                if (m.MissileHead != null) Destroy(m.MissileHead.gameObject);
                                _missiles.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    if (m.Intercepted) continue;
                }

                if (m.Progress >= 1.0f)
                {
                    DetonateNuke(m);
                    if (m.MissileHead != null) Destroy(m.MissileHead.gameObject);
                    _missiles.RemoveAt(i);
                }
            }
        }

        private void DetonateNuke(MissileInFlight m)
        {
            int radius = m.BombType == StructureType.HydrogenBomb ? 26 : 13;
            int rSq = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > rSq) continue;
                    int x = m.TargetGridX + dx;
                    int y = m.TargetGridY + dy;
                    if (!_grid.InBounds(x, y)) continue;

                    int idx = _grid.GetIndex(x, y);
                    if (!_grid.IsLand(idx)) continue;

                    ushort victimId = _grid.OwnerId[idx];
                    if (victimId != 0 && _territoryManager.Nations.TryGetValue(victimId, out NationData victim))
                    {
                        victim.Troops = System.Math.Max(0.0, victim.Troops - 45.0);
                    }

                    _grid.OwnerId[idx] = 0;
                    if (dx * dx + dy * dy <= (radius * radius) / 2)
                    {
                        _grid.Terrain[idx] = TerrainType.Scorched;
                    }
                }
            }

            _grid.UpdateBordersInRect(m.TargetGridX - radius - 2, m.TargetGridY - radius - 2, m.TargetGridX + radius + 2, m.TargetGridY + radius + 2);
            _territoryManager.RecalculateStatsAndCenters();
            _mapRenderer.MarkDirty();

            float visualRadius = (radius * 2f) / GameConfig.PixelsPerUnit;
            SpawnBlastVisual(m.TargetWorld, visualRadius, new Color(1f, 0.55f, 0.1f, 0.85f));
        }

        private void SpawnBlastVisual(Vector3 worldPos, float diameter, Color color)
        {
            GameObject blastObj = new GameObject("BlastWave");
            blastObj.transform.SetParent(_structuresRoot, false);
            blastObj.transform.position = new Vector3(worldPos.x, worldPos.y, -0.8f);
            blastObj.transform.localScale = Vector3.one * diameter;

            SpriteRenderer sr = blastObj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteFactory.GetCircleSprite(false);
            sr.color = color;
            sr.sortingOrder = 35;

            Destroy(blastObj, 0.65f);
        }

        private void UpdateWarships(float dt)
        {
            for (int i = _warships.Count - 1; i >= 0; i--)
            {
                WarshipUnit ship = _warships[i];
                ship.MoveTimer += dt;
                ship.FireCooldown -= dt;

                if (ship.MoveTimer >= 0.45f)
                {
                    ship.MoveTimer = 0f;
                    // Patrol random neighboring water cell
                    int rx = ship.GridX + UnityEngine.Random.Range(-1, 2);
                    int ry = ship.GridY + UnityEngine.Random.Range(-1, 2);
                    if (_grid.InBounds(rx, ry) && _grid.IsWater(_grid.GetIndex(rx, ry)))
                    {
                        ship.GridX = rx;
                        ship.GridY = ry;
                        if (ship.Visual != null)
                        {
                            Vector3 wp = _mapRenderer.GridToWorld(rx, ry);
                            ship.Visual.position = new Vector3(wp.x, wp.y, -0.6f);
                        }
                    }
                }

                // Bombard enemy coastal cells within 10 pixels
                if (ship.FireCooldown <= 0f)
                {
                    ship.FireCooldown = 1.2f;
                    for (int dy = -8; dy <= 8; dy += 2)
                    {
                        bool fired = false;
                        for (int dx = -8; dx <= 8; dx += 2)
                        {
                            int tx = ship.GridX + dx;
                            int ty = ship.GridY + dy;
                            if (!_grid.InBounds(tx, ty)) continue;
                            int idx = _grid.GetIndex(tx, ty);
                            ushort cellOwner = _grid.OwnerId[idx];
                            if (cellOwner != 0 && cellOwner != ship.OwnerId)
                            {
                                _grid.OwnerId[idx] = 0;
                                _grid.UpdateBordersInRect(tx - 1, ty - 1, tx + 1, ty + 1);
                                _mapRenderer.MarkDirty();
                                fired = true;
                                break;
                            }
                        }
                        if (fired) break;
                    }
                }
            }
        }

        private bool IsWithinCoastDistance(int gx, int gy, int radius)
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

        private bool FindNearestWaterCell(int gx, int gy, int maxRadius, out int wx, out int wy)
        {
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int x = gx + dx;
                        int y = gy + dy;
                        if (_grid.InBounds(x, y) && _grid.IsWater(_grid.GetIndex(x, y)))
                        {
                            wx = x;
                            wy = y;
                            return true;
                        }
                    }
                }
            }
            wx = gx;
            wy = gy;
            return false;
        }

        private static void IncrementStructureCount(NationData n, StructureType t)
        {
            switch (t)
            {
                case StructureType.City: n.CityCount++; break;
                case StructureType.Port: n.PortCount++; break;
                case StructureType.Factory: n.FactoryCount++; break;
                case StructureType.DefensePost: n.DefensePostCount++; break;
                case StructureType.SAMLauncher: n.SamCount++; break;
                case StructureType.MissileSilo: n.SiloCount++; break;
            }
        }

        private static void DecrementStructureCount(NationData n, StructureType t)
        {
            switch (t)
            {
                case StructureType.City: n.CityCount = Mathf.Max(0, n.CityCount - 1); break;
                case StructureType.Port: n.PortCount = Mathf.Max(0, n.PortCount - 1); break;
                case StructureType.Factory: n.FactoryCount = Mathf.Max(0, n.FactoryCount - 1); break;
                case StructureType.DefensePost: n.DefensePostCount = Mathf.Max(0, n.DefensePostCount - 1); break;
                case StructureType.SAMLauncher: n.SamCount = Mathf.Max(0, n.SamCount - 1); break;
                case StructureType.MissileSilo: n.SiloCount = Mathf.Max(0, n.SiloCount - 1); break;
            }
        }
    }
}
