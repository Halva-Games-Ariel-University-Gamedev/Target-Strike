using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmFireDialog : MonoBehaviour
{
    [Header("UI")]
    public Button yesButton;
    public Button noButton;

    [Header("References")]
    public DroneController drone;   // drag your DroneController here

    private BuildingInfo _currentTarget;
    private CarInfo _currentCarTarget;

    void Awake()
    {
        // dialog should start hidden
        gameObject.SetActive(false);

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesClicked);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoClicked);
    }

    // called from DroneController when a building is clicked
    public void Show(BuildingInfo target)
    {
        _currentCarTarget = null;
        _currentTarget = null;
        _currentTarget = target;
        gameObject.SetActive(true);

        if (drone != null)
            drone.SetInputEnabled(false);  // stop drone movement + unlock cursor
    }

    public void Show(CarInfo target)
    {
        _currentCarTarget = null;
        _currentTarget = null;
        _currentCarTarget = target;
        gameObject.SetActive(true);

        if (drone != null)
            drone.SetInputEnabled(false);  // stop drone movement + unlock cursor
    }

    void Close()
    {
        gameObject.SetActive(false);
        _currentCarTarget = null;
        _currentTarget = null;

        if (drone != null)
            drone.SetInputEnabled(true);   // re-enable movement + lock cursor
    }

    void OnYesClicked()
    {
        if (drone != null && _currentTarget != null)
        {
            drone.ConfirmFire(_currentTarget);
        }
        else if (drone != null && _currentCarTarget != null)
        {
            drone.ConfirmFire(_currentCarTarget);
        }
        Close();
    }

    void OnNoClicked()
    {
        Close();
    }
}