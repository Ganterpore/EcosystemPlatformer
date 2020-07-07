using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private List<Tuple<DateTime, Func<bool>>> taskQueue;

    public List<Habitat> worldHabitats;
    int DAY_LENGTH_IN_SECONDS = 10;
    private int timeSinceLastPopulationUpdate = 0;

    public static GameController Instance { get; private set; }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        taskQueue = new List<Tuple<DateTime, Func<bool>>>();
        worldHabitats = new List<Habitat>();
    }

    // Update is called once per frame
    void Update()
    {
        //Check all tasks in queue and check if any completed
        //loops through backwards so that any removals do not affect the loop
        for(int i=taskQueue.Count-1;i>=0;i--)
        {
            Tuple<DateTime, Func<bool>> task = taskQueue[i];
            //if the finish time of the task has passed, call the function and remove it from the queue
            if (task.Item1 < DateTime.Now)
            {
                task.Item2();
                taskQueue.RemoveAt(i);
            }
        }
    }

    void FixedUpdate()
    {
        //Checks if day has passed, if so, update populations in all habitats
        timeSinceLastPopulationUpdate++;
        if (timeSinceLastPopulationUpdate * Time.fixedDeltaTime > DAY_LENGTH_IN_SECONDS)
        {
            foreach(Habitat habitat in worldHabitats)
            {
                habitat.UpdatePopulations();
            }
            timeSinceLastPopulationUpdate = 0;
        }
    }

    public void QueueTask(double time, Func<bool> task)
    {
        DateTime endTime = DateTime.Now.AddSeconds(time);
        taskQueue.Add(new Tuple<DateTime, Func<bool>>(endTime, task));
    } 

}
