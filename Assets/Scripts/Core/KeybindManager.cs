using System;
using System.Collections.Generic;
using MeWorld.Structures;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MeWorld.Core
{
    public enum GameDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Chaos = 3
    }

    public enum GamePlayMode
    {
        SoloConquest = 0,
        Spectator = 1
    }

    public enum KeyAction
    {
        SelectCity = 0,
        SelectPort = 1,
        SelectFactory = 2,
        SelectDefensePost = 3,
        SelectSAMLauncher = 4,
        SelectMissileSilo = 5,
        SelectAtomBomb = 6,
        SelectHydrogenBomb = 7,
        SelectWarship = 8,
        SelectAlliance = 9,
        FocusPlayer = 10,
        TogglePause = 11,
        CancelOrMenu = 12
    }

    public class KeybindManager : MonoBehaviour
    {
        private static KeybindManager _instance;
        public static KeybindManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<KeybindManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MeWorld_KeybindManager");
                        _instance = go.AddComponent<KeybindManager>();
                        UnityEngine.Object.DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Gameplay Settings
        public GameDifficulty CurrentDifficulty { get; set; } = GameDifficulty.Normal;
        public GamePlayMode CurrentGameMode { get; set; } = GamePlayMode.SoloConquest;
        public int CustomSeed { get; set; } = 20261103;
        public string PlayerNationName { get; set; } = "SnickTrix";

        public static readonly string[] PresetNationNames = {
            "SnickTrix", "Avalon", "Gokturk", "Prussia", "Carthage",
            "Valoria", "Sparta", "Byzantium", "Ottoman", "Novgorod",
            "Eldoria", "Imperium", "Viking Realm", "Solaria", "Hyperborea",
            "Ironclad", "Crimson Dawn", "Mooncrest", "Thunderborn", "Aethelgard"
        };

        public void SetPlayerNationName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "SnickTrix";
            PlayerNationName = name.Trim();
            PlayerPrefs.SetString("MeWorld_PlayerNationName", PlayerNationName);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public string GetRandomPresetNationName()
        {
            int idx = UnityEngine.Random.Range(0, PresetNationNames.Length);
            return PresetNationNames[idx];
        }

        // Audio & Display Settings
        public float MasterVolume { get; private set; } = 1.0f;
        public bool IsMuted { get; private set; } = false;
        public bool ShowWorldLabels { get; private set; } = true;
        public bool SmoothCamera { get; private set; } = true;

        // Keybinding Mapping
        private readonly Dictionary<KeyAction, Key> _keybindings = new Dictionary<KeyAction, Key>();

        // Rebinding State
        public bool IsRebinding { get; private set; } = false;
        public KeyAction RebindingAction { get; private set; }
        public Action<KeyAction, Key> OnKeybindChanged;
        public Action<KeyAction> OnRebindWaiting;
        public Action OnSettingsChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                LoadAllSettings();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void LoadAllSettings()
        {
            // Load Keybindings
            LoadDefaultKeybindings();
            foreach (KeyAction act in Enum.GetValues(typeof(KeyAction)))
            {
                string prefKey = "MeWorld_Key_" + act.ToString();
                if (PlayerPrefs.HasKey(prefKey))
                {
                    int val = PlayerPrefs.GetInt(prefKey);
                    if (Enum.IsDefined(typeof(Key), val))
                    {
                        _keybindings[act] = (Key)val;
                    }
                }
            }

            // Load Other Settings
            CurrentDifficulty = (GameDifficulty)PlayerPrefs.GetInt("MeWorld_Difficulty", (int)GameDifficulty.Normal);
            CurrentGameMode = (GamePlayMode)PlayerPrefs.GetInt("MeWorld_GameMode", (int)GamePlayMode.SoloConquest);
            PlayerNationName = PlayerPrefs.GetString("MeWorld_PlayerNationName", "SnickTrix");
            MasterVolume = PlayerPrefs.GetFloat("MeWorld_MasterVolume", 1.0f);
            IsMuted = PlayerPrefs.GetInt("MeWorld_IsMuted", 0) == 1;
            ShowWorldLabels = PlayerPrefs.GetInt("MeWorld_ShowLabels", 1) == 1;
            SmoothCamera = PlayerPrefs.GetInt("MeWorld_SmoothCam", 1) == 1;
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
        }

        public void ResetKeybindingsToDefaults()
        {
            LoadDefaultKeybindings();
            foreach (KeyAction act in Enum.GetValues(typeof(KeyAction)))
            {
                PlayerPrefs.DeleteKey("MeWorld_Key_" + act.ToString());
            }
            PlayerPrefs.Save();
            OnKeybindChanged?.Invoke(KeyAction.SelectFactory, GetKey(KeyAction.SelectFactory));
            OnSettingsChanged?.Invoke();
        }

        private void LoadDefaultKeybindings()
        {
            _keybindings.Clear();
            _keybindings[KeyAction.SelectCity] = Key.Digit1;
            _keybindings[KeyAction.SelectPort] = Key.Digit2;
            _keybindings[KeyAction.SelectFactory] = Key.F; // Explicitly F for Factory as requested!
            _keybindings[KeyAction.SelectDefensePost] = Key.Digit4;
            _keybindings[KeyAction.SelectSAMLauncher] = Key.Digit5;
            _keybindings[KeyAction.SelectMissileSilo] = Key.Digit6;
            _keybindings[KeyAction.SelectAtomBomb] = Key.Digit7;
            _keybindings[KeyAction.SelectHydrogenBomb] = Key.Digit8;
            _keybindings[KeyAction.SelectWarship] = Key.Digit9;
            _keybindings[KeyAction.SelectAlliance] = Key.Digit0;
            _keybindings[KeyAction.FocusPlayer] = Key.Space;
            _keybindings[KeyAction.TogglePause] = Key.P;
            _keybindings[KeyAction.CancelOrMenu] = Key.Escape;
        }

        public Key GetKey(KeyAction action)
        {
            if (_keybindings.TryGetValue(action, out Key key))
                return key;
            return Key.None;
        }

        public string GetKeyDisplayName(KeyAction action)
        {
            Key key = GetKey(action);
            return FormatKeyName(key);
        }

        public static string FormatKeyName(Key key)
        {
            switch (key)
            {
                case Key.None: return "-";
                case Key.Digit0: return "0";
                case Key.Digit1: return "1";
                case Key.Digit2: return "2";
                case Key.Digit3: return "3";
                case Key.Digit4: return "4";
                case Key.Digit5: return "5";
                case Key.Digit6: return "6";
                case Key.Digit7: return "7";
                case Key.Digit8: return "8";
                case Key.Digit9: return "9";
                case Key.Space: return "Space";
                case Key.Escape: return "Esc";
                case Key.Enter: return "Enter";
                case Key.Tab: return "Tab";
                default: return key.ToString();
            }
        }

        public string GetActionDisplayName(KeyAction action)
        {
            switch (action)
            {
                case KeyAction.SelectFactory: return "Fabrika İnşa Et (Factory)";
                case KeyAction.SelectCity: return "Şehir İnşa Et (City)";
                case KeyAction.SelectPort: return "Liman İnşa Et (Port)";
                case KeyAction.SelectDefensePost: return "Savunma Karakolu (Defense)";
                case KeyAction.SelectSAMLauncher: return "SAM Hava Savunma (SAM)";
                case KeyAction.SelectMissileSilo: return "Füze Silosu (Silo)";
                case KeyAction.SelectAtomBomb: return "Atom Bombası (Atom)";
                case KeyAction.SelectHydrogenBomb: return "Hidrojen Bombası (H-Bomb)";
                case KeyAction.SelectWarship: return "Savaş Gemisi (Warship)";
                case KeyAction.SelectAlliance: return "İttifak Kur (Alliance)";
                case KeyAction.FocusPlayer: return "Başkente Odaklan";
                case KeyAction.TogglePause: return "Duraklat / Devam Et";
                case KeyAction.CancelOrMenu: return "İptal / Menü (Escape)";
                default: return action.ToString();
            }
        }

        public StructureType GetStructureType(KeyAction action)
        {
            switch (action)
            {
                case KeyAction.SelectCity: return StructureType.City;
                case KeyAction.SelectPort: return StructureType.Port;
                case KeyAction.SelectFactory: return StructureType.Factory;
                case KeyAction.SelectDefensePost: return StructureType.DefensePost;
                case KeyAction.SelectSAMLauncher: return StructureType.SAMLauncher;
                case KeyAction.SelectMissileSilo: return StructureType.MissileSilo;
                case KeyAction.SelectAtomBomb: return StructureType.AtomBomb;
                case KeyAction.SelectHydrogenBomb: return StructureType.HydrogenBomb;
                case KeyAction.SelectWarship: return StructureType.Warship;
                case KeyAction.SelectAlliance: return StructureType.Alliance;
                default: return StructureType.None;
            }
        }

        public KeyAction? GetKeyActionForStructure(StructureType type)
        {
            switch (type)
            {
                case StructureType.City: return KeyAction.SelectCity;
                case StructureType.Port: return KeyAction.SelectPort;
                case StructureType.Factory: return KeyAction.SelectFactory;
                case StructureType.DefensePost: return KeyAction.SelectDefensePost;
                case StructureType.SAMLauncher: return KeyAction.SelectSAMLauncher;
                case StructureType.MissileSilo: return KeyAction.SelectMissileSilo;
                case StructureType.AtomBomb: return KeyAction.SelectAtomBomb;
                case StructureType.HydrogenBomb: return KeyAction.SelectHydrogenBomb;
                case StructureType.Warship: return KeyAction.SelectWarship;
                case StructureType.Alliance: return KeyAction.SelectAlliance;
                default: return null;
            }
        }

        public bool IsActionTriggered(KeyAction action)
        {
            if (IsRebinding) return false;
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;

            Key key = GetKey(action);
            if (key == Key.None) return false;

            try
            {
                KeyControl control = kb[key];
                return control != null && control.wasPressedThisFrame;
            }
            catch
            {
                return false;
            }
        }

        // Rebinding workflow
        public void StartRebinding(KeyAction action)
        {
            IsRebinding = true;
            RebindingAction = action;
            OnRebindWaiting?.Invoke(action);
        }

        public void CancelRebinding()
        {
            IsRebinding = false;
            OnRebindWaiting?.Invoke(KeyAction.CancelOrMenu);
        }

        public void UpdateRebindingInput()
        {
            if (!IsRebinding) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            foreach (KeyControl keyControl in kb.allKeys)
            {
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    Key pressed = keyControl.keyCode;
                    if (pressed == Key.Escape)
                    {
                        CancelRebinding();
                        return;
                    }

                    // Apply new key
                    _keybindings[RebindingAction] = pressed;
                    PlayerPrefs.SetInt("MeWorld_Key_" + RebindingAction.ToString(), (int)pressed);
                    PlayerPrefs.Save();

                    IsRebinding = false;
                    OnKeybindChanged?.Invoke(RebindingAction, pressed);
                    OnSettingsChanged?.Invoke();
                    return;
                }
            }
        }

        public void SetMasterVolume(float vol)
        {
            MasterVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat("MeWorld_MasterVolume", MasterVolume);
            PlayerPrefs.Save();
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
            OnSettingsChanged?.Invoke();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt("MeWorld_IsMuted", muted ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
            OnSettingsChanged?.Invoke();
        }

        public void SetShowWorldLabels(bool show)
        {
            ShowWorldLabels = show;
            PlayerPrefs.SetInt("MeWorld_ShowLabels", show ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public void SetSmoothCamera(bool smooth)
        {
            SmoothCamera = smooth;
            PlayerPrefs.SetInt("MeWorld_SmoothCam", smooth ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public void SetDifficulty(GameDifficulty diff)
        {
            CurrentDifficulty = diff;
            PlayerPrefs.SetInt("MeWorld_Difficulty", (int)diff);
            PlayerPrefs.Save();
        }

        public void SetGameMode(GamePlayMode mode)
        {
            CurrentGameMode = mode;
            PlayerPrefs.SetInt("MeWorld_GameMode", (int)mode);
            PlayerPrefs.Save();
        }
    }
}
