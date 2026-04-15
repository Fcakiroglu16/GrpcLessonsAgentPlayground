var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");

var locationService = builder.AddProject<Projects.LocationService_GRPC>("locationservice-grpc")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.AddProject<Projects.LocationService_Client>("locationservice-client")
    .WithReference(locationService)
    .WaitFor(locationService);

var notificationService = builder.AddProject<Projects.NotificationService_SignalR>("notificationservice")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.AddProject<Projects.WebApplication_RazorPages>("webapplication-razorpages")
    .WithReference(notificationService);

builder.Build().Run();
