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

public class Storage : MonoBehaviour
{   
    public List<HabitatSpecies> speciesList;

    private Text storageDisplayText;

    void Awake()    
    {
        speciesList = new List<HabitatSpecies>();
    }

    //Start is called before the first frame update
    void Start()
    {
        storageDisplayText = GameObject.Find(this.name + "/Canvas/StorageText").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        String newText = "";

        foreach(HabitatSpecies species in speciesList)
        {
            newText += species.name + ": " + species.count + "\n";
        }

        storageDisplayText.text = newText;
    }

    public bool HasSpecies(String speciesName)
    {
        foreach (HabitatSpecies species in speciesList)
        {
            if (species.name == speciesName)
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
        return null;
    }

    public HabitatSpecies AddSpecies(String speciesName)
    {
        HabitatSpecies newSpecies = new HabitatSpecies(speciesName, 0, 1, 0, false);
        speciesList.Add(newSpecies);
        return newSpecies;
    }
}
