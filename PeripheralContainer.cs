using System;
using Godot;

[Tool]
public class PeripheralContainer : Node
{
    [Export] public string Id = "";

    public Peripheral Peripheral;

    [Export] public string SubjectNodePath;

    public override void _EnterTree()
    {
        if (!(GetParent() is ComputerContainer))
            throw new Exception("Parent of Peripheral must be a Computer!");

        var subject = GetTree().GetRoot().GetNode(SubjectNodePath);
        Peripheral = new Peripheral {Parent = GetParent<ComputerContainer>().Computer};
    }
}