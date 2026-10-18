// Scripts/Stations/Station.cs
using System;
using UnityEngine;

/// <summary>
/// Base class for every station (espresso, milk, assembly, checkout...).
/// GameManager calls Enter(), the station runs its own mini-game, and calls
/// Complete() when the player is done. Stations never talk to each other.
/// </summary>
public abstract class Station : MonoBehaviour
{
    public event Action Completed;

    protected Order Order { get; private set; }
    protected DrinkState Drink { get; private set; }

    public virtual void Enter(Order order, DrinkState drink)
    {
        Order = order;
        Drink = drink;
        gameObject.SetActive(true);
        OnEnter();
    }

    public virtual void Exit()
    {
        OnExit();
        gameObject.SetActive(false);
    }

    protected abstract void OnEnter();
    protected virtual void OnExit() { }

    /// <summary>Call when the player has finished this station.</summary>
    protected void Complete() => Completed?.Invoke();
}
