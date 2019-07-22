using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class ComputerContainer : Spatial
{
    public Computer Computer;

    [Export(PropertyHint.MultilineText)]
    public string Dockerfile = "";

    [Export]
    public string Id = "";
    
    public BridgeContainer Bridge;

    public override void _EnterTree()
    {
        base._EnterTree();

        Computer = new Computer
        {
            Peripherals = GetPeripherals().ToList(),
            Id = Id,
            Dockerfile = Dockerfile
        };

        Bridge = null;

        var rootNode = GetTree().Root.GetChildren()[0] as Node;
        
        foreach (var child in rootNode.GetChildren())
        {
            if (child is BridgeContainer c)
            {
                if (Bridge == null)
                    Bridge = c;
                else
                {
                    throw new Exception("Cannot have more than one bridge container!");
                }
            }
        }

        GD.Print($"Bridge found: {Bridge != null}");
    }

    public IEnumerable<Peripheral> GetPeripherals()
    {
        foreach (var child in GetChildren())
            if (child is Peripheral p)
                yield return p;
    }
}