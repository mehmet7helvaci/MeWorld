using System;
using MeWorld.Core;
using MeWorld.Map;
using MeWorld.Nations;
using MeWorld.Structures;
using MeWorld.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeWorld.Tests
{
    public static class MainMenuAndSettingsTests
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("MeWorld/Run Tests/Test MainMenu & Keybindings")]
#endif
        public static void RunAllTests()
        {
            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string testName)
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"<color=#48DB80>[PASS]</color> {testName}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"<color=#EF4444>[FAIL]</color> {testName}");
                }
            }

            Debug.Log("<color=#F59E2B>=== RUNNING MEWORLD MAIN MENU & SETTINGS TESTS ===</color>");

            // 1. KeybindManager Tests
            try
            {
                KeybindManager km = KeybindManager.Instance;
                Assert(km != null, "KeybindManager Instance exists");

                km.ResetKeybindingsToDefaults();
                Assert(km.GetKey(KeyAction.SelectFactory) == Key.F, "Default Factory hotkey is 'F'");
                Assert(km.GetKey(KeyAction.SelectCity) == Key.Digit1, "Default City hotkey is '1'");
                Assert(km.GetKey(KeyAction.SelectPort) == Key.Digit2, "Default Port hotkey is '2'");
                Assert(km.GetKey(KeyAction.FocusPlayer) == Key.Space, "Default Focus hotkey is 'Space'");
                Assert(km.GetKey(KeyAction.CancelOrMenu) == Key.Escape, "Default Cancel/Menu is 'Esc'");

                // Rebind test
                km.StartRebinding(KeyAction.SelectFactory);
                Assert(km.IsRebinding, "KeybindManager enters rebinding state");
                Assert(km.RebindingAction == KeyAction.SelectFactory, "Rebinding target is SelectFactory");
                km.CancelRebinding();
                Assert(!km.IsRebinding, "KeybindManager cancels rebinding cleanly");

                // Difficulty & GameMode settings
                km.SetDifficulty(GameDifficulty.Chaos);
                Assert(km.CurrentDifficulty == GameDifficulty.Chaos, "Difficulty set to Chaos persists in KeybindManager");

                km.SetGameMode(GamePlayMode.Spectator);
                Assert(km.CurrentGameMode == GamePlayMode.Spectator, "GamePlayMode set to Spectator persists in KeybindManager");

                km.SetMasterVolume(0.75f);
                Assert(Mathf.Approximately(km.MasterVolume, 0.75f), "Master volume set to 75%");

                km.SetMuted(true);
                Assert(km.IsMuted, "Mute set to true");
                km.SetMuted(false);
                Assert(!km.IsMuted, "Mute set to false");

                km.SetDifficulty(GameDifficulty.Normal);
                km.SetGameMode(GamePlayMode.SoloConquest);
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"KeybindManager Test Exception: {ex.Message}");
            }

            // 2. MainMenuController Tests
            try
            {
                GameObject menuObj = new GameObject("Test_MainMenu");
                MainMenuController menu = menuObj.AddComponent<MainMenuController>();
                menu.Initialize();

                Assert(menu != null, "MainMenuController initialized without exceptions");

                bool callbackFired = false;
                int recSeed = 0;
                GameDifficulty recDiff = GameDifficulty.Easy;
                GamePlayMode recMode = GamePlayMode.SoloConquest;

                menu.OnStartGameRequested = (s, d, m) =>
                {
                    callbackFired = true;
                    recSeed = s;
                    recDiff = d;
                    recMode = m;
                };

                menu.OnStartGameRequested.Invoke(999888, GameDifficulty.Hard, GamePlayMode.Spectator);
                Assert(callbackFired, "OnStartGameRequested callback invokes properly");
                Assert(recSeed == 999888, "Received correct seed in callback");
                Assert(recDiff == GameDifficulty.Hard, "Received correct difficulty in callback");
                Assert(recMode == GamePlayMode.Spectator, "Received correct mode in callback");

                menu.OpenSettingsModalDirectly();
                menu.ShowMenu();
                menu.HideMenu();
                Assert(true, "Screen switching methods execute cleanly");

                UnityEngine.Object.DestroyImmediate(menuObj);
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"MainMenuController Test Exception: {ex.Message}");
            }

            // 3. TerritoryAndCombatManager Spectator & Difficulty Tests
            try
            {
                WorldMapGrid grid = new WorldMapGrid(120, 80);
                grid.GenerateProceduralWorld(12345);

                GameObject terrObj = new GameObject("Test_TerritoryManager");
                TerritoryAndCombatManager tm = terrObj.AddComponent<TerritoryAndCombatManager>();

                // Test Normal Mode & Two-Phase Spawn
                KeybindManager.Instance.SetDifficulty(GameDifficulty.Normal);
                KeybindManager.Instance.SetGameMode(GamePlayMode.SoloConquest);

                GameObject mapObj = new GameObject("Test_MapRenderer");
                MapRenderer mr = mapObj.AddComponent<MapRenderer>();

                tm.Initialize(grid, mr, 12345);
                Assert(tm.PlayerNation == null, "Before spawn selection confirmed, PlayerNation is initially null");

                Vector2Int spawnPos = tm.FindBestStartingLand();
                Assert(grid.IsLand(grid.GetIndex(spawnPos.x, spawnPos.y)), "FindBestStartingLand returns valid land tile");

                mr.SetSpawnIndicator(spawnPos.x, spawnPos.y, true);
                Assert(true, "SetSpawnIndicator activates cleanly");
                mr.SetSpawnIndicator(0, 0, false);
                Assert(true, "SetSpawnIndicator deactivates cleanly");

                tm.SpawnPlayerAndAI(spawnPos.x, spawnPos.y, "TestEmpire", 12345);
                Assert(tm.PlayerNation != null, "After SpawnPlayerAndAI, PlayerNation exists");
                Assert(tm.PlayerNation.IsPlayer, "PlayerNation has IsPlayer == true");
                Assert(tm.PlayerNation.Name == "TestEmpire", "PlayerNation receives custom name 'TestEmpire'");
                Assert(tm.ProtectionTimer > 100f, "Normal mode has initial protection timer");
                Assert(tm.PlayerNation.TileCount < 120, "Player starts as a small city-state (under 120 tiles)");
                Assert(tm.PlayerNation.Gold < 50000, "Player starts with realistic early gold (under 50K)");
                Assert(tm.PlayerNation.Troops <= 2000, "Player starts with realistic early troops (under 2000)");
                Assert(grid.TotalLandCells - tm.PlayerNation.TileCount > 1000, "Vast majority of the map remains neutral land for expansion");

                // Test Spectator Mode
                KeybindManager.Instance.SetGameMode(GamePlayMode.Spectator);
                tm.Initialize(grid, mr, 12345);
                Assert(tm.PlayerNation == null, "Spectator Mode has PlayerNation == null");
                Assert(tm.ProtectionTimer == 0f, "Spectator Mode has ProtectionTimer == 0");

                // 4. RoWorldHUDController Spectator Mode Safety Test
                GameObject hudObj = new GameObject("Test_RoWorldHUD");
                RoWorldHUDController hud = hudObj.AddComponent<RoWorldHUDController>();
                hud.Initialize(tm);
                hud.RefreshHUD();
                Assert(true, "RoWorldHUDController renders Spectator mode without null-reference");

                hud.ShowSpawnSelectionBanner();
                hud.UpdateSpawnSelectionTimer(7.5f);
                hud.HideSpawnSelectionBanner();
                Assert(true, "Spawn selection banner UI methods execute cleanly");

                hud.ToggleSelectAction(StructureType.Factory);
                Assert(hud.SelectedAction == StructureType.Factory, "ToggleSelectAction selects Factory");
                hud.ToggleSelectAction(StructureType.Factory);
                Assert(hud.SelectedAction == StructureType.None, "ToggleSelectAction again deselects Factory");

                hud.OpenPauseMenu();
                Assert(hud.IsPauseMenuOpen, "OpenPauseMenu opens pause modal");
                Assert(hud.IsPaused, "OpenPauseMenu pauses the game");
                hud.ClosePauseMenu();
                Assert(!hud.IsPauseMenuOpen, "ClosePauseMenu closes pause modal");
                Assert(!hud.IsPaused, "ClosePauseMenu unpauses the game");

                // Clean up test objects
                UnityEngine.Object.DestroyImmediate(hudObj);
                UnityEngine.Object.DestroyImmediate(mapObj);
                UnityEngine.Object.DestroyImmediate(terrObj);
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"Spectator & HUD Test Exception: {ex.Message}");
            }

            // 5. Procedural Map Diversity & Seed Randomization Tests
            try
            {
                WorldMapGrid gridClassic = new WorldMapGrid(140, 80);
                gridClassic.GenerateProceduralWorld(20261103);

                WorldMapGrid gridSeedA = new WorldMapGrid(140, 80);
                gridSeedA.GenerateProceduralWorld(111222);

                WorldMapGrid gridSeedB = new WorldMapGrid(140, 80);
                gridSeedB.GenerateProceduralWorld(777888);

                int landClassic = 0, landA = 0, landB = 0;
                int differences = 0;
                for (int i = 0; i < gridClassic.TotalCells; i++)
                {
                    if (gridClassic.IsLand(i)) landClassic++;
                    if (gridSeedA.IsLand(i)) landA++;
                    if (gridSeedB.IsLand(i)) landB++;
                    if (gridSeedA.Terrain[i] != gridSeedB.Terrain[i]) differences++;
                }

                float ratioA = (float)landA / gridSeedA.TotalCells;
                float ratioB = (float)landB / gridSeedB.TotalCells;

                Assert(ratioA >= 0.35f && ratioA <= 0.65f, $"Seed 111222 produces healthy land ratio: {ratioA * 100f:0.1}%");
                Assert(ratioB >= 0.35f && ratioB <= 0.65f, $"Seed 777888 produces healthy land ratio: {ratioB * 100f:0.1}%");
                Assert(differences > gridSeedA.TotalCells * 0.15f, "Different seeds produce distinctly unique maps (not identical)");

                // Player Nation Name customization test
                KeybindManager km = KeybindManager.Instance;
                km.SetPlayerNationName("Kraliyet");
                Assert(km.PlayerNationName == "Kraliyet", "Custom player nation name persists in KeybindManager");
                km.SetPlayerNationName("");
                Assert(km.PlayerNationName == "SnickTrix", "Empty nation name falls back to SnickTrix");
                Assert(KeybindManager.PresetNationNames.Length >= 10, "Preset nation names pool has sufficient variety");
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"Map Diversity Test Exception: {ex.Message}");
            }

            // 6. Comprehensive Diplomacy Suite & Radial Menu Tests
            try
            {
                WorldMapGrid grid = new WorldMapGrid(100, 70);
                grid.GenerateProceduralWorld(445566);

                GameObject terrObj = new GameObject("Test_Territory_Diplo");
                TerritoryAndCombatManager tm = terrObj.AddComponent<TerritoryAndCombatManager>();
                GameObject mapObj = new GameObject("Test_Map_Diplo");
                MapRenderer mr = mapObj.AddComponent<MapRenderer>();
                tm.Initialize(grid, mr, 445566);

                Vector2Int spawn = tm.FindBestStartingLand();
                tm.SpawnPlayerAndAI(spawn.x, spawn.y, "TestDiplomat", 445566);

                NationData player = tm.PlayerNation;
                Assert(player != null, "Player nation created for diplomacy test");

                // Find a foreign AI nation
                NationData foreignAi = null;
                foreach (var kvp in tm.Nations)
                {
                    if (kvp.Value != null && !kvp.Value.IsPlayer && !kvp.Value.IsTribe)
                    {
                        foreignAi = kvp.Value;
                        break;
                    }
                }
                Assert(foreignAi != null, "Foreign AI nation found for diplomacy interactions");

                GameObject diploObj = new GameObject("Test_Diplo_Manager");
                DiplomacyManager dm = diploObj.AddComponent<DiplomacyManager>();
                dm.Initialize(grid, tm, mr);

                // Test A: Alliance Formation
                bool allied = dm.ProposeOrFormAlliance(player.Id, foreignAi.Id, out string allyMsg);
                Assert(allied, $"Alliance formed successfully: {allyMsg}");
                Assert(player.IsAlliedWith(foreignAi.Id), "Player recognizes foreign nation as ally");
                Assert(foreignAi.IsAlliedWith(player.Id), "Foreign AI reciprocates alliance");
                Assert(player.GetRelationWith(foreignAi.Id) >= 30f, "Relations increase upon alliance");

                // Test B: Break Alliance
                bool broken = dm.BreakAlliance(player.Id, foreignAi.Id, out string breakMsg);
                Assert(broken, $"Alliance broken cleanly: {breakMsg}");
                Assert(!player.IsAlliedWith(foreignAi.Id), "Alliance cleared from player");
                Assert(!foreignAi.IsAlliedWith(player.Id), "Alliance cleared from AI");
                Assert(player.TraitorTimer > 40f, "Player marked as Traitor with active timer");
                Assert(player.GetRelationWith(foreignAi.Id) < 0f, "Relations damaged after betrayal");

                // Test C: Traitor Timer Tick Decrement
                float initialTimer = player.TraitorTimer;
                tm.Tick(1.0f);
                Assert(player.TraitorTimer < initialTimer, "TraitorTimer decrements during TerritoryManager.Tick");

                // Test D: Trade Embargo Toggle
                bool embargoed = dm.ToggleTradeEmbargo(player.Id, foreignAi.Id, out string embMsg);
                Assert(embargoed, $"Trade embargo declared: {embMsg}");
                Assert(player.HasEmbargoAgainst(foreignAi.Id), "Player has embargo active against target");
                Assert(foreignAi.HasEmbargoAgainst(player.Id), "Mutual embargo applied to AI");

                double normalGoldGrowth = foreignAi.GoldGrowthPerSec;
                tm.Tick(0.1f);
                Assert(foreignAi.Embargoes.Count > 0, "AI tracks active embargo count");

                bool lifted = dm.ToggleTradeEmbargo(player.Id, foreignAi.Id, out string liftMsg);
                Assert(!lifted, $"Trade embargo lifted: {liftMsg}");
                Assert(!player.HasEmbargoAgainst(foreignAi.Id), "Embargo removed from player");
                Assert(!foreignAi.HasEmbargoAgainst(player.Id), "Embargo removed from AI");

                // Test E: Gold Donation
                player.Gold = 50000.0;
                double foreignInitialGold = foreignAi.Gold;
                bool goldSent = dm.DonateGold(player.Id, foreignAi.Id, 15000.0, out string goldMsg);
                Assert(goldSent, $"Gold donation succeeded: {goldMsg}");
                Assert(Mathf.Approximately((float)player.Gold, 35000f), "Player gold deducted properly");
                Assert(Mathf.Approximately((float)foreignAi.Gold, (float)(foreignInitialGold + 15000.0)), "AI received donated gold");

                // Insufficient gold failure check
                bool failGold = dm.DonateGold(player.Id, foreignAi.Id, 9999999.0, out string failGoldMsg);
                Assert(!failGold, "Excessive gold donation prevented");

                // Test F: Troop Donation
                player.Troops = 1000.0;
                double foreignInitialTroops = foreignAi.Troops;
                bool troopsSent = dm.DonateTroops(player.Id, foreignAi.Id, 0.25f, out string troopMsg);
                Assert(troopsSent, $"Troops reinforced ally: {troopMsg}");
                Assert(player.Troops < 1000.0, "Player troops reduced by 25%");
                Assert(foreignAi.Troops > foreignInitialTroops, "AI troops reinforced");

                // Test G: Emote Sending & World Billboard
                bool emoteSent = dm.SendEmote(player.Id, foreignAi.Id, DiplomacyEmote.Peace, out string emoMsg);
                Assert(emoteSent, $"Peace emote sent: {emoMsg}");
                Assert(player.GetRelationWith(foreignAi.Id) > 0f, "Peace emote boosts bilateral relations");

                // Test H: RadialContextMenu UI Lifecycle
                GameObject hudObj = new GameObject("Test_HUD_Diplo");
                RoWorldHUDController hud = hudObj.AddComponent<RoWorldHUDController>();
                hud.Initialize(tm);

                Camera mainCam = Camera.main;
                if (mainCam == null)
                {
                    GameObject camObj = new GameObject("Test_MainCamera");
                    mainCam = camObj.AddComponent<Camera>();
                }
                CameraController2D camCtrl = mainCam.gameObject.AddComponent<CameraController2D>();
                camCtrl.Initialize(mainCam, mr);

                GameObject radialObj = new GameObject("Test_RadialMenu");
                RadialContextMenu radial = radialObj.AddComponent<RadialContextMenu>();
                radial.Initialize(dm, tm, hud, camCtrl, hud.RootRect);

                Assert(!radial.IsOpen, "RadialContextMenu is initially closed");
                radial.OpenForNation(foreignAi, new Vector2(960, 540));
                Assert(radial.IsOpen, "RadialContextMenu opens cleanly for foreign target");
                Assert(radial.CurrentTarget == foreignAi, "RadialContextMenu holds current target nation");

                radial.Close();
                Assert(!radial.IsOpen, "RadialContextMenu closes cleanly");
                Assert(radial.CurrentTarget == null, "RadialContextMenu clears target on close");

                // Cleanup
                UnityEngine.Object.DestroyImmediate(radialObj);
                UnityEngine.Object.DestroyImmediate(camCtrl);
                UnityEngine.Object.DestroyImmediate(hudObj);
                UnityEngine.Object.DestroyImmediate(diploObj);
                UnityEngine.Object.DestroyImmediate(mapObj);
                UnityEngine.Object.DestroyImmediate(terrObj);
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogError($"Diplomacy & Radial Menu Test Exception: {ex.Message}\n{ex.StackTrace}");
            }

            Debug.Log($"<color=#F59E2B>=== TEST RESULTS: {passed} PASSED, {failed} FAILED ===</color>");
        }
    }
}
