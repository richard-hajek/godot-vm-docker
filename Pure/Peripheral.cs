using System.IO;

public class Peripheral
{
    public string Identificator;
    public Computer Parent;
    public object Subject;

    private StreamReader _ingoing;
    private StreamWriter _outgoing;
    
    public void Start(StreamReader ingoing, StreamWriter outgoing)
    {
        streamRead(ingoing);
    }
    
    private void streamRead(object o)
    {
        var reader = (StreamReader) o;
        while (reader != null && !reader.EndOfStream)
        {
            var i = reader.Read();
            OnInput(i);
        }
    }

    public void OnInput(int i)
    {
        switch (Subject)
        {
            case null:
                _outgoing.Write(i);
                break;
        }
    }
}