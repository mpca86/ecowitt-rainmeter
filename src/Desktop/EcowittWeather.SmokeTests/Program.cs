using EcowittWeather.Core.Abstractions;
using EcowittWeather.Core.Models;
using EcowittWeather.Infrastructure.Ecowitt;
using EcowittWeather.Infrastructure.Ecowitt.Local;
using EcowittWeather.Infrastructure.Updates;
using System.Text.Json;
using EcowittWeather.Infrastructure.Configuration;
using EcowittWeather.Infrastructure.Ecowitt.Cloud;
using EcowittWeather.Desktop.ViewModels;

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
    {"id":"0x0C","val":"3.6 m/s"},
    {"id":"0x19","val":"12.0 m/s"},
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

var fahrenheitReading = EcowittLocalParser.Parse("""
{
 "common_list":[{"id":"0x02","val":"68","unit":"F"}],
 "ch_aisle":[{"channel":"1","temp":"50","unit":"F","humidity":"70%"}]
}
""", DateTimeOffset.UtcNow);
Expect(Math.Abs((fahrenheitReading.OutdoorTemperatureC ?? -100) - 20) < 0.001 &&
       Math.Abs((fahrenheitReading.Sensors[0].TemperatureC ?? -100) - 10) < 0.001,
       "Local API Fahrenheit values normalized to Celsius");

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

// P0.3: router regression tests are deterministic and never contact a gateway.
// Each scenario uses an injected clock and scripted in-memory IWeatherSource.
{
    var onlyCloudLocal = new FakeSource(localReading) { Throw = true };
    var onlyCloudCloud = new FakeSource(snapshot);
    var onlyCloudRouter = new StationWeatherRouter(onlyCloudLocal, onlyCloudCloud);
    var cloudOnlyReading = await onlyCloudRouter.FetchAsync(
        autoStation with { SourceMode = SourceMode.Cloud });
    Expect(onlyCloudLocal.Count == 0 && onlyCloudCloud.Count == 1 &&
           cloudOnlyReading.Source == "Ecowitt Web API",
        "P0.3 T01: Cloud mode never probes LAN and never marks direct Cloud as fallback");

    var onlyLocalLocal = new FakeSource(localReading);
    var onlyLocalCloud = new FakeSource(snapshot) { Throw = true };
    var onlyLocalRouter = new StationWeatherRouter(onlyLocalLocal, onlyLocalCloud);
    var localOnlyReading = await onlyLocalRouter.FetchAsync(
        autoStation with { SourceMode = SourceMode.Local });
    Expect(onlyLocalLocal.Count == 1 && onlyLocalCloud.Count == 0 &&
           localOnlyReading.Source == "Ecowitt Local API",
        "P0.3 T01: Local mode never calls Cloud");

    var errorLocal = new ScriptedSource((_, _) => Task.FromException<WeatherSnapshot>(
        new InvalidDataException("Simulované neplatné údaje LAN")));
    var errorCloud = new FakeSource(snapshot);
    var errorRouter = new StationWeatherRouter(errorLocal, errorCloud);
    var recoveredWithCloud = await errorRouter.FetchAsync(autoStation);
    Expect(errorLocal.Count == 1 && errorCloud.Count == 1 &&
           recoveredWithCloud.Source.Contains("záložný zdroj"),
        "P0.3 T02: malformed Local response triggers Cloud fallback");

    var isolatedNow = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    var isolatedLocal = new ScriptedSource((p, _) => p.Id == "offline"
        ? Task.FromException<WeatherSnapshot>(new TimeoutException("Offline LAN"))
        : Task.FromResult(localReading));
    var isolatedCloud = new FakeSource(snapshot);
    var isolatedRouter = new StationWeatherRouter(isolatedLocal, isolatedCloud, () => isolatedNow);
    var offline = autoStation with { Id = "offline" };
    var online = autoStation with { Id = "online" };
    var fallbackA = await isolatedRouter.FetchAsync(offline);
    var firstB = await isolatedRouter.FetchAsync(online);
    var repeatA = await isolatedRouter.FetchAsync(offline);
    Expect(fallbackA.Source.Contains("záložný zdroj") &&
           firstB.Source == "Ecowitt Local API" &&
           repeatA.Source.Contains("záložný zdroj") &&
           isolatedLocal.Count == 2 && isolatedCloud.Count == 2,
        "P0.3 T06: two stations have independent Local retry cooldowns");

    isolatedNow += StationWeatherRouter.LocalRetryInterval - TimeSpan.FromTicks(1);
    await isolatedRouter.FetchAsync(offline);
    Expect(isolatedLocal.Count == 2 && isolatedCloud.Count == 3,
        "P0.3 T03: LAN probe is suppressed immediately before retry boundary");
    isolatedNow += TimeSpan.FromTicks(1);
    await isolatedRouter.FetchAsync(offline);
    Expect(isolatedLocal.Count == 3 && isolatedCloud.Count == 4,
        "P0.3 T03: LAN probe retries exactly at 2-minute boundary");

    var resetNow = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    var resetLocal = new FakeSource(localReading) { Throw = true };
    var resetCloud = new FakeSource(snapshot);
    var resetRouter = new StationWeatherRouter(resetLocal, resetCloud, () => resetNow);
    await resetRouter.FetchAsync(autoStation);
    resetLocal.Throw = false;
    var duringCooldown = await resetRouter.FetchAsync(autoStation);
    Expect(duringCooldown.Source.Contains("záložný zdroj") && resetLocal.Count == 1,
        "P0.3 T03: restored LAN is not probed before cooldown");
    resetRouter.Reset();
    var afterReset = await resetRouter.FetchAsync(autoStation);
    Expect(afterReset.Source == "Ecowitt Local API" &&
           resetLocal.Count == 2 && resetCloud.Count == 2,
        "P0.3 T03: Reset forces immediate Local retry");

    var bothNow = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    var bothLocal = new FakeSource(localReading) { Throw = true };
    var bothCloud = new FakeSource(snapshot) { Throw = true };
    var bothRouter = new StationWeatherRouter(bothLocal, bothCloud, () => bothNow);
    var bothFailed = false;
    try { await bothRouter.FetchAsync(autoStation); }
    catch (TimeoutException) { bothFailed = true; }
    Expect(bothFailed && bothLocal.Count == 1 && bothCloud.Count == 1,
        "P0.3 T04: when both sources fail no fake successful snapshot is returned");
    bothLocal.Throw = false;
    bothCloud.Throw = false;
    var cloudDuringCooldown = await bothRouter.FetchAsync(autoStation);
    Expect(cloudDuringCooldown.Source.Contains("záložný zdroj") &&
           bothLocal.Count == 1 && bothCloud.Count == 2,
        "P0.3 T04: Cloud can recover while Local cooldown is still active");
    bothNow += StationWeatherRouter.LocalRetryInterval;
    var localRecovered = await bothRouter.FetchAsync(autoStation);
    Expect(localRecovered.Source == "Ecowitt Local API" &&
           bothLocal.Count == 2 && bothCloud.Count == 2,
        "P0.3 T04: Local recovers automatically after both were unavailable");

    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    var canceledLocal = new ScriptedSource((_, ct) =>
        Task.FromCanceled<WeatherSnapshot>(ct));
    var canceledCloud = new FakeSource(snapshot);
    var canceledRouter = new StationWeatherRouter(canceledLocal, canceledCloud);
    var propagatedCancellation = false;
    try { await canceledRouter.FetchAsync(autoStation, canceled.Token); }
    catch (OperationCanceledException) { propagatedCancellation = true; }
    Expect(propagatedCancellation && canceledLocal.Count == 1 && canceledCloud.Count == 0,
        "P0.3 T05: caller cancellation propagates without Cloud fallback");

    Console.WriteLine("PASS: P0.3 mode isolation, invalid Local fallback, per-profile cooldown, " +
                      "retry boundary, Reset, dual failure recovery and cancellation");
}

