using System.Collections.Generic;
using UnityEngine;

namespace MeWorld.Nations
{
    public class NationData
    {
        public ushort Id;
        public string Name;
        public bool IsPlayer;
        public bool IsTribe;
        public bool IsAlive;

        public Color32 FillColor;
        public Color32 BorderColor;

        public double Troops;
        public double MaxTroops;
        public double TroopGrowthPerSec;

        public double Gold;
        public double GoldGrowthPerSec;

        public int TileCount;
        public float WorldControlPercent;

        public Vector2 CenterGridPos;
        public int CapitalX;
        public int CapitalY;

        public readonly HashSet<ushort> Allies = new HashSet<ushort>();

        public int CityCount;
        public int PortCount;
        public int FactoryCount;
        public int DefensePostCount;
        public int SiloCount;
        public int SamCount;

        public bool IsAlliedWith(ushort otherId)
        {
            return Allies.Contains(otherId);
        }
    }

    public class AttackWave
    {
        public ushort AttackerId;
        public ushort TargetOwnerId;
        public double RemainingTroops;
        public int SeedGridX;
        public int SeedGridY;
        public bool IsNavalInvasion;

        public readonly List<int> ActiveFrontier = new List<int>(128);
        public readonly HashSet<int> InFrontier = new HashSet<int>();
    }

    public static class NationNamePool
    {
        // Priority names visible in the RoWorld reference screenshot
        public static readonly string[] FeaturedNationNames =
        {
            "India", "Oman", "Fernhold", "Tideborn", "Mooncrest",
            "Flintcarvers", "Frostwardens", "Yak Tribe", "Mammoth Moot", "Bramblefire",
            "Schreckseers", "Pelican Folk", "The Pinebridlers", "Unrivaled Company",
            "Persia", "Anatolia", "Bengal", "Siam", "Ceylon", "Aksum",
            "Bactria", "Sogdia", "Khwarezm", "Parthia", "Medes", "Assyria",
            "Babylon", "Carthage", "Numidia", "Lusitania", "Caledonia", "Hibernia",
            "Noricum", "Dacia", "Thrace", "Macedon", "Epirus", "Illyria",
            "Sarmatia", "Scythia", "Cimmeria", "Colchis", "Iberia", "Pontus",
            "Galatia", "Cappadocia", "Cilicia", "Phoenicia", "Nabataea", "Sabaea",
            "Himyar", "Makran", "Gedrosia", "Drangiana", "Arachosia", "Gandhara",
            "Kamboja", "Kosala", "Magadha", "Vanga", "Kalinga", "Andhra",
            "Chola", "Pandya", "Chera", "Kamarupa", "Pagan", "Mon",
            "Khmer", "Champa", "Srivijaya", "Majapahit", "Sunda", "Mataram",
            "Goryeo", "Silla", "Baekje", "Yamato", "Emishi", "Ryukyu",
            "Xiongnu", "Xianbei", "Rouran", "Gokturk", "Uyghur", "Kyrgyz",
            "Khitan", "Jurchen", "Tangut"
        };

        public static readonly string[] TribeNames =
        {
            "Ironfoot Clan", "Saltwind Folk", "Redcliff Tribe", "Ashwood Kin", "Stonetusk Moot",
            "Stormpeak Herd", "Duskvale Band", "Sunscale Riders", "Cinderhorn", "Mistwater Folk",
            "Thornback Clan", "Cragwalker", "Bogstriders", "Sandvipers", "Duneweavers",
            "Amberfang", "Timberwolf Pact", "Whitehorn Tribe", "Ravenwing", "Hollowbone",
            "Coppervein", "Greenmoss Folk", "Driftwood Kin", "Kestrel Clan", "Boarhead",
            "Siltmarsh", "Oakhaven", "pebbleshoal", "Galewatch", "Bitterroot",
            "Crowfeather", "Silverbrook", "Mudflat Herd", "Deepwell Folk", "Ridgeback",
            "Highgrass Clan", "Coldspring", "Firebrand", "Sunkenreef", "Wildbriar",
            "Dustdevil", "Flintlock", "Blackwater", "Serpentcoil", "Owlcrest",
            "Mossbeard", "Swiftfoot", "Graveltooth", "Cloudberry", "Starfall Tribe",
            "Longspear", "Redfeather", "Brokenhorn", "Palecliff", "Eaglerock",
            "Silentpine", "Drycreek", "Clearwater", "Rustedblade", "Goldpan Folk",
            "Bramblehorn", "Wolfblood", "Tallgrass", "Weepingwillow", "Saltybay",
            "Sharpstone", "Warmhearth", "Darkhollow", "Brightmeadow", "Ironside",
            "Smokestack", "Frostbite", "Heatwave", "Tidewalker", "Cliffhanger",
            "Cavebear", "Hilltop", "Riverbend", "Lakeview", "Seashore",
            "Timberline", "Snowcap", "Magmaflow", "Crystalcave", "Shadowfen",
            "Sunburst", "Moonbeam", "Windchime", "Thunderclap", "Lightningrod",
            "Raindrop", "Hailstone", "Fogbank", "Dewdrop", "Hoarfrost",
            "Permafrost", "Quicksand", "Mudslide", "Avalanche", "Whirlpool",
            "Riptide", "Undertow", "Waterspout", "Maelstrom", "Monsoon",
            "Typhoon", "Cyclone", "Sirocco", "Tradewind", "Zephyr"
        };

        public static void GetNationColors(ushort id, bool isPlayer, bool isTribe, string name, out Color32 fill, out Color32 border)
        {
            if (isPlayer)
            {
                // Vibrant Pink/Magenta matching SnickTrix in screenshot
                fill = new Color32(236, 135, 192, 255);
                border = new Color32(214, 34, 118, 255);
                return;
            }

            if (name == "India")
            {
                fill = new Color32(115, 168, 212, 255);
                border = new Color32(32, 92, 150, 255);
                return;
            }

            if (name == "Oman")
            {
                fill = new Color32(235, 165, 110, 255);
                border = new Color32(194, 98, 25, 255);
                return;
            }

            if (isTribe)
            {
                // Soft neutral parchment tones with subtle borders for tribes so player & major empires stand out
                fill = new Color32(232, 238, 222, 255);
                border = new Color32(182, 194, 178, 255);
                return;
            }

            // Major AI Nations: Distinct rich strategy colors
            float h = ((id * 0.381966f + 0.13f) % 1.0f);
            Color mFill = Color.HSVToRGB(h, 0.46f, 0.88f);
            Color mBorder = Color.HSVToRGB(h, 0.78f, 0.60f);
            fill = mFill;
            border = mBorder;
        }
    }
}
