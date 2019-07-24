using System;
using Godot;

public class PeripheralContainer : Node
{
    public Peripheral Peripheral;

    [Export]
    public NodePath SubjectNodePath;

    public override void _EnterTree()
    {
        if (!(GetParent() is ComputerContainer))
            throw new Exception("Parent of Peripheral must be a Computer!");
        
        var subject = GetNode(SubjectNodePath);
        
        Peripheral = new Peripheral
        {
            Identificator = GetName(),
            Parent = GetParent<ComputerContainer>().Computer,
            Subject = subject
        };
        
        GetParent<ComputerContainer>().Computer.Peripherals.Add(Peripheral);
    }
}