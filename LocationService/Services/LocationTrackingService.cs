using System.Text.Json;
using Confluent.Kafka;
using Grpc.Core;

namespace LocationService.Services;

public class LocationTrackingService(
    IProducer<string, string> producer,
    ILogger<LocationTrackingService> logger) : LocationTracking.LocationTrackingBase
{
    public override async Task<LocationResponse> StreamLocations(
        IAsyncStreamReader<LocationUpdate> requestStream,
        ServerCallContext context)
    {
        var count = 0;

        while (await requestStream.MoveNext(context.CancellationToken))
        {
            var update = requestStream.Current;
            count++;

            logger.LogInformation(
                "Received location update #{Count}: DeviceId={DeviceId}, MobileAppId={MobileAppId}, CourierId={CourierId}, Lat={Latitude}, Lon={Longitude}",
                count, update.DeviceId, update.MobileAppId, update.CourierId, update.Latitude, update.Longitude);

            var json = JsonSerializer.Serialize(new
            {
                update.Latitude,
                update.Longitude,
                DeviceId = update.DeviceId,
                MobileAppId = update.MobileAppId,
                CourierId = update.CourierId,
                Timestamp = update.Timestamp?.ToDateTimeOffset()
            });

            await producer.ProduceAsync("location-updates", new Message<string, string>
            {
                Key = update.DeviceId,
                Value = json
            });
        }

        logger.LogInformation("Stream completed. Total updates received: {Count}", count);

        return new LocationResponse
        {
            ReceivedCount = count,
            Status = "completed"
        };
    }
}
