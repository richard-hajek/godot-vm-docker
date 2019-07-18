using Godot;

[Tool]
public class BridgeContainer : Node
{
    public VagrantBridge VagrantBridge;

    public override void _EnterTree()
    {
        VagrantBridge = new VagrantBridge();
        VagrantBridge.Begin();
    }

    public override void _ExitTree()
    {
        VagrantBridge.Stop();
        VagrantBridge = null;
    }
}