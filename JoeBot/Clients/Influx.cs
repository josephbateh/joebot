using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace JoeBot.Clients;

// TODO: Properly implement this.
// public class Influx {
//     public void UploadMetricAsync(
//         string hostname,
//         string database,
//         int port,
//         string metricName,
//         int metricValue,
//         DateTime timestamp)
//     {
//         // Generate connection string
//         var connectionString = $"{hostname}:{port}";

//         // Create a new InfluxDB client
//         var influxDBClient = new InfluxDBClient(connectionString);

//         // Create a new point for the metric
//         var point = PointData.Measurement(metricName)
//             .Timestamp(timestamp)
//             .Field("value", metricValue)
//             .Create();

//         // Write the point to the database
//         var writeApi = influxDBClient.GetWriteApi();
//         writeApi.Write(database, "mymeasurement", WritePrecision.Ms, point);

//         // Close the InfluxDB client when you're finished
//         influxDBClient.Dispose();
//     }
// }