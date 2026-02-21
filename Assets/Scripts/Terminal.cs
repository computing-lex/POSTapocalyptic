using UnityEngine;
using UnityEngine.InputSystem;

public class Terminal : MonoBehaviour
{
    InputAction interact;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interact = InputSystem.actions.FindAction("Interact");    
    }

    // Update is called once per frame
    void Update()
    {
        if (interact.IsPressed())
        {
            
        }
    }

    
}
