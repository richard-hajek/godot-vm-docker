using System.IO;
using Godot;
using System;
using hackfest2.addons.Pure;
using Thread = System.Threading.Thread;

public class Peripheral
{
    public string Identificator;
    public Computer Parent;
    public object Subject;

    private VagrantBridge _bridge;
    private StreamReader _ingoing;
    private StreamWriter _outgoing;
    
    public void Start(VagrantBridge bridge)
    {
        _bridge = bridge;
        _outgoing = bridge.GetPeripheralOutgoingStream(this);
        
        var thread = new Thread(streamRead);
        thread.Start();
    }
    
    private void streamRead()
    {
        while (true)
        {
            _ingoing = _bridge.GetPeripheralIngoingStream(this);
            
            while (_ingoing != null && !_ingoing.EndOfStream)
            {
                var i = _ingoing.ReadLine();
                OnInput(i);
            }
            
            GD.Print("[Peripheral] Lost stream, recreating...");
        }
    }

    public void OnInput(string i)
    {
        GD.Print("Receiving '" + i + "'");
        switch (Subject)
        {
            case null:
                _outgoing.WriteLine(i);
                break;
            case Peripheralable p:
                p.OnData(this, i);
                break;
            default:
                Console.WriteLine("Unknown subject!");
                break;
        }
    }
}