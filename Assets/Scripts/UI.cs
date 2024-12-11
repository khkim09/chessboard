using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
//using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    private bool timerOn;
    public TMP_Text timerUI;
    public TMP_Text runeUI;
    public TMP_Text chooseUI;
    public GameObject runeIcon;
    public GameObject chooseCheck;
    private float uiTimer;
    private float runeTimer;
    public bool timeOut = false;
    public bool runeActive = true;
    public bool chooseDone = false;



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
            Image icon = runeIcon.GetComponent<Image>();
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, math.max(0, runeTimer / 5));
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
        char[] deli = { ' ', '\n' };
        string[] runes = runeName.Split(deli);
        displayIcon(runes[0]);

        runeUI.text = runeName;
        runeTimer = 5;
    }

    public void displayIcon(string runeName)
    {
        runeIcon.GetComponent<Image>().sprite = Resources.Load<Sprite>(runeName);
        runeTimer = 5;
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

    public void OnActivateButtonClick()
    {
        runeActive = true;
        chooseDone = true;
        Debug.Log("Choose: Activate");
    }

    public void OnCancelButtonClick()
    {
        runeActive = false;
        chooseDone = true;
        Debug.Log("Choose: Cancel");
    }
}
