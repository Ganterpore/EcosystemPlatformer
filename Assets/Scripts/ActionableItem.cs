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
            _actionButton = value;
            value.transform.Find("ActionText").GetComponent<Text>().text = actionName;
            value.transform.Find("RequirementsText").GetComponent<Text>().text = actionRequirements;
            value.transform.Find("OutputText").GetComponent<Text>().text = "-->" + actionOutput;

            //Setting action of the button
            value.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (IsActionPossible())
                {
                    String id = actionButton.GetInstanceID() + actionName;
                    GameController.Instance.QueueTask(id, ExecutionTime(), this);
                    value.transform.Find("RadialProgress").gameObject.SetActive(true);
                }
            });
        } 
    }

    public ActionableItem(String name, string actionRequirements, string actionOutput)
    {
        this.actionName = name;
        this.actionRequirements = actionRequirements;
        this.actionOutput = actionOutput;
    }

    public String GetActionID()
    {
        if (actionButton == null)
        {
            return actionName;
        } else
        {
            return actionButton.GetInstanceID() + actionName;
        }
    }

    public abstract bool IsActionPossible();

    public abstract double ExecutionTime();

    public abstract void StartExecution();

    public abstract bool Execute();

    public abstract void CancelTask();
}
