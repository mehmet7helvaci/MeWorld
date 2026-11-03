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
            System.Random rng = new System.Random(seed);
            float ox1 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy1 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float ox2 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy2 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float ox3 = (float)(rng.NextDouble() * 500.0 + 10.0);
            float oy3 = (float)(rng.NextDouble() * 500.0 + 10.0);

            TotalLandCells = 0;

            // Regional continental anchors inspired by the Indian Ocean / Eurasia / Peninsula layout in RoWorld screenshot
            for (int y = 0; y < Height; y++)
            {
                float ny = (float)y / Height; // 0 bottom, 1 top
                for (int x = 0; x < Width; x++)
                {
                    float nx = (float)x / Width; // 0 left, 1 right
                    int idx = y * Width + x;

                    // Multi-octave Perlin noise (fBm)
                    float n1 = Mathf.PerlinNoise(ox1 + nx * 4.2f, oy1 + ny * 4.2f);
                    float n2 = Mathf.PerlinNoise(ox2 + nx * 9.5f, oy2 + ny * 9.5f) * 0.45f;
                    float n3 = Mathf.PerlinNoise(ox3 + nx * 21.0f, oy3 + ny * 21.0f) * 0.22f;
                    float fbm = (n1 + n2 + n3) / 1.67f;

                    // Continental landmass mask matching RoWorld screenshot geography:
                    // 1) Broad Northern Eurasian belt (ny > 0.52)
                    float northBelt = Mathf.Clamp01((ny - 0.48f) * 2.4f);

                    // 2) Central Indian triangular peninsula tapering down to (nx=0.41, ny=0.16)
                    float penWidth = Mathf.Lerp(0.025f, 0.14f, Mathf.Clamp01((ny - 0.16f) / 0.44f));
                    float dxPen = Mathf.Abs(nx - 0.41f) / Mathf.Max(0.02f, penWidth);
                    float dyPen = (ny >= 0.16f && ny <= 0.68f) ? 1.0f : 0.0f;
                    float centralPeninsula = dyPen * Mathf.Clamp01(1.15f - dxPen);

                    // 3) Western Oman/Arabian peninsula on the left (nx: 0.16..0.28, ny: 0.44..0.66)
                    float dxOman = (nx - 0.21f) / 0.09f;
                    float dyOman = (ny - 0.54f) / 0.14f;
                    float omanPeninsula = Mathf.Clamp01(1.12f - Mathf.Sqrt(dxOman * dxOman + dyOman * dyOman));

                    // 4) Eastern Indochina/Malay peninsula on the right (nx: 0.58..0.76, ny: 0.25..0.68)
                    float dxEast = (nx - 0.65f) / 0.11f;
                    float dyEast = (ny - 0.46f) / 0.24f;
                    float eastPeninsula = Mathf.Clamp01(1.10f - Mathf.Sqrt(dxEast * dxEast + dyEast * dyEast));

                    // Carve Arabian Sea (between Oman and India) and Bay of Bengal (between India and East)
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
