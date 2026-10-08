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

Console.WriteLine("PASS: 11 cloud-parser smoke checks");
