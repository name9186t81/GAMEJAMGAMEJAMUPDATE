using Rendering;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Architecture
{
    public class MapRenderer : MonoSingletone<MapRenderer>
    {
        [SerializeField] private GameObject _disableDuringRendering;
        [SerializeField] private string _name;
        [SerializeField] private Tilemap _mainTileMap;

        [SerializeField] private int _maxOffset;
        [SerializeField] private bool _offsetToCenter;
        [SerializeField] private float _offsetAngle;

        [SerializeField] private Color _endOffsetColor;
        [SerializeField, Range(0, 1f)] private float _maxEndColorMerge;

        [SerializeField, Range(0, 1f)] private float _minHeight;

        [SerializeField] private int _seed;
        [SerializeField] private int _maxDrainPixels;

        public const int PPU = 16;

        public void BeignRendering(string mapName, Tilemap mainTilemap)
        {
            StartCoroutine(Rendering(mapName, mainTilemap));

        }

        private IEnumerator Rendering(string mapName, Tilemap mainTilemap)
        {
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Maps")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Maps"));
            }

            _disableDuringRendering.SetActive(false);
            var parts = FindObjectsByType<RenderingParticipant>(FindObjectsSortMode.None);

            GameObject camGo = new GameObject();
            var cam = camGo.AddComponent<Camera>();
            cam.CopyFrom(Camera.main);

            _mainTileMap.CompressBounds();

            var localBounds = mainTilemap.localBounds;

            Vector2 min = mainTilemap.transform.TransformPoint(localBounds.min);
            Vector2 max = mainTilemap.transform.TransformPoint(localBounds.max);

            int pMinX = Mathf.RoundToInt(min.x * PPU);
            int pMaxX = Mathf.RoundToInt(max.x * PPU);
            int pMinY = Mathf.RoundToInt(min.y * PPU);
            int pMaxY = Mathf.RoundToInt(max.y * PPU);

            int pWidth = pMaxX - pMinX;
            int pHeight = pMaxY - pMinY;

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.orthographicSize = pHeight / (2 * PPU) + 0.5f;
            cam.transform.position = new Vector3((pMaxX + pMinX) * 0.5f / PPU, (pMaxY + pMinY) * 0.5f / PPU, -10f);

            RenderTexture rt = new RenderTexture(pWidth, pHeight, 24);
            rt.filterMode = FilterMode.Point;

            foreach (var part in parts)
            {
                part.SetDrawMode(RenderingParticipant.DrawMode.Albedo);
            }
            Shader.SetGlobalFloat("_Mode1", 0);
            Shader.SetGlobalFloat("_Mode2", 0);
            cam.targetTexture = rt;

            cam.aspect = (float)pWidth / pHeight;
            cam.ResetProjectionMatrix();
            cam.ResetWorldToCameraMatrix();

            yield return new WaitForEndOfFrame();
            cam.Render();

            var albedoMap = EncodeRenderResult(rt, mapName + "_albedo");

            foreach (var part in parts)
            {
                part.SetDrawMode(RenderingParticipant.DrawMode.Height);
            }
            Shader.SetGlobalFloat("_Mode1", 1);
            Shader.SetGlobalFloat("_Mode2", 0);
            cam.targetTexture = rt;

            yield return new WaitForEndOfFrame();
            cam.Render();

            var heightMap = EncodeRenderResult(rt, mapName + "_height");

            foreach (var part in parts)
            {
                part.SetDrawMode(RenderingParticipant.DrawMode.Normal);
            }
            Shader.SetGlobalFloat("_Mode1", 0);
            Shader.SetGlobalFloat("_Mode2", 1);
            cam.targetTexture = rt;

            yield return new WaitForEndOfFrame();
            cam.Render();

            var normalMap =  EncodeRenderResult(rt, mapName + "_normal");

            foreach (var part in parts)
            {
                part.SetDrawMode(RenderingParticipant.DrawMode.Albedo);
            }

            Shader.SetGlobalFloat("_Mode1", 0);
            Shader.SetGlobalFloat("_Mode2", 0);
            rt.Release();
            Destroy(cam.gameObject);


            var ignored = OffsetPixels(albedoMap, normalMap, heightMap, out Texture2D newA, out Texture2D newN, out Texture2D newH);
            DrainPixels(newA, newN, newH, ignored, out newA, out newN, out newH);

            SaveTexture(newA, _name + "_albedoProcessed");
            SaveTexture(newN, _name + "_normalProcessed");
            SaveTexture(newH, _name + "_heightProcessed");

            AssetDatabase.Refresh();
            _disableDuringRendering.SetActive(true);
        }

        private Texture2D EncodeRenderResult(RenderTexture result, string name)
        {
            RenderTexture prev = RenderTexture.active;

            RenderTexture.active = result;

            Texture2D tex = new Texture2D(result.width, result.height);
            tex.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;

            SaveTexture(tex, name);

            return tex;
        }

        private void SaveTexture(Texture2D tex, string name)
        {
            byte[] bytes = tex.EncodeToPNG();
            string filePath = Path.Combine(Application.dataPath, "Maps", name + ".png");

            File.WriteAllBytes(filePath, bytes);
        }

        private HashSet<int> OffsetPixels(Texture2D albedo, Texture2D normal, Texture2D heightMap, out Texture2D newTexAlbedo, out Texture2D newTexNormal, out Texture2D newTexHeight)
        {
            int width = albedo.width;
            int height = albedo.height;

            Texture2D nTexAlbedo = new Texture2D(albedo.width, albedo.height);
            Texture2D nTexNormal = new Texture2D(albedo.width, albedo.height);
            Texture2D nTexHeight = new Texture2D(albedo.width, albedo.height);

            HashSet<int> totalVisited = new HashSet<int>();
            HashSet<int> ignored = new HashSet<int>();
            const float MIN_HEIGHT = 0.6f;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = heightMap.GetPixel(x, y);
                    var pixelAlbedo = albedo.GetPixel(x, y);
                    var pixelNormal = normal.GetPixel(x, y);

                    nTexAlbedo.SetPixel(x, y, pixelAlbedo);
                    nTexNormal.SetPixel(x, y, pixelNormal);
                    nTexHeight.SetPixel(x, y, pixel);
                }
            }

            float[] cachedDts = new float[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = heightMap.GetPixel(x, y);
                    var pixelAlbedo = albedo.GetPixel(x, y);
                    var pixelNormal = normal.GetPixel(x, y);

                    if (pixel.r >= 0.9f)
                    {
                        continue;
                    }

                    Vector2 dir = _offsetToCenter ? (new Vector2(width / 2 - x, height / 2 - y)).normalized : new Vector2(Mathf.Cos(_offsetAngle * Mathf.Deg2Rad), Mathf.Sin(_offsetAngle * Mathf.Deg2Rad));

                    List<(int, int)> pixelsList = new List<(int, int)>();
                    HashSet<int> visitedPixels = new HashSet<int>();
                    int i = 0;

                    float stepDelta = 1f / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                    while (visitedPixels.Count < _maxOffset + 1)
                    {
                        i++;

                        if (i > _maxOffset * 2 + 16)
                        {
                            Debug.Log("OVERFLOW");
                            break;
                        }

                        Vector2 point = -dir * (stepDelta * i);

                        int pX = x + Mathf.RoundToInt(point.x);
                        int pY = y + Mathf.RoundToInt(point.y);


                        if (pX < 0 || pX >= width || pY < 0 || pY >= height)
                        {
                            break;
                        }

                        if (!visitedPixels.Contains(pX + pY * width)) visitedPixels.Add(pX + pY * width);
                        else continue;

                        var nPixel = heightMap.GetPixel(pX, pY);
                        if (nPixel.r >= MIN_HEIGHT)
                        {
                            for (int j = 0; j < pixelsList.Count; j++)
                            {
                                int tx = pixelsList[j].Item1;
                                int ty = pixelsList[j].Item2;

                                float dt = (j + 1) / (float)(_maxOffset + 1);
                                if (dt <= cachedDts[tx + ty * width]) continue;
                                cachedDts[tx + ty * width] = dt;

                                var albedoLerp = Color.Lerp(pixelAlbedo, Color.Lerp(pixelAlbedo, _endOffsetColor, _maxEndColorMerge), 1 - dt);
                                nTexAlbedo.SetPixel(tx, ty, new Color(albedoLerp.r, albedoLerp.g, albedoLerp.b, 1));
                                nTexNormal.SetPixel(tx, ty, pixelNormal);
                                var heightLerp = Color.Lerp(Color.red * nPixel, Color.red * _minHeight * Mathf.InverseLerp(0.6f, 0.9f, nPixel.r), 1 - dt);
                                nTexHeight.SetPixel(tx, ty, new Color(heightLerp.r, 0, 0, 1));

                                ignored.Add(tx + ty * width);
                            }
                            break;
                        }
                        else
                        {
                            pixelsList.Add((pX, pY));
                        }
                    }
                }
            }

            newTexAlbedo = nTexAlbedo;
            newTexNormal = nTexNormal;
            newTexHeight = nTexHeight;

            return ignored;
        }

        private void DrainPixels(Texture2D albedo, Texture2D normal, Texture2D heightMap, HashSet<int> ignoredForHeightCheck, out Texture2D newTexAlbedo, out Texture2D newTexNormal, out Texture2D newTexHeight)
        {
            var state = Random.state;
            int width = albedo.width;
            int height = albedo.height;

            Texture2D nTexAlbedo = new Texture2D(albedo.width, albedo.height);
            Texture2D nTexNormal = new Texture2D(albedo.width, albedo.height);
            Texture2D nTexHeight = new Texture2D(albedo.width, albedo.height);
            const float MIN_HEIGHT = 0.6f;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = heightMap.GetPixel(x, y);
                    var pixelAlbedo = albedo.GetPixel(x, y);
                    var pixelNormal = normal.GetPixel(x, y);

                    nTexAlbedo.SetPixel(x, y, pixelAlbedo);
                    nTexNormal.SetPixel(x, y, pixelNormal);
                    nTexHeight.SetPixel(x, y, pixel);
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    var pixel = heightMap.GetPixel(x, y);
                    var pixelAlbedo = albedo.GetPixel(x, y);
                    var pixelNormal = normal.GetPixel(x, y);

                    if (pixel.r >= MIN_HEIGHT)
                    {
                        continue;
                    }

                    Random.InitState(_seed + x * 10 + y % _maxDrainPixels);
                    float rnd = Random.value;
                    int len = (int)(_maxDrainPixels * rnd);
                    List<(int, int)> visited = new List<(int, int)>();
                    int i = 0;

                    while (i < len)
                    {
                        i++;

                        int pX = x;
                        int pY = y + i;


                        if (pX < 0 || pX >= width || pY < 0 || pY >= height) break;

                        var nPixel = heightMap.GetPixel(pX, pY);
                        const float THRESHOLD = 0.11f;
                        if((nPixel.r - pixel.r) > THRESHOLD && !ignoredForHeightCheck.Contains(pX + pY * width))
                        {
                            var nPixelAlbedo = albedo.GetPixel(pX, pY);
                            var nPixelNormal = normal.GetPixel(pX, pY);
                            for (int j = 0; j < visited.Count; j++)
                            {
                                float dt = (float)j / len;

                                var albedoLerp = Color.Lerp(nPixelAlbedo, Color.Lerp(nPixelAlbedo, _endOffsetColor, _maxEndColorMerge), 1 - dt);
                                nTexAlbedo.SetPixel(visited[j].Item1, visited[j].Item2, new Color(albedoLerp.r, albedoLerp.g, albedoLerp.b, 1));
                                nTexNormal.SetPixel(visited[j].Item1, visited[j].Item2, nPixelNormal);
                                var heightLerp = Color.Lerp(Color.red * nPixel, Color.red * _minHeight * Mathf.InverseLerp(MIN_HEIGHT, 0.9f, nPixel.r), 1 - dt);

                                nTexHeight.SetPixel(visited[j].Item1, visited[j].Item2, new Color(heightLerp.r, 0, 0, 1));
                            }
                            break;
                        }
                        else
                        {
                            visited.Add((pX, pY));
                        }
                    }
                }
            }

            Random.state = state;
            newTexAlbedo = nTexAlbedo;
            newTexNormal = nTexNormal;
            newTexHeight = nTexHeight;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                BeignRendering(_name, _mainTileMap);
            }
        }
#endif

        protected override MapRenderer GetInstance()
        {
            return this;
        }
    }
}
