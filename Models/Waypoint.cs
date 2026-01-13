
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;

[ProtoContract]
public sealed class Waypoint
{
    public const double P2PPadding = 1.0;
    public const double MileagePadding = 1.0;
    public const int MAX_VEHICLE = 200000;
    public const int MAX_VEHICLE_PER_PAGE = 1000;
    public const byte SPIN_MIX = 1;
    public const byte SPIN_PUMP = 2;
    public const int VERSION_1 = 1;
    private const int _7DaysSeconds = 7 * 86400;
    private const int _30DaysSeconds = 30 * 86400;
    public const int TYPE_NORMAL = 0;
    public const int TYPE_LBS = 1;
    public const int TYPE_DELETED = 2;

    [ProtoMember(1)] [DataMember] public string VehicleId { get; set; }
    [ProtoMember(2)] [DataMember] public int GpsTime { get; set; }
    [ProtoMember(3)] [DataMember] public long SysTime { get; set; }
    [ProtoMember(4)] [DataMember] public short EventId { get; set; }
    [ProtoMember(5)] [DataMember] public int Status { get; set; }
    [ProtoMember(6)] [DataMember] public byte Satellite { get; set; }
    [ProtoMember(7)] [DataMember] public byte Input { get; set; }
    [ProtoMember(8)] [DataMember] public byte Output { get; set; }
    [ProtoMember(9)] [DataMember] public short Voltage { get; set; }
    [ProtoMember(10)] [DataMember] public short Battery { get; set; }
    [ProtoMember(11)] [DataMember] public short Input1 { get; set; }
    [ProtoMember(12)] [DataMember] public short Input2 { get; set; }
    [ProtoMember(13)] [DataMember] public short Input3 { get; set; }
    [ProtoMember(14)] [DataMember] public short Input4 { get; set; }
    [ProtoMember(15)] [DataMember] public uint RegionId { get; set; }
    [ProtoMember(16)] [DataMember] public int X { get; set; }
    [ProtoMember(17)] [DataMember] public int Y { get; set; }
    [ProtoMember(18)] [DataMember] public int Z { get; set; }
    [ProtoMember(19)] [DataMember] public short Speed { get; set; }
    [ProtoMember(20)] [DataMember] public byte Heading { get; set; }
    [ProtoMember(21)] [DataMember] public int Mile { get; set; }
    [ProtoMember(22)] [DataMember] public int GpsMile { get; set; }
    [ProtoMember(23)] [DataMember] public int CheckSum { get; set; }
    [ProtoMember(24)] [DataMember] public int DeviceTypeId { get; set; }
    [ProtoMember(25)] [DataMember] [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)] public string GeometryId { get; set; }
    [ProtoMember(26)] [DataMember] [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)] public string DriverId { get; set; }
    [ProtoMember(27)] [DataMember] [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)] public string DriverCode { get; set; }
    [ProtoMember(28)] [DataMember] public string Info { get; set; }
    [ProtoMember(29)] [DataMember] public string Data { get; set; }
    [ProtoMember(30)] [DataMember] public string CompanyId { get; set; }
    [ProtoMember(31)] [DataMember] public string UnitId { get; set; }
    [ProtoMember(32)] [DataMember] public string Message { get; set; }
    [ProtoMember(33)] [DataMember] public int Type { get; set; }
    [ProtoMember(34)] [DataMember] public int Version { get; set; }
    [ProtoMember(35)] [DataMember] public ValueSensor[]? Sensors { get; set; }
    [ProtoMember(36)] [DataMember] public int MaxSpeed { get; set; }
    [ProtoMember(37)] [DataMember] public int RoadSpeed { get; set; }
    [ProtoMember(38)] [DataMember] public string Road { get; set; } = "";
    [ProtoMember(39)] [DataMember] public string DeviceId { get; set; }
    [ProtoMember(40)] [DataMember] public string Params { get; set; }
    [ProtoMember(41)] [DataMember] public int[]? ValueSensors { get; set; }

    public int Distance { get; set; }
    public int Distance2 { get; set; }

    [System.Text.Json.Serialization.JsonIgnore] public JObject JReport { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore] public bool IsParsed { get; set; }

    [DataMember] public bool Free { get; set; }
    [DataMember] public int CamInterval { get; set; }
    [DataMember] public int CamMask { get; set; }
    [DataMember] public bool Fake { get; set; }
    [DataMember] public string Pin { get; set; } = "0000";
    public int GPS { get; set; }
    public int GpsMileage { get; set; }
    public int DistanceP2P { get; set; }
    [DataMember] public string Imei { get; set; } = "";
    [DataMember] public int ServerType { get; set; }
    [DataMember] public string VehicleType { get; set; }
    [DataMember] public int SensorMask { get; set; }
    [DataMember] [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)] public string Geometry { get; set; }
    [DataMember] public string VehicleKeys { get; set; }
    [DataMember] public string CompanyKeys { get; set; }
    public JObject? Keys { get; set; }
}