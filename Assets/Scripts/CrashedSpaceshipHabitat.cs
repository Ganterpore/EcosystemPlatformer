using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashedSpaceshipHabitat : Habitat
{
    // Start is called before the first frame update
    override public void Start()
    {
        base.Start();
        base.habitatActions.Add(new CollectWoodAction(this));
        base.habitatActions.Add(new BuildToolsAction(this));
        base.habitatActions.Add(new WakeWorkerAction(this, "Co-Pilot 1"));
        base.habitatActions.Add(new WakeWorkerAction(this, "Co-Pilot 2"));
    }

    // Update is called once per frame
    override public void Update()
    {
        base.Update();
    }

    protected class CollectWoodAction : ActionableItem
    {
        CrashedSpaceshipHabitat parent;
        public CollectWoodAction(CrashedSpaceshipHabitat parent) : base("Collect Wood", "", "3 Wood")
        {
            this.parent = parent;
        }

        public override bool IsActionPossible()
        {
            return GameController.Instance.CountTask(GetActionID()) == 0;
        }
        public override double ExecutionTime()
        {
            return 5;
        }

        public override void StartExecution()
        {
            //Nothing to do at start
        }

        public override bool Execute()
        {
            Storage storage = GameController.Instance.storage;
            if (!storage.HasSpecies("Wood"))
            {
                storage.AddSpecies("Wood");
            }
            storage.GetSpecies("Wood").count += 3;
            parent.removeHabitatAction(base.actionName);
            return true;
        }

        public override void CancelTask()
        {
            //nothing to change on cancel
        }
    }

    protected class WakeWorkerAction : ActionableItem
    {
        CrashedSpaceshipHabitat parent;
        Storage storage;
        public WakeWorkerAction(CrashedSpaceshipHabitat parent, string workerName) : base("Awaken "+workerName, "3 Food", "Worker")
        {
            this.parent = parent;
            storage = GameController.Instance.storage;
        }

        public override bool IsActionPossible()
        {
            //and there is at least 3 food
            return storage.HasSpecies("Food") && storage.GetSpecies("Food").count >= 3;
        }

        public override void StartExecution()
        {
            //eat food on start
            if (!storage.HasSpecies("Food"))
            {
                storage.AddSpecies("Food");
            }
            storage.GetSpecies("Food").count -= 3;
        }

        public override double ExecutionTime()
        {
            return 20;
        }

        public override bool Execute()
        {
            GameController.Instance.numberOfWorkers++;
            parent.removeHabitatAction(base.actionName);
            return true;
        }

        public override void CancelTask()
        {
            //in this case, even on cancel, workers recover overnight
            GameController.Instance.numberOfWorkers++;
            parent.removeHabitatAction(base.actionName);
        }
    }

    protected class BuildToolsAction : ActionableItem
    {
        CrashedSpaceshipHabitat parent;
        Storage storage;
        public BuildToolsAction(CrashedSpaceshipHabitat parent) : base("Build Tools", "3 Wood", "Tools")
        {
            this.parent = parent;
            storage = GameController.Instance.storage;
        }

        public override bool IsActionPossible()
        {
            return storage.HasSpecies("Wood") && storage.GetSpecies("Wood").count >= 3 && GameController.Instance.CountTask(GetActionID()) == 0;
        }

        public override void StartExecution()
        {
            if (!storage.HasSpecies("Wood"))
            {
                storage.AddSpecies("Wood");
            }
            storage.GetSpecies("Wood").count -= 3;
        }

        public override double ExecutionTime()
        {
            return 10;
        }

        public override bool Execute()
        {
            GameController.Instance.hasWoodTools = true;
            parent.removeHabitatAction(base.actionName);
            return true;
        }

        public override void CancelTask()
        {
            if (!storage.HasSpecies("Wood"))
            {
                storage.AddSpecies("Wood");
            }
            storage.GetSpecies("Wood").count += 3;
        }
    }
}
