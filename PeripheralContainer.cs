using System;
using Godot;

public class PeripheralContainer : Node
{
    public Peripheral Peripheral;

    [Export]
    public string SubjectNodePath;

    public override void _EnterTree()
    {
        if (!(GetParent() is ComputerContainer))
            throw new Exception("Parent of Peripheral must be a Computer!");
    }

    public override void _Ready()
    {
        var subject = GetTree().GetRoot().GetNode(SubjectNodePath);
        Peripheral = new Peripheral
        {
            Identificator = GetName(),
            Parent = GetParent<ComputerContainer>().Computer,
            Subject = subject
        };
    }
}