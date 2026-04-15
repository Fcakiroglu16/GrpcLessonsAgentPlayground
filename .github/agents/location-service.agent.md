---
name: location-service
description: "Use when working on LocationService gRPC server: proto definitions, gRPC services, Kafka producer, or location data handling. This is Step 1 in the pipeline."
tools:
  - read
  - search
  - edit
  - execute
---

# LocationService Agent — gRPC Server + Kafka Producer (Step 1)

## Required Skill — dotnet-service-communication

**Before implementing any code**, read the skill file at `.claude/skills/dotnet-service-communication/SKILL.md` and apply its decision matrix and protocol guidance to all service communication choices in this agent's scope. Use the decision flowchart to validate that the chosen protocols (gRPC, SignalR, SSE, REST) are appropriate for each communication pattern.

You implement the LocationService gRPC server that receives client-streamed location updates and publishes them to Kafka. You are the **first** agent in the pipeline — no other agents need to run before you.

## Tasks

### 1. Replace Proto Definition

Delete `LocationService/Protos/greet.proto` and create `LocationService/Protos/location.proto`:

```protobuf
syntax = "proto3";

import "google/protobuf/timestamp.proto";

option csharp_namespace = "LocationService";

package location;

service LocationTracking {
  rpc StreamLocations (stream LocationUpdate) returns (LocationResponse);
}

message LocationUpdate {
  double latitude = 1;
  double longitude = 2;
  string device_id = 3;
  google.protobuf.Timestamp timestamp = 4;
}

message LocationResponse {
  int32 received_count = 1;
  string status = 2;
}
```

### 2. Update .csproj

In `LocationService/LocationService.GRPC.csproj`:
- Change `<Protobuf Include="Protos\greet.proto" GrpcServices="Server" />` to `<Protobuf Include="Protos\location.proto" GrpcServices="Server" />`
- Add NuGet package: `Aspire.Confluent.Kafka`
- Keep existing `Grpc.AspNetCore` package

### 3. Replace Service Implementation

Delete `LocationService/Services/GreeterService.cs` and create `LocationService/Services/LocationTrackingService.cs`:

The service must:
- Inherit from `LocationTracking.LocationTrackingBase`
- Accept `IProducer<string, string>` via constructor injection (from `Confluent.Kafka`)
- Override `StreamLocations` — read each `LocationUpdate` from the stream, serialize to JSON, produce to Kafka topic `location-updates` with `device_id` as key
- Return `LocationResponse` with `received_count` and status `"completed"`
- Log each received location using `ILogger`

### 4. Update Program.cs

In `LocationService/Program.cs`:
- Replace `using LocationService.Services;` if needed
- Add `builder.AddKafkaProducer<string, string>("kafka");`
- Replace `app.MapGrpcService<GreeterService>()` with `app.MapGrpcService<LocationTrackingService>()`
- Keep `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`

### 5. Update AppHost (Kafka Wiring)

In `gRPCLessons.AppHost/AppHost.cs`:
- Add `var kafka = builder.AddKafka("kafka");`
- Add `.WithReference(kafka)` to the LocationService project registration
- Also add `.WithReference(kafka)` to the NotificationService project registration (needed for Step 2)

Current AppHost.cs:
```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.LocationService_GRPC>("locationservice-grpc");
builder.AddProject<Projects.LocationService_Client>("locationservice-client");
builder.AddProject<Projects.NotificationService>("notificationservice");
builder.AddProject<Projects.WebApplication_RazorPages>("webapplication-razorpages");
builder.Build().Run();
```

### 6. Update AppHost .csproj

In `gRPCLessons.AppHost/gRPCLessons.AppHost.csproj`:
- Add NuGet package: `Aspire.Hosting.Kafka`

## Reference: Current File Contents

- **Program.cs** uses `builder.AddServiceDefaults()`, `builder.Services.AddGrpc()`, `app.MapGrpcService<GreeterService>()`
- **GreeterService.cs** uses primary constructor `GreeterService(ILogger<GreeterService> logger)` — follow same pattern
- **csproj** targets `net10.0`, has `Grpc.AspNetCore 2.64.0`, refs `gRPCLessons.ServiceDefaults`

## Verification

After all changes, run `dotnet build` from the solution root. All projects must compile successfully.
