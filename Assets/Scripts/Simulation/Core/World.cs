using System.Collections.Generic;

public class World
{
    public NavigationGraph Navigation;
    public List<Agent> Agents = new();
    public float DeltaTime { get; private set; }

    public World(NavigationGraph navigation)
    {
        Navigation = navigation;
    }

    public void Tick(float dt)
    {
        DeltaTime = dt;

        foreach (var node in Navigation.nodes.Values)
        node.Light?.Update(dt);

        foreach (var agent in Agents)
            agent.Perceive(this);

        foreach (var agent in Agents)
            agent.Deliberate(this);

        foreach (var agent in Agents)
            agent.Act(this);
    }
}