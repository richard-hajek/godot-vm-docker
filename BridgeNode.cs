using Godot;

public class BridgeNode : Node
{
    public static bool Attached;

    public static bool DryMode
    {
        get
        {
            if (!ProjectSettings.HasSetting("global/Dry Mode")) return false;

            return (bool) ProjectSettings.GetSetting("global/Dry Mode");
        }
    }

    public static DockerBridge DockerBridge { get; private set; }

    public static void PreStart()
    {
        _begin();
    }

    public override void _EnterTree()
    {
        _begin();
        Attached = true;
    }

    private static void _begin()
    {
        if (DockerBridge != null)
            return;

        DockerBridge = new DockerBridge();
        DockerBridge.Begin();
    }

    public override void _ExitTree()
    {
        DockerBridge.Stop();
        DockerBridge = null;
        Attached = false;
    }
}