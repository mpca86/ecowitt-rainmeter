using EcowittWeather.Core.Abstractions;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Ecowitt;
using EcowittWeather.Infrastructure.Ecowitt.Local;
using EcowittWeather.Infrastructure.Updates;
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

// When deleting a station, the old widget must be reassigned safely.
migrated.Profiles.RemoveAll(p => p.Id == "old-station");
migrated.Normalize();
Expect(migrated.Widgets.All(w => w.ProfileId == "second-station"),
    "deleted station widgets are reassigned to an existing station");

// Release channel must not accidentally pick Rainmeter beta tags, old
// Desktop alphas or a release whose ZIP/checksum are missing.
const string releases = """
[
  {
    "tag_name": "v1.11.0-beta.6", "draft": false, "body": "Rainmeter",
    "assets": [
      {"name": "wrong.zip", "browser_download_url": "https://github.com/test/wrong.zip"}
    ]
  },
  {
    "tag_name": "desktop-v0.2.0-alpha.1", "draft": false, "body": "Initial",
    "assets": [
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.1-win-x64.zip",
       "browser_download_url": "https://github.com/test/alpha1.zip"},
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.1-win-x64.zip.sha256",
       "browser_download_url": "https://github.com/test/alpha1.sha256"}
    ]
  },
  {
    "tag_name": "desktop-v0.2.0-alpha.2", "draft": false, "body": "Novinky v alphe 2",
    "assets": [
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.2-win-x64.zip",
       "browser_download_url": "https://github.com/test/alpha2.zip"},
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.2-win-x64.zip.sha256",
       "browser_download_url": "https://github.com/test/alpha2.sha256"}
    ]
  },
  {
    "tag_name": "desktop-v0.2.0-alpha.3", "draft": true, "body": "Draft",
    "assets": [
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.3-win-x64.zip",
       "browser_download_url": "https://github.com/test/alpha3.zip"},
      {"name": "EcowittWeather-Desktop-v0.2.0-alpha.3-win-x64.zip.sha256",
       "browser_download_url": "https://github.com/test/alpha3.sha256"}
    ]
  }
]
""";
var next = DesktopUpdateService.SelectUpdate(releases, "0.2.0-alpha.1");
Expect(next?.Tag == "desktop-v0.2.0-alpha.2" &&
       next.Notes == "Novinky v alphe 2",
    "alpha channel finds newest complete Desktop release");
Expect(DesktopUpdateService.SelectUpdate(releases, "0.2.0-alpha.2") == null,
    "installed alpha is not offered again");
Expect(DesktopUpdateService.SelectUpdate(releases, "0.2.0-alpha.3") == null,
    "draft alpha is not offered");
Expect(DesktopUpdateService.TryVersion("0.2.0-alpha.1+abcdef", out var parsed) &&
       parsed.prerelease == 1,
    "assembly informational version suffix is accepted");

// Sample GW3000 Local API payload shape from Rainmeter Edition fixture.
// Numbers intentionally use mixed raw / suffixed units.
const string localJson = """
{
  "common_list": [
    {"id":"0x02","val":"5.2","unit":"C"},
    {"id":"0x07","val":"83%"},
    {"id":"0x0B","val":"7.2 km/h"},
    {"id":"0x19","val":"3.6 m/s"},
    {"id":"0x0A","val":"71"},
    {"id":"0x15","val":"0.00 W/m2"},
    {"id":"0x17","val":"0"}
  ],
  "rain": [
    {"id":"0x0E","val":"0.0 mm/Hr"},
    {"id":"0x10","val":"1.5 mm"}
  ],
  "wh25":[{"intemp":"20.6","inhumi":"40%","abs":"946.3 hPa","rel":"1027.2 hPa"}],
  "ch_aisle": [
    {"channel":"1","name":"Kancelária","battery":"0","temp":"21.1","unit":"C","humidity":"41%"},
    {"channel":"2","name":"Dielňa","battery":"1","temp":"16.9","unit":"C","humidity":"46%"}
  ]
}
""";
var localReading = EcowittLocalParser.Parse(localJson, DateTimeOffset.UtcNow);
Expect(localReading.Source == "Ecowitt Local API", "local origin");
Expect(localReading.OutdoorTemperatureC == 5.2 && localReading.OutdoorHumidityPercent == 83,
    "local outdoor temperature and humidity");
