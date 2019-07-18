using System.Collections.Generic;

public class Computer
{
    public string Dockerfile;
    public string Id;
    public List<Peripheral> Peripherals = new List<Peripheral>();
    public bool Running;
}