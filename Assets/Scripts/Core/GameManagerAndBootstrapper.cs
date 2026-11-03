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

        private float _tickAccumulator;
        private bool _isGameOver;
        private bool _initialized;

        private void Start()
        {
            if (!_initialized)
            {
                StartNewGame(WorldSeed);
            }
        }

        public void StartNewGame(int seed)
        {
            _initialized = true;
            WorldSeed = seed;
            _isGameOver = false;
            _tickAccumulator = 0f;

            // Clean up previous children if restarting
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
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

            if (_territoryManager.PlayerNation != null)
            {
                _cameraController.FocusOnGridPosition(
                    _territoryManager.PlayerNation.CenterGridPos.x,
                    _territoryManager.PlayerNation.CenterGridPos.y + 8f,
                    7.8f
                );
            }

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
            };

            _hudController.OnRestartRequested = () =>
            {
                StartNewGame(WorldSeed + 7);
            };
        }

        private void Update()
        {
            if (!_initialized || _grid == null) return;

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

            if (mouse.rightButton.wasPressedThisFrame && _hudController != null && _hudController.SelectedAction != StructureType.None)
            {
                _hudController.ClearSelectedAction();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            // Ignore clicks on UI panels
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_cameraController == null || _cameraController.Cam == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 worldPos = _cameraController.Cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            if (!_mapRenderer.WorldToGrid(worldPos, out int gx, out int gy))
                return;

            NationData player = _territoryManager.PlayerNation;
            if (player == null || !player.IsAlive) return;

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
            if (player == null || !player.IsAlive)
            {
                _isGameOver = true;
                _hudController.ShowEndGameModal(false, "Your nation was conquered by rivals.");
                return;
            }

            NationData leader = _territoryManager.LeadingNation;
            if (leader != null && leader.WorldControlPercent >= GameConfig.WinControlPercentage)
            {
                _isGameOver = true;
                bool playerWon = leader.IsPlayer;
                string reason = playerWon
                    ? $"You conquered {(leader.WorldControlPercent * 100f):0.0}% of the world!"
                    : $"{leader.Name} reached {(leader.WorldControlPercent * 100f):0.0}% world control!";
                _hudController.ShowEndGameModal(playerWon, reason);
            }
        }
    }
}
