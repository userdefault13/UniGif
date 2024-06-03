/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UniGifTest : MonoBehaviour
{
    [SerializeField]
    private InputField m_inputField;
    [SerializeField]
    private UniGifImage m_uniGifImage;

    private bool m_mutex;

    public void OnButtonClicked()
    {
        if (m_mutex || m_uniGifImage == null || string.IsNullOrEmpty(m_inputField.text))
        {
            Debug.LogError("Mutex is active, UniGifImage is null, or input field is empty.");
            return;
        }

        m_mutex = true;
        StartCoroutine(ViewGifCoroutine());
    }

    private IEnumerator ViewGifCoroutine()
    {
        string filePath = m_inputField.text;

        // Log the file path to help with debugging
        Debug.Log($"Attempting to load GIF from file path: {filePath}");

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("The file path from InputField is empty.");
            m_mutex = false;
            yield break;
        }

        yield return StartCoroutine(m_uniGifImage.SetGifFromFilePathCoroutine(filePath));
        m_mutex = false;
    }
}
