using Amazon.Runtime;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Auth.Shared;
using Microsoft.OpenApi;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ---- Serviços ----

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minha API",
        Version = "v1"
    });
});

// AWS SQS
// AWS SQS com credenciais do appsettings
var awsSection = builder.Configuration.GetSection("Aws");
var awsRegion = awsSection["Region"];
var accessKey = awsSection["AccessKey"];
var secretKey = awsSection["SecretKey"];
var sessionToken = awsSection["SessionToken"];

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    AWSCredentials creds;

    if (!string.IsNullOrWhiteSpace(sessionToken))
    {
        // Cenário de LAB / credencial temporária (tem SessionToken)
        creds = new SessionAWSCredentials(accessKey, secretKey, sessionToken);
    }
    else
    {
        // Cenário de IAM user normal (sem SessionToken)
        creds = new BasicAWSCredentials(accessKey, secretKey);
    }

    return new AmazonSQSClient(creds, RegionEndpoint.GetBySystemName(awsRegion));
});



builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("Aws"));

// "Banco" em memória
builder.Services.AddSingleton<UserStore>();

// CORS
var allowedOrigin = builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();
app.UseCors("DefaultCors");


// ---- Pipeline HTTP ----

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minha API v1");
    });
}

// Endpoint simples só pra testar
app.MapGet("/hello", () => "Olá Swagger!");

// Cadastro
app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserStore userStore,
    IAmazonSQS sqs,
    IConfiguration config) =>
{
    if (userStore.EmailExists(request.Email))
        return Results.BadRequest("E-mail já cadastrado.");

    var user = userStore.CreateUser(request.Email, request.Password);

    var msg = new UserRegisteredMessage
    {
        UserId = user.Id.ToString(),
        Email = user.Email,
        RegisteredAt = DateTime.UtcNow
    };

    var queueUrl = config["Aws:UserRegisteredQueueUrl"];

    var sendRequest = new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = JsonSerializer.Serialize(msg)
    };

    await sqs.SendMessageAsync(sendRequest);

    return Results.Ok(new { user.Id, user.Email });
});

// Login
app.MapPost("/api/auth/login", (LoginRequest request, UserStore userStore) =>
{
    var user = userStore.ValidateUser(request.Email, request.Password);
    if (user is null)
        return Results.Unauthorized();

    // Token fake só para exemplo
    var token = Guid.NewGuid().ToString("N");
    return Results.Ok(new { token, userId = user.Id, email = user.Email });
});

app.Run();

// ---- Tipos auxiliares ----

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);

class AwsSettings
{
    public string Region { get; set; } = default!;
    public string UserRegisteredQueueUrl { get; set; } = default!;
}

// Model/Store simples em memória
class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
}

class UserStore
{
    private readonly List<User> _users = new();

    public bool EmailExists(string email) => _users.Any(u => u.Email == email);

    public User CreateUser(string email, string password)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) // precisa do pacote BCrypt.Net-Next
        };
        _users.Add(user);
        return user;
    }

    public User? ValidateUser(string email, string password)
    {
        var user = _users.FirstOrDefault(u => u.Email == email);
        if (user is null) return null;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}
