using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using hackfest2.addons.Utils;

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
        
        var rootNode = GetTree().Root.GetChildren()[0] as Node;
        Bridge = Nodes.FindChildOfType<BridgeContainer>(rootNode);
        
        if (Bridge == null)
            throw new Exception("Did not find a bridge!");
        
        Computer = new Computer
        {
            Peripherals = new List<Peripheral>(),
            Id = Id,
            Dockerfile = Dockerfile
        };
    }

    public override void _Ready()
    {
        if (Bridge.DryMode)
            return;
        
        Bridge.VagrantBridge.PrepareComputer(Computer);
        Bridge.VagrantBridge.StartComputer(Computer);
    }

    public void OpenTerminal(TerminalContainer terminalContainer)
    {
        if (Bridge.DryMode)
        {
            GD.PrintErr("Dry Mode active, refusing to open a terminal.");
            return;
        }
        
        Bridge.VagrantBridge.AttachToComputer(Computer, out var stdin, out var stdout, out var stderr, true);
        terminalContainer.Open(stdin, stdout);
    }

    public void HotCode(string command)
    {
        if (Bridge.DryMode)
        {
            GD.PrintErr("Dry Mode active, refusing to execute HotCode.");
            return;
        }
        
        
        Bridge.VagrantBridge.AttachToComputer(Computer, out var stdin, out var stdout, out var stderr);
        stdin.WriteLine(command);
        stdin.Close();
        stdout.Close();
        stderr.Close();
    }

    public IEnumerable<Peripheral> GetPeripherals()
    {
        foreach (var child in GetChildren())
            if (child is PeripheralContainer p)
                yield return p.Peripheral;
    }
}