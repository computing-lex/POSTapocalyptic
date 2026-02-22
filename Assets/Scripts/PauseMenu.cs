using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Slider slider;
    InputAction pause;
    [SerializeField] private GameObject menu;
    private bool isActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pause = InputSystem.actions.FindAction("Pause");
    }

    // Update is called once per frame
    void Update()
    {
        PlayerController.Instance.SetSensitivity(slider.value);
        
        if (pause.WasPressedThisFrame())
        {
            isActive = !isActive;
            menu.SetActive(isActive);
            Cursor.lockState = isActive ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = isActive;
        }
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}
