---
name: notification-service
description: "Use when working on NotificationService: Kafka consumer, SignalR hub, or real-time notification broadcasting. This is Step 2 in the pipeline — depends on LocationService Kafka setup."
tools:
  - read
  - search
  - edit
  - execute
---

# NotificationService Agent — Kafka Consumer + SignalR Hub (Step 2)

## Required Skill — dotnet-service-communication

**Before implementing any code**, read the skill file at `.claude/skills/dotnet-service-communication/SKILL.md` and apply its decision matrix and protocol guidance to all service communication choices in this agent's scope. Use the decision flowchart to validate that the chosen protocols (gRPC, SignalR, SSE, REST) are appropriate for each communication pattern.

You transform the NotificationService from a gRPC server into a Kafka consumer + SignalR hub that bridges location updates to web clients. You run **after** the location-service agent completes.

## Dependency

The location-service agent must have already:
- Created the `location-updates` Kafka topic (via Aspire's `AddKafka()`)
- Added `.WithReference(kafka)` for NotificationService in AppHost.cs

## Tasks

### 1. Remove gRPC Components

- Delete `NotificationService/Protos/greet.proto`
- Delete `NotificationService/Services/GreeterService.cs`

### 2. Update .csproj

In `NotificationService/NotificationService.csproj`:
- **Remove** `<Protobuf Include="Protos\greet.proto" GrpcServices="Server" />` item
- **Remove** `<PackageReference Include="Grpc.AspNetCore" ... />`
- **Add** NuGet package: `Aspire.Confluent.Kafka`

### 3. Create SignalR Hub

Create `NotificationService/Hubs/LocationHub.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

public class LocationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
```

The hub exposes method `ReceiveLocationUpdate` that clients listen to (server-to-client only, no client-to-server methods needed).

### 4. Create Kafka Consumer Background Service

Create `NotificationService/Services/KafkaConsumerService.cs`:

The service must:
- Inherit from `BackgroundService`
- Accept `IConsumer<string, string>` and `IHubContext<LocationHub>` via constructor injection
- In `ExecuteAsync`, subscribe to topic `location-updates`
- On each consumed message, deserialize the JSON location data
- Broadcast to all SignalR clients via `hubContext.Clients.All.SendAsync("ReceiveLocationUpdate", latitude, longitude, deviceId, timestamp)`
- Use `ILogger` for logging consumed messages
- Handle cancellation properly via `stoppingToken`

### 5. Update Program.cs

Replace the entire `NotificationService/Program.cs` with:
- Remove gRPC references (`AddGrpc()`, `MapGrpcService`)
- Add `builder.AddKafkaConsumer<string, string>("kafka", consumerConfig => { consumerConfig.Config.GroupId = "notification-service"; consumerConfig.Config.AutoOffsetReset = AutoOffsetReset.Earliest; });`
- Add `builder.Services.AddSignalR();`
- Add `builder.Services.AddHostedService<KafkaConsumerService>();`
- Map `app.MapHub<LocationHub>("/locationHub");`
- Add CORS to allow RazorPages origin: `builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)));`
- Use `app.UseCors();` before mapping endpoints
- Keep `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`

Current Program.cs:
```csharp
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddGrpc();
var app = builder.Build();
app.MapDefaultEndpoints();
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints...");
app.Run();
```

## Reference

- Follow the same primary constructor pattern as LocationService: `KafkaConsumerService(ILogger<KafkaConsumerService> logger, IConsumer<string, string> consumer, IHubContext<LocationHub> hubContext)`
- The Kafka message value is a JSON string with fields: `latitude`, `longitude`, `device_id`, `timestamp`

## Verification

After all changes, run `dotnet build` from the solution root. All projects must compile successfully.
