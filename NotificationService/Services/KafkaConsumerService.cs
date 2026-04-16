using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.SignalR.Services;

public class KafkaConsumerService(
    ILogger<KafkaConsumerService> logger,
    IConsumer<string, string> consumer,
    IHubContext<LocationHub> hubContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("location-updates");
        logger.LogInformation("Subscribed to topic 'location-updates'");

        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var message = result.Message.Value;

                    logger.LogInformation("Consumed message: {Message}", message);

                    using var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("Latitude", out var latitudeElement) || !latitudeElement.TryGetDouble(out var latitude))
                    {
                        logger.LogWarning("Skipping Kafka message because Latitude is missing or invalid: {Message}", message);
                        continue;
                    }

                    if (!root.TryGetProperty("Longitude", out var longitudeElement) || !longitudeElement.TryGetDouble(out var longitude))
                    {
                        logger.LogWarning("Skipping Kafka message because Longitude is missing or invalid: {Message}", message);
                        continue;
                    }

                    if (!root.TryGetProperty("DeviceId", out var deviceIdElement))
                    {
                        logger.LogWarning("Skipping Kafka message because DeviceId is missing: {Message}", message);
                        continue;
                    }

                    if (!root.TryGetProperty("Timestamp", out var timestampElement))
                    {
                        logger.LogWarning("Skipping Kafka message because Timestamp is missing: {Message}", message);
                        continue;
                    }

                    if (!root.TryGetProperty("MobileAppId", out var mobileAppIdElement))
                    {
                        logger.LogWarning("Skipping Kafka message because MobileAppId is missing: {Message}", message);
                        continue;
                    }

                    var deviceId = deviceIdElement.GetString() ?? string.Empty;
                    var timestamp = timestampElement.GetString() ?? string.Empty;
                    var mobileAppId = mobileAppIdElement.GetString() ?? string.Empty;
                    var courierId = root.TryGetProperty("CourierId", out var courierIdElement)
                        ? courierIdElement.GetString() ?? string.Empty
                        : string.Empty;
                    var courierName = root.TryGetProperty("CourierName", out var courierNameElement)
                        ? courierNameElement.GetString() ?? string.Empty
                        : string.Empty;

                    await hubContext.Clients.All.SendAsync(
                        "ReceiveLocationUpdate",
                        latitude,
                        longitude,
                        deviceId,
                        timestamp,
                        mobileAppId,
                        courierId,
                        courierName,
                        stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, "Skipping Kafka message due to invalid JSON");
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Skipping Kafka message due to unexpected JSON content");
                }
            }
        }, stoppingToken);
    }
}
