using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using ThesisTestAPI;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Services;
using ThesisTestAPI.Validators.Post;
using ThesisTestAPI.Validators.Producer;
using ThesisTestAPI.Validators.User;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "http://localhost",
            ValidAudience = "http://localhost",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("linggangguliguliguligwacalingganggulingganggu"))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
//// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEntityFrameworkSqlServer();
builder.Services.AddDbContextPool<ThesisDbContext>(options =>
{
    var conString = configuration.GetConnectionString("SQLServerDB");
    options.UseSqlServer(conString, x =>x.UseNetTopologySuite());
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<SellerService>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddTransient<LikesService>();
builder.Services.AddTransient<PostService>();
builder.Services.AddTransient<ImageService>();
builder.Services.AddTransient<MessageAttachmentService>();
builder.Services.AddTransient<RatingService>();
builder.Services.AddTransient<MidtransService>();
builder.Services.AddTransient<ProcessService>();
builder.Services.AddTransient<BiteshipService>();
builder.Services.AddTransient<NotificationService>();
builder.Services.AddTransient<WalletTransactionService>();
builder.Services.AddTransient<StepService>();
builder.Services.AddTransient<ShipmentService>();
builder.Services.AddScoped<IXmlRepository, DatabaseXmlRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection()
    .SetApplicationName("ThesisApp-Backend") // Ensures all instances use the same key ring
    .AddKeyManagementOptions(options =>
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        var xmlRepository = serviceProvider.GetRequiredService<IXmlRepository>();
        options.XmlRepository = xmlRepository;
    });

builder.Services.AddValidatorsFromAssembly(typeof(UploadPfpValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UserEditValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UserLoginValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(UserRegisterValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(SellerQueryValidator).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(GetCursorPostValidator).Assembly);
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB, adjust as needed
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
builder.Services.AddHttpClient("midtrans", (sp, http) =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MidtransOptions>>().Value;
    var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(opt.ServerKey + ":"));
    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
    http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient("midtrans-iris", (sp, http) =>
{
    var iris = sp.GetRequiredService<IOptions<IrisOptions>>().Value;

    // IRIS uses its own API key (NOT the Snap/Core ServerKey)
    var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes(iris.ApiKey + ":"));

    http.BaseAddress = new Uri(iris.BaseUrl.TrimEnd('/')); // e.g. https://app.sandbox.midtrans.com/iris/api/v1
    http.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", basic);
    http.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    // You�ll add "X-Idempotency-Key" per request when creating payouts,
    // because it must be unique per payout. Don�t set it globally here.
});
builder.Services.Configure<MidtransOptions>(configuration.GetSection("Midtrans"));
builder.Services.Configure<IrisOptions>(configuration.GetSection("Iris"));
builder.Services.Configure<BiteshipOptions>(configuration.GetSection("Biteship"));
var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ChatHub>("/hubs/chat");

app.MapControllers();

app.Run();
public class MidtransOptions
{
    public string ServerKey { get; set; } = default!;
    public string ClientKey { get; set; } = default!;
    public string SnapUrl { get; set; } = default!;
    public string CoreStatusUrl { get; set; } = default!;
}
public sealed class IrisOptions
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string SenderName { get; set; } = "Your Company";
}

public class BiteshipOptions
{
    public string ApiKey { get; set; } = "";
}