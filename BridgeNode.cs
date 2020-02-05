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

    public static ContainerAPI ContainerApi { get; private set; }

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
        if (ContainerApi != null)
            return;

        if (ProjectSettings.HasSetting("global/Diagnostics") && (bool) ProjectSettings.GetSetting("global/Diagnostics"))
        {
            Diagnostics.Run();
        }
        
        ContainerApi = new ContainerAPI();
        ContainerApi.Begin();
    }

    public override void _ExitTree()
    {
        ContainerApi.Stop();
        ContainerApi = null;
        Attached = false;
    }
}