// P0.2: real GW3000 FW 1.2.4 payloads, anonymized before committing.
// Home gateway has no rain/ch_aisle sections or wind/UV/solar IDs.
// Office gateway has all of them. Missing must stay null, real zero must stay zero.
static string LoadGw3000Fixture(string filename)
{
    var resource = "EcowittWeather.SmokeTests.Fixtures." + filename;
    using var stream = typeof(FakeSource).Assembly.GetManifestResourceStream(resource)
        ?? throw new InvalidDataException("Missing GW3000 fixture: " + resource);
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

var homeGw3000 = EcowittLocalParser.Parse(
    LoadGw3000Fixture("gw3000_fw1_2_4_home.json"), DateTimeOffset.UtcNow);
Expect(homeGw3000.OutdoorTemperatureC == 15.0 &&
       homeGw3000.OutdoorHumidityPercent == 60 &&
       homeGw3000.IndoorTemperatureC == 21.6 &&
       homeGw3000.IndoorHumidityPercent == 52 &&
       homeGw3000.RelativePressureHpa == 1008.5,
    "GW3000 FW 1.2.4 home basic readings");
Expect(homeGw3000.WindSpeedMs is null &&
       homeGw3000.WindGustMs is null &&
       homeGw3000.WindDirectionDegrees is null &&
       homeGw3000.RainRateMmH is null &&
       homeGw3000.DailyRainMm is null &&
       homeGw3000.UvIndex is null &&
       homeGw3000.SolarWattsM2 is null &&
       homeGw3000.Sensors.Count == 0,
    "GW3000 home missing sensors must remain unavailable, not zero");
Expect(homeGw3000.ObservedAt is null,
    "GW3000 payload without observation timestamp must not invent one");

var officeGw3000 = EcowittLocalParser.Parse(
    LoadGw3000Fixture("gw3000_fw1_2_4_office.json"), DateTimeOffset.UtcNow);
Expect(officeGw3000.OutdoorTemperatureC == 14.0 &&
       officeGw3000.OutdoorHumidityPercent == 62 &&
       officeGw3000.IndoorTemperatureC == 21.6 &&
       officeGw3000.IndoorHumidityPercent == 41 &&
       officeGw3000.RelativePressureHpa == 1009.4,
    "GW3000 FW 1.2.4 office basic readings");
Expect(officeGw3000.WindSpeedMs == 0.0 &&
       officeGw3000.WindGustMs == 0.0 &&
       officeGw3000.WindDirectionDegrees == 151 &&
       officeGw3000.RainRateMmH == 0.0 &&
       officeGw3000.DailyRainMm == 0.0 &&
       officeGw3000.UvIndex == 0.0 &&
       officeGw3000.SolarWattsM2 == 0.0,
    "GW3000 office gust (0x0C) and other zero values must remain valid readings, not daily max 0x19");
Expect(officeGw3000.Sensors.Count == 4 &&
       officeGw3000.Sensors.Select(s => s.Channel).SequenceEqual(new[] { 1, 2, 3, 4 }) &&
       officeGw3000.Sensors[0].TemperatureC == 20.1 &&
       officeGw3000.Sensors[3].HumidityPercent == 45,
    "GW3000 office four CH sensors and normalized values");
// Live office GW3000 FW 1.2.4 capture: wind 0.3 m/s, current gust 0.5 m/s,
// daily maximum gust 7.7 m/s. Keep the two distinct in normalized output.
var observedOfficeWind = EcowittLocalParser.Parse("""
{
  "common_list": [
    { "id": "0x0B", "val": "0.3 m/s" },
    { "id": "0x0C", "val": "0.5 m/s" },
    { "id": "0x19", "val": "7.7 m/s" }
  ]
}
""", DateTimeOffset.UtcNow);
Expect(observedOfficeWind.WindSpeedMs == 0.3 &&
       observedOfficeWind.WindGustMs == 0.5,
    "GW3000 live office gust 0x0C is not daily max 0x19");

// Same payload contains decimal IDs for feels-like (3) and VPD (5)
// alongside a hexadecimal dew-point ID (0x03). These must not collide.
using (var homeFields = JsonDocument.Parse(LoadGw3000Fixture("gw3000_fw1_2_4_home.json")))
{
    var data = homeFields.RootElement;
    Expect(EcowittLocalParser.Common(data, "3") == 15.0 &&
           EcowittLocalParser.Common(data, "0x03") == 7.3 &&
           EcowittLocalParser.Common(data, "5") == 0.682,
        "GW3000 decimal feels-like/VPD and hexadecimal dew point are distinct");
    Expect(EcowittLocalParser.Common(data, "0X3") == 7.3 &&
           EcowittLocalParser.Common(data, "0x003") == 7.3 &&
           EcowittLocalParser.Common(data, "03") == 15.0,
        "ID normalization preserves decimal/hex namespaces");
    Expect(EcowittLocalParser.Common(data, "0x05") is null,
        "hex ID 0x05 must not match decimal VPD ID 5");
}
Console.WriteLine("PASS: GW3000 gust ID 0x0C and decimal/hex field ID isolation");

// P0.2 UI semantics: never present Local HTTP retrieval time as a sensor
// observation time. Also confirm the timestamp continues to use local time.
var sampleRetrieval = new DateTimeOffset(2026, 10, 8, 18, 20, 0, TimeSpan.Zero);
var sampleObservation = new DateTimeOffset(2026, 10, 8, 18, 15, 0, TimeSpan.Zero);
var timeViewModel = new WeatherViewModel();
var localTimed = officeGw3000 with { RetrievedAt = sampleRetrieval, ObservedAt = null };
timeViewModel.ShowSnapshot(localTimed, "Kancelária", new Dictionary<int, string>());
Expect(timeViewModel.Updated == "Načítané: " +
       sampleRetrieval.ToLocalTime().ToString("dd.MM. HH:mm", System.Globalization.CultureInfo.CurrentCulture),
    "Local GW3000 without ObservedAt labels retrieved timestamp as Načítané");

var cloudTimed = localTimed with { ObservedAt = sampleObservation, Source = "Ecowitt Web API" };
timeViewModel.ShowSnapshot(cloudTimed, "Kancelária", new Dictionary<int, string>());
Expect(timeViewModel.Updated == "Meranie: " +
       sampleObservation.ToLocalTime().ToString("dd.MM. HH:mm", System.Globalization.CultureInfo.CurrentCulture),
    "Cloud snapshot with ObservedAt labels actual measurement timestamp");
Expect(timeViewModel.Status == "Ecowitt Web API",
    "timestamp fix preserves source status");
Console.WriteLine("PASS: Desktop time labels use ObservedAt or RetrievedAt correctly");

Console.WriteLine("PASS: real anonymized GW3000 FW 1.2.4 home/office fixtures");

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

// In-memory source allows deterministic error/cancellation tests without HTTP.
public sealed class ScriptedSource(
    Func<StationProfile, CancellationToken, Task<WeatherSnapshot>> handler) : IWeatherSource
{
    public int Count { get; private set; }

    public Task<WeatherSnapshot> FetchAsync(
        StationProfile profile, CancellationToken cancellationToken = default)
    {
        Count++;
        return handler(profile, cancellationToken);
    }
}
