using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.Structures;
using UnityEngine;

namespace MeWorld.Nations
{
    public class AIController : MonoBehaviour
    {
        private WorldMapGrid _grid;
        private TerritoryAndCombatManager _territoryManager;
        private StructureAndNukeManager _structureManager;

        private int _rollingIndex = 0;
        private readonly int[] _neighborBuffer = new int[4];
        private readonly HashSet<ushort> _neighborOwners = new HashSet<ushort>();

        public void Initialize(WorldMapGrid grid, TerritoryAndCombatManager territoryManager, StructureAndNukeManager structureManager)
        {
            _grid = grid;
            _territoryManager = territoryManager;
            _structureManager = structureManager;
            _rollingIndex = 0;
        }

        public void TickAI()
        {
            IReadOnlyList<NationData> list = _territoryManager.NationList;
            if (list.Count == 0) return;

            GameDifficulty diff = KeybindManager.Instance.CurrentDifficulty;
            int batchSize = 12;
            switch (diff)
            {
                case GameDifficulty.Easy: batchSize = 8; break;
                case GameDifficulty.Hard: batchSize = 18; break;
                case GameDifficulty.Chaos: batchSize = 26; break;
            }

            for (int b = 0; b < batchSize; b++)
            {
                _rollingIndex = (_rollingIndex + 1) % list.Count;
                NationData ai = list[_rollingIndex];
                if (ai.IsPlayer || !ai.IsAlive) continue;

                EvaluateAI(ai, diff);
            }
        }

        private void EvaluateAI(NationData ai, GameDifficulty diff)
        {
            double fillRatio = ai.MaxTroops > 0 ? (ai.Troops / ai.MaxTroops) : 0.0;

            // Tribes expand very rarely and never build high-tech structures
            if (ai.IsTribe)
            {
                float tribeExpandThreshold = diff == GameDifficulty.Chaos ? 0.60f : 0.78f;
                float tribeChance = diff == GameDifficulty.Chaos ? 0.35f : 0.18f;
                if (fillRatio > tribeExpandThreshold && UnityEngine.Random.value < tribeChance)
                {
                    if (FindNeighborTarget(ai, out ushort targetId, out int gx, out int gy, true))
                    {
                        _territoryManager.StartAttack(ai.Id, targetId, 0.20f, gx, gy);
                    }
                }
                return;
            }

            // Major AI Nations (87 AI Competitors)
            // 1. Try building structures when Gold allows
            TryAIBuildStructures(ai);

            // 2. Expand into neutral territory or weak neighbors when troop reserve is healthy
            double expandThreshold = 0.36;
            switch (diff)
            {
                case GameDifficulty.Easy: expandThreshold = 0.48; break;
                case GameDifficulty.Hard: expandThreshold = 0.28; break;
                case GameDifficulty.Chaos: expandThreshold = 0.20; break;
            }

            if (fillRatio >= expandThreshold)
            {
                if (FindNeighborTarget(ai, out ushort targetId, out int gx, out int gy, false))
                {
                    float attackRatio = targetId == 0 ? 0.25f : 0.35f;
                    _territoryManager.StartAttack(ai.Id, targetId, attackRatio, gx, gy);
                }
            }
        }

