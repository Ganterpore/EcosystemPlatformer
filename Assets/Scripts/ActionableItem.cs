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


public abstract class ActionableItem
{
    public String actionName { get; }
    public String actionRequirements { get; }
    public String actionOutput { get; }

    private GameObject _actionButton;
    public GameObject actionButton 
    {
        get
        {
            return _actionButton;
        }
        set
        {
            value.transform.GetChild(0).GetComponent<Text>().text = actionName;

            //Setting action of the button
            value.GetComponent<Button>().onClick.AddListener(() =>
            {
                //String id = this.name + "-" + actionName;
                String id = actionButton.GetInstanceID() + actionName;
                GameController.Instance.QueueTask(id, ExecutionTime(), () => Execute());
                value.transform.GetChild(1).gameObject.SetActive(true);
            });
            _actionButton = value;
        } 
    }

    public ActionableItem(String name, string actionRequirements, string actionOutput)
    {
        this.actionName = name;
        this.actionRequirements = actionRequirements;
        this.actionOutput = actionOutput;
    }

    public abstract bool IsActionPossible();

    public abstract double ExecutionTime();

    public abstract bool Execute();
}
