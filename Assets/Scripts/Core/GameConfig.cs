using System;
using System.Globalization;
using UnityEngine;

namespace MeWorld.Core
{
    public enum TerrainType : byte
    {
        DeepOcean = 0,
        ShallowWater = 1,
        Plains = 2,
        Highlands = 3,
        Mountains = 4,
        Scorched = 5
    }

    public enum StructureType : byte
    {
        None = 0,
        City = 1,
        Port = 2,
        Factory = 3,
        DefensePost = 4,
        SAMLauncher = 5,
        MissileSilo = 6,
        AtomBomb = 7,
        HydrogenBomb = 8,
        Warship = 9,
        Alliance = 10
    }

    public static class GameConfig
    {
        public const int MapWidth = 560;
        public const int MapHeight = 320;
        public const float PixelsPerUnit = 10f;

        public const float TickInterval = 0.1f; // 10 ticks per second
        public const float WinControlPercentage = 0.80f; // 80% Goal
        public const float InitialProtectionSeconds = 157f; // 2:37 Protected timer

        public const int PlayerNationCount = 88; // 1 Player + 87 AI Nations
        public const int TribeCount = 110;       // 110 Tribes

        public static readonly float[] AttackPresets = { 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 1.00f };

        public static double GetActionCost(StructureType type)
        {
            switch (type)
            {
                case StructureType.City: return 250000;
                case StructureType.Port: return 125000;
                case StructureType.Factory: return 125000;
                case StructureType.DefensePost: return 50000;
                case StructureType.SAMLauncher: return 1500000;
                case StructureType.MissileSilo: return 1000000;
                case StructureType.AtomBomb: return 750000;
                case StructureType.HydrogenBomb: return 5000000;
                case StructureType.Warship: return 250000;
                case StructureType.Alliance: return 0;
                default: return 0;
            }
        }

        public static string GetActionCostLabel(StructureType type)
        {
            if (type == StructureType.Alliance) return "ALLY";
            double cost = GetActionCost(type);
            if (cost >= 1000000)
            {
                double m = cost / 1000000.0;
                return m.ToString("0.#", CultureInfo.InvariantCulture) + "M";
            }
            if (cost >= 1000)
            {
                double k = cost / 1000.0;
                return k.ToString("0.#", CultureInfo.InvariantCulture) + "K";
            }
            return cost.ToString("0", CultureInfo.InvariantCulture);
        }

        public static string GetActionDisplayName(StructureType type)
        {
            switch (type)
            {
                case StructureType.City: return "City (+Max Troops & Income)";
                case StructureType.Port: return "Port (Naval Trade & Invasion)";
                case StructureType.Factory: return "Factory (+950 Gold/s)";
                case StructureType.DefensePost: return "Defense Post (2.5x Border Defense)";
                case StructureType.SAMLauncher: return "SAM Launcher (Intercepts Nukes)";
                case StructureType.MissileSilo: return "Missile Silo (Launches Nukes)";
                case StructureType.AtomBomb: return "Atom Bomb (Tactical Nuclear Strike)";
                case StructureType.HydrogenBomb: return "Hydrogen Bomb (Massive Devastation)";
                case StructureType.Warship: return "Warship (Coastal Bombardment)";
                case StructureType.Alliance: return "Forge Alliance (Click Neighbor Nation)";
                default: return string.Empty;
            }
        }

        public static string FormatNumber(double val)
        {
            double abs = Math.Abs(val);
            string sign = val < 0 ? "-" : "";
            if (abs >= 1000000000)
                return sign + (abs / 1000000000.0).ToString("0.00", CultureInfo.InvariantCulture) + "B";
            if (abs >= 10000000)
                return sign + (abs / 1000000.0).ToString("0.0", CultureInfo.InvariantCulture) + "M";
            if (abs >= 1000000)
                return sign + (abs / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture) + "M";
            if (abs >= 100000)
                return sign + (abs / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "K";
            if (abs >= 10000)
                return sign + (abs / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "K";
            if (abs >= 1000)
                return sign + (abs / 1000.0).ToString("0.00", CultureInfo.InvariantCulture) + "K";
            return sign + Math.Round(abs).ToString("0", CultureInfo.InvariantCulture);
        }

        public static string FormatTime(float seconds)
        {
            int totalSec = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int mins = totalSec / 60;
            int secs = totalSec % 60;
            return $"{mins}:{secs:00}";
        }
    }
}
