using System.Collections.Generic;
using MeWorld.Core;
using MeWorld.Nations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MeWorld.Map
{
    public class MapRenderer : MonoBehaviour
    {
        private WorldMapGrid _grid;
        private IReadOnlyDictionary<ushort, NationData> _nations;

        private Texture2D _mapTexture;
        private Color32[] _pixelBuffer;
        private SpriteRenderer _spriteRenderer;
        private bool _isDirty = true;

        // RoWorld exact topographic & ocean palette
        private static readonly Color32 ColorDeepOcean = new Color32(94, 151, 209, 255);    // #5E97D1
        private static readonly Color32 ColorShallowWater = new Color32(120, 173, 224, 255); // #78ADE0
        private static readonly Color32 ColorCoastFoam = new Color32(146, 193, 237, 255);    // #92C1ED
        private static readonly Color32 ColorPlains = new Color32(223, 232, 207, 255);       // #DFE8CF
        private static readonly Color32 ColorHighlands = new Color32(204, 214, 188, 255);    // #CCD6BC
        private static readonly Color32 ColorMountains = new Color32(179, 190, 164, 255);    // #B3BEA4
        private static readonly Color32 ColorScorched = new Color32(62, 56, 52, 255);        // #3E3834
        private static readonly Color32 ColorCoastStroke = new Color32(150, 168, 150, 255);

        public float WorldWidth => _grid != null ? _grid.Width / GameConfig.PixelsPerUnit : 56f;
        public float WorldHeight => _grid != null ? _grid.Height / GameConfig.PixelsPerUnit : 32f;

        public void Initialize(WorldMapGrid grid, IReadOnlyDictionary<ushort, NationData> nations)
        {
            _grid = grid;
            _nations = nations;

            _pixelBuffer = new Color32[_grid.TotalCells];
            _mapTexture = new Texture2D(_grid.Width, _grid.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            Sprite mapSprite = Sprite.Create(
                _mapTexture,
                new Rect(0, 0, _grid.Width, _grid.Height),
                new Vector2(0.5f, 0.5f),
                GameConfig.PixelsPerUnit
            );
            _spriteRenderer.sprite = mapSprite;
            _spriteRenderer.sortingOrder = -100;

            MarkDirty();
            RefreshTextureIfNeeded();
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        private void LateUpdate()
        {
            RefreshTextureIfNeeded();
        }

        public void RefreshTextureIfNeeded()
        {
            if (!_isDirty || _grid == null) return;
            _isDirty = false;

            int total = _grid.TotalCells;
            for (int i = 0; i < total; i++)
            {
                TerrainType terrain = _grid.Terrain[i];
                if (terrain == TerrainType.DeepOcean)
                {
                    _pixelBuffer[i] = ColorDeepOcean;
                    continue;
                }
                if (terrain == TerrainType.ShallowWater)
                {
                    float elev = _grid.Elevation[i];
                    _pixelBuffer[i] = elev > 0.475f ? ColorCoastFoam : ColorShallowWater;
                    continue;
                }

                // Base land color
                Color32 baseLand;
                switch (terrain)
                {
                    case TerrainType.Plains:
                        baseLand = _grid.IsCoast[i] ? ColorCoastStroke : ColorPlains;
                        break;
                    case TerrainType.Highlands:
                        baseLand = ColorHighlands;
                        break;
                    case TerrainType.Mountains:
                        baseLand = ColorMountains;
                        break;
                    case TerrainType.Scorched:
                        baseLand = ColorScorched;
                        break;
                    default:
                        baseLand = ColorPlains;
                        break;
                }

                ushort owner = _grid.OwnerId[i];
                if (owner == 0 || _nations == null || !_nations.TryGetValue(owner, out NationData nation))
                {
                    _pixelBuffer[i] = baseLand;
                }
                else
                {
                    if (_grid.IsBorder[i])
                    {
                        _pixelBuffer[i] = nation.BorderColor;
                    }
                    else
                    {
                        Color32 fill = nation.FillColor;
                        int fillWeight = nation.IsTribe ? 35 : 158;
                        int landWeight = 256 - fillWeight;
                        byte r = (byte)((fill.r * fillWeight + baseLand.r * landWeight) >> 8);
                        byte g = (byte)((fill.g * fillWeight + baseLand.g * landWeight) >> 8);
                        byte b = (byte)((fill.b * fillWeight + baseLand.b * landWeight) >> 8);
                        _pixelBuffer[i] = new Color32(r, g, b, 255);
                    }
                }
            }

            _mapTexture.SetPixels32(_pixelBuffer);
            _mapTexture.Apply(false);
        }

        public Vector3 GridToWorld(float gx, float gy)
        {
            if (_grid == null) return Vector3.zero;
            float wx = (gx - _grid.Width * 0.5f + 0.5f) / GameConfig.PixelsPerUnit;
            float wy = (gy - _grid.Height * 0.5f + 0.5f) / GameConfig.PixelsPerUnit;
            return new Vector3(wx, wy, 0f);
        }

        public bool WorldToGrid(Vector3 worldPos, out int gx, out int gy)
        {
            if (_grid == null)
            {
                gx = 0;
                gy = 0;
                return false;
            }

            gx = Mathf.FloorToInt(worldPos.x * GameConfig.PixelsPerUnit + _grid.Width * 0.5f);
            gy = Mathf.FloorToInt(worldPos.y * GameConfig.PixelsPerUnit + _grid.Height * 0.5f);
            return _grid.InBounds(gx, gy);
        }
    }

    public class CameraController2D : MonoBehaviour
    {
        private Camera _cam;
        private MapRenderer _mapRenderer;

        public float MinOrthoSize = 3.2f;
        public float MaxOrthoSize = 16.5f;
        public float ZoomSpeed = 1.15f;
        public float KeyboardPanSpeed = 12f;

        private bool _isDragging;
        private Vector3 _dragOriginWorld;
        private float _targetOrthoSize = 8.5f;

        public Camera Cam => _cam;

        public void Initialize(Camera cam, MapRenderer mapRenderer)
        {
            _cam = cam;
            _mapRenderer = mapRenderer;
            _cam.orthographic = true;
            _targetOrthoSize = 8.2f;
            _cam.orthographicSize = _targetOrthoSize;
            _cam.backgroundColor = new Color32(74, 128, 184, 255);
        }

        public void FocusOnGridPosition(float gx, float gy, float orthoSize = 7.2f)
        {
            if (_mapRenderer == null || _cam == null) return;
            Vector3 wp = _mapRenderer.GridToWorld(gx, gy);
            _targetOrthoSize = Mathf.Clamp(orthoSize, MinOrthoSize, MaxOrthoSize);
            _cam.orthographicSize = _targetOrthoSize;
            _cam.transform.position = new Vector3(wp.x, wp.y, -10f);
            ClampCameraPosition();
        }

        private void Update()
        {
            if (_cam == null || _mapRenderer == null) return;

            HandleKeyboardPan();
            HandleMouseZoomAndDrag();
            ClampCameraPosition();
        }

        private void HandleKeyboardPan()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            Vector2 move = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;

            if (move.sqrMagnitude > 0.001f)
            {
                float scaleFactor = _cam.orthographicSize / 8.5f;
                Vector3 delta = new Vector3(move.normalized.x, move.normalized.y, 0f) * (KeyboardPanSpeed * scaleFactor * Time.unscaledDeltaTime);
                _cam.transform.position += delta;
            }
        }

        private void HandleMouseZoomAndDrag()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector2 mouseScreen = mouse.position.ReadValue();

            // Scroll wheel zoom toward cursor
            if (!pointerOverUI)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    Vector3 worldBefore = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
                    float factor = scrollY > 0f ? (1f / ZoomSpeed) : ZoomSpeed;
                    _targetOrthoSize = Mathf.Clamp(_targetOrthoSize * factor, MinOrthoSize, MaxOrthoSize);
                    _cam.orthographicSize = _targetOrthoSize;
                    Vector3 worldAfter = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
                    _cam.transform.position += (worldBefore - worldAfter);
                }
            }

            // Middle mouse or Right mouse drag pan
            bool dragButtonDown = mouse.middleButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
            bool dragButtonHeld = mouse.middleButton.isPressed || mouse.rightButton.isPressed;

            if (dragButtonDown && !pointerOverUI)
            {
                _isDragging = true;
                _dragOriginWorld = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
            }

            if (_isDragging && dragButtonHeld)
            {
                Vector3 currentWorld = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 10f));
                Vector3 diff = _dragOriginWorld - currentWorld;
                _cam.transform.position += new Vector3(diff.x, diff.y, 0f);
            }

            if (!dragButtonHeld)
            {
                _isDragging = false;
            }
        }

        private void ClampCameraPosition()
        {
            float halfW = _mapRenderer.WorldWidth * 0.5f;
            float halfH = _mapRenderer.WorldHeight * 0.5f;

            Vector3 pos = _cam.transform.position;
            pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
            pos.y = Mathf.Clamp(pos.y, -halfH, halfH);
            pos.z = -10f;
            _cam.transform.position = pos;
        }
    }
}
