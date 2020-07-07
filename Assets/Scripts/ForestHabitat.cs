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

    private GameObject treeCountTextObject;
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
        treeCountTextObject = GameObject.Find(this.name + "/Canvas/TreeCount");
        animalCountTextObject = GameObject.Find(this.name + "/Canvas/AnimalCount");
        berryBushCountTextObject = GameObject.Find(this.name + "/Canvas/BerryBushCount");
        berryCountTextObject = GameObject.Find(this.name + "/Canvas/BerryCount");
        bugCountTextObject = GameObject.Find(this.name + "/Canvas/BugCount");
        spareLandCountTextObject = GameObject.Find(this.name + "/Canvas/SpareLandCount");

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
        animals.AddFoodSource(bugs, 0.0, 10.0, 1.1);
        animals.AddHabitat(trees, 0.0, 1.0 / 3.0, 1.05);
        berryBush.SetLandCompetition(0.2);
        berries.AddPredator(bugs, 1.0, 1.0, 0);
        berries.AddProducer(berryBush, 20.0, 200.0, 0.0);
        bugs.AddPredator(animals, 1.0, 1.0 / 8.0, 0.0);
        bugs.AddFoodSource(berries, 0.25, 1.0, 3.0);

        //Adding actions
        trees.AddMoveAction("Axe Tree", ((Double count) => (5 * count)/200), //5 seconds at 200 trees
                                        10, "Wood", 100); // 10 trees moved, produces 100 wood
        animals.AddMoveAction("Hunt Animal", ((Double count) => (60 * count) / 90), //1 minute at 90 animals
                                        1, "Food", 5); //1 animal killed, produces 5 food
        berries.AddMoveAction("Collect Berries", ((Double count) => (5 * count) / 2000), //5 seconds at 2000 berries
                                        100, "Food", 5); //100 berries collected produces 5 food

        //adding the species to the species list
        //species = new List<HabitatSpecies>();
        base.speciesList.Add(trees);
        base.speciesList.Add(animals);
        base.speciesList.Add(berryBush);
        base.speciesList.Add(berries);
        base.speciesList.Add(bugs);

    }

    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    override public void Update()
    {
        base.Update();
        UpdateUI();
        UpdateForestDensity();
    }

    override public void FixedUpdate()
    {
       
    }

    void UpdateUI()
    {
        //updating UI text
        Text treeCountText = treeCountTextObject.GetComponent<Text>();
        Text animalCountText = animalCountTextObject.GetComponent<Text>();
        Text berryBushCountText = berryBushCountTextObject.GetComponent<Text>();
        Text berryCountText = berryCountTextObject.GetComponent<Text>();
        Text bugCountText = bugCountTextObject.GetComponent<Text>();
        Text spareLandCountText = spareLandCountTextObject.GetComponent<Text>();
        treeCountText.text = string.Format("q Trees: {0:0.0}", speciesList[0].count);
        animalCountText.text = string.Format("e Animals: {0:0.0}", speciesList[1].count);
        berryBushCountText.text = string.Format("Berry Bushes: {0:0.0}", speciesList[2].count);
        berryCountText.text = string.Format("r Berries: {0:0.0}", speciesList[3].count);
        bugCountText.text = string.Format("Bugs: {0:0.0}", speciesList[4].count);
        double remainingLand = base.totalLandCount;
        foreach (HabitatSpecies species in speciesList)
        {
            remainingLand -= species.landSpaceUsed();
        }
        spareLandCountText.text = string.Format("Spare Land: {0:0.0}", remainingLand);
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
