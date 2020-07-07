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

public class Habitat : MonoBehaviour
{
    public List<HabitatSpecies> speciesList;
    public double totalLandCount = 350;

    public Habitat habitatToTheRight;

    virtual public void Awake()
    {
        speciesList = new List<HabitatSpecies>();

    }

    public GameObject actionButton;

    // Start is called before the first frame update
    virtual public void Start()
    {
        GameController.Instance.worldHabitats.Add(this);

        //update populations 100 times to find stable levels
        for (int i = 0; i < 9999; i++)
        {
            UpdatePopulations();
        }

        //GameObject actionButton = GameObject.Find("ActionButton");

        int actionCount = 0;
        foreach (HabitatSpecies species in speciesList)
        {
            if(species.actions.Count > 0)
            {
                foreach (HabitatSpecies.SpeciesAction action in species.actions)
                {
                    //creates new button, and adds the text for the button
                    GameObject newActionButton = Instantiate(actionButton, transform.GetChild(1));
                    newActionButton.transform.GetChild(0).GetComponent<Text>().text = action.name;

                    //Setting action of the button
                    //newActionButton.GetComponent<Button>().onClick.AddListener(() => action.execute(species, this));
                    newActionButton.GetComponent<Button>().onClick.AddListener(() =>
                        GameController.Instance.QueueTask(action.ExecutionTime(species), () => action.Execute(species, this)));

                    //setting positions of the button
                    Vector3 currentPosition = newActionButton.GetComponent<RectTransform>().localPosition;
                    currentPosition.y = currentPosition.y - actionCount * 25;
                    newActionButton.GetComponent<RectTransform>().localPosition = currentPosition;
                    actionCount++;
                }
            }
        }
    }

    // Update is called once per frame
    virtual public void Update()
    {

    }

    virtual public void FixedUpdate()
    {

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
