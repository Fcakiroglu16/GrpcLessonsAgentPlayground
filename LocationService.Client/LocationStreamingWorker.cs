using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using LocationService;

namespace LocationService.Client;

public class LocationStreamingWorker(IConfiguration configuration, ILogger<LocationStreamingWorker> logger) : BackgroundService
{
    private static readonly (string DeviceId, string CourierId, (double Lat, double Lng)[] Waypoints)[] Routes =
    [
        ("courier-istanbul-ankara", "KRY-1001", [
            (41.0082, 28.9784),  // Istanbul
            (40.8027, 29.4307),  // Gebze
            (40.6940, 30.4028),  // Sakarya
            (40.7356, 31.6089),  // Bolu
            (39.9334, 32.8597),  // Ankara
        ]),
        ("courier-istanbul-izmir", "KRY-1002", [
            (41.0082, 28.9784),  // Istanbul
            (40.1885, 29.0610),  // Bursa
            (39.6484, 27.8826),  // Balıkesir
            (38.6191, 27.4289),  // Manisa
            (38.4192, 27.1287),  // İzmir
        ]),
        ("courier-ankara-antalya", "KRY-1003", [
            (39.9334, 32.8597),  // Ankara
            (37.8746, 32.4932),  // Konya
            (37.7648, 30.5566),  // Isparta
            (37.7203, 30.2906),  // Burdur
            (36.8969, 30.7133),  // Antalya
        ]),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        var address = configuration["services:locationservice-grpc:https:0"]
            ?? configuration["services:locationservice-grpc:http:0"]
            ?? "https://localhost:5001";

        logger.LogInformation("Connecting to LocationService at {Address}", address);

        using var channel = GrpcChannel.ForAddress(address);
        var client = new LocationTracking.LocationTrackingClient(channel);

        var tasks = Routes.Select(route =>
            StreamCourierRoute(client, route.DeviceId, route.CourierId, route.Waypoints, stoppingToken));

        await Task.WhenAll(tasks);

        logger.LogInformation("All courier streams completed");
    }

    private async Task StreamCourierRoute(
        LocationTracking.LocationTrackingClient client,
        string deviceId,
        string courierId,
        (double Lat, double Lng)[] waypoints,
        CancellationToken stoppingToken)
    {
        using var call = client.StreamLocations(cancellationToken: stoppingToken);
        var random = new Random();
        var messageCount = 0;

        for (int w = 0; w < waypoints.Length - 1 && !stoppingToken.IsCancellationRequested; w++)
        {
            var (aLat, aLng) = waypoints[w];
            var (bLat, bLng) = waypoints[w + 1];

            for (int step = 0; step < 10 && !stoppingToken.IsCancellationRequested; step++)
            {
                var lat = aLat + (bLat - aLat) * step / 10.0 + (random.NextDouble() - 0.5) * 0.004;
                var lng = aLng + (bLng - aLng) * step / 10.0 + (random.NextDouble() - 0.5) * 0.004;

                var update = new LocationUpdate
                {
                    Latitude = lat,
                    Longitude = lng,
                    DeviceId = deviceId,
                    CourierId = courierId,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    MobileAppId = "com.cargo.tracker"
                };

                await call.RequestStream.WriteAsync(update, stoppingToken);
                messageCount++;
                logger.LogInformation("[{DeviceId}] Sent #{Count}: Lat={Lat:F6}, Lng={Lng:F6}",
                    deviceId, messageCount, lat, lng);

                await Task.Delay(1500, stoppingToken);
            }
        }

        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseAsync;
        logger.LogInformation("[{DeviceId}] Stream completed. Server received {Count} locations. Status: {Status}",
            deviceId, response.ReceivedCount, response.Status);
    }
}
