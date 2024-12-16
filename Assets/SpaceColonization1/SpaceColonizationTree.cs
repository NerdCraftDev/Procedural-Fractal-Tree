using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SpaceColonizationTree : MonoBehaviour
{
    public int mapWidth = 256;
    public int mapHeight = 256;
    public float falloff = 0.1f;
    public List<Vector2> points = new List<Vector2>();
    public Texture2D heatmap;

    async void Start()
    {
        points.Add(new Vector2(128, 128));
        points.Add(new Vector2(64, 64));
        points.Add(new Vector2(192, 192));
        points.Add(new Vector2(64, 192));
        points.Add(new Vector2(192, 64));
        heatmap = await GenerateHeatmap(points, mapWidth, mapHeight, falloff);
        SaveTexture(heatmap);
    }

    private void SaveTexture(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/SpaceColonization1";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + "/Heatmap.png", bytes);
        #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    public async Task<Texture2D> GenerateHeatmap(List<Vector2> positions, int width, int height, float falloff)
    {
        // Create the Texture2D on the main thread
        Texture2D heatmap = new Texture2D(width, height);
        Color32[] pixels = new Color32[width * height];

        await Task.Run(() => 
        {
            List<Color32[]> positionPixels = new List<Color32[]>();
            // Calculate cutoff distance where intensity becomes negligible
            float cutoffDistance = -Mathf.Log(0.01f) / falloff;
            float cutoffDistanceSquared = cutoffDistance * cutoffDistance;

            // Process each attraction point
            Parallel.ForEach(positions, pos =>
            {
                Color32[] localPixels = new Color32[width * height];
                for (int i = 0; i < localPixels.Length; i++)
                {
                    localPixels[i] = new Color32(0, 0, 0, 0);
                }

                int startX = Mathf.Max(0, Mathf.FloorToInt(pos.x - cutoffDistance));
                int endX = Mathf.Min(width, Mathf.CeilToInt(pos.x + cutoffDistance));
                int startY = Mathf.Max(0, Mathf.FloorToInt(pos.y - cutoffDistance));
                int endY = Mathf.Min(height, Mathf.CeilToInt(pos.y + cutoffDistance));

                for (int x = startX; x < endX; x++)
                {
                    float dx = x - pos.x;
                    for (int y = startY; y < endY; y++)
                    {
                        float dy = y - pos.y;
                        float distanceSquared = dx * dx + dy * dy;
                        if (distanceSquared < cutoffDistanceSquared)
                        {
                            float intensity = Mathf.Exp(-falloff * Mathf.Sqrt(distanceSquared));
                            int index = y * width + x;
                            localPixels[index].r = (byte)Mathf.Clamp(localPixels[index].r + intensity * 255, 0, 255);
                            localPixels[index].g = (byte)Mathf.Clamp(localPixels[index].g + intensity * 255, 0, 255);
                            localPixels[index].b = (byte)Mathf.Clamp(localPixels[index].b + intensity * 255, 0, 255);
                            localPixels[index].a = 255;
                        }
                    }
                }

                lock (positionPixels)
                {
                    positionPixels.Add(localPixels);
                }
            });

            // Combine all local pixels into the final pixel array
            foreach (var localPixels in positionPixels)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].r = (byte)Mathf.Clamp(pixels[i].r + localPixels[i].r, 0, 255);
                    pixels[i].g = (byte)Mathf.Clamp(pixels[i].g + localPixels[i].g, 0, 255);
                    pixels[i].b = (byte)Mathf.Clamp(pixels[i].b + localPixels[i].b, 0, 255);
                    pixels[i].a = 255;
                }
            }
        });

        // Apply the pixels to the heatmap on the main thread
        heatmap.SetPixels32(pixels);
        heatmap.Apply();

        return heatmap;
    }

    public Vector2 GetWeightedHeatmapPosition(Texture2D heatmap)
    {
        // Get all pixels at once
        Color32[] pixels = heatmap.GetPixels32();
        float totalIntensity = 0;
        
        // Calculate total intensity
        for (int i = 0; i < pixels.Length; i++)
        {
            totalIntensity += pixels[i].r;
        }

        // Pick random value
        float randomValue = Random.value * totalIntensity;
        float currentIntensity = 0;
        
        // Find selected position
        for (int i = 0; i < pixels.Length; i++)
        {
            currentIntensity += pixels[i].r;
            if (currentIntensity >= randomValue)
            {
                // Convert 1D index back to 2D coordinates
                int x = i % heatmap.width;
                int y = i / heatmap.width;
                return new Vector2(x, y);
            }
        }

        return Vector2.zero;
    }
}