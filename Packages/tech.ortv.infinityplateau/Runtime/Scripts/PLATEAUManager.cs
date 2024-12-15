using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using UnityEngine.Networking;
using Draco;
using System.Threading.Tasks;

namespace InfinityPLATEAU
{
    public class PLATEAUManager : MonoBehaviour
    {
        [SerializeField]
        private double lat;
        [SerializeField]
        private double lon;

        [SerializeField]
        private Material material;

        [SerializeField]
        private int range = 1;

        private static double centerLat;
        private static double centerLon;

        private const string baseURL = "https://pub-f8a6dfa3b7f74a50b4a23fd5a29f666b.r2.dev/drc";

        private long currentSpatialCode = 0;
        private Dictionary<long, bool> spatialGrids = new Dictionary<long, bool>();

        private Queue<long> downloadQueue = new Queue<long>();

        // Start is called before the first frame update
        void Awake()
        {
            SetCurrentPosition(lon, lat);
            DownloadAndInstantiate();
        }

        async void DownloadAndInstantiate()
        {
            // Infinite loop for watch queue and download, instantiate
            while (true)
            {
                await Task.Delay(100);

                long code;
                if (!downloadQueue.TryDequeue(out code))
                {
                    // no item
                    continue;
                }

                bool grid;
                if (spatialGrids.TryGetValue(code, out grid))
                {
                    // Item processing or done
                    continue;
                }

                // Add Dic to prevent dupe download
                spatialGrids.Add(code, true);

                (int x, int y) = SpatialCode.FromSpatialCode(code);

                int x0 = x / 1000;
                int x1 = (x / 10) % 100;
                int y0 = y / 1000;
                int y1 = (y / 10) % 100;

                string dracoDLURL = $"{baseURL}/{x0}/{y0}/{x1}/{y1}/{x}_{y}.draco";

                Debug.Log("DracoDL:" + dracoDLURL);

                byte[] dracoData = await DownloadDraco(new Uri(dracoDLURL));

                if (dracoData == null) continue;

                Debug.Log("Draco Data Success");

                var draco = new DracoMeshLoader();
                var mesh = await draco.ConvertDracoMeshToUnity(dracoData);
                if (mesh != null)
                {
                    Debug.Log("Draco Mesh Success");

                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    GameObject go = new GameObject();
                    var meshFilter = go.AddComponent<MeshFilter>();
                    meshFilter.mesh = mesh;
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;
                    meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                    // タイルの中心座標を計算
                    (var lon, var lat) = SpatialCode.tile2deg(2 * x + 1, 2 * y + 1, 17);
                    Debug.Log($"{lon} {lat}");

                    (var ox, var oy, var oz) = CoordConv.CalcOffset(lat, lon, centerLat, centerLon);

                    go.SetActive(true);
                    go.transform.parent = this.transform;
                    go.transform.localPosition = new Vector3(-(float)oy, (float)ox, -(float)oz);
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = new Vector3(1, 1, -1);
                }
            }
        }

        public static Vector3 CalcOffsetPos(double lat, double lon)
        {
            (var ox, var oy, var oz) = CoordConv.CalcOffset(lat, lon, centerLat, centerLon);
            return new Vector3(-(float)oy, (float)ox, -(float)oz);
        }

        public void SetCurrentPosition(double lon, double lat)
        {
            (var x, var y, var z) = SpatialCode.deg2tile(lon, lat, 16);
            long spatialCode = SpatialCode.ToSpatialCode(x, y);

            (var tilelon, var tilelat) = SpatialCode.tile2deg(2 * x + 1, 2 * y + 1, 17);

            centerLat = tilelat; centerLon = tilelon;

            if (currentSpatialCode == spatialCode)
            {
                return;
            }

            // EnQueue around current position
            for (int i = -range; i <= range; i++)
            {
                for (int j = -range; j <= range; j++)
                {
                    downloadQueue.Enqueue(SpatialCode.ToSpatialCode(x + i, y + j));
                }
            }

            currentSpatialCode = spatialCode;
        }

        private async Task<byte[]> DownloadDraco(Uri uri)
        {
            UnityWebRequest req = UnityWebRequest.Get(uri);
            req.downloadHandler = new DownloadHandlerBuffer();

            await req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(req.error);
                return null;
            }
            else
            {
                return req.downloadHandler.data;
            }
        }
    }
}
