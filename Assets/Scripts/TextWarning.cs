using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Diagnostics;
using System.Security.AccessControl;
using System;
using UnityEngine.InputSystem;
using System.Runtime.Remoting;
using UnityEngine.EventSystems;

public class TextWarning : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Warning()
    {
        StartCoroutine(FlashTextRed());
    }

    IEnumerator FlashTextRed()
    {
        for (int i = 0; i < 5; i++)
        {
            GetComponent<Text>().color = UnityEngine.Color.red;
            yield return new WaitForSeconds(0.05f);
            GetComponent<Text>().color = UnityEngine.Color.white;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
