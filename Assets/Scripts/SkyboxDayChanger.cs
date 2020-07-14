using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SkyboxDayChanger : MonoBehaviour
{
    Camera skyCamera;
    // Start is called before the first frame update
    void Start()
    {
        skyCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        double dayPercent = GameController.Instance.GetDayPercentage();
        //start at 0 percent light. quickly rise to 100%, hold there for most of the day, then drop back to 0
        double percentLight = Math.Min(1, (0.5 - Math.Abs(dayPercent - 0.5)) * 3); 
        skyCamera.backgroundColor = Color.HSVToRGB(0.417f, .71f, ((float) percentLight)*.56f);
    }
}
