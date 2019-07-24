using System.Threading;
using System.IO;
using Godot;
using System;
using Thread = System.Threading.Thread;

public class Peripheral
{
    public string Identificator;
    public Computer Parent;
    public object Subject;

    private StreamReader _ingoing;
    private StreamWriter _outgoing;
    
    public void Start(StreamReader ingoing, StreamWriter outgoing)
    {
        _ingoing = ingoing;
        _outgoing = outgoing;
        
        var thread = new Thread(streamRead);
        thread.Start(ingoing);
    }
    
    private void streamRead(object o)
    {
        var reader = (StreamReader) o;
        while (reader != null && !reader.EndOfStream)
        {
            var i = reader.ReadLine();
            OnInput(i);
        }
    }

    public void OnInput(string i)
    {
        switch (Subject)
        {
            case null:
                _outgoing.WriteLine(i);
                break;
            default:
                Console.WriteLine("Unknown subject!");
                break;
        }
    }
}