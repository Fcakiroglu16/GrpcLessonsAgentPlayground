---
name: razorpages-ui
description: "Use when working on WebApplication.RazorPages: SignalR client, location display UI, or real-time map/table updates. This is Step 3 in the pipeline — depends on NotificationService SignalR hub."
tools:
  - read
  - search
  - edit
  - execute
---

# RazorPages UI Agent — SignalR Client + Real-Time Display (Step 3)

## Required Skill — dotnet-service-communication

**Before implementing any code**, read the skill file at `.claude/skills/dotnet-service-communication/SKILL.md` and apply its decision matrix and protocol guidance to all service communication choices in this agent's scope. Use the decision flowchart to validate that the chosen protocols (gRPC, SignalR, SSE, REST) are appropriate for each communication pattern.

You implement the web UI that connects to NotificationService's SignalR hub and displays real-time location updates. You run **after** the notification-service agent completes.

## Dependency

The notification-service agent must have already:
- Created the SignalR hub at `/locationHub`
- Defined the `ReceiveLocationUpdate` method signature: `(double latitude, double longitude, string deviceId, string timestamp)`

## Tasks

### 1. Update Index.cshtml

Replace `WebApplication.RazorPages/Pages/Index.cshtml` with a real-time location tracking page:

Requirements:
- Page title: "Real-Time Location Tracker"
- Connection status indicator (Connected/Disconnected/Reconnecting) with color coding
- HTML table with columns: **Device ID**, **Latitude**, **Longitude**, **Timestamp**
- Table updates in real-time — new rows added at the top, keep max 100 rows
- Include SignalR JS client via CDN: `https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js`
- JavaScript that:
  - Creates HubConnection to NotificationService's `/locationHub` endpoint
  - Listens for `ReceiveLocationUpdate` events
  - Adds a new row to the table for each update
  - Handles connection lifecycle (start, reconnecting, reconnected, closed)
  - Auto-reconnects on disconnect
- Basic CSS styling for the table (borders, alternating row colors, responsive)

### 2. Update Index.cshtml.cs (if needed)

The page model likely needs no changes — the real-time updates happen client-side via JavaScript. Keep the existing `OnGet()` method.

### 3. Determine NotificationService URL

The SignalR hub URL should be resolved via Aspire service discovery. In the JavaScript, connect to the NotificationService endpoint. The URL pattern depends on whether running via Aspire or standalone:
- Via Aspire: Use the service name from AppHost (e.g., `https+http://notificationservice`)
- For simplicity in JS: Use a relative path if the RazorPages app proxies, OR inject the URL from server-side configuration

**Recommended approach**: Pass the NotificationService URL from the server to the client. Add the URL to `appsettings.json` or resolve it via Aspire's service discovery configuration. In `Index.cshtml.cs`, expose a property with the hub URL. In the Razor page, use `@Model.HubUrl` in the JavaScript.

Alternatively for simplicity: hardcode the URL from Aspire's service endpoint and document it clearly.

### 4. Program.cs Changes (if needed)

`WebApplication.RazorPages/Program.cs` likely needs no changes. The existing setup with `AddRazorPages()`, `MapRazorPages()`, and `AddServiceDefaults()` is sufficient.

If service discovery is used for the NotificationService URL, you may need to add configuration binding.

## Reference: Current Files

**Index.cshtml** — Currently the default Aspire/Razor welcome page. Replace entirely.

**Program.cs**:
```csharp
var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorPages();
// ... standard Razor Pages pipeline
```

## Verification

After all changes, run `dotnet build` from the solution root. All projects must compile successfully.
