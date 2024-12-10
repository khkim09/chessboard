using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class UI : MonoBehaviour
{
    private bool timerOn;
    public TMP_Text timerUI;
    public TMP_Text runeUI;
    private float uiTimer;
    public bool timeOut = false;


    void Awake()
    {
        timerOn = false;
        timerUI.text = "";
        runeUI.text = "";
        timeOut = false;
        uiTimer = 30;
    }

    // Update is called once per frame
    void Update()
    {
        if (timerOn)
        {
            uiTimer -= Time.deltaTime;
            timerUI.text = (math.max(0, uiTimer)).ToString("00");
            if (uiTimer <= 0)
            {
                timeOut = true;
            }
        }
        else
        {
            timerUI.text = "";
            runeUI.text = "";
            timeOut = false;
        }
    }

    public void resetTimer(int interval)
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();
        runeUI.text = "";
        uiTimer = interval;
        timeOut = false;
        if (chessboard.isHaste)
        {
            uiTimer = interval + 15;
            chessboard.isHaste = false;
        }
        Debug.Log("resetTimer called");
    }

    public void displayRune(string runeName)
    {
        runeUI.text = runeName;
    }

    public void startTimer()
    {
        timerOn = true;
        resetTimer(30);
    }

    public void stopTimer()
    {
        timerOn = false;
    }
}
