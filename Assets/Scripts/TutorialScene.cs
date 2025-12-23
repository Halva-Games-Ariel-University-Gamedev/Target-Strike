using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialScene : MonoBehaviour
{
    [SerializeField]
    private RawImage imageUI;
    [SerializeField]
    private TMP_Text textUI;

    [Header("Typewriter")]
    [SerializeField]
    private float charsPerSecond = 35f;
    [SerializeField]
    private bool richText = true;

    void Start()
    {
        var mission = MissionsMenu.Instance.CurrentMission;
        if (mission == null) return;

        imageUI.texture = mission.prefaceImage;

        var pages = new List<string>();

        if (!string.IsNullOrWhiteSpace(mission.prefaceTitle))
            pages.Add(mission.prefaceTitle);

        // Change .text to whatever your TargetHint uses
        foreach (var m in mission.msgs)
            pages.Add(m.text);

        if (pages.Count == 0) pages.Add("...");

        StartCoroutine(RunPages(pages));
    }

    IEnumerator RunPages(List<string> pages)
    {
        for (int i = 0; i < pages.Count; i++)
            yield return StartCoroutine(ShowPage(pages[i]));

        SceneManager.LoadScene("AttackerSchene");

        // optional: close tutorial / load next scene here
    }

    bool EnterDown() =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

    bool EnterHeld() =>
        Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);

    IEnumerator ShowPage(string fullText)
    {
        // Start with Enter released so a held key doesn't auto-skip
        while (EnterHeld()) yield return null;

        textUI.text = "";
        float secondsPerChar = 1f / Mathf.Max(1f, charsPerSecond);

        int idx = 0;

        // TYPE PHASE (poll input EVERY FRAME)
        while (idx < fullText.Length)
        {
            // If Enter pressed while typing => complete current message
            if (EnterDown())
            {
                textUI.text = fullText;
                while (EnterHeld()) yield return null; // consume that press
                break;
            }

            // Rich text: append tags instantly
            if (richText && fullText[idx] == '<')
            {
                int close = fullText.IndexOf('>', idx);
                if (close == -1) close = idx;
                textUI.text += fullText.Substring(idx, close - idx + 1);
                idx = close + 1;
                continue;
            }

            // Add next visible character
            textUI.text += fullText[idx];
            idx++;

            // Wait secondsPerChar, but keep polling Enter each frame
            float t = 0f;
            while (t < secondsPerChar)
            {
                if (EnterDown())
                {
                    textUI.text = fullText;
                    while (EnterHeld()) yield return null; // consume
                    idx = fullText.Length; // end typing
                    break;
                }

                t += Time.unscaledDeltaTime; // works even if Time.timeScale = 0
                yield return null;
            }
        }

        // NEXT PHASE: only after fully shown, Enter goes to next msg
        while (EnterHeld()) yield return null;   // ensure released
        while (!EnterDown()) yield return null;  // wait for fresh press
        while (EnterHeld()) yield return null;   // consume so it doesn't affect next page
    }
}
