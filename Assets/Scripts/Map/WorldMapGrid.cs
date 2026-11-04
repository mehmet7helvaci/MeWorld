using System;
using MeWorld.Core;
using UnityEngine;

namespace MeWorld.Map
{
    public class WorldMapGrid
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int TotalCells;

        public readonly TerrainType[] Terrain;
        public readonly float[] Elevation;
        public readonly ushort[] OwnerId;
        public readonly bool[] IsBorder;
        public readonly bool[] IsCoast;

        public int TotalLandCells { get; private set; }

        public WorldMapGrid(int width, int height)
        {
            Width = width;
            Height = height;
            TotalCells = width * height;

            Terrain = new TerrainType[TotalCells];
            Elevation = new float[TotalCells];
            OwnerId = new ushort[TotalCells];
            IsBorder = new bool[TotalCells];
            IsCoast = new bool[TotalCells];
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public int GetIndex(int x, int y)
        {
            return y * Width + x;
        }

        public void GetXY(int index, out int x, out int y)
        {
            y = index / Width;
            x = index - (y * Width);
        }

        public bool IsLand(int index)
        {
            TerrainType t = Terrain[index];
            return t == TerrainType.Plains || t == TerrainType.Highlands || t == TerrainType.Mountains || t == TerrainType.Scorched;
        }

        public bool IsWater(int index)
        {
            TerrainType t = Terrain[index];
            return t == TerrainType.DeepOcean || t == TerrainType.ShallowWater;
        }

        public int GetNeighbors4(int x, int y, int[] buffer)
        {
            int count = 0;
            if (x > 0) buffer[count++] = y * Width + (x - 1);
            if (x < Width - 1) buffer[count++] = y * Width + (x + 1);
            if (y > 0) buffer[count++] = (y - 1) * Width + x;
            if (y < Height - 1) buffer[count++] = (y + 1) * Width + x;
            return count;
        }

        public void GenerateProceduralWorld(int seed)
        {
            TotalLandCells = 0;

            if (seed == 20261103)
            {
                GenerateClassicRoWorldMap();
                return;
            }

            GenerateDynamicProceduralMap(seed);
        }

        private void GenerateDynamicProceduralMap(int seed)
        {
            System.Random rng = new System.Random(seed);

            // 4 to 8 continental cores distributed across the map
            int coreCount = rng.Next(5, 9);
            Vector2[] cores = new Vector2[coreCount];
            float[] coreRadiusX = new float[coreCount];
            float[] coreRadiusY = new float[coreCount];
            float[] coreWeight = new float[coreCount];

            for (int i = 0; i < coreCount; i++)
            {
                cores[i] = new Vector2(
                    (float)(rng.NextDouble() * 0.70 + 0.15),
                    (float)(rng.NextDouble() * 0.66 + 0.17)
                );
                coreRadiusX[i] = (float)(rng.NextDouble() * 0.16 + 0.12);
                coreRadiusY[i] = (float)(rng.NextDouble() * 0.14 + 0.10);
                coreWeight[i] = (float)(rng.NextDouble() * 0.35 + 0.75);
            }

            int seaCount = rng.Next(2, 5);
            Vector2[] seaCenters = new Vector2[seaCount];
            float[] seaRadius = new float[seaCount];
            for (int s = 0; s < seaCount; s++)
            {
                seaCenters[s] = new Vector2(
                    (float)(rng.NextDouble() * 0.60 + 0.20),
                    (float)(rng.NextDouble() * 0.60 + 0.20)
                );
                seaRadius[s] = (float)(rng.NextDouble() * 0.08 + 0.06);
            }

            float ox1 = (float)(rng.NextDouble() * 1000.0);
            float oy1 = (float)(rng.NextDouble() * 1000.0);
            float ox2 = (float)(rng.NextDouble() * 1000.0);
            float oy2 = (float)(rng.NextDouble() * 1000.0);
            float ox3 = (float)(rng.NextDouble() * 1000.0);
            float oy3 = (float)(rng.NextDouble() * 1000.0);
            float oxWarp = (float)(rng.NextDouble() * 1000.0);
            float oyWarp = (float)(rng.NextDouble() * 1000.0);

            float[] rawHeights = new float[TotalCells];
            float maxH = float.MinValue;

            for (int y = 0; y < Height; y++)
            {
                float ny = (float)y / Height;
                for (int x = 0; x < Width; x++)
                {
                    float nx = (float)x / Width;
                    int idx = y * Width + x;

                    // Domain Warping for organic, non-circular coastlines
                    float warpX = (Mathf.PerlinNoise(oxWarp + nx * 3.2f, oyWarp + ny * 3.2f) - 0.5f) * 0.15f;
                    float warpY = (Mathf.PerlinNoise(oxWarp + nx * 3.2f + 30f, oyWarp + ny * 3.2f + 30f) - 0.5f) * 0.15f;
                    float wnx = nx + warpX;
                    float wny = ny + warpY;

                    float maxCoreScore = 0f;
                    for (int i = 0; i < coreCount; i++)
                    {
                        float dx = (wnx - cores[i].x) / coreRadiusX[i];
                        float dy = (wny - cores[i].y) / coreRadiusY[i];
                        float distSq = dx * dx + dy * dy;
                        float score = Mathf.Exp(-distSq * 2.0f) * coreWeight[i];
                        if (score > maxCoreScore) maxCoreScore = score;
                    }

                    float seaCarve = 0f;
                    for (int s = 0; s < seaCount; s++)
                    {
                        float dx = (wnx - seaCenters[s].x) / seaRadius[s];
                        float dy = (wny - seaCenters[s].y) / seaRadius[s];
                        float dSq = dx * dx + dy * dy;
                        seaCarve = Mathf.Max(seaCarve, Mathf.Exp(-dSq * 3.0f) * 0.55f);
                    }

                    float n1 = Mathf.PerlinNoise(ox1 + wnx * 3.6f, oy1 + wny * 3.6f);
                    float n2 = Mathf.PerlinNoise(ox2 + wnx * 8.5f, oy2 + wny * 8.5f) * 0.48f;
                    float n3 = Mathf.PerlinNoise(ox3 + wnx * 19.0f, oy3 + wny * 19.0f) * 0.24f;
                    float fbm = (n1 + n2 + n3) / 1.72f;

                    float edgeX = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(nx, 1f - nx) / 0.055f));
                    float edgeY = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(ny, 1f - ny) / 0.065f));
                    float edgeMask = edgeX * edgeY;

