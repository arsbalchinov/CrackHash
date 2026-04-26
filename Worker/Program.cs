using Worker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<WordGenerator>(sp => 
    new WordGenerator(builder.Configuration["Alphabet"] ?? "abcdefghijklmnopqrstuvwxyz0123456789"));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<WorkerRegistrar>();
builder.Services.AddHostedService<HeartbeatSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();