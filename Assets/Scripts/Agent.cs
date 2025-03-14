using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Agent : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    public Transform doorPosition;
    public Transform roomPosition;
    public Door doorScript;

    private Task rootTask;
    private bool isBehaviorRunning = false;
    private bool canRunBehavior = false;

    void Start()
    {
        this.navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null || doorPosition == null || roomPosition == null || doorScript == null)
        {
            Debug.LogError("Please assign NavMeshAgent, Door Position, Room Position, and Door Script in the Inspector!");
            return;
        }

        // Construct the Behavior Tree (same as before)
        rootTask = new Selector("Root Selector");

        // --- If door is open ---
        Sequence openDoorSequence = new Sequence("Open Door Sequence");
        openDoorSequence.children.Add(new IsDoorOpen(doorScript));
        openDoorSequence.children.Add(new MoveIntoRoom(navMeshAgent, roomPosition));
        ((Selector)rootTask).children.Add(openDoorSequence);

        // --- Else if door is locked ---
        Sequence lockedDoorSequence = new Sequence("Locked Door Sequence");
        lockedDoorSequence.children.Add(new IsDoorLocked(doorScript));

        Sequence lockedDoorActions = new Sequence("Locked Door Actions");
        lockedDoorActions.children.Add(new MoveTo(navMeshAgent, doorPosition));
        lockedDoorActions.children.Add(new OpenDoorTask(doorScript));
        lockedDoorActions.children.Add(new MoveIntoRoom(navMeshAgent, roomPosition));

        lockedDoorSequence.children.Add(lockedDoorActions);
        ((Selector)rootTask).children.Add(lockedDoorSequence);

        // --- Else if door is closed (and not locked) ---
        Sequence closedDoorSequence = new Sequence("Closed Door Sequence");
        closedDoorSequence.children.Add(new Inverter(new IsDoorOpen(doorScript)));
        closedDoorSequence.children.Add(new Inverter(new IsDoorLocked(doorScript)));

        Sequence closedDoorActions = new Sequence("Closed Door Actions");
        closedDoorActions.children.Add(new MoveTo(navMeshAgent, doorPosition));
        closedDoorActions.children.Add(new BargeDoorTask(doorScript, transform)); // Pass agent's transform
        closedDoorActions.children.Add(new MoveIntoRoom(navMeshAgent, roomPosition));

        closedDoorSequence.children.Add(closedDoorActions);
        ((Selector)rootTask).children.Add(closedDoorSequence);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isBehaviorRunning)
        {
            canRunBehavior = true;
            doorScript.AllowUserInput(false); // Disable user input on door
            StartCoroutine(ExecuteBehaviorTree());
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current scene
        }
    }

    IEnumerator ExecuteBehaviorTree()
    {
        isBehaviorRunning = true;
        Debug.Log("Starting behavior tree execution");

        bool result = false;

        // Run the behavior tree once
        while (!result && isBehaviorRunning && canRunBehavior)
        {
            result = rootTask.Run();

            if (result)
            {
                Debug.Log("Behavior Tree execution completed successfully!");
            }
            else
            {
                // Wait one frame before trying again
                // This allows movement to progress
                yield return null;
            }
        }

        isBehaviorRunning = false;
        canRunBehavior = false;
        Debug.Log("Behavior Tree execution finished");
    }

    // Optional: Add a public method to stop the behavior tree if needed
    public void StopBehaviorTree()
    {
        isBehaviorRunning = false;
        canRunBehavior = false;
    }
}