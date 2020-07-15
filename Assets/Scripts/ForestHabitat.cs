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

public class ForestHabitat : Habitat
{
    public int InitialTreeCount = 150;
    public int InitialAnimalCount = 40;
    public int InitialBerryBushCount = 100;
    public int InitialBugCount = 1000;

    private readonly double FIRST_TREE_ROW_HIDE_PERCENT = 0.8;
    private readonly double SECOND_TREE_ROW_HIDE_PERCENT = 0.4;
    private readonly double THIRD_TREE_ROW_HIDE_PERCENT = 0;

    HabitatSpecies trees;
    HabitatSpecies animals;
    HabitatSpecies berries;

    private GameObject speciesCountTextObject;
    private GameObject animalCountTextObject;
    private GameObject berryBushCountTextObject;
    private GameObject berryCountTextObject;
    private GameObject bugCountTextObject;
    private GameObject spareLandCountTextObject;

    private GameObject closeTrees;
    private GameObject midTrees;
    private GameObject farTrees;

    override public void Awake()
    {
        base.Awake();
        //finding the UI text to display the counts
        speciesCountTextObject = GameObject.Find(this.name + "/Canvas/SpeciesCount");

        //getting the tree views to update
        //TODO will need to change this in the future
        closeTrees = GameObject.Find(this.name + "/Grid/CloseTrees");
        midTrees = GameObject.Find(this.name + "/Grid/MidTrees");
        farTrees = GameObject.Find(this.name + "/Grid/FarTrees");

        //Creating the species in the habitat
        //totalLandCount = initialLandCount;
        trees = new HabitatSpecies("Trees", InitialTreeCount, 0.99, 0.02, false);
        animals = new HabitatSpecies("Animals", InitialAnimalCount, 1, 0.05, true);
        HabitatSpecies berryBush = new HabitatSpecies("Berry Bushes", InitialBerryBushCount, 0.9, 0.03, false);
        berries = new HabitatSpecies("Berries", InitialBerryBushCount * 100, .95, 0.0, false);
        HabitatSpecies bugs = new HabitatSpecies("Bugs", InitialBugCount, 1.3, 0.06, true);

        //creating the relationships withing the habitat
        trees.SetLandCompetition(0.08);
        animals.AddFoodSource(bugs, 0.0, 50.0, 1.1);
        animals.AddHabitat(trees, 0.0, 1.0 / 3.0, 1.05);
        berryBush.SetLandCompetition(0.2);
        berries.AddPredator(bugs, 1.0, 1.0, 0);
        berries.AddProducer(berryBush, 20.0, 200.0, 0.0);
        bugs.AddPredator(animals, 1.0, 1.0 / 40.0, 0.0);
        bugs.AddFoodSource(berries, 0.25, 1.0, 3);

        //adding the species to the species list
        //species = new List<HabitatSpecies>();
        base.speciesList.Add(berries);
        base.speciesList.Add(trees);
        base.speciesList.Add(animals);
        base.speciesList.Add(berryBush);
        
        base.speciesList.Add(bugs);

    }

    // Start is called before the first frame update
    override public void Start()
    {
        //Adding actions
        berries.AddMoveAction("Collect Berries", "Berries", "1 Food", GameController.Instance.storage, ((Double count) => (5 * count) / 2600), //5 seconds at 1200 berries
                                        100, "Food", 0.01, () => true); //100 berries collected produces 1 food
        animals.AddMoveAction("Hunt Animal", "Animals", "10 Food", GameController.Instance.storage, ((Double count) => (30 * count) / 25), //30 seconds at 25 animals
                                        1, "Food", 10, () => { return GameController.Instance.hasWoodTools; }); //1 animal killed, produces 10 food
        trees.AddMoveAction("Axe Tree", "Trees", "1 Wood", GameController.Instance.storage, ((Double count) => (20 * count) / 250), //20 seconds at 250 trees
                                        1, "Wood", 1, () => { return GameController.Instance.hasWoodTools; }); // 1 trees moved, produces 1 wood
        base.Start();
    }

    // Update is called once per frame
    override public void Update()
    {
        base.Update();
        UpdateUI();
        UpdateForestDensity();

        //if count of trees < X, add action to clear trees
    }

    override public void FixedUpdate()
    {
       
    }

    void UpdateUI()
    {
        //updating UI text
        Text treeCountText = speciesCountTextObject.GetComponent<Text>();
        String newString = "";
        foreach(HabitatSpecies species in speciesList)
        {
            newString += String.Format(species.name +": {0:0.0}\n", species.count);
        }
        
        double remainingLand = base.totalLandCount;
        foreach (HabitatSpecies species in speciesList)
        {
            remainingLand -= species.landSpaceUsed();
        }
        newString += string.Format("Spare Land: {0:0.0}", remainingLand);

        treeCountText.text = newString;
    }

    void UpdateForestDensity()
    {
        if ((trees.count) / (base.totalLandCount) <= FIRST_TREE_ROW_HIDE_PERCENT)
        {
            closeTrees.GetComponent<TilemapRenderer>().enabled = false;
        }
        else
        {
            closeTrees.GetComponent<TilemapRenderer>().enabled = true;
        }

        if ((trees.count) / (base.totalLandCount) <= SECOND_TREE_ROW_HIDE_PERCENT)
        {
            midTrees.GetComponent<TilemapRenderer>().enabled = false;
        }
        else
        {
            midTrees.GetComponent<TilemapRenderer>().enabled = true;
        }

        if ((trees.count) / (base.totalLandCount) <= THIRD_TREE_ROW_HIDE_PERCENT)
        {
            farTrees.GetComponent<TilemapRenderer>().enabled = false;
        }
        else
        {
            farTrees.GetComponent<TilemapRenderer>().enabled = true;
        }
    }

}