Expect(localReading.IndoorTemperatureC == 20.6 &&
       localReading.IndoorHumidityPercent == 40, "local indoor measurements");
Expect(Math.Abs((localReading.WindSpeedMs ?? 0) - 2.0) < 0.001 &&
       localReading.WindGustMs == 3.6, "local wind unit conversion");
Expect(localReading.RelativePressureHpa == 1027.2, "local relative pressure");
Expect(localReading.DailyRainMm == 1.5 && localReading.RainRateMmH == 0,
    "local rainfall");
Expect(localReading.Sensors.Count == 2 && localReading.Sensors[1].Channel == 2 &&
       localReading.Sensors[0].TemperatureC == 21.1,
    "local CH1 and CH2 normalized");
Expect(localReading.SolarWattsM2 == 0 && localReading.UvIndex == 0,
    "local solar/UV zero values");

bool malformedLocal = false;
try { EcowittLocalParser.Parse("""{"debug":[{"runtime":"20"}]}""", DateTimeOffset.UtcNow); }
catch (InvalidDataException) { malformedLocal = true; }
Expect(malformedLocal, "malformed local weather falls back");

Expect(EcowittLocalSource.GetGatewayUrl("192.168.1.42:8080").AbsoluteUri ==
       "http://192.168.1.42:8080/get_livedata_info",
       "local endpoint URI and port");
Expect(EcowittLocalSource.GetGatewayUrl("gw3000.local").Host == "gw3000.local",
    "LAN mDNS hostname allowed");
bool invalidHost = false;
try { EcowittLocalSource.GetGatewayUrl("api.ecowitt.net:80"); }
catch (InvalidOperationException) { invalidHost = true; }
Expect(invalidHost, "public hosts rejected as Local API destinations");

// Auto always prefers LAN, switches to Web on failure, throttles unsuccessful
// LAN probes and tries local again after cooldown.
var simulatedNow = DateTimeOffset.UtcNow;
var localMock = new FakeSource(localReading);
var cloudMock = new FakeSource(snapshot);
var router = new StationWeatherRouter(localMock, cloudMock, () => simulatedNow);
var autoStation = new StationProfile
{
    Id = "hybrid-test", Name = "Hybridná stanica", SourceMode = SourceMode.Auto,
    LocalGatewayHost = "192.168.1.42", CloudMac = "AA:BB:CC:DD:EE:FF"
};
var first = await router.FetchAsync(autoStation);
Expect(first.Source == "Ecowitt Local API" &&
       localMock.Count == 1 && cloudMock.Count == 0, "auto prefers Local API");

localMock.Throw = true;
var secondRead = await router.FetchAsync(autoStation);
Expect(secondRead.Source.Contains("záložný zdroj") &&
       localMock.Count == 2 && cloudMock.Count == 1,
       "auto fails over to Web API");

var third = await router.FetchAsync(autoStation);
Expect(localMock.Count == 2 && cloudMock.Count == 2,
       "auto skips unreachable LAN until retry");
localMock.Throw = false;
simulatedNow += StationWeatherRouter.LocalRetryInterval + TimeSpan.FromSeconds(1);
var recovered = await router.FetchAsync(autoStation);
Expect(recovered.Source == "Ecowitt Local API" &&
       localMock.Count == 3 && cloudMock.Count == 2,
       "auto returns to LAN after recovery");

Console.WriteLine("PASS: cloud and local parsers, hybrid failover/recovery, migration and update tests");

// This helper never contacts a physical Ecowitt gateway or exposes keys.
public sealed class FakeSource(WeatherSnapshot reading) : IWeatherSource
{
    public int Count { get; private set; }
    public bool Throw { get; set; }
    public Task<WeatherSnapshot> FetchAsync(
        StationProfile profile, CancellationToken cancellationToken = default)
    {
        Count++;
        return Throw
            ? Task.FromException<WeatherSnapshot>(new TimeoutException("Simulovaný výpadok"))
            : Task.FromResult(reading);
    }
}


