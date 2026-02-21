using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Terminal : MonoBehaviour
{
    InputAction interact;
    [SerializeField] private float interactDistance = 2;
    [SerializeField] private float viewDistance = 5;
    [SerializeField] private float maxViewDistance = 110;
    [SerializeField] private float minFontSize = 12;
    [SerializeField] private float maxFontSize = 200;
    [SerializeField] private TMP_Text prompt;

    private float playerDistance = 100;

    void Start()
    {
        interact = InputSystem.actions.FindAction("Interact");
    }

    void Update()
    {
        playerDistance = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);

        if (interact.IsPressed() && playerDistance < interactDistance)
        {
            GameManager.Instance.OnDelivery();
        }

        if (playerDistance > maxViewDistance)
        {
            prompt.text = "!";
        }
        else if (playerDistance > interactDistance)
        {
            prompt.text = playerDistance.ToString("F1") + " m";
        }
        else
        {
            prompt.text = "Deposit Data\n[E]";
        }

        /*  FONT SCALING
        At 100 m away, the player is at the max distance, so the font should
        be at its max size. When at 10 or closer, the font is at minimum.
        To simplify, call it 110 m. So the font needs to vary from min to max
        over 100 units. Distance is alrerady stored in playerDistance 
        */

        if (playerDistance < viewDistance)
        {
            prompt.fontSize = minFontSize;
        }
        else if (playerDistance < maxViewDistance)
        {
            /* if view < playerDistance < max
               0 < playerDistance - view < max - view
               0 < (playerDistance - view) / (max - view) < 1
               wow math */
            prompt.fontSize = Mathf.Lerp(minFontSize, maxFontSize, (playerDistance - viewDistance)/(maxViewDistance-viewDistance));
        }
        else
        {
            prompt.fontSize = maxFontSize;
        }
    }


}
