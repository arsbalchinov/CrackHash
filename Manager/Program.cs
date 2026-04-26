using Manager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем сервисы
builder.Services.AddSingleton<RequestStore>();
builder.Services.AddSingleton<WorkerRegistry>();
builder.Services.AddSingleton<WorkerClient>();
builder.Services.AddHttpClient<WorkerClient>();

// Добавляем фоновый сервис мониторинга
builder.Services.AddHostedService<HeartbeatMonitor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();