        private void TryAIBuildStructures(NationData ai)
        {
            int cx = Mathf.Clamp(Mathf.RoundToInt(ai.CenterGridPos.x), 2, _grid.Width - 3);
            int cy = Mathf.Clamp(Mathf.RoundToInt(ai.CenterGridPos.y), 2, _grid.Height - 3);

            if (_grid.OwnerId[_grid.GetIndex(cx, cy)] != ai.Id)
            {
                cx = ai.CapitalX;
                cy = ai.CapitalY;
                if (!_grid.InBounds(cx, cy) || _grid.OwnerId[_grid.GetIndex(cx, cy)] != ai.Id)
                    return;
            }

            if (ai.Gold >= 55000 && ai.DefensePostCount < 1 && UnityEngine.Random.value < 0.25f)
            {
                _structureManager.TryBuildOrExecuteAction(ai.Id, StructureType.DefensePost, cx, cy, out _);
                return;
            }

            if (ai.Gold >= 135000 && ai.FactoryCount < 3 && UnityEngine.Random.value < 0.35f)
            {
                _structureManager.TryBuildOrExecuteAction(ai.Id, StructureType.Factory, cx, cy, out _);
                return;
            }

            if (ai.Gold >= 260000 && ai.CityCount < 4 && UnityEngine.Random.value < 0.40f)
            {
                _structureManager.TryBuildOrExecuteAction(ai.Id, StructureType.City, cx, cy, out _);
                return;
            }

            if (ai.Gold >= 1100000 && ai.SiloCount < 1 && UnityEngine.Random.value < 0.25f)
            {
                _structureManager.TryBuildOrExecuteAction(ai.Id, StructureType.MissileSilo, cx, cy, out _);
                return;
            }

            // Occasional AI nuclear strike against strongest rival (never hits player while Protected!)
            if (ai.SiloCount > 0 && ai.Gold >= 850000 && UnityEngine.Random.value < 0.08f)
            {
                NationData lead = _territoryManager.LeadingNation;
                if (lead != null && lead.Id != ai.Id && !(lead.IsPlayer && _territoryManager.ProtectionTimer > 0f))
                {
                    int tx = Mathf.RoundToInt(lead.CenterGridPos.x);
                    int ty = Mathf.RoundToInt(lead.CenterGridPos.y);
                    _structureManager.TryBuildOrExecuteAction(ai.Id, StructureType.AtomBomb, tx, ty, out _);
                }
            }
        }

        private bool FindNeighborTarget(NationData ai, out ushort chosenTargetId, out int sampleX, out int sampleY, bool neutralOnly)
        {
            chosenTargetId = 0;
            sampleX = ai.CapitalX;
            sampleY = ai.CapitalY;

            _neighborOwners.Clear();
            int cx = Mathf.RoundToInt(ai.CenterGridPos.x);
            int cy = Mathf.RoundToInt(ai.CenterGridPos.y);
            int scanRadius = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(ai.TileCount)) + 14, 14, 65);

            int minX = Mathf.Max(1, cx - scanRadius);
            int maxX = Mathf.Min(_grid.Width - 2, cx + scanRadius);
            int minY = Mathf.Max(1, cy - scanRadius);
            int maxY = Mathf.Min(_grid.Height - 2, cy + scanRadius);

            bool foundNeutral = false;
            int neutralX = 0, neutralY = 0;

            ushort weakestEnemyId = 0;
            double weakestTroops = double.MaxValue;
            int enemyX = 0, enemyY = 0;

            for (int y = minY; y <= maxY; y += 2)
            {
                int row = y * _grid.Width;
                for (int x = minX; x <= maxX; x += 2)
                {
                    int idx = row + x;
                    if (_grid.OwnerId[idx] != ai.Id || !_grid.IsBorder[idx]) continue;

                    int nCount = _grid.GetNeighbors4(x, y, _neighborBuffer);
                    for (int n = 0; n < nCount; n++)
                    {
                        int nIdx = _neighborBuffer[n];
                        if (!_grid.IsLand(nIdx)) continue;

                        ushort other = _grid.OwnerId[nIdx];
                        if (other == ai.Id) continue;

                        if (other == 0)
                        {
                            foundNeutral = true;
                            _grid.GetXY(nIdx, out neutralX, out neutralY);
                            break;
                        }

                        if (!neutralOnly && _territoryManager.Nations.TryGetValue(other, out NationData otherNation) && otherNation.IsAlive)
                        {
                            if (ai.IsAlliedWith(other)) continue;
                            if (otherNation.IsPlayer && _territoryManager.ProtectionTimer > 0f) continue;

                            if (otherNation.Troops < ai.Troops * 0.88 && otherNation.Troops < weakestTroops)
                            {
                                weakestTroops = otherNation.Troops;
                                weakestEnemyId = other;
                                _grid.GetXY(nIdx, out enemyX, out enemyY);
                            }
                        }
                    }
                    if (foundNeutral) break;
                }
                if (foundNeutral) break;
            }

            if (foundNeutral)
            {
                chosenTargetId = 0;
                sampleX = neutralX;
                sampleY = neutralY;
                return true;
            }

            if (weakestEnemyId != 0)
            {
                chosenTargetId = weakestEnemyId;
                sampleX = enemyX;
                sampleY = enemyY;
                return true;
            }

            return false;
        }
    }
}