                    float h = (fbm * 0.48f + maxCoreScore * 0.58f - seaCarve) * edgeMask;
                    rawHeights[idx] = h;
                    if (h > maxH) maxH = h;
                }
            }

            int sampleCount = 4000;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int sIdx = rng.Next(0, TotalCells);
                samples[i] = rawHeights[sIdx];
            }
            Array.Sort(samples);
            float waterThreshold = samples[Mathf.RoundToInt(sampleCount * 0.51f)];

            for (int idx = 0; idx < TotalCells; idx++)
            {
                float h = rawHeights[idx];
                OwnerId[idx] = 0;
                IsBorder[idx] = false;

                if (h < waterThreshold - 0.075f)
                {
                    Terrain[idx] = TerrainType.DeepOcean;
                    Elevation[idx] = 0.2f;
                }
                else if (h < waterThreshold)
                {
                    Terrain[idx] = TerrainType.ShallowWater;
                    Elevation[idx] = 0.38f;
                }
                else
                {
                    TotalLandCells++;
                    float landElev = (h - waterThreshold) / Mathf.Max(0.001f, maxH - waterThreshold);
                    Elevation[idx] = Mathf.Clamp01(0.5f + landElev * 0.5f);

                    if (landElev < 0.42f)
                    {
                        Terrain[idx] = TerrainType.Plains;
                    }
                    else if (landElev < 0.74f)
                    {
                        Terrain[idx] = TerrainType.Highlands;
                    }
                    else
                    {
                        Terrain[idx] = TerrainType.Mountains;
                    }
                }
            }

            RecalculateCoasts();
        }

        private void GenerateClassicRoWorldMap()
        {
            System.Random rng = new System.Random(20261103);
            float ox1 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy1 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float ox2 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy2 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float ox3 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy3 = (float)(rng.NextDouble() * 500.0 + 10.0);

            for (int y = 0; y < Height; y++)
            {
                float ny = (float)y / Height;
                for (int x = 0; x < Width; x++)
                {
                    float nx = (float)x / Width;
                    int idx = y * Width + x;

                    float n1 = Mathf.PerlinNoise(ox1 + nx * 4.2f, oy1 + ny * 4.2f);
                    float n2 = Mathf.PerlinNoise(ox2 + nx * 9.5f, oy2 + ny * 9.5f) * 0.45f;
                    float n3 = Mathf.PerlinNoise(ox3 + nx * 21.0f, oy3 + ny * 21.0f) * 0.22f;
                    float fbm = (n1 + n2 + n3) / 1.67f;

                    float northBelt = Mathf.Clamp01((ny - 0.48f) * 2.4f);
                    float penWidth = Mathf.Lerp(0.025f, 0.14f, Mathf.Clamp01((ny - 0.16f) / 0.44f));
                    float dxPen = Mathf.Abs(nx - 0.41f) / Mathf.Max(0.02f, penWidth);
                    float dyPen = (ny >= 0.16f && ny <= 0.68f) ? 1.0f : 0.0f;
                    float centralPeninsula = dyPen * Mathf.Clamp01(1.15f - dxPen);

                    float dxOman = (nx - 0.21f) / 0.09f;
                    float dyOman = (ny - 0.54f) / 0.14f;
                    float omanPeninsula = Mathf.Clamp01(1.12f - Mathf.Sqrt(dxOman * dxOman + dyOman * dyOman));

                    float dxEast = (nx - 0.65f) / 0.11f;
                    float dyEast = (ny - 0.46f) / 0.24f;
                    float eastPeninsula = Mathf.Clamp01(1.10f - Mathf.Sqrt(dxEast * dxEast + dyEast * dyEast));

                    float arabianSea = Mathf.Exp(-((nx - 0.30f) * (nx - 0.30f) / 0.0025f + (ny - 0.42f) * (ny - 0.42f) / 0.032f)) * 0.72f;
                    float persianGulf = Mathf.Exp(-((nx - 0.26f) * (nx - 0.26f) / 0.0018f + (ny - 0.60f) * (ny - 0.60f) / 0.005f)) * 0.62f;
                    float bayOfBengal = Mathf.Exp(-((nx - 0.51f) * (nx - 0.51f) / 0.0038f + (ny - 0.33f) * (ny - 0.33f) / 0.030f)) * 0.75f;

                    float continentMask = Mathf.Max(
                        northBelt * 0.86f,
                        Mathf.Max(centralPeninsula * 0.92f, Mathf.Max(omanPeninsula * 0.90f, eastPeninsula * 0.88f))
                    );

                    if (ny < 0.06f) continentMask *= ny / 0.06f;

                    float h = fbm * 0.46f + continentMask * 0.68f - arabianSea - persianGulf - bayOfBengal;
                    Elevation[idx] = Mathf.Clamp01(h);
                    OwnerId[idx] = 0;
                    IsBorder[idx] = false;

                    if (h < 0.42f)
                    {
                        Terrain[idx] = TerrainType.DeepOcean;
                    }
                    else if (h < 0.495f)
                    {
                        Terrain[idx] = TerrainType.ShallowWater;
                    }
                    else if (h < 0.67f)
                    {
                        Terrain[idx] = TerrainType.Plains;
                        TotalLandCells++;
                    }
                    else if (h < 0.79f)
                    {
                        Terrain[idx] = TerrainType.Highlands;
                        TotalLandCells++;
                    }
                    else
                    {
                        Terrain[idx] = TerrainType.Mountains;
                        TotalLandCells++;
                    }
                }
            }

            RecalculateCoasts();
        }

        public void RecalculateCoasts()
        {
            int[] nBuf = new int[4];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int idx = y * Width + x;
                    if (!IsLand(idx))
                    {
                        IsCoast[idx] = false;
                        continue;
                    }

                    bool coast = false;
                    int nCount = GetNeighbors4(x, y, nBuf);
                    for (int i = 0; i < nCount; i++)
                    {
                        if (IsWater(nBuf[i]))
                        {
                            coast = true;
                            break;
                        }
                    }
                    IsCoast[idx] = coast;
                }
            }
        }

        public void UpdateBordersInRect(int minX, int minY, int maxX, int maxY)
        {
            minX = Mathf.Clamp(minX - 1, 0, Width - 1);
            minY = Mathf.Clamp(minY - 1, 0, Height - 1);
            maxX = Mathf.Clamp(maxX + 1, 0, Width - 1);
            maxY = Mathf.Clamp(maxY + 1, 0, Height - 1);

            for (int y = minY; y <= maxY; y++)
            {
                int rowOffset = y * Width;
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = rowOffset + x;
                    ushort owner = OwnerId[idx];
                    if (owner == 0)
                    {
                        IsBorder[idx] = false;
                        continue;
                    }

                    bool border = false;
                    if (x > 0 && OwnerId[idx - 1] != owner) border = true;
                    else if (x < Width - 1 && OwnerId[idx + 1] != owner) border = true;
                    else if (y > 0 && OwnerId[idx - Width] != owner) border = true;
                    else if (y < Height - 1 && OwnerId[idx + Width] != owner) border = true;

                    IsBorder[idx] = border;
                }
            }
        }

        public void UpdateAllBorders()
        {
            UpdateBordersInRect(0, 0, Width - 1, Height - 1);
        }
    }
}
