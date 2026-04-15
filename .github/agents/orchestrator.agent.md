---
name: orchestrator
description: "Use when implementing the full real-time location tracking pipeline end-to-end, or when you need to coordinate work across all services in order. Automatically commits and creates a PR after all agents complete."
tools:
  - read
  - search
  - agent
agents:
  - location-service
  - notification-service
  - razorpages-ui
  - location-client
  - git-committer
---

# Orchestrator — Real-Time Location Tracking Pipeline

You coordinate 5 sub-agents to implement a complete real-time location tracking pipeline. You MUST call them **sequentially** — never in parallel.

## Architecture

```
LocationService.Client          LocationService              NotificationService         WebApplication.RazorPages
 (Console App)                   (gRPC Server)                (Kafka→SignalR)              (Razor Pages UI)
┌──────────────┐   gRPC Client  ┌──────────────────┐  Kafka  ┌─────────────────┐ SignalR  ┌─────────────────┐
│ Dummy coords ├──Streaming────►│ StreamLocations  ├────────►│ KafkaConsumer   ├────────►│ JS Client       │
│ İstanbul     │                │ LocationTracking │ topic:  │ LocationHub     │  push   │ Real-time table │
│ 50 messages  │                │ Service          │location-│ (BackgroundSvc) │         │ (lat,lng,device)│
└──────────────┘                └──────────────────┘updates  └─────────────────┘         └─────────────────┘
                                         │
                                  AppHost (Aspire)
                                  AddKafka("kafka")
```

## Auto-Commit Rule

**CRITICAL**: After all 4 service agents (steps 1-4) complete successfully and the final `dotnet build` passes, you MUST **automatically** run **@git-committer** (step 5) to commit, push, and create a pull request. Do NOT ask the user for confirmation — this is part of the pipeline and runs unconditionally.

## Strict Sequential Execution Rules

**CRITICAL**: You MUST execute agents in this exact order. Do NOT proceed to the next agent until the current one is fully complete and `dotnet build` succeeds.

### Execution Order

1. **@location-service** — Proto definitions, gRPC server, Kafka producer, AppHost Kafka wiring
2. **@notification-service** — Kafka consumer, SignalR hub, Kafka→SignalR bridge
3. **@razorpages-ui** — SignalR JS client, real-time location display table
4. **@location-client** — gRPC client, dummy Istanbul coordinates, client streaming
5. **@git-committer** *(automatic)* — Commit and push all changes to GitHub, create a pull request. Runs automatically after step 4 succeeds — do NOT ask the user.

### After Each Agent

Run `dotnet build` from the solution root to verify no compilation errors before moving to the next agent. If the build fails, fix the issues before proceeding.

> **After step 4**: Once the build succeeds, immediately proceed to step 5 (@git-committer) without prompting the user.

## Shared Conventions

- **.NET 10** (`net10.0`) — all projects target this
- **Aspire** — `Aspire.AppHost.Sdk/13.1.0`, all services use `AddServiceDefaults()`
- **gRPC Type** — Client Streaming: `rpc StreamLocations(stream LocationUpdate) returns (LocationResponse)`
- **Kafka Topic** — `location-updates`
- **SignalR Method** — `ReceiveLocationUpdate(double latitude, double longitude, string deviceId, string timestamp)`

## Proto Contract

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

## AppHost Kafka Wiring (Done by location-service agent)

In `gRPCLessons.AppHost/AppHost.cs`:
```csharp
var kafka = builder.AddKafka("kafka");

builder.AddProject<Projects.LocationService_GRPC>("locationservice-grpc")
    .WithReference(kafka);

builder.AddProject<Projects.NotificationService>("notificationservice")
    .WithReference(kafka);
```

## Key Files

| Project | Key Files |
|---|---|
| AppHost | `gRPCLessons.AppHost/AppHost.cs` |
| LocationService | `LocationService/Program.cs`, `LocationService/Protos/location.proto`, `LocationService/Services/LocationTrackingService.cs` |
| NotificationService | `NotificationService/Program.cs`, `NotificationService/Services/KafkaConsumerService.cs`, `NotificationService/Hubs/LocationHub.cs` |
| RazorPages | `WebApplication.RazorPages/Pages/Index.cshtml`, `WebApplication.RazorPages/Pages/Index.cshtml.cs` |
| Client | `LocationService.Client/Program.cs` |
