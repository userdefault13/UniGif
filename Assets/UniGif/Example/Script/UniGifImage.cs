// Path: Assets/UniGif/Assets/UniGif/Example/Script/UniGifImage.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UniGifImage : MonoBehaviour
{
    public enum State
    {
        None,
        Loading,
        Ready,
        Playing,
        Pause,
    }

    [SerializeField]
    private RawImage m_rawImage;
    [SerializeField]
    private UniGifImageAspectController m_imgAspectCtrl;
    [SerializeField]
    private FilterMode m_filterMode = FilterMode.Point;
    [SerializeField]
    private TextureWrapMode m_wrapMode = TextureWrapMode.Clamp;
    [SerializeField]
    private bool m_loadOnStart;
    [SerializeField]
    private bool m_rotateOnLoading;
    [SerializeField]
    private bool m_outputDebugLog;

    private List<UniGif.GifTexture> m_gifTextureList;
    private float m_delayTime;
    private int m_gifTextureIndex;
    private int m_nowLoopCount;

    public State nowState { get; private set; }
    public int loopCount { get; private set; }
    public int width { get; private set; }
    public int height { get; private set; }

    private void Start()
    {
        if (m_rawImage == null)
        {
            m_rawImage = GetComponent<RawImage>();
        }
        // LoadOnStart logic is removed
    }

    private void OnDestroy()
    {
        Clear();
    }

    private void Update()
    {
        switch (nowState)
        {
            case State.None:
                break;
            case State.Loading:
                if (m_rotateOnLoading)
                {
                    transform.Rotate(0f, 0f, 30f * Time.deltaTime, Space.Self);
                }
                break;
            case State.Ready:
                break;
            case State.Playing:
                if (m_rawImage == null || m_gifTextureList == null || m_gifTextureList.Count <= 0)
                {
                    return;
                }
                if (m_delayTime > Time.time)
                {
                    return;
                }
                m_gifTextureIndex++;
                if (m_gifTextureIndex >= m_gifTextureList.Count)
                {
                    m_gifTextureIndex = 0;
                    if (loopCount > 0)
                    {
                        m_nowLoopCount++;
                        if (m_nowLoopCount >= loopCount)
                        {
                            Stop();
                            return;
                        }
                    }
                }
                m_rawImage.texture = m_gifTextureList[m_gifTextureIndex].m_texture2d;
                m_delayTime = Time.time + m_gifTextureList[m_gifTextureIndex].m_delaySec;
                break;
            case State.Pause:
                break;
            default:
                break;
        }
    }

    public void SetGifFromFilePath(string filePath, bool autoPlay = true)
    {
        StartCoroutine(SetGifFromFilePathCoroutine(filePath, autoPlay));
    }

    public IEnumerator SetGifFromFilePathCoroutine(string filePath, bool autoPlay = true)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("File path is empty.");
            yield break;
        }

        if (nowState == State.Loading)
        {
            Debug.LogWarning("Already loading.");
            yield break;
        }
        nowState = State.Loading;

        byte[] fileData;

        Debug.Log($"Reading local file from path: {filePath}");

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            nowState = State.None;
            yield break;
        }

        try
        {
            fileData = File.ReadAllBytes(filePath);
            Debug.Log($"File read successfully: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error reading file: {filePath}\n{ex.Message}");
            nowState = State.None;
            yield break;
        }

        if (fileData == null || fileData.Length == 0)
        {
            Debug.LogError("File data is empty.");
            nowState = State.None;
            yield break;
        }

        Debug.Log($"File data length: {fileData.Length}");

        // Check the first few bytes for the GIF header
        if (!IsGifHeaderValid(fileData))
        {
            Debug.LogError("This is not a valid GIF image.");
            nowState = State.None;
            yield break;
        }

        Clear();
        nowState = State.Loading;

        yield return StartCoroutine(UniGif.GetTextureListCoroutine(fileData, (gifTexList, loopCount, width, height) =>
        {
            if (gifTexList != null)
            {
                m_gifTextureList = gifTexList;
                this.loopCount = loopCount;
                this.width = width;
                this.height = height;
                nowState = State.Ready;

                m_imgAspectCtrl.FixAspectRatio(width, height);

                if (m_rotateOnLoading)
                {
                    transform.localEulerAngles = Vector3.zero;
                }

                if (autoPlay)
                {
                    Play();
                }
            }
            else
            {
                Debug.LogError("Gif texture get error.");
                nowState = State.None;
            }
        },
        m_filterMode, m_wrapMode, m_outputDebugLog));
    }

    private bool IsGifHeaderValid(byte[] data)
    {
        if (data.Length < 6)
        {
            return false;
        }

        string header = System.Text.Encoding.ASCII.GetString(data, 0, 6);
        return header == "GIF87a" || header == "GIF89a";
    }

    public void SetGifTextures(List<UniGif.GifTexture> textures)
    {
        m_gifTextureList = textures;
        m_gifTextureIndex = 0;
        m_nowLoopCount = 0;

        if (m_gifTextureList != null && m_gifTextureList.Count > 0)
        {
            m_rawImage.texture = m_gifTextureList[0].m_texture2d;
            m_delayTime = Time.time + m_gifTextureList[0].m_delaySec;
            nowState = State.Ready;

            if (m_imgAspectCtrl != null)
            {
                m_imgAspectCtrl.FixAspectRatio(m_gifTextureList[0].m_texture2d.width, m_gifTextureList[0].m_texture2d.height);
            }
        }
    }

    public void Clear()
    {
        if (m_gifTextureList != null)
        {
            foreach (var gifTex in m_gifTextureList)
            {
                if (gifTex != null && gifTex.m_texture2d != null)
                {
                    Destroy(gifTex.m_texture2d);
                }
            }
            m_gifTextureList.Clear();
        }
        nowState = State.None;
        m_rawImage.texture = null;
        m_gifTextureIndex = 0;
        m_nowLoopCount = 0;
        loopCount = 0;
        width = 0;
        height = 0;
    }

    public void Play()
    {
        if (nowState == State.Ready || nowState == State.Pause)
        {
            nowState = State.Playing;
        }
    }

    public void Stop()
    {
        if (nowState == State.Playing)
        {
            nowState = State.Pause;
        }
    }
}
