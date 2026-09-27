/// <summary>
/// Something in the simulation that makes its own decisions: a car, a taxi, a pedestrian or the taxi dispatcher.
///
/// Every simulation step the World calls, for all agents and in this order:
///   1. Perceive   - look around (read the world, change nothing that other agents rely on).
///   2. Deliberate - decide what to do.
///   3. Act        - do it.
/// All agents finish a phase before any agent starts the next one.
/// </summary>
public abstract class Agent
{
    public virtual void Perceive(World world) { }

    public virtual void Deliberate(World world) { }

    public virtual void Act(World world) { }
}
