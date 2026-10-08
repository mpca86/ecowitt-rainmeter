using System.Text.Json;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;

static void Expect(bool condition, string message)
{
    if (!condition) throw new Exception("FAILED: " + message);
}

const string cloudResponse = """
{
  "code": 0,
  "msg": "success",
  "time": 1791456000,
  "data": {
    "outdoor": {
      "temperature": { "value": "-3.2", "unit": "ºC", "time": 1791456000 },
      "humidity": { "value": "82", "unit": "%" }
    },
    "indoor": {
      "temperature": { "value": "22.1", "unit": "ºC" },
      "humidity": { "value": "45", "unit": "%" }
    },
    "wind": {
      "wind_speed": { "value": "7.2", "unit": "km/h" },
      "wind_gust": { "value": "4.0", "unit": "m/s" },
      "wind_direction": { "value": "270", "unit": "º" }
    },
    "pressure": {
      "relative": { "value": "1025.5", "unit": "hPa" },
      "absolute": { "value": "29.92", "unit": "inHg" }
    },
    "rainfall": {
      "rain_rate": { "value": "0.1", "unit": "in/hr" },
      "daily": { "value": "2.5", "unit": "mm" }
    },
    "solar_and_uvi": {
      "solar": { "value": "100.1", "unit": "W/m2" },
      "uvi": { "value": "2" }
    },
    "temp_and_humidity_ch1": {
      "temperature": { "value": "18.4", "unit": "ºC" },
      "humidity": { "value": "51", "unit": "%" }
    },
    "battery": {
      "temp_humidity_sensor_ch1": { "value": "1.5", "unit": "V" }
    }
  }
}
""";

var snapshot = EcowittCloudParser.Parse(cloudResponse, DateTimeOffset.UtcNow);
Expect(snapshot.OutdoorTemperatureC == -3.2, "outdoor temperature");
Expect(snapshot.OutdoorHumidityPercent == 82, "outdoor humidity");
Expect(snapshot.IndoorTemperatureC == 22.1, "indoor temperature");
Expect(Math.Abs((snapshot.WindSpeedMs ?? 0) - 2.0) < 0.001, "km/h to m/s");
Expect(Math.Abs((snapshot.AbsolutePressureHpa ?? 0) - (29.92 * 33.8638866667)) < 0.01,
    "inHg to hPa");
Expect(Math.Abs((snapshot.RainRateMmH ?? 0) - 2.54) < 0.001, "in/hr to mm/h");
Expect(snapshot.Sensors.Count == 1 && snapshot.Sensors[0].Channel == 1,
    "dynamic sensors");
Expect(snapshot.Sensors[0].Battery == 1.5, "battery reading");
Expect(snapshot.ObservedAt is not null, "sensor timestamp");

var missing = EcowittCloudParser.Parse("""{"code":0,"data":{}}""", DateTimeOffset.UtcNow);
Expect(missing.Sensors.Count == 0 && missing.OutdoorTemperatureC is null,
    "missing fields");
Expect(missing.ObservedAt is null, "missing timestamp");

var rejected = false;
try
{
    EcowittCloudParser.Parse("""{"code":401,"msg":"Invalid key"}""", DateTimeOffset.UtcNow);
}
catch (InvalidDataException)
{
    rejected = true;
}
Expect(rejected, "API failure must be rejected");

// /device/list is linked to the API account and requires no station MAC.
const string deviceList = """
{
  "code": 0, "msg": "success",
  "data": {
    "list": [
      {"id":1,"type":1,"name":"Záhrada","mac":"AA:BB:CC:DD:EE:FF","stationtype":"GW2000"},
      {"id":2,"type":2,"name":"Kamera","mac":"11:22:33:44:55:66","stationtype":"Camera"},
      {"id":3,"type":"1","name":"Doma","mac":"00:11:22:33:44:55","stationtype":"GW3000"},
      {"id":4,"type":1,"name":"Duplicitná","mac":"aa:bb:cc:dd:ee:ff"}
    ]
  }
}
""";

var devices = EcowittDeviceCatalog.Parse(deviceList);
Expect(devices.Count == 2, "list should include only weather stations with unique MACs");
Expect(devices.Any(x => x.Mac == "AA:BB:CC:DD:EE:FF"), "first station MAC");
Expect(devices.Any(x => x.Name == "Doma" && x.Model == "GW3000"),
    "station name and model");
Expect(!devices.Any(x => x.Name == "Kamera"), "exclude camera devices");
Expect(EcowittDeviceCatalog.Parse("""{"code":0,"data":{"list":[]}}""").Count == 0,
    "empty registered station list");

var listRejected = false;
try
{
    EcowittDeviceCatalog.Parse("""{"code":45001,"msg":"limit"}""");
}
catch (InvalidDataException)
{
    listRejected = true;
}
Expect(listRejected, "device-list API failure must be rejected");

// Existing installations (single Profile + root SensorAliases) must migrate safely.
const string oldSettings = """
{
    "Profile": {
        "Id": "old-station", "Name": "Pôvodná stanica",
        "CloudMac": "AA:BB:CC:DD:EE:FF", "SourceMode": 0
    },
    "RefreshSeconds": 60,
    "SensorAliases": { "1": "Kancelária", "2": "Dielňa" },
    "Widgets": [{"Id":"old-widget"}]
}
""";
var migrated = JsonSerializer.Deserialize<AppSettings>(oldSettings)!;
migrated.Normalize();
Expect(migrated.Profiles.Count == 1, "legacy settings migrate to one station");
Expect(migrated.Profiles[0].Name == "Pôvodná stanica" &&
       migrated.Profiles[0].CloudMac == "AA:BB:CC:DD:EE:FF",
       "legacy station properties preserved");
Expect(migrated.Profiles[0].SensorAliases[1] == "Kancelária",
       "legacy sensor aliases preserved");
Expect(migrated.Widgets[0].ProfileId == migrated.Profiles[0].Id,
       "legacy widget assigned to migrated station");
using (var saved = JsonDocument.Parse(JsonSerializer.Serialize(migrated)))
    Expect(!saved.RootElement.TryGetProperty("SensorAliases", out _),
        "legacy root sensor aliases removed on save");

// Adding second station does not alter the first station's aliases or widget.
var second = migrated.Profiles[0] with
{
    Id = "second-station", Name = "Druhá stanica",
    CloudMac = "00:11:22:33:44:55",
    SensorAliases = new Dictionary<int, string> { [1] = "Chata" }
};
migrated.Profiles.Add(second);
migrated.Widgets.Add(new WidgetPlacement { Id="second-widget", ProfileId=second.Id });
migrated.Normalize();
Expect(migrated.Widgets.Count == 2 &&
       migrated.Widgets[0].ProfileId != migrated.Widgets[1].ProfileId,
       "different widgets can target different stations");
Expect(migrated.Profiles[0].SensorAliases[1] == "Kancelária" &&
       migrated.Profiles[1].SensorAliases[1] == "Chata",
       "per-station aliases remain independent");

Console.WriteLine("PASS: parser, device-list and multi-station migration checks");

