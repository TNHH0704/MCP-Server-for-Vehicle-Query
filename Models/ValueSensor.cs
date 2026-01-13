
using ProtoBuf;

[ProtoContract]
public class ValueSensor
{
    /// <summary>
    /// Sensor Type Id || Time
    /// </summary>
    [ProtoMember(1)]
    public int T { get; set; } 

    /// <summary>
    /// Sensor Value
    /// </summary>
    [ProtoMember(2)]
    public int V { get; set; }

    [ProtoMember(3)]
    public int P { get; set; }

    /// <summary>
    /// Sensor Input
    /// </summary>
    [ProtoMember(4)]
    public int I { get; set; }

    /// <summary>
    /// Sensor Data
    /// </summary>
    [ProtoMember(5)]
    public string D { get; set; } = string.Empty;
}