using MeWorld.Map;
using MeWorld.Nations;
using MeWorld.Structures;
using MeWorld.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MeWorld.Core
{
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            EnsureGameManagerExists();
        }

        public static GameManager EnsureGameManagerExists()
        {
            GameManager existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null) return existing;

            GameObject go = new GameObject("MeWorld_GameManager");
            return go.AddComponent<GameManager>();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("MeWorld/Setup Scene Objects")]
        public static void SetupSceneInEditor()
        {
            string tmpPkg = System.IO.Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (System.IO.File.Exists(tmpPkg) && !System.IO.Directory.Exists("Assets/TextMesh Pro"))
            {
                UnityEditor.AssetDatabase.ImportPackage(tmpPkg, false);
            }
            EnsureGameManagerExists();
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
#endif
    }

    public class GameManager : MonoBehaviour
    {
        public int WorldSeed = 20261103;

        private WorldMapGrid _grid;
        private MapRenderer _mapRenderer;
        private CameraController2D _cameraController;
        private TerritoryAndCombatManager _territoryManager;
        private StructureAndNukeManager _structureManager;
        private AIController _aiController;
        private WorldLabelsController _labelsController;
        private RoWorldHUDController _hudController;
        private DiplomacyManager _diplomacyManager;
        private RadialContextMenu _radialMenu;
        private MainMenuController _mainMenuController;

        private float _tickAccumulator;
        private bool _isGameOver;
        private bool _isInGame;
        private bool _isSpawnSelecting;
        private float _spawnSelectionTimer;
        private Vector2Int _hoveredLandPos;

        private void Start()
        {
            // Ensure Keybind and settings system is loaded
            KeybindManager km = KeybindManager.Instance;

            // Instantiate Main Menu
            GameObject menuObj = new GameObject("MeWorld_MainMenu");
            menuObj.transform.SetParent(transform, false);
            _mainMenuController = menuObj.AddComponent<MainMenuController>();
            _mainMenuController.Initialize();
            _mainMenuController.OnStartGameRequested = (seed, diff, mode) =>
            {
                StartNewGame(seed);
            };

            // Start in Main Menu screen
            _mainMenuController.ShowMenu();
        }

        public void StartNewGame(int seed)
        {
            _isInGame = true;
            WorldSeed = seed;
            _isGameOver = false;
            _tickAccumulator = 0f;

            if (_mainMenuController != null)
            {
                _mainMenuController.HideMenu();
            }

            // Clean up previous in-game children if restarting
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "MeWorld_MainMenu")
                {
                    Destroy(child.gameObject);
                }
            }

            // 1. Create Grid & Procedural Continents
            _grid = new WorldMapGrid(GameConfig.MapWidth, GameConfig.MapHeight);
            _grid.GenerateProceduralWorld(seed);

            // 2. Setup MapRenderer
            GameObject mapObj = new GameObject("WorldMap_Renderer");
            mapObj.transform.SetParent(transform, false);
            _mapRenderer = mapObj.AddComponent<MapRenderer>();

            // 3. Setup Territory & Combat Manager
            GameObject terrObj = new GameObject("TerritoryAndCombat_Manager");
            terrObj.transform.SetParent(transform, false);
            _territoryManager = terrObj.AddComponent<TerritoryAndCombatManager>();
            _territoryManager.Initialize(_grid, _mapRenderer, seed);

            // Initialize MapRenderer with populated nations
            _mapRenderer.Initialize(_grid, _territoryManager.Nations);

            // 4. Setup Camera Controller
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            _cameraController = mainCam.GetComponent<CameraController2D>();
            if (_cameraController == null)
            {
                _cameraController = mainCam.gameObject.AddComponent<CameraController2D>();
            }
            _cameraController.Initialize(mainCam, _mapRenderer);

            // 5. Setup Structures & Nuclear Weapons Manager
            GameObject structObj = new GameObject("StructureAndNuke_Manager");
            structObj.transform.SetParent(transform, false);
            _structureManager = structObj.AddComponent<StructureAndNukeManager>();
            _structureManager.Initialize(_grid, _mapRenderer, _territoryManager);

            // 6. Setup AI Controller
            GameObject aiObj = new GameObject("AI_Controller");
            aiObj.transform.SetParent(transform, false);
            _aiController = aiObj.AddComponent<AIController>();
            _aiController.Initialize(_grid, _territoryManager, _structureManager);

            // 7. Setup World Labels (Country names & troop counts)
            GameObject labelsObj = new GameObject("WorldLabels_Controller");
            labelsObj.transform.SetParent(transform, false);
            _labelsController = labelsObj.AddComponent<WorldLabelsController>();
            _labelsController.Initialize(_mapRenderer, _territoryManager, mainCam);

            // 8. Setup RoWorld HUD
            GameObject hudObj = new GameObject("RoWorld_HUD");
            hudObj.transform.SetParent(transform, false);
            _hudController = hudObj.AddComponent<RoWorldHUDController>();
            _hudController.Initialize(_territoryManager);

            // 9. Setup Diplomacy Manager & Reactive AI
            GameObject diploObj = new GameObject("Diplomacy_Manager");
            diploObj.transform.SetParent(transform, false);
            _diplomacyManager = diploObj.AddComponent<DiplomacyManager>();
            _diplomacyManager.Initialize(_grid, _territoryManager, _mapRenderer);

            // 10. Setup Contextual Radial Diplomacy Menu
            GameObject radialObj = new GameObject("RadialContext_Menu");
            radialObj.transform.SetParent(transform, false);
            _radialMenu = radialObj.AddComponent<RadialContextMenu>();
            _radialMenu.Initialize(_diplomacyManager, _territoryManager, _hudController, _cameraController, _hudController.RootRect);

            _hudController.OnFocusPlayerRequested = () =>
            {
                if (_territoryManager.PlayerNation != null)
                {
                    _cameraController.FocusOnGridPosition(
                        _territoryManager.PlayerNation.CenterGridPos.x,
                        _territoryManager.PlayerNation.CenterGridPos.y,
                        6.8f
                    );
                }
                else if (_territoryManager.LeadingNation != null)
                {
                    _cameraController.FocusOnGridPosition(
                        _territoryManager.LeadingNation.CenterGridPos.x,
                        _territoryManager.LeadingNation.CenterGridPos.y,
                        7.5f
                    );
                }
            };

            _hudController.OnRestartRequested = () =>
            {
                StartNewGame(WorldSeed + 7);
            };

            _hudController.OnOpenSettingsRequested = () =>
            {
                if (_mainMenuController != null)
                {
                    _mainMenuController.OpenSettingsModalDirectly();
                }
            };

            _hudController.OnReturnToMainMenuRequested = () =>
            {
                ReturnToMainMenu();
            };

            _territoryManager.OnNationPlundered = (conqueror, victim) =>
            {
                if (conqueror.IsPlayer && _hudController != null)
                {
                    _hudController.ShowStatusMessage($"Tebrikler! {victim.Name} fethedildi ve hazinesinden +{GameConfig.FormatNumber(victim.Gold)} altın yağmalandı!", 3.2f);
                }
            };

            // 9. Initial mode setup and spawn selection phase
            GamePlayMode mode = KeybindManager.Instance.CurrentGameMode;
            if (mode == GamePlayMode.Spectator)
            {
                _isSpawnSelecting = false;
                _cameraController.FocusOnGridPosition(GameConfig.MapWidth * 0.5f, GameConfig.MapHeight * 0.5f, 13f);
                _hudController.HideSpawnSelectionBanner();
                _hudController.RefreshHUD();
            }
            else
            {
                _isSpawnSelecting = true;
                _spawnSelectionTimer = 10f;
                _hoveredLandPos = _territoryManager.FindBestStartingLand();
                _cameraController.FocusOnGridPosition(_hoveredLandPos.x, _hoveredLandPos.y, 8.5f);
                _mapRenderer.SetSpawnIndicator(_hoveredLandPos.x, _hoveredLandPos.y, true);
                _hudController.ShowSpawnSelectionBanner();
                _hudController.OnSpawnRandomRequested = () =>
                {
                    Vector2Int rndLand = _territoryManager.FindBestStartingLand();
                    ConfirmPlayerSpawnAt(rndLand.x, rndLand.y);
                };
            }
        }

        public void ConfirmPlayerSpawnAt(int gx, int gy)
        {
            if (!_isSpawnSelecting) return;
            _isSpawnSelecting = false;

            if (_mapRenderer != null)
            {
                _mapRenderer.SetSpawnIndicator(0, 0, false);
            }

            string pName = KeybindManager.Instance.PlayerNationName;
            _territoryManager.SpawnPlayerAndAI(gx, gy, pName, WorldSeed);

            Camera mainCam = _cameraController != null ? _cameraController.Cam : Camera.main;
            if (_labelsController != null && mainCam != null)
            {
                _labelsController.Initialize(_mapRenderer, _territoryManager, mainCam);
            }

            if (_hudController != null)
            {
                _hudController.HideSpawnSelectionBanner();
                _hudController.RefreshHUD();
                _hudController.ShowStatusMessage($"Başkent kuruldu! Ulus: {pName}", 3.0f);
            }

            if (_cameraController != null && _territoryManager.PlayerNation != null)
            {
                _cameraController.FocusOnGridPosition(
                    _territoryManager.PlayerNation.CenterGridPos.x,
                    _territoryManager.PlayerNation.CenterGridPos.y,
                    6.2f
                );
            }
        }

        public void ReturnToMainMenu()
        {
            _isInGame = false;
            _isGameOver = false;
            _isSpawnSelecting = false;
            if (_mapRenderer != null)
            {
                _mapRenderer.SetSpawnIndicator(0, 0, false);
            }

            // Clean up in-game child components
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name != "MeWorld_MainMenu")
                {
                    Destroy(child.gameObject);
                }
            }

            _grid = null;
            _mapRenderer = null;
            _territoryManager = null;
            _structureManager = null;
            _aiController = null;
            _labelsController = null;
            _hudController = null;
            _diplomacyManager = null;
            _radialMenu = null;

            if (_mainMenuController != null)
            {
                _mainMenuController.ShowMenu();
            }
        }

        private void Update()
        {
            if (!_isInGame || _grid == null) return;

            if (_isSpawnSelecting)
            {
                UpdateSpawnSelection();
                return;
            }

            HandleKeybindingsInput();
            HandleMapClickInput();

            if (_isGameOver || (_hudController != null && _hudController.IsPaused))
                return;

            _tickAccumulator += Time.deltaTime;
            while (_tickAccumulator >= GameConfig.TickInterval)
            {
                _tickAccumulator -= GameConfig.TickInterval;
                PerformGameTick();
            }
        }

        private void UpdateSpawnSelection()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                ReturnToMainMenu();
                return;
            }

            _spawnSelectionTimer -= Time.deltaTime;
            if (_hudController != null)
            {
                _hudController.UpdateSpawnSelectionTimer(_spawnSelectionTimer);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && _cameraController != null && _cameraController.Cam != null)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Vector3 worldPos = _cameraController.Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                if (_mapRenderer != null && _mapRenderer.WorldToGrid(worldPos, out int gx, out int gy))
                {
                    int cIdx = _grid.GetIndex(gx, gy);
                    if (_grid.IsLand(cIdx))
                    {
                        _hoveredLandPos = new Vector2Int(gx, gy);
                        _mapRenderer.SetSpawnIndicator(gx, gy, true);

                        if (mouse.leftButton.wasPressedThisFrame)
                        {
                            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                            {
                                ConfirmPlayerSpawnAt(gx, gy);
                                return;
                            }
                        }
                    }
                }
            }

            if (_spawnSelectionTimer <= 0f)
            {
                ConfirmPlayerSpawnAt(_hoveredLandPos.x, _hoveredLandPos.y);
            }
        }

        private void HandleKeybindingsInput()
        {
            KeybindManager km = KeybindManager.Instance;
            if (km == null || km.IsRebinding || _hudController == null) return;

            // Building Shortcuts
            if (km.IsActionTriggered(KeyAction.SelectFactory))
                _hudController.ToggleSelectAction(StructureType.Factory);
            else if (km.IsActionTriggered(KeyAction.SelectCity))
                _hudController.ToggleSelectAction(StructureType.City);
            else if (km.IsActionTriggered(KeyAction.SelectPort))
                _hudController.ToggleSelectAction(StructureType.Port);
            else if (km.IsActionTriggered(KeyAction.SelectDefensePost))
                _hudController.ToggleSelectAction(StructureType.DefensePost);
            else if (km.IsActionTriggered(KeyAction.SelectSAMLauncher))
                _hudController.ToggleSelectAction(StructureType.SAMLauncher);
            else if (km.IsActionTriggered(KeyAction.SelectMissileSilo))
                _hudController.ToggleSelectAction(StructureType.MissileSilo);
            else if (km.IsActionTriggered(KeyAction.SelectAtomBomb))
                _hudController.ToggleSelectAction(StructureType.AtomBomb);
            else if (km.IsActionTriggered(KeyAction.SelectHydrogenBomb))
                _hudController.ToggleSelectAction(StructureType.HydrogenBomb);
            else if (km.IsActionTriggered(KeyAction.SelectWarship))
                _hudController.ToggleSelectAction(StructureType.Warship);
            else if (km.IsActionTriggered(KeyAction.SelectAlliance))
                _hudController.ToggleSelectAction(StructureType.Alliance);

            // Utility Shortcuts
            else if (km.IsActionTriggered(KeyAction.FocusPlayer))
                _hudController.OnFocusPlayerRequested?.Invoke();
            else if (km.IsActionTriggered(KeyAction.TogglePause))
                _hudController.TogglePause();
            else if (km.IsActionTriggered(KeyAction.CancelOrMenu))
            {
                if (_radialMenu != null && _radialMenu.IsOpen)
                {
                    _radialMenu.Close();
                }
                else if (_hudController.IsPauseMenuOpen)
                {
                    _hudController.ClosePauseMenu();
                }
                else if (_hudController.SelectedAction != StructureType.None)
                {
                    _hudController.ClearSelectedAction();
                }
                else
                {
                    _hudController.OpenPauseMenu();
                }
            }
        }

        private void PerformGameTick()
        {
            _territoryManager.Tick(GameConfig.TickInterval);
            _aiController.TickAI();
            _hudController.RefreshHUD();
            CheckWinLossConditions();
        }

        private void HandleMapClickInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Right-click handling: dismiss radial menu, clear bottom bar action, or open radial menu on foreign nation
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_radialMenu != null && _radialMenu.IsOpen)
                {
                    _radialMenu.Close();
                    return;
                }

                if (_hudController != null && _hudController.SelectedAction != StructureType.None)
                {
                    _hudController.ClearSelectedAction();
                    return;
                }

                if (_cameraController != null && _cameraController.Cam != null && _territoryManager != null && _mapRenderer != null && _grid != null)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    Vector3 wPos = _cameraController.Cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
                    if (_mapRenderer.WorldToGrid(wPos, out int rx, out int ry))
                    {
                        int rIdx = _grid.GetIndex(rx, ry);
                        if (_grid.IsLand(rIdx))
                        {
                            ushort rightClickTargetOwner = _grid.OwnerId[rIdx];
                            NationData p = _territoryManager.PlayerNation;
                            if (rightClickTargetOwner > 0 && (p == null || rightClickTargetOwner != p.Id))
                            {
                                if (_territoryManager.Nations.TryGetValue(rightClickTargetOwner, out NationData targetNation))
                                {
                                    if (_radialMenu != null)
                                    {
                                        _radialMenu.OpenForNation(targetNation, mousePos);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // If radial menu is open and player clicks outside of UI, close it
            if (_radialMenu != null && _radialMenu.IsOpen)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    _radialMenu.Close();
                    return;
                }
            }

            // Ignore clicks on UI panels
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_cameraController == null || _cameraController.Cam == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 worldPos = _cameraController.Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            if (!_mapRenderer.WorldToGrid(worldPos, out int gx, out int gy))
                return;

            NationData player = _territoryManager.PlayerNation;

            // Spectator Mode Click: Inspect Nation Stats
            if (player == null)
            {
                int cIdx = _grid.GetIndex(gx, gy);
                if (_grid.IsLand(cIdx))
                {
                    ushort owner = _grid.OwnerId[cIdx];
                    if (owner > 0 && _territoryManager.Nations.TryGetValue(owner, out NationData inspected))
                    {
                        _hudController.ShowStatusMessage(
                            $"[ULUS: {inspected.Name}] Asker: {GameConfig.FormatNumber(inspected.Troops)}/{GameConfig.FormatNumber(inspected.MaxTroops)} (+{GameConfig.FormatNumber(inspected.TroopGrowthPerSec)}/s) | Altın: {GameConfig.FormatNumber(inspected.Gold)} | Hakimiyet: {(inspected.WorldControlPercent * 100f):0.1}% | Şehir: {inspected.CityCount} Liman: {inspected.PortCount}",
                            3.5f
                        );
                    }
                }
                return;
            }

            if (!player.IsAlive) return;

            // 1. If a build/nuke/diplomacy action is selected in bottom bar
            if (_hudController.SelectedAction != StructureType.None)
            {
                StructureType act = _hudController.SelectedAction;
                if (_structureManager.TryBuildOrExecuteAction(player.Id, act, gx, gy, out string feedback))
                {
                    _hudController.ShowStatusMessage(feedback, 2.5f);
                    _hudController.ClearSelectedAction();
                    _hudController.RefreshHUD();
                }
                else
                {
                    _hudController.ShowStatusMessage(feedback, 2.5f);
                }
                return;
            }

            // 2. Otherwise, clicking on land initiates an Attack / Expansion wave
            int cellIdx = _grid.GetIndex(gx, gy);
            if (!_grid.IsLand(cellIdx)) return;

            ushort targetOwner = _grid.OwnerId[cellIdx];
            if (targetOwner == player.Id) return;

            bool started = _territoryManager.StartAttack(player.Id, targetOwner, _hudController.SelectedAttackRatio, gx, gy);
            if (started)
            {
                if (targetOwner == 0)
                {
                    _hudController.ShowStatusMessage("Expanding into neutral territory...", 1.5f);
                }
                else if (_territoryManager.Nations.TryGetValue(targetOwner, out NationData defender))
                {
                    _hudController.ShowStatusMessage($"Attacking {defender.Name}!", 1.8f);
                }
                _hudController.RefreshHUD();
            }
            else
            {
                _hudController.ShowStatusMessage("Target must share a border with your territory (or build a Port for coastal invasion).", 2.4f);
            }
        }

        private void CheckWinLossConditions()
        {
            if (_isGameOver) return;

            NationData player = _territoryManager.PlayerNation;
            if (player != null && !player.IsAlive)
            {
                _isGameOver = true;
                _hudController.ShowEndGameModal(false, "Your nation was conquered by rivals.");
                return;
            }

            NationData leader = _territoryManager.LeadingNation;
            if (leader != null && leader.WorldControlPercent >= GameConfig.WinControlPercentage)
            {
                _isGameOver = true;
                bool playerWon = player != null && leader.IsPlayer;
                string reason = playerWon
                    ? $"You conquered {(leader.WorldControlPercent * 100f):0.0}% of the world!"
                    : $"{leader.Name} reached {(leader.WorldControlPercent * 100f):0.0}% world control!";
                _hudController.ShowEndGameModal(playerWon, reason);
            }
        }
    }
}
