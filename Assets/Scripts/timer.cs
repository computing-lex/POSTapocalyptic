using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class timer : MonoBehaviour
{
    [SerializeField] private Slider slider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = GameManager.Instance.currentTimer / GameManager.Instance.terminals[GameManager.Instance.currentTerminal].timeToDeliver;
    }
}
