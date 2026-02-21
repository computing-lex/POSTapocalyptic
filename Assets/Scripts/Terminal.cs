using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Terminal : MonoBehaviour
{
    InputAction interact;
    [SerializeField] private float interactDistance = 0.5f;
    [SerializeField] private float viewDistance = 1;
    [SerializeField] private TMP_Text prompt;

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
            GameManager.Instance.OnDelivery();   
        }

        float playerDistance = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        
        if (playerDistance < viewDistance)
        {
            prompt.text = playerDistance + " m";
        }
    }


}
