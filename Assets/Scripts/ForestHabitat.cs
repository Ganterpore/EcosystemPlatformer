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

public class ForestHabitat : MonoBehaviour
{
    public int InitialTreeCount = 150;
    public int InitialAnimalCount = 40;
    public int InitialBerryBushCount = 100;
    public int InitialBugCount = 1000;
    public int initialSpareLandCount = 100;

    private readonly double FIRST_TREE_ROW_HIDE_PERCENT = 0.8;
    private readonly double SECOND_TREE_ROW_HIDE_PERCENT = 0.4;
    private readonly double THIRD_TREE_ROW_HIDE_PERCENT = 0;

    //private double treeCount { get; set; }
    //private double animalCount { get; set; }
    //private double berryBushCount { get; set; }
    //private double berryCount { get; set; }
    //private double bugCount { get; set; }
    private double totalLandCount { get; set; }
    private List<HabitatSpecies> species;
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

    private int timeSinceLastPopulationUpdate = 0;

    // Start is called before the first frame update
    void Start()
    {
        //getting the inputted counts
        //treeCount = InitialTreeCount;
        //animalCount = InitialAnimalCount;
        //berryBushCount = InitialBerryBushCount;
        //berryCount = InitialBerryBushCount * 100;
        //bugCount = InitialBugCount;
        totalLandCount = initialSpareLandCount + InitialTreeCount + InitialBerryBushCount;

        //finding the UI text to display the counts
        treeCountTextObject = GameObject.Find("Canvas/TreeCount");
        animalCountTextObject = GameObject.Find("Canvas/AnimalCount");
        berryBushCountTextObject = GameObject.Find("Canvas/BerryBushCount");
        berryCountTextObject = GameObject.Find("Canvas/BerryCount");
        bugCountTextObject = GameObject.Find("Canvas/BugCount");
        spareLandCountTextObject = GameObject.Find("Canvas/SpareLandCount");

        //getting the tree views to update
        //TODO will need to change this in the future
        closeTrees = GameObject.Find("Grid/CloseTrees");
        midTrees = GameObject.Find("Grid/MidTrees");
        farTrees = GameObject.Find("Grid/FarTrees");

        

        trees = new HabitatSpecies("Trees", InitialTreeCount, 0.99);
        animals = new HabitatSpecies("Animals", InitialAnimalCount, 1);
        HabitatSpecies berryBush = new HabitatSpecies("Berry Bushes", InitialBerryBushCount, 0.9);
        berries = new HabitatSpecies("Berries", InitialBerryBushCount*100, .95);
        HabitatSpecies bugs = new HabitatSpecies("Bugs", InitialBugCount, 1.3);

        trees.SetLandCompetition(0.08);
        animals.AddFoodSource(bugs, 0.0, 10.0, 1.1);
        animals.AddHabitat(trees, 0.0, 1.0 / 3.0, 1.05);
        berryBush.SetLandCompetition(0.2);
        berries.AddPredator(bugs, 1.0, 1.0, 0);
        berries.AddProducer(berryBush, 20.0, 200.0, 0.0);
        bugs.AddPredator(animals, 1.0, 1.0 / 8.0, 0.0);
        bugs.AddFoodSource(berries, 0.25, 1.0, 3.0);

        species = new List<HabitatSpecies>();
        species.Add(trees);
        species.Add(animals);
        species.Add(berryBush);
        species.Add(berries);
        species.Add(bugs);


        //update populations 100 times to find stable levels
        for (int i = 0; i < 1000; i++)
        {
            UpdatePopulations();
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCountsFromInput();
        UpdateUI();
        UpdateForestDensity();
    }

    void FixedUpdate()
    {
        if(action1Pressed)
        {
            if (action1Held == 0 || action1Held * Time.fixedDeltaTime >= ACTION_HOLD_LENGTH)
            {
                if (trees.count > 0)
                {
                    trees.count = Math.Max(trees.count - 1, 0);
                }
            }
            action1Held++;
        }
        if (action2Pressed)
        {
            if (action2Held == 0 || action2Held * Time.fixedDeltaTime >= ACTION_HOLD_LENGTH)
            {
                if (animals.count > 0)
                {
                    animals.count = Math.Max(animals.count - 1, 0);
                }
            }
            action2Held++;
        }
        if (action3Pressed)
        {
            if (action3Held == 0 || action3Held * Time.fixedDeltaTime >= ACTION_HOLD_LENGTH)
            {
                if (berries.count > 0)
                {
                    berries.count = Math.Max(berries.count - 10, 0);
                }
            }
            action3Held++;
        }
        int SECONDS_BETWEEN_UPDATES = 1;

        timeSinceLastPopulationUpdate++;
        if(timeSinceLastPopulationUpdate * Time.fixedDeltaTime > SECONDS_BETWEEN_UPDATES) //update population every SECONDS_BETWEEN_UPDATES seconds
        {
            UpdatePopulations();
            timeSinceLastPopulationUpdate = 0;
        }
    }

    private class HabitatSpecies
    {
        public String name;
        public double count { get; set; }
        private double reproductionRate;
        private bool usesLandSpace = false;
        List<Func<Double, Double>> growthFuncs;
        List<HabitatSpecies> dependantSpecies;
        List<Func<Double, Double>> producerRates;
        List<HabitatSpecies> producers;
        Func<Double, Double> LandGrowthRate;

        public HabitatSpecies(String name, double initialCount, double reproductionRate)
        {
            this.name = name;
            this.count = initialCount;
            this.reproductionRate = reproductionRate;
            this.growthFuncs = new List<Func<double, double>>();
            this.dependantSpecies = new List<HabitatSpecies>();
            this.producerRates = new List<Func<double, double>>();
            this.producers= new List<HabitatSpecies>();
        }

        public double landSpaceUsed()
        {
            if(usesLandSpace)
            {
                return count;
            }
            return 0.0;
        }

        public void IterateGrowth(double remainingLand)
        {
            if(count <= 0)
            {
                count = 0;
                return;
            }
            double growthRate = reproductionRate;
            for(int i=0;i<growthFuncs.Count;i++)
            {
                growthRate = growthRate * growthFuncs[i](dependantSpecies[i].count/count);
            }
            if(usesLandSpace)
            {
                growthRate = growthRate * LandGrowthRate(remainingLand / count);
            }
            count = count * growthRate;

            for(int i=0;i<producerRates.Count;i++)
            {
                count += producers[i].count * producerRates[i](count / producers[i].count);
            }
        }

        public void AddPredator(HabitatSpecies species, double valueAt0, double valueWhenHalf, double asymptote)
        {
            growthFuncs.Add(PredatorGrowthFunction(valueAt0, valueWhenHalf, asymptote));
            dependantSpecies.Add(species);
        }

        public void AddFoodSource(HabitatSpecies species, double valueAt0, double valueWhen1, double asymptote)
        {
            growthFuncs.Add(FoodGrowthFunction(valueAt0, valueWhen1, asymptote));
            dependantSpecies.Add(species);
        }

        public void AddHabitat(HabitatSpecies species, double valueAt0, double valueWhen1, double asymptote)
        {
            growthFuncs.Add(HabitatGrowthFunction(valueAt0, valueWhen1, asymptote));
            dependantSpecies.Add(species);
        }

        public void SetLandCompetition(double landCompetitiveness)
        {
            usesLandSpace = true;
            LandGrowthRate = LandGrowthFunction(landCompetitiveness);
        }

        public void AddProducer(HabitatSpecies producer, double valueAt0, double valueWhen1, double asymptote)
        {
            producerRates.Add(NewFruitPerBush(valueAt0, valueWhen1, asymptote));
            producers.Add(producer);
        }
    }

    double AnimalGrowthFactor(double treeRatio, double animalRatio, double berryBushRatio, double berryRatio, double bugRatio, double remainingLandRatio)
    {
        //all functions must have an output in the range [0, inf) for the input of [0, inf).
        //an output of 0 means the animal will completely die out at that input
        //an output of one means that input will not affect the population growth of the animal
        Func<Double, Double> BugFood = FoodGrowthFunction(0.0, 10.0, 1.1);
        Func<Double, Double> TreeHabitat = HabitatGrowthFunction(0.0, 1.0/3.0, 1.05); //TODO change to habitat function
        double growthFactor = 1.0;
        //growthFactor *= Math.Pow(-1.1 * (33 / 23), -treeRatio) + 1.1; //all animals die if no trees, animal growth rate unchanged at 3 animals per tree, rising to an asymptote of growth of 1.1 at infinite trees
        growthFactor *= TreeHabitat(treeRatio); //all animals die if no trees, animal growth rate unchanged at 3 animals per tree, rising to an asymptote of growth of pi/3 at infinite trees
        growthFactor *= animalRatio * 1; //always 1.1
        growthFactor *= 1; //berryBush does not affect animal population growth
        growthFactor *= 1; //berries do no affect animal population growth
        growthFactor *= BugFood(bugRatio); //at 0 bugs, animals die out. At 5 Bugs per animal the remain the same, reaching an asymptote 5% growth
        growthFactor *= 1; //land does not affect animal population
        if(growthFactor < 0)
        {
            growthFactor = 0;
        }
        return growthFactor;
    }

    double TreeGrowthFactor(double treeRatio, double animalRatio, double berryBushRatio, double berryRatio, double bugRatio, double remainingLandRatio)
    {
        //all functions must have an output in the range [0, inf) for the input of [0, inf).
        //an output of 0 means the animal will completely die out at that input
        //an output of one means that input will not affect the population growth of the animal
        Func<Double, Double> LandGrowth = LandGrowthFunction(0.08);
        double growthFactor = 1;
        growthFactor *= 0.99; //1 percent of trees die/day
        growthFactor *= 1; //animals do not affect tree population growth
        growthFactor *= 1; //berryBush does not affect tree population growth
        growthFactor *= 1; //berries do no affect tree population growth
        growthFactor *= 1; //Bugs do not affect tree population growth
        growthFactor *= LandGrowth(remainingLandRatio); //no change at 0 remaining land, increasing growth rate as more spare land is available

        if (growthFactor < 0)
        {
            growthFactor = 0;
        }
        return growthFactor;
    }

    //TODO Berry Bushes should begin dyinng out if berries are all eaten
    double BerryBushGrowthFactor(double treeRatio, double animalRatio, double berryBushRatio, double berryRatio, double bugRatio, double remainingLandRatio)
    {
        //all functions must have an output in the range [0, inf) for the input of [0, inf).
        //an output of 0 means the animal will completely die out at that input
        //an output of one means that input will not affect the population growth of the animal
        Func<Double, Double> LandGrowth = LandGrowthFunction(0.2);
        double growthFactor = 1;
        growthFactor *= 1; //trees do not effect berry bush populations //TODO perhaps they should though
        growthFactor *= 1; //animals do not affect berry bush population growth
        growthFactor *= 0.9; //berryBushes die at 10 percent per day
        growthFactor *= 1; //berries do no affect berry bush population growth
        growthFactor *= 1; //Bugs do not affect berry bush population growth
        growthFactor *= LandGrowth(remainingLandRatio); //no change at 0 remaining land, increasing growth rate as more spare land is available

        if (growthFactor < 0)
        {
            growthFactor = 0;
        }
        return growthFactor;
    }

    double BerryGrowthFactor(double treeRatio, double animalRatio, double berryBushRatio, double berryRatio, double bugRatio, double remainingLandRatio)
    {
        //all functions must have an output in the range [0, inf) for the input of [0, inf).
        //an output of 0 means the animal will completely die out at that input
        //an output of one means that input will not affect the population growth of the animal
        Func<Double, Double> BugPredators = PredatorGrowthFunction(1.0, 1.0, 0);
        double growthFactor = 1;
        growthFactor *= 1; //trees do not affect berry growth
        growthFactor *= 1; //animals do not affect berry population growth
        growthFactor *= 1; //berryBush does not affect berry population growth, relative to the current population of berries
        growthFactor *= 0.95; //5% of berries die off
        growthFactor *= BugPredators(bugRatio); //at 0 bugs, berries don't change population. At 1 bug per berry, half the berries will be eaten. At 2 bugs all will be eaten.
        growthFactor *= 1; //Remaining land does not effect berry population

        if (growthFactor < 0)
        {
            growthFactor = 0;
        }
        return growthFactor;
    }

    double NewBerriesFromBushes(double berryCount, double berryBushCount)
    {
        double berriesPerBush = berryCount / berryBushCount;
        double newBerriesPerBush = Math.Pow(2, -4.5 * berriesPerBush / 200 + 4.5);//at 0 berries per bush, 22 grow. at 200 1 grows, trending towards 0 at infinity
        return newBerriesPerBush * berryBushCount;
    }

    double BugGrowthFactor(double treeRatio, double animalRatio, double berryBushRatio, double berryRatio, double bugRatio, double remainingLandRatio)
    {
        //all functions must have an output in the range [0, inf) for the input of [0, inf).
        //an output of 0 means the animal will completely die out at that input
        //an output of one means that input will not affect the population growth of the animal
        Func<Double, Double> AnimalPredators = PredatorGrowthFunction(1.0, 1.0/8.0, 0.0);
        Func<Double, Double > BerryFood = FoodGrowthFunction(0.25, 1.0, 3);
        double growthFactor = 1;
        growthFactor *= 1; //trees do not affect bug growth
        growthFactor *= AnimalPredators(animalRatio); //At 0 animals per bug, growth is stable. At one animal per 5 bugs, 50% are eaten, tending towards 100% at infinity
        growthFactor *= 1; //berryBush does not affect bug population growth
        //growthFactor *= 0.75*Math.Sqrt(berryRatio) + 0.25; //at 0 berries, 75% of bugs die. at 1 berry per bug, bugs rate is stable. At 5 berries per bug, bugs have 100% growth
        growthFactor *= BerryFood(berryRatio); //at 0 berries, 75% of bugs die. at 1 berry per bug, bugs rate is stable. At 5 berries per bug, bugs have 100% growth
        growthFactor *= 1.3; //Bugs reproduce at a rate of 30%
        growthFactor *= 1; //Remaining land does not effect bug population

        if (growthFactor < 0)
        {
            growthFactor = 0;
        }
        return growthFactor;
    }

    static Func<Double, Double> PredatorGrowthFunction(double valueAt0, double valueWhenHalf, double asymptote)
    {
        Double coefficient = (1 / valueWhenHalf) * Math.Log((0.5 - asymptote) / (valueAt0 - asymptote), 2);
        Func <Double, Double> growthFunction = (Double predatorToPreyRatio) 
            => (valueAt0 - asymptote)*Math.Pow(2,predatorToPreyRatio*coefficient) + asymptote;

        return growthFunction;
    }

    static Func<Double, Double> FoodGrowthFunction(double valueAt0, double valueWhen1, double asymptote)
    {
        Double coefficient = Math.Tan(((1-valueAt0)*Math.PI)/((asymptote - valueAt0)*2))/valueWhen1;
        Func<Double, Double> growthFunction = (Double preyToPredatorRatio)
           => (asymptote -valueAt0)* (2.0/Math.PI) * Math.Atan(preyToPredatorRatio*coefficient) + valueAt0;

        return growthFunction;
    }

    static Func<Double, Double> HabitatGrowthFunction(double valueAt0, double valueWhen1, double asymptote)
    {
        Double coefficient = Math.Tan(((1 - valueAt0) * Math.PI) / ((asymptote - valueAt0) * 2)) / valueWhen1;
        Func<Double, Double> growthFunction = (Double habitatToSpeciesRatio)
           => (asymptote - valueAt0) * (2.0 / Math.PI) * Math.Atan(habitatToSpeciesRatio * coefficient) + valueAt0;

        return growthFunction;
    }

    static Func<Double, Double> LandGrowthFunction(Double landCompetitiveness)
    {
        Func<Double, Double> growthFunction = (Double remainingLandToPlantRatio)
           => landCompetitiveness * remainingLandToPlantRatio + 1;
        return growthFunction;
    }

    static Func<Double, Double> NewFruitPerBush(Double valueAt0, double valueWhen1, double asymptote)
    {
        double coefficient = Math.Log(((1 - asymptote) / (valueAt0 - asymptote)), 2) / valueWhen1;
        Func<Double, Double> growthFunction = (Double fruitPerBush)
           => (valueAt0 - asymptote) * Math.Pow(2, coefficient * fruitPerBush) + asymptote;
        return growthFunction;
    }

    //reproduction

    void UpdatePopulations()
    {

        double remainingLand = totalLandCount;
        foreach(HabitatSpecies specie in species) {
            remainingLand -= specie.landSpaceUsed();
        }
        foreach (HabitatSpecies specie in species)
        {
            specie.IterateGrowth(remainingLand);
        }


        //TODO animal population dying does not cause a huge rise in bug population, which kills all the berry bushes. I would like this to occurr.
        //if (treeCount != 0)
        //{
        //    treeCount = treeCount * TreeGrowthFactor(treeCount / treeCount, animalCount / treeCount, berryBushCount / treeCount, berryCount / treeCount, bugCount / treeCount, (totalLandCount - berryBushCount - treeCount) / treeCount);
        //}
        //if (animalCount != 0)
        //{
        //    animalCount = animalCount * AnimalGrowthFactor(treeCount / animalCount, animalCount / animalCount, berryBushCount / animalCount, berryCount / animalCount, bugCount / animalCount, (totalLandCount - berryBushCount - treeCount) / animalCount);
        //}
        //if (berryBushCount != 0) { 
        //    berryBushCount = berryBushCount * BerryBushGrowthFactor(treeCount / berryBushCount, animalCount / berryBushCount, berryBushCount / berryBushCount, berryCount / berryBushCount, bugCount / berryBushCount, (totalLandCount - berryBushCount - treeCount) / berryBushCount);
        //}
        ////berryCount += NewBerriesFromBushes(berryCount, berryBushCount);
        //Func<Double, Double> BerriesPerBush = NewFruitPerBush(20, 200, 0);
        //berryCount += berryBushCount * BerriesPerBush(berryCount / berryBushCount);
        //if (berryCount != 0)
        //{
        //    berryCount = berryCount * BerryGrowthFactor(treeCount / berryCount, animalCount / berryCount, berryBushCount / berryCount, berryCount / berryCount, bugCount / berryCount, (totalLandCount - berryBushCount - treeCount) / berryCount);
        //}
        //if (bugCount != 0)
        //{
        //    bugCount = bugCount * BugGrowthFactor(treeCount / bugCount  , animalCount / bugCount, berryBushCount / bugCount, berryCount / bugCount, bugCount / bugCount, (totalLandCount - berryBushCount - treeCount) / bugCount);
        //}
        //UnityEngine.Debug.Log("Trees: " + treeCount);
        //UnityEngine.Debug.Log("Animals: " + animalCount);
        //UnityEngine.Debug.Log("Berry Bushes: " + berryBushCount);
        //UnityEngine.Debug.Log("Berries: " + berryCount);
        //UnityEngine.Debug.Log("Bugs: " + bugCount);
        //UnityEngine.Debug.Log("Spare Land: " + (totalLandCount - treeCount - berryBushCount));
    }

    void UpdateCountsFromInput()
    {
        //Check if the player has edited the counts and update
        //if (Input.GetButtonDown("Action2"))
        //{
        //    if (treeCount > 0)
        //    {
        //        treeCount--;
        //    }
        //}
        //if (Input.GetButtonDown("Action"))
        //{
        //    if (animalCount > 0)
        //    { 
        //        animalCount--;
        //    }
        //}
        //if (Input.GetButtonDown("Action3"))
        //{
        //    if (berryCount > 0)
        //    {
        //        berryCount--;
        //    }
        //}
    }


    private int ACTION_HOLD_LENGTH = 1;

    private bool action1Pressed = false;
    private int action1Held = 0;
    void OnAction1(InputValue value)
    {
        action1Pressed = !action1Pressed;
        if(!action1Pressed)
        {
            action1Held = 0;
        }
    }

    private bool action2Pressed = false;
    private int action2Held = 0;
    void OnAction2(InputValue value)
    {
        action2Pressed = !action2Pressed;
        if (!action2Pressed)
        {
            action2Held = 0;
        }
    }

    private bool action3Pressed = false;
    private int action3Held = 0;
    void OnAction3(InputValue value)
    {
        action3Pressed = !action3Pressed;
        if (!action3Pressed)
        {
            action3Held = 0;
        }
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
        treeCountText.text = string.Format("q Trees: {0:0.0}", species[0].count);
        animalCountText.text = string.Format("e Animals: {0:0.0}", species[1].count);
        berryBushCountText.text = string.Format("Berry Bushes: {0:0.0}", species[2].count);
        berryCountText.text = string.Format("r Berries: {0:0.0}", species[3].count);
        bugCountText.text = string.Format("Bugs: {0:0.0}", species[4].count);
        double remainingLand = totalLandCount;
        foreach (HabitatSpecies specie in species)
        {
            remainingLand -= specie.landSpaceUsed();
        }
        spareLandCountText.text = string.Format("Spare Land: {0:0.0}", remainingLand);
    }

    void UpdateForestDensity()
    {
        //if (((double)treeCount) / ((double)totalLandCount) <= FIRST_TREE_ROW_HIDE_PERCENT)
        //{
        //    closeTrees.GetComponent<TilemapRenderer>().enabled = false;
        //}
        //else
        //{
        //    closeTrees.GetComponent<TilemapRenderer>().enabled = true;
        //}

        //if (((double)treeCount) / ((double)totalLandCount) <= SECOND_TREE_ROW_HIDE_PERCENT)
        //{
        //    midTrees.GetComponent<TilemapRenderer>().enabled = false;
        //}
        //else
        //{
        //    midTrees.GetComponent<TilemapRenderer>().enabled = true;
        //}

        //if (((double)treeCount) / ((double)totalLandCount) <= THIRD_TREE_ROW_HIDE_PERCENT)
        //{
        //    farTrees.GetComponent<TilemapRenderer>().enabled = false;
        //}
        //else
        //{
        //    farTrees.GetComponent<TilemapRenderer>().enabled = true;
        //}
    }

}
