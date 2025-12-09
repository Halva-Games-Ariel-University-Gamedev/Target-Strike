using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialTextBox : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textLabel;     // TextMeshProUGUI on your text box
    public GameObject boxRoot;     // Panel / background object for the box

    private readonly List<string> _lines = new List<string>();
    private int _index = 0;

    void Awake()
    {
        // If not set, assume this object is the root
        if (!boxRoot)
        {
            boxRoot = gameObject;
        }
    }

    void Start()
    {
        // 1) Get messages from the current mission (its hints)
        var mission = MissionsMenu.Instance.CurrentMission;

        if (mission != null && mission.msgs != null && mission.msgs.Count > 0)
        {
            foreach (var h in mission.msgs)
            {
                if (!string.IsNullOrWhiteSpace(h.text))
                {
                    _lines.Add(h.text);
                }
            }
        }

        // If no messages, just hide the box
        if (_lines.Count == 0 || textLabel == null)
        {
            boxRoot.SetActive(false);
            return;
        }

        // Show first line
        _index = 0;
        boxRoot.SetActive(true);
        textLabel.text = _lines[_index];
    }

    void Update()
    {
        if (!boxRoot.activeSelf) return;

        // Enter or left mouse click
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    public void NextLine()
    {
        _index++;

        if (_index >= _lines.Count)
        {
            // No more lines → hide the box
            boxRoot.SetActive(false);
            return;
        }

        textLabel.text = _lines[_index];
    }
}
