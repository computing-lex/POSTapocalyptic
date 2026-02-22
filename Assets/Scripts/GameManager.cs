using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private List<Terminal> terminals;
    [SerializeField] private int currentTerminal = 0;
    public float currentTimer = 0;

    public UnityEvent gameOver;
    public UnityEvent gameWin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null) Instance = this; 
        else Destroy(this);
        terminals[0].isActive = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDelivery()
    {
        terminals[currentTerminal].isActive = false;
        currentTerminal++;
        if (terminals.Count > currentTerminal)
        {
            terminals[currentTerminal].isActive = true;
            Debug.Log("Switching Terminal");
            StartCoroutine(Countdown(terminals[currentTerminal].timeToDeliver, currentTerminal));
        } else
        {
            gameWin.Invoke();
        }
        
    }

    private IEnumerator Countdown(float timeLeft, int currterminal)
    {
        float t = timeLeft;
        
        while (t > 0 && currentTerminal == currterminal)
        {
            t -= Time.deltaTime;
            currentTimer = t;
            yield return null;
        } 

        if (t <= 0)
        {
            gameOver.Invoke();
        }

    }
}
