using NotificationService.Hubs;
using NotificationService.SignalR.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
{
    consumerBuilder.Config.GroupId = "notification-service";
    consumerBuilder.Config.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Latest;
});

builder.Services.AddSignalR();
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

app.MapHub<LocationHub>("/locationHub");

app.Run();
