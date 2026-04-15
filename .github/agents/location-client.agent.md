---
name: location-client
description: "Use when working on LocationService.Client: gRPC client setup, dummy data generation, or client streaming to LocationService. This is Step 4 (last) in the pipeline — depends on LocationService proto."
tools:
  - read
  - search
  - edit
  - execute
---

# LocationService.Client Agent — gRPC Client + Dummy Data (Step 4)

## Required Skill — dotnet-service-communication

**Before implementing any code**, read the skill file at `.claude/skills/dotnet-service-communication/SKILL.md` and apply its decision matrix and protocol guidance to all service communication choices in this agent's scope. Use the decision flowchart to validate that the chosen protocols (gRPC, SignalR, SSE, REST) are appropriate for each communication pattern.

You implement the console client that streams dummy location data to LocationService via gRPC client streaming. You are the **last** agent in the pipeline.

## Dependency

The location-service agent must have already:
- Created `LocationService/Protos/location.proto` with `StreamLocations` RPC
- The gRPC server is fully implemented and builds successfully

## Tasks

### 1. Update .csproj

In `LocationService.Client/LocationService.Client.csproj`:
- Change SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web` (needed for Aspire hosting)
- Add NuGet packages:
  - `Google.Protobuf`
  - `Grpc.Net.Client`
  - `Grpc.Tools`
- Add Protobuf reference (as Client):
  ```xml
  <ItemGroup>
    <Protobuf Include="..\LocationService\Protos\location.proto" GrpcServices="Client" Link="Protos\location.proto" />
  </ItemGroup>
  ```
- Add ServiceDefaults project reference:
  ```xml
  <ProjectReference Include="..\gRPCLessons.ServiceDefaults\gRPCLessons.ServiceDefaults.csproj" />
  ```

Current csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.23.0" />
  </ItemGroup>
</Project>
```

### 2. Replace Program.cs

Replace `LocationService.Client/Program.cs` entirely. The new implementation must:

- Create a `WebApplicationBuilder` with `AddServiceDefaults()` (for Aspire service discovery)
- Resolve the LocationService gRPC endpoint via Aspire service discovery using `https+http://locationservice-grpc`
- Create a `GrpcChannel` to the LocationService
- Create a `LocationTracking.LocationTrackingClient`
- Open a client streaming call via `client.StreamLocations()`
- Generate and send **50 location messages** with:
  - Random latitude: between 40.9 and 41.1 (Istanbul area)
  - Random longitude: between 28.8 and 29.1 (Istanbul area)
  - `device_id`: randomly picked from a set like `["device-1", "device-2", "device-3"]`
  - `timestamp`: current UTC time as `Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)`
- Wait 1-2 seconds between each message (random delay)
- After sending all messages, call `CompleteAsync()` on the request stream
- Read and log the `LocationResponse` (received_count, status)

### 3. Update AppHost (if needed)

In `gRPCLessons.AppHost/AppHost.cs`:
- The LocationService.Client may need a reference to LocationService for service discovery:
  ```csharp
  builder.AddProject<Projects.LocationService_Client>("locationservice-client")
      .WithReference(locationService);
  ```
- This requires the LocationService to be stored in a variable:
  ```csharp
  var locationService = builder.AddProject<Projects.LocationService_GRPC>("locationservice-grpc")
      .WithReference(kafka);
  ```

## Reference: Current Program.cs

```csharp
Console.WriteLine("Hello, World!");
```

## Dummy Data Example

```
Message 1: device-2 @ (41.0082, 28.9784) — 2026-04-15T10:30:00Z
Message 2: device-1 @ (40.9631, 29.0251) — 2026-04-15T10:30:01Z
Message 3: device-3 @ (41.0551, 28.8410) — 2026-04-15T10:30:03Z
...
```

## Verification

After all changes, run `dotnet build` from the solution root. All projects must compile successfully. Then run the full Aspire AppHost to test the end-to-end pipeline.
