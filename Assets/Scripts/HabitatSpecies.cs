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

public class HabitatSpecies
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

    public List<SpeciesAction> actions { get; }

    private List<HabitatSpecies> necessarryMigrationSpecies;
    private double migrationRate;
    private bool migratorySpecies;

    public HabitatSpecies(String name, double initialCount, double reproductionRate, double migrationRate, bool migratorySpecies)
    {
        this.name = name;
        this.count = initialCount;
        this.reproductionRate = reproductionRate;
        this.growthFuncs = new List<Func<double, double>>();
        this.dependantSpecies = new List<HabitatSpecies>();
        this.producerRates = new List<Func<double, double>>();
        this.producers = new List<HabitatSpecies>();
        this.necessarryMigrationSpecies = new List<HabitatSpecies>();
        this.migrationRate = migrationRate;
        this.migratorySpecies = migratorySpecies;
        this.actions = new List<SpeciesAction>();
    }

    public void Migrate(Habitat habitatA, Habitat habitatB)
    {
        //if either are null, return
        if (habitatA == null || habitatB == null)
        {
            return;
        }
        bool aHabitable = true;
        bool bHabitable = true;
        //check whether A and B have the necessary species within them to be habitable
        foreach (HabitatSpecies species in necessarryMigrationSpecies) {
            if (!habitatA.HasSpecies(species.name) || habitatA.GetSpecies(species.name).count <= 0)
            {
                aHabitable = false;
            }
            if (!habitatB.HasSpecies(species.name) || habitatB.GetSpecies(species.name).count <= 0)
            {
                bHabitable = false;
            }
        }
        //if one or the either or both is not habitable, no migration
        if (!(aHabitable && bHabitable))
        {
            return;
        }
        //if there are no population in either, there is no migration
        HabitatSpecies speciesInA = habitatA.GetSpecies(name);
        HabitatSpecies speciesInB = habitatB.GetSpecies(name);
        if ((speciesInA == null || speciesInA.count <= 0) && (speciesInB == null || speciesInB.count <= 0))
        {
            return;
        }
        //if one is still null, and the other has a count, instantiate it
        if(speciesInA == null)
        {
            speciesInA = habitatA.AddSpecies(name);
        }
        if (speciesInB == null)
        {
            speciesInB = habitatB.AddSpecies(name);
        }
        //determine direction of movement
        bool moveToB = speciesInA.count > speciesInB.count;
        //get the difference between the count of the species in each habitat
        double difference = Math.Abs(speciesInA.count - speciesInB.count);
        //move species to the lesser habitat by the difference * migration factor
        double amountMigrating = difference * migrationRate;
        if (moveToB)
        {
            speciesInB.count = speciesInB.count + amountMigrating;
        } else
        {
            speciesInA.count = speciesInA.count + amountMigrating;
        }
        //if the species is migratory, remove that amount from the larger land
        //it the species spreads seeds, don't change the amount from larger
        if (migratorySpecies)
        {
            if (moveToB)
            {
                speciesInA.count = speciesInA.count - amountMigrating;
            } else
            {
                speciesInB.count = speciesInB.count - amountMigrating;
            }
        }
    }

    public double landSpaceUsed()
    {
        if (usesLandSpace)
        {
            return count;
        }
        return 0.0;
    }

    public void IterateGrowth(double remainingLand)
    {
        Grow(remainingLand);
        Produce();
    }

    private void Produce()
    {
        for (int i = 0; i < producerRates.Count; i++)
        {
            if (producers[i].count <= 0)
            {
                return;
            }
            count += producers[i].count * producerRates[i](count / producers[i].count);
        }
    }

    private void Grow(double remainingLand)
    {
        if (count <= 0)
        {
            count = 0;
            return;
        }
        double growthRate = reproductionRate;
        for (int i = 0; i < growthFuncs.Count; i++)
        {
            growthRate = growthRate * growthFuncs[i](dependantSpecies[i].count / count);
        }
        if (usesLandSpace)
        {
            growthRate = growthRate * LandGrowthRate(remainingLand / count);
        }
        count = count * growthRate;
    }

    public double BeginAction(String actionName)
    {
        foreach (SpeciesAction action in actions)
        {
            if (action.name == actionName)
            {
                return action.ExecutionTime(this);
            }
        }
        return 0;
    }

    public void ExecuteAction(String actionName, Habitat storage)
    {
        foreach (SpeciesAction action in actions)
        {
            if (action.name == actionName) {
                action.Execute(this, storage);
            }
        }
    }

    /** Moves HabitatSpecies from were it is to the moveTo habitat (usually storage)
    * timeTaken: a function of the count of the habitat species, determines the time taken based on the amount in the habitat
    * determines the amount moved as a function of the current amount. */
    public void AddMoveAction(String actionName, Func<Double, Double> timeTaken, double amountRemoved, String outputName, double amountProduced)
    {
        SpeciesAction action = new SpeciesAction(actionName, timeTaken, amountRemoved, outputName, amountProduced);
        actions.Add(action);
    }

    //TODO allow for a different output than the input
    //axe trees produces wood, not trees
    public class SpeciesAction
    {
        public String name { get; }
        public String outputName;
        private Func<Double, Double> timeTakenFunc;
        private double amountRemoved;
        private double amountProduced;

        public SpeciesAction(String name, Func<Double, Double> timeTaken, double amountRemoved, String outputName, double amountProduced)
        {
            this.name = name;
            this.timeTakenFunc = timeTaken;
            this.amountRemoved = amountRemoved;
            this.outputName = outputName;
            this.amountProduced = amountProduced;
        }

        public double ExecutionTime(HabitatSpecies species)
        {
            double count = species.count;
            double timeTaken = timeTakenFunc(count);
            return timeTaken;
        }

        //TODO return boolean successful
        public bool Execute(HabitatSpecies parentSpecies, Habitat storage)
        {
            double amountMissed = 0;
            parentSpecies.count = parentSpecies.count - amountRemoved;
            if(parentSpecies.count <= 0)
            {
                amountMissed = amountRemoved + parentSpecies.count;
                parentSpecies.count = 0;
            }
            if (!storage.HasSpecies(outputName))
            {
                storage.AddSpecies(outputName);
            }
            storage.GetSpecies(outputName).count = storage.GetSpecies(outputName).count + amountProduced - (amountMissed*(amountProduced/amountRemoved));
            if(amountMissed > 0)
            {
                return false;
            }
            return true;
        }

    }

    public void AddPredator(HabitatSpecies species, double valueAt0, double valueWhenHalf, double asymptote)
    {
        growthFuncs.Add(PredatorGrowthFunction(valueAt0, valueWhenHalf, asymptote));
        dependantSpecies.Add(species);
    }

    //allow for multiple food sources, where it will continue to survive if one dries out
    public void AddFoodSource(HabitatSpecies species, double valueAt0, double valueWhen1, double asymptote)
    {
        growthFuncs.Add(FoodGrowthFunction(valueAt0, valueWhen1, asymptote));
        dependantSpecies.Add(species);
        necessarryMigrationSpecies.Add(species);
    }

    //allow for multiple habitats, where it will continue to survive if one dries out
    public void AddHabitat(HabitatSpecies species, double valueAt0, double valueWhen1, double asymptote)
    {
        growthFuncs.Add(HabitatGrowthFunction(valueAt0, valueWhen1, asymptote));
        dependantSpecies.Add(species);
        necessarryMigrationSpecies.Add(species);
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


    //Static functions

    static Func<Double, Double> PredatorGrowthFunction(double valueAt0, double valueWhenHalf, double asymptote)
    {
        Double coefficient = (1 / valueWhenHalf) * Math.Log((0.5 - asymptote) / (valueAt0 - asymptote), 2);
        Func<Double, Double> growthFunction = (Double predatorToPreyRatio)
           => (valueAt0 - asymptote) * Math.Pow(2, predatorToPreyRatio * coefficient) + asymptote;

        return growthFunction;
    }

    static Func<Double, Double> FoodGrowthFunction(double valueAt0, double valueWhen1, double asymptote)
    {
        Double coefficient = Math.Tan(((1 - valueAt0) * Math.PI) / ((asymptote - valueAt0) * 2)) / valueWhen1;
        Func<Double, Double> growthFunction = (Double preyToPredatorRatio)
           => (asymptote - valueAt0) * (2.0 / Math.PI) * Math.Atan(preyToPredatorRatio * coefficient) + valueAt0;

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
}
