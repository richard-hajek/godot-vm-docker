using System.Collections.Generic;
using System.IO;
using Godot;
using Thread = System.Threading.Thread;

public class ContainerNode : Spatial
{
    private Dictionary<string, StreamWriter> _peripheralWriters = new Dictionary<string, StreamWriter>();
    private readonly Dictionary<string, Node> _peripheralNodes = new Dictionary<string, Node>();
    private readonly List<string> _peripherals = new List<string>();

    private bool _running;

    [Export(PropertyHint.MultilineText)] public string Dockerfile = "";

    [Export] public string Id = "";

    [Export] public NodePath Peripheral1 = null;

    [Export] public NodePath Peripheral2 = null;

    [Export] public NodePath Peripheral3 = null;
    

    public override void _EnterTree()
    {
        base._EnterTree();
        
        // Generate custom ID, if not provided
        if (string.IsNullOrEmpty(Id)) 
            Id = GetHashCode().ToString();

        // Register peripherals
        foreach (var nodePath in new[]{Peripheral1, Peripheral2, Peripheral3})
        {
			
			if (nodePath == null)
			    continue;
			
            var node = GetNode(nodePath);
            var id = node.Name;
            _peripherals.Add(id);
            _peripheralNodes.Add(id, node);
        }
        
        // Boot VM if not booted already
        if (BridgeWrapper.DryMode == false && BridgeWrapper.Attached == false)
        {
            GetTree().Root.CallDeferred("add_child", new BridgeWrapper());
            BridgeWrapper.PreStart();
            BridgeWrapper.Attached = true;
        }
    }

    public override void _Ready()
    {
        if (BridgeWrapper.DryMode)
            return;

        var status = BridgeWrapper.DockerBridge.CreateContainer(Id, Dockerfile, _peripherals);

        if (status != 0)
        {
            GD.Print($"Container failed to create! - {(Errors) status}");
        }
        
        status = BridgeWrapper.DockerBridge.StartContainer(Id);
        
        if (status != 0)
        {
            GD.Print($"Failed to start container! - {(Errors) status}");
        }

        foreach (var pair in _peripheralNodes)
        {
            var node = pair.Value;
            var id = pair.Key;

            var writer = BridgeWrapper.DockerBridge.GetPeripheralOutgoingStream(Id, id);
            _peripheralWriters.Add(id, writer);
            
            node.Call("peripheral_initialization", id, GD.FuncRef(this, "_sendData"));
        }

        _startStreams();
    }

    public override void _ExitTree()
    {
        _running = false;
    }
    
    /// <summary>
    /// Sends a singular command to a computer
    /// </summary>
    /// <param name="command"></param>
    public void HotCode(string command)
    {
        if (BridgeWrapper.DryMode)
        {
            GD.PrintErr("Dry Mode active, refusing to execute HotCode.");
            return;
        }

        BridgeWrapper.DockerBridge.CreateTTY(Id, out var stdin, out var stdout);
        stdin.WriteLine(command);
        stdin.Close();
        stdout.Close();
    }
    
    // --------------------
    // THIS mess below is taking care of input and output streams, do not know how to make it better
    // --------------------
    private void _sendData(string id, string message)
    {
        _peripheralWriters[id].WriteLine(message);
    }

    private void _startStreams()
    {
        _running = true;
        foreach (var peripheral in _peripherals)
        {
            var thread = new Thread(() => _streamRead(peripheral));
            thread.Start();
        }
    }

    private void _streamRead(string peripheral)
    {
        while (_running)
        {
            var ingoing = BridgeWrapper.DockerBridge.GetPeripheralIngoingStream(Id, peripheral);

            while (ingoing != null && !ingoing.EndOfStream)
            {
                var i = ingoing.Read().ToString();
                GD.Print("[Peripheral] Sending " + i);
                _peripheralNodes[peripheral].Call("peripheral_receive", i);
            }

            GD.Print("[Peripheral] Lost stream, recreating...");
        }
    }
}