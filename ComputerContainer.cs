using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public class ComputerContainer : Spatial
{
    public Computer Computer;

    [Export(PropertyHint.MultilineText)] public string Dockerfile = "";

    [Export] public string Id = "";

    public override void _EnterTree()
    {
        base._EnterTree();

        Computer = new Computer
        {
            Peripherals = GetPeripherals().ToList(),
            Id = Id,
            Dockerfile = Dockerfile
        };
    }

    public IEnumerable<Peripheral> GetPeripherals()
    {
        foreach (var child in GetChildren())
            if (child is Peripheral p)
                yield return p;
    }
}