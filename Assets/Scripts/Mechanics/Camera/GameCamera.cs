using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mechanics
{
    public sealed class GameCamera : MonoBehaviour
    {
        [SerializeField] private float _traceMoveTime;
        [SerializeField] private float _maxOffset;
        [SerializeField] private Sprite _cameraTraceSrite;
        [SerializeField] private Sprite _pictureSprite;
        [SerializeField] private Material _pictureDisplayerMat;
        [SerializeField] private Material _pictureDisplayerProjectionMat;
        [SerializeField] private Vector2Int _pictureSize;
        [SerializeField] private LayerMask _renderMask;
        [SerializeField] private LayerMask _pictureMask;

        private Collider2D[] _cached = new Collider2D[32];

        public Vector2 LookDirection = Vector2.right;
        private float _traceElapsed;
        private bool _traceEnabled = false;
        private Vector2 _prevRendererPosition;
        private SpriteRenderer _traceRenderer;
        private Transform _pictureDisplayer;

        private bool _hasProjection;
        private GameObject _projection; //DEBUG
        private Transform _grid;
        private Tilemap _ownTileMap;

        public event Action<Vector2> OnPictureTaken;

        public RenderTexture CurrentTexture;
        public RenderTexture CurrentTextureUncut;
        private Camera _renderCamera;

        private void Awake()
        {
            var go1 = new GameObject("TRACE_RENDERER");
            go1.layer = 3;
            _traceRenderer = go1.AddComponent<SpriteRenderer>();
            _traceRenderer.drawMode = SpriteDrawMode.Sliced;
            _traceRenderer.size = Size;
            _traceRenderer.sortingOrder = 1000;
            _traceRenderer.sprite = _cameraTraceSrite;
            go1.transform.SetParent(transform, false);

            var gridGo = new GameObject("CameraGrid");
            gridGo.layer = 3;
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            gridGo.transform.SetParent(transform, false);
            _grid = gridGo.transform;

            var go2 = new GameObject("TILEMAP");
            go2.layer = 3;
            _ownTileMap = go2.AddComponent<Tilemap>();
            var renderer = go2.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -1;

            _ownTileMap.color = new Color(1, 1, 1, 0.5f);
            _ownTileMap.transform.SetParent(gridGo.transform, false);

            var camGo = new GameObject("CAMERACAMERA");
            _renderCamera = camGo.AddComponent<Camera>();
            _renderCamera.enabled = false;

            var go3 = new GameObject("PICTURE_RENDERER");
            go3.layer = 3;
            go3.transform.SetParent(go1.transform, false);
            _pictureDisplayer = go3.transform;
            var render = go3.AddComponent<SpriteRenderer>();
            render.sprite = _pictureSprite;
            render.sortingOrder = 999;
            render.color = new Color(1, 1, 1, 0.8f);
            render.material = _pictureDisplayerMat;

            EnableTrace();
        }

        private void Update()
        {
            if (_traceEnabled)
            {
                _traceRenderer.size = Size;
                float delta = _traceElapsed / _traceMoveTime;
                _traceElapsed += Time.deltaTime;

                var newPosition = AllignToGrid(RawCameraCenter);
                _traceRenderer.transform.position = Vector3.Lerp(_prevRendererPosition, newPosition, delta * delta);
                if(Vector2.Distance(_traceRenderer.transform.position, newPosition) < 0.05f)
                {
                    _prevRendererPosition = newPosition;
                    _traceElapsed = 0f;
                }
                _grid.position = _traceRenderer.transform.position + new Vector3(_pictureSize.x % 2 == 0 ? 0f : -0.5f, _pictureSize.y % 2 == 0 ? 0f : -0.5f, 0);
                _pictureDisplayer.localScale = Size;
            }

            if (Input.GetKey(KeyCode.Mouse0))
            {
                TakePicture();
            }

            if(Input.GetKey(KeyCode.Mouse1) && _hasProjection)
            {
                if(_projection != null)
                {
                    Destroy(_projection.gameObject);
                }
                _projection = Project(AllignToGrid(RawCameraCenter));
            }

            LookDirection = ((Vector2)(Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position)).normalized;
        }

        public GameObject Project(Vector2 worldPosition)
        {
            Vector2 alligned = AllignToGrid(worldPosition) + new Vector2(_pictureSize.x % 2 == 0 ? 0f : -0.5f, _pictureSize.y % 2 == 0 ? 0f : -0.5f);

            var gridGo = new GameObject("PROJECTION");
            gridGo.AddComponent<Grid>().cellSize = Vector3.one;
            gridGo.transform.position = alligned;

            var tileMapGo = new GameObject("TILEMAP_PROJECTION");
            var tilemap = tileMapGo.AddComponent<Tilemap>();
            tilemap.color = Color.yellow;
            tilemap.transform.SetParent(gridGo.transform, false);

            Vector2Int ssize = _pictureSize / 2;

            BoundsInt bounds = new BoundsInt(-ssize.x, -ssize.y, 0, _pictureSize.x, _pictureSize.y, 1);
            TileBase[] tiles = _ownTileMap.GetTilesBlock(bounds);
            tilemap.SetTilesBlock(bounds, tiles);
            tilemap.RefreshAllTiles();

            tilemap.gameObject.AddComponent<TilemapRenderer>();
            tilemap.gameObject.AddComponent<TilemapCollider2D>().ProcessTilemapChanges();

            var pictureGo = new GameObject("PICTURE_DISPLAYER");
            pictureGo.transform.SetParent(gridGo.transform, false);
            pictureGo.transform.localScale = Size;
            pictureGo.transform.localPosition = new Vector3(_pictureSize.x % 2 == 0 ? 0f : 0.5f, _pictureSize.y % 2 == 0 ? 0f : 0.5f, 0);

            var render = pictureGo.AddComponent<SpriteRenderer>();
            render.sprite = _pictureSprite;
            render.material = _pictureDisplayerProjectionMat;
            render.sortingOrder = -5;
            render.color = Color.yellow;

            pictureGo.AddComponent<LocalPictureDisplayer>().Init(CurrentTexture, render);

            return gridGo;
        }

        public void TakePicture()
        {
            var newPosition = AllignToGrid(RawCameraCenter);
            var overlap = Physics2D.OverlapArea(newPosition + Size / 2, newPosition - Size / 2, new ContactFilter2D() { layerMask = _pictureMask }, _cached);

            for(int i = 0; i < overlap; i++)
            {
                if (_cached[i].gameObject.TryGetComponent<IReactToPicture>(out var comp))
                {
                    comp.ReactToPicture(transform.position);
                }

                if (_cached[i].gameObject.TryGetComponent<Tilemap>(out var tilemap))
                {
                    Vector2Int ssize = _pictureSize / 2;

                    Vector3Int center = tilemap.WorldToCell(newPosition);

                    BoundsInt bounds = new BoundsInt(center.x - ssize.x, center.y - ssize.y, 0, _pictureSize.x, _pictureSize.y, 1);

                    TileBase[] tiles = tilemap.GetTilesBlock(bounds);

                    _ownTileMap.SetTilesBlock(new BoundsInt(-ssize.x, -ssize.y, 0, _pictureSize.x, _pictureSize.y, 1), tiles);
                    _ownTileMap.RefreshAllTiles();
                }
            }

            _hasProjection = true;

            Camera target = Camera.main;
            _renderCamera.CopyFrom(target);
            _renderCamera.cullingMask = _renderMask;
            _renderCamera.transform.position = Camera.main.transform.position;

            if(CurrentTextureUncut == null || CurrentTextureUncut.width != target.pixelWidth || CurrentTextureUncut.height != target.pixelHeight)
            {
                if(CurrentTextureUncut != null)
                {
                    CurrentTexture.Release();
                }

                CurrentTextureUncut = new RenderTexture(target.pixelWidth, target.pixelHeight, 0, RenderTextureFormat.DefaultHDR);
            }

            _renderCamera.targetTexture = CurrentTextureUncut;
            _renderCamera.Render();

            Vector3 left = _renderCamera.WorldToViewportPoint(newPosition - Size / 2);
            Vector3 right = _renderCamera.WorldToViewportPoint(newPosition + Size / 2);

            int sizeX = Mathf.RoundToInt((right.x - left.x) * CurrentTextureUncut.width);
            int sizeY = Mathf.RoundToInt((right.y - left.y) * CurrentTextureUncut.height);
            Vector2Int size = new Vector2Int(sizeX, sizeY);

            Vector2Int pos = new Vector2Int(Mathf.RoundToInt(left.x * CurrentTextureUncut.width), Mathf.RoundToInt(left.y * CurrentTextureUncut.height));

            RectInt rect = new RectInt(pos, size);

            if(CurrentTexture == null || CurrentTexture.width != size.x || CurrentTexture.height != size.y)
            {
                if (CurrentTexture != null)
                {
                    CurrentTexture.Release();
                }

                CurrentTexture = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.DefaultHDR);
            }

            Graphics.CopyTexture(CurrentTextureUncut, 0, 0, rect.x, rect.y, rect.width, rect.height, CurrentTexture, 0, 0, 0, 0);
            Shader.SetGlobalTexture("_CurrentPicture", CurrentTexture);

            OnPictureTaken?.Invoke(newPosition);
        }

        public void EnableTrace()
        {
            _traceEnabled = true;
            _traceRenderer.gameObject.SetActive(true);
            _grid.gameObject.SetActive(true);
            _prevRendererPosition = AllignToGrid(RawCameraCenter);
        }

        public void DisableTrace()
        {
            _traceEnabled = false;
            _traceRenderer.gameObject.SetActive(false);
            _grid.gameObject.SetActive(false);
        }

        private Vector2 AllignToGrid(Vector2 worldPosition)
        {
            return new Vector2(
                (int)(worldPosition.x) + (Size.x % 2 == 0 ? 0 : -0.5f), 
                (int)(worldPosition.y) + (Size.y % 2 == 0 ? 0 : -0.5f));
        }

        private Vector2 Size => new Vector2(_pictureSize.x, _pictureSize.y);
        private Vector3 RawCameraCenter => transform.position + new Vector3(LookDirection.x * Size.x / 2, LookDirection.y * Size.y / 2) + (Vector3)LookDirection * _maxOffset;
    }
}
