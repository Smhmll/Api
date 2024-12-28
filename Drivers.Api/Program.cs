using Drivers.Api.Configurations;
using Drivers.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// JWT Secret kontrolü ve yapılandırması
var secret = builder.Configuration["JwtConfig:Secret"];
if (string.IsNullOrWhiteSpace(secret))
{
    throw new ArgumentNullException("JwtConfig:Secret is missing or empty in appsettings.json");
}
Console.WriteLine($"JWT Secret: {secret}");

// JWT Config ayarlarını yapılandır
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

// Database bağlantısını yapılandır
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity yapılandırması
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // E-posta onayı gerekmiyor
})
.AddEntityFrameworkStores<ApiDbContext>();

// Authentication ve JWT Bearer yapılandırması
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt =>
{
    var key = Encoding.ASCII.GetBytes(secret);
    jwt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, // Gerekirse düzenleyen ekleyin
        ValidateAudience = false, // Gerekirse hedef kitle ekleyin
        RequireExpirationTime = true,
        ValidateLifetime = true
    };
});

// Controller ekle
builder.Services.AddControllers();

// OpenAPI (Swagger) yapılandırması
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Geliştirme ortamı için Swagger/OpenAPI etkinleştirme
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS yönlendirme
app.UseHttpsRedirection();

// Middleware sırası
app.UseAuthentication();
app.UseAuthorization();

// Controller yönlendirmeleri
app.MapControllers();

// Uygulamayı başlat
app.Run();
