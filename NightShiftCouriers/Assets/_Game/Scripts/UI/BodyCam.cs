using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Every courier's body cam (plan 4.11). Keeps a rolling buffer of small snapshots (the last ~20 seconds).
// When something big happens, the frames around it are kept as a clip. After the shift the best clips
// are shown on the results screen and can be saved as PNG images. (Snapshots first; video can come later.)
// Only the owner runs this (NetworkPlayer enables it).
public class BodyCam : MonoBehaviour
{
    public class Clip
    {
        public string label;
        public int score;
        public readonly List<Texture2D> frames = new List<Texture2D>();
        public int framesStillToAdd;
    }

    [SerializeField] private Camera sourceCamera;
    [SerializeField] private int width = 320;
    [SerializeField] private int height = 180;
    [SerializeField, Tooltip("Seconds between snapshots.")]
    private float interval = 0.5f;
    [SerializeField, Tooltip("Snapshots kept in the rolling buffer (40 x 0.5 s = 20 s).")]
    private int bufferFrames = 40;
    [SerializeField] private int framesBefore = 8;
    [SerializeField] private int framesAfter = 6;
    [SerializeField] private int maxClips = 6;

    private static readonly List<Clip> clips = new List<Clip>();

    public static BodyCam Local { get; private set; }
    public static IReadOnlyList<Clip> Clips => clips;

    private RenderTexture target;
    private Texture2D[] ring;
    private int head;
    private int filled;
    private float timer;

    private void OnEnable()
    {
        Local = this;
        target = new RenderTexture(width, height, 24);
        ring = new Texture2D[bufferFrames];
        for (int i = 0; i < bufferFrames; i++)
        {
            ring[i] = new Texture2D(width, height, TextureFormat.RGB24, false);
        }
        head = 0;
        filled = 0;
    }

    private void OnDisable()
    {
        if (Local == this)
        {
            Local = null;
        }
        if (target != null)
        {
            target.Release();
            target = null;
        }
        if (ring != null)
        {
            foreach (Texture2D frame in ring)
            {
                Destroy(frame);
            }
            ring = null;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval || sourceCamera == null)
        {
            return;
        }
        timer = 0f;

        Texture2D frame = Capture();
        foreach (Clip clip in clips)
        {
            if (clip.framesStillToAdd > 0)
            {
                clip.frames.Add(CopyOf(frame));
                clip.framesStillToAdd--;
            }
        }
    }

    // Renders the player's view into the small texture and stores it in the ring buffer.
    private Texture2D Capture()
    {
        RenderTexture previousTarget = sourceCamera.targetTexture;
        sourceCamera.targetTexture = target;
        sourceCamera.Render();
        sourceCamera.targetTexture = previousTarget;

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D frame = ring[head];
        frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        frame.Apply();
        RenderTexture.active = previousActive;

        head = (head + 1) % ring.Length;
        filled = Mathf.Min(filled + 1, ring.Length);
        return frame;
    }

    public void AddMoment(string label, int score)
    {
        if (filled == 0)
        {
            return;
        }

        Clip clip = new Clip { label = label, score = score, framesStillToAdd = framesAfter };
        int count = Mathf.Min(framesBefore, filled);
        for (int i = count; i >= 1; i--)
        {
            int index = (head - i + ring.Length) % ring.Length;
            clip.frames.Add(CopyOf(ring[index]));
        }
        clips.Add(clip);

        // Keep only the best clips.
        while (clips.Count > maxClips)
        {
            Clip worst = clips[0];
            foreach (Clip candidate in clips)
            {
                if (candidate.score < worst.score)
                {
                    worst = candidate;
                }
            }
            DestroyClip(worst);
            clips.Remove(worst);
        }
    }

    private Texture2D CopyOf(Texture2D source)
    {
        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        copy.SetPixels32(source.GetPixels32());
        copy.Apply();
        return copy;
    }

    // The best clips first.
    public static List<Clip> TopClips(int count)
    {
        List<Clip> sorted = new List<Clip>(clips);
        sorted.Sort((a, b) => b.score.CompareTo(a.score));
        if (sorted.Count > count)
        {
            sorted.RemoveRange(count, sorted.Count - count);
        }
        return sorted;
    }

    public static void ClearClips()
    {
        foreach (Clip clip in clips)
        {
            DestroyClip(clip);
        }
        clips.Clear();
    }

    private static void DestroyClip(Clip clip)
    {
        foreach (Texture2D frame in clip.frames)
        {
            Destroy(frame);
        }
    }

    // Saves every kept clip as numbered PNG images. Returns the folder.
    public static string SaveAll()
    {
        string folder = Path.Combine(Application.persistentDataPath, "BodyCam", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(folder);

        int clipNumber = 1;
        foreach (Clip clip in TopClips(clips.Count))
        {
            string safeLabel = clip.label.Replace(" ", "_").Replace("!", "");
            for (int i = 0; i < clip.frames.Count; i++)
            {
                string file = Path.Combine(folder, "clip" + clipNumber + "_" + safeLabel + "_" + i.ToString("00") + ".png");
                File.WriteAllBytes(file, clip.frames[i].EncodeToPNG());
            }
            clipNumber++;
        }
        return folder;
    }
}
