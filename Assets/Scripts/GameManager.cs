using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private List<Terminal> terminals;
    [SerializeField] private int currentTerminal;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null) Instance = this; 
        else Destroy(this);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnDelivery()
    {
        currentTerminal++;
    }
}
