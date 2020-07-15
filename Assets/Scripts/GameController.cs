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
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour
{
    struct TaskItem {
        public String id;
        public DateTime startTime;
        public DateTime endTime;
        public ActionableItem task;

        public TaskItem(String id, DateTime startTime, DateTime endTime, ActionableItem task)
        {
            this.id = id;
            this.startTime = startTime;
            this.endTime = endTime;
            this.task = task;
        }
    }
    private List<TaskItem> taskQueue;
   
    public int numberOfWorkers = 1;
    [HideInInspector]
    public List<Habitat> worldHabitats;
    int DAY_LENGTH_IN_SECONDS = 120;
    private int timeSinceLastPopulationUpdate = 0;

    public static GameController Instance { get; private set; }

    public Storage storage;

    GameObject workersDisplayText;

    public bool hasWoodTools = false;

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void Awake()
    {
        taskQueue = new List<TaskItem>();
    }

    // Start is called before the first frame update
    void Start()
    {
        workersDisplayText = GameObject.Find(this.name + "/Canvas/WorkersText");

        //update populations 100 times to find stable levels
        for (int i = 0; i < 100; i++)
        {
            UpdateAllPopulations();
        }
    }

    private bool foundEquilibrium = false;
    // Update is called once per frame
    void Update()
    {
        //on very first update, find equilibrium point
        if(!foundEquilibrium)
        {
            //update populations 100 times to find stable levels
            for (int i = 0; i < 1000; i++)
            {
                UpdateAllPopulations();
            }
            foundEquilibrium = true;
        }

        workersDisplayText.GetComponent<Text>().text = "Workers: " + numberOfWorkers;
        //Check all tasks in queue and check if any completed
        //loops through backwards so that any removals do not affect the loop
        for(int i=taskQueue.Count-1;i>=0;i--)
        {
            TaskItem task = taskQueue[i];
            //if the finish time of the task has passed, call the function and remove it from the queue
            if (task.endTime < DateTime.Now)
            {
                task.task.Execute();
                taskQueue.RemoveAt(i);
                numberOfWorkers++;
            }
        }
    }

    void FixedUpdate()
    {
        //Checks if day has passed, if so, update populations in all habitats
        timeSinceLastPopulationUpdate++;
        if (timeSinceLastPopulationUpdate * Time.fixedDeltaTime > DAY_LENGTH_IN_SECONDS)
        {
            UpdateAllPopulations();
            timeSinceLastPopulationUpdate = 0;

            //as the day ends all jobs are cancelled
            numberOfWorkers += taskQueue.Count;
            foreach(TaskItem task in taskQueue)
            {
                task.task.CancelTask();
            }
            taskQueue.Clear();

            HabitatSpecies food = storage.GetSpecies("Food");
            if (food == null || food.count <= 0)
            {
                //if no food, 40% of workers die
                numberOfWorkers = (int)(0.6 * numberOfWorkers);
            }
            else
            {
                //every worker eats 3 food at the end of the day
                food.count = food.count - numberOfWorkers * 3;
                if (food.count <= 0)
                {
                    //if this takes us below 0, find the number that were fed, and the rest die at a rate of 40%
                    int workersUnfed = numberOfWorkers - (int)(food.count / 3);
                    numberOfWorkers -= (int)(0.6 * numberOfWorkers);
                }
            }
        }
    }

    private void UpdateAllPopulations()
    {
        foreach (Habitat habitat in worldHabitats)
        {
            habitat.UpdatePopulations();
        }
    }

    public double GetDayPercentage()
    {
        return (1.0 * timeSinceLastPopulationUpdate * Time.fixedDeltaTime) / (1.0 * DAY_LENGTH_IN_SECONDS);
    }

    public bool QueueTask(String id, double time, ActionableItem task)
    {
        if(numberOfWorkers <= 0)
        {
            workersDisplayText.GetComponent<TextWarning>().Warning();
            return false;
        }
        DateTime endTime = DateTime.Now.AddSeconds(time);
        taskQueue.Add(new TaskItem(id, DateTime.Now, endTime, task));
        numberOfWorkers--;
        task.StartExecution();
        return true;
    } 

    public int CountTask(String id)
    {
        int count = 0;
        foreach(TaskItem task in taskQueue)
        {
            if(task.id == id)
            {
                count++;
            }
        }
        return count;
    }

    public double TaskPercentComplete(String id)
    {
        foreach (TaskItem task in taskQueue)
        {
            //grabs the first one placed in queue
            if(task.id == id)
            {
                return (DateTime.Now - task.startTime).TotalSeconds / (task.endTime - task.startTime).TotalSeconds;
            }
        }
        return 0.0;
    }

}
