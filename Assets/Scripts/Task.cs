using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections; // Required for Coroutines

public abstract class Task
{
    public string name = "Task"; // Added name for debugging

    public Task() { }
    public Task(string name)
    {
        this.name = name;
    }

    public abstract bool Run();
}

public class Sequence : Task
{
    public List<Task> children = new List<Task>();
    private int currentChildIndex = 0; // Track which child we're currently running

    public Sequence() : base("Sequence") { }
    public Sequence(string name) : base(name) { }

    public override bool Run()
    {
        // If we have no children, we're done
        if (children.Count == 0)
            return true;

        // Continue from where we left off
        while (currentChildIndex < children.Count)
        {
            Task currentChild = children[currentChildIndex];

            bool childResult = currentChild.Run();

            if (!childResult)
            {
                return false; // Child is in progress or failed, so sequence is not done
            }
            currentChildIndex++; // Move to the next child
        }

        // Reset for future runs
        currentChildIndex = 0;
        return true; // All children succeeded, so sequence succeeded
    }

    public void Reset()
    {
        currentChildIndex = 0;
        foreach (var child in children)
        {
            if (child is Sequence sequence)
                sequence.Reset();
            else if (child is Selector selector)
                selector.Reset();
            else if (child is MoveTo moveTo)
                moveTo.Reset();
        }
    }
}

public class Selector : Task
{
    public List<Task> children = new List<Task>();
    private int currentChildIndex = 0; // Track which child we're currently running

    public Selector() : base("Selector") { }
    public Selector(string name) : base(name) { }

    public override bool Run()
    {
        // If we have no children, we fail
        if (children.Count == 0)
            return false;

        // Continue from where we left off
        while (currentChildIndex < children.Count)
        {
            Task currentChild = children[currentChildIndex];

            bool childResult = currentChild.Run();

            if (childResult)
            {
                currentChildIndex = 0; // Reset for future runs
                return true; // Child succeeded, so selector succeeded
            }

            // If the child is a MoveTo task or another task that might be in progress,
            // we need to check if it's actually failed or just in progress
            if (currentChild is MoveTo ||
                currentChild is Sequence ||
                currentChild is Selector)
            {
                // If the child is a composite task (Sequence/Selector) or MoveTo,
                // it might be in progress rather than failed
                // For now, assume any task returning false is either failed or in progress
                // and we should wait before trying the next one
                if (currentChild is MoveTo)
                {
                    return false;
                }
            }
            currentChildIndex++; // Try the next child
        }

        // Reset for future runs
        currentChildIndex = 0;
        return false; // All children failed, so selector failed
    }

    public void Reset()
    {
        currentChildIndex = 0;
        foreach (var child in children)
        {
            if (child is Sequence sequence)
                sequence.Reset();
            else if (child is Selector selector)
                selector.Reset();
            else if (child is MoveTo moveTo)
                moveTo.Reset();
        }
    }
}

public class Inverter : Task
{
    public Task child;

    public Inverter(Task child) : base("Inverter") // Added name for debugging
    {
        this.child = child;
    }

    public override bool Run()
    {
        bool result = child.Run();
        return !result; // Inverts the result of the child task
    }
}


public class OpenDoorTask : Task
{
    private Door door;

    public OpenDoorTask(Door door) : base("Open Door Task") // Added name for debugging
    {
        this.door = door;
    }

    public override bool Run()
    {
        if (door == null) return false;

        door.OpenDoor(); // Call the OpenDoor method on the Door script
        Debug.Log("Opening door.");
        return true;
    }
}

public class BargeDoorTask : Task
{
    private Door door;
    private Transform agentTransform; // Store the agent's transform

    public BargeDoorTask(Door door, Transform agentTransform) : base("Barge Door Task") // Modified constructor
    {
        this.door = door;
        this.agentTransform = agentTransform; // Store agent transform
    }

    public override bool Run()
    {
        if (door == null) return false; // Safety check

        door.BargeDoor(agentTransform); // Call the BargeDoor method on the Door script, pass agent transform
        Debug.Log("Barging door.");
        return true;
    }
}

public class IsDoorOpen : Task
{
    private Door door; // Assuming you have a Door script

    public IsDoorOpen(Door door) : base("Is Door Open") // Added name for debugging
    {
        this.door = door;
    }

    public override bool Run()
    {
        bool isDoorOpen = door.IsOpen();
        if (isDoorOpen)
        {
            Debug.Log("Door is open.");
            return true;
        }
        else
        {
            Debug.Log("Door is closed.");
            return false;
        }
    }
}

public class IsDoorLocked : Task
{
    private Door door;

    public IsDoorLocked(Door door) : base("Is Door Locked") // Added name for debugging
    {
        this.door = door;
    }

    public override bool Run()
    {
        bool isDoorLocked = door.IsLocked();
        if (isDoorLocked)
        {
            Debug.Log("Door is locked.");
            return true;
        }
        else
        {
            Debug.Log("Door is not locked.");
            return false;
        }
    }
}

public class MoveTo : Task
{
    private NavMeshAgent agent;
    private Transform targetPosition;
    private bool hasStartedMoving = false;
    private bool isWaitingForDestination = false;

    public MoveTo(NavMeshAgent agent, Transform targetPosition) : base("Move To")
    {
        this.agent = agent;
        this.targetPosition = targetPosition;
    }

    public override bool Run()
    {
        if (agent == null || targetPosition == null) return false;

        // First time this task is run - start the movement
        if (!hasStartedMoving)
        {
            agent.SetDestination(targetPosition.position);
            Debug.Log("MoveTo: " + name + " - Setting destination to: " + targetPosition.position);
            hasStartedMoving = true;
            isWaitingForDestination = true;
            return false; // Indicate that we're not done yet
        }

        // We're already moving, check if we've reached the destination
        if (isWaitingForDestination)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // We've reached the destination
                Debug.Log("MoveTo: " + name + " - Reached destination: " + targetPosition.position);
                agent.ResetPath(); // Clear path for future movements
                isWaitingForDestination = false;
                return true; // Task completed successfully
            }
            else
            {
                // Still moving
                return false; // Not done yet
            }
        }

        // We've already reached the destination in a previous call
        return true;
    }

    public void Reset()
    {
        hasStartedMoving = false;
        isWaitingForDestination = false;
        if (agent != null)
            agent.ResetPath();
    }
}

// Update MoveIntoRoom to use the same pattern
public class MoveIntoRoom : MoveTo
{
    public MoveIntoRoom(NavMeshAgent agent, Transform roomPosition) : base(agent, roomPosition)
    {
        name = "Move Into Room";
    }
}