using System;
using System.Threading.Tasks;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("awaked");

        // Force the cursor to be usable in the menu
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;

        if (Time.timeScale != 1f)
            Time.timeScale = 1f;
    }
}
