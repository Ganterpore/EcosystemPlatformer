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
using System.Linq;

public class Habitat : MonoBehaviour
{
    public List<HabitatSpecies> speciesList;
    public double totalLandCount = 350;

    public Habitat habitatToTheRight;
    public GameObject buttonListContent;

    [HideInInspector]
    public List<ActionableItem> habitatActions = new List<ActionableItem>();

    virtual public void Awake()
    {
        speciesList = new List<HabitatSpecies>();
    }

    public GameObject actionButtonTemplate;

    // Start is called before the first frame update
    virtual public void Start()
    {
        //adding to the world habitats list
        GameController.Instance.worldHabitats.Add(this);
        
        //adding each species actions to the habitat actions
        foreach (HabitatSpecies species in speciesList)
        {
            if(species.actions.Count > 0)
            {
                foreach (HabitatSpecies.SpeciesAction action in species.actions)
                {
                    habitatActions.Add(action);
                }
            }
        }
    }

    // Update is called once per frame
    virtual public void Update()
    {
        foreach(ActionableItem action in habitatActions)
        {
            //checking if button instantiated, if not, creating it
            GameObject actionButton = action.actionButton;
            if(actionButton == null)
            {
                actionButton = Instantiate(actionButtonTemplate);
                actionButton.transform.SetParent(buttonListContent.transform, false);
                action.actionButton = actionButton;
            }
            
            String taskId = action.GetActionID();
            int taskCount = GameController.Instance.CountTask(taskId);
            //if no current tasks for the button, hide the progress circle
            if(taskCount <= 0)
            {
                actionButton.transform.Find("RadialProgress").gameObject.SetActive(false);
            } else
            {
                //otherwise, update the number of current tasks and the progress circle completion
                actionButton.transform.Find("RadialProgress").gameObject.transform.Find("Text").GetComponent<Text>().text = "" + taskCount;
                double completionPercent = GameController.Instance.TaskPercentComplete(taskId);
                actionButton.transform.Find("RadialProgress").gameObject.transform.Find("RadialFill").GetComponent<Image>().fillAmount = (float) completionPercent;
            }

            //if action not currently possible, grey out the button and move to the bottom of the list
            if (!action.IsActionPossible())
            {
                actionButton.GetComponent<Image>().color = UnityEngine.Color.gray;
                //if no current tasks in queue for that action, move button to the end
                if (!actionButton.transform.Find("RadialProgress").gameObject.activeSelf) {
                    actionButton.transform.SetAsLastSibling();
                }
            }
            else
            {
                actionButton.GetComponent<Image>().color = UnityEngine.Color.white;
            }
        }
    }

    virtual public void FixedUpdate()
    {

    }

    public void removeHabitatAction(String actionName)
    {
        for(int i=habitatActions.Count-1;i>=0;i--)
        {
            ActionableItem action = habitatActions.ElementAt(i);
            if (action.actionName == actionName)
            {
                GameObject button = action.actionButton;
                if (button != null)
                {
                    UnityEngine.Object.Destroy(button);
                }
                habitatActions.RemoveAt(i);
            }
        }
    }

    public void UpdatePopulations()
    {
        double remainingLand = totalLandCount;
        foreach (HabitatSpecies species in speciesList)
        {
            remainingLand -= species.landSpaceUsed();
        }
        foreach (HabitatSpecies species in speciesList)
        {
            species.IterateGrowth(remainingLand);
            species.Migrate(this, habitatToTheRight);
        }
    }

    public bool HasSpecies(String speciesName)
    {
        foreach(HabitatSpecies species in speciesList) {
            if(species.name == speciesName)
            {
                return true;
            }
        }
        return false;
    }

    public HabitatSpecies GetSpecies(String speciesName)
    {
        foreach (HabitatSpecies species in speciesList)
        {
            if (species.name == speciesName)
            {
                return species;
            }
        }
        return null ;
    }

    public HabitatSpecies AddSpecies(String speciesName)
    {
        HabitatSpecies newSpecies = new HabitatSpecies(speciesName, 0, 1, 0, false);
        speciesList.Add(newSpecies);
        return newSpecies;
    }
}
