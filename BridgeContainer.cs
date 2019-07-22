using Godot;

public class BridgeContainer : Node
{
    public VagrantBridge VagrantBridge;

    [Export]
    public bool DryMode = false;
    
    public override void _EnterTree()
    {
        if (DryMode)
            return;
        
        VagrantBridge = new VagrantBridge();
        VagrantBridge.Begin();
    }

    public override void _ExitTree()
    {
        if (DryMode)
            return;

        VagrantBridge.Stop();
        VagrantBridge = null;
    }
}