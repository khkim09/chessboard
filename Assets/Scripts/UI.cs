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
    private float runeTimer;
    public bool timeOut = false;
    private bool isSmited = false;

    void Awake()
    {
        timerOn = false;
        timerUI.text = "";
        runeUI.text = "";
        timeOut = false;
        uiTimer = 30;
        runeTimer = 3;
    }

    // Update is called once per frame
    void Update()
    {
        if (timerOn)
        {
            uiTimer -= Time.deltaTime;
            runeTimer -= Time.deltaTime;
            timerUI.text = (math.max(0, uiTimer)).ToString("00");
            runeUI.alpha = math.max(0, runeTimer / 5);
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
        if (isSmited)
        {
            runeUI.text += "\n" + runeName;
            isSmited = false;
        }
        else
        {
            runeUI.text = runeName;
        }
        runeTimer = 3;
        if (runeName == "Smite" || runeName == "Observe") isSmited = true;
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
