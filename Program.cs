using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;
using System.IO;
using VCashApp.Data;
using VCashApp.Data.Seed;
using VCashApp.Extentions;
using VCashApp.Filters;
using VCashApp.Models;
using VCashApp.Services;
using VCashApp.Services.Cef;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "AppLogs",
            AutoCreateSqlTable = true
        },
        columnOptions: new ColumnOptions
        {
            TimeStamp = { ColumnName = "Timestamp", DataType = SqlDbType.DateTimeOffset, NonClusteredIndex = true },
            Level = { ColumnName = "Level", DataType = SqlDbType.NVarChar, DataLength = 12 },
            Message = { ColumnName = "Message", DataType = SqlDbType.NVarChar, DataLength = -1 },
            Exception = { ColumnName = "Exception", DataType = SqlDbType.NVarChar, DataLength = -1 },
            Properties = { ColumnName = "Properties", DataType = SqlDbType.NVarChar, DataLength = -1 },

            AdditionalColumns = new List<SqlColumn>
            {
                // SourceContext se añade automáticamente por ILogger<T>
                new SqlColumn("SourceContext", SqlDbType.NVarChar) { ColumnName = "SourceContext", DataLength = 255, AllowNull = true },

                new SqlColumn{ ColumnName = "Username", PropertyName = "Usuario", DataType = SqlDbType.NVarChar, DataLength = 255, AllowNull = true },
                new SqlColumn{ ColumnName = "IpAddress", PropertyName = "IP", DataType = SqlDbType.NVarChar, DataLength = 45, AllowNull = true },
                new SqlColumn{ ColumnName = "Action", PropertyName = "Accion", DataType = SqlDbType.NVarChar, DataLength = 255, AllowNull = true },

                new SqlColumn("Resultado", SqlDbType.NVarChar) { ColumnName = "Result", DataLength = 255, AllowNull = true },
                new SqlColumn("Conteo", SqlDbType.Int, true) { ColumnName = "Count" },
                new SqlColumn("TipoEntidad", SqlDbType.NVarChar) { ColumnName = "EntityType", DataLength = 50, AllowNull = true },
                new SqlColumn("ID_Entidad", SqlDbType.NVarChar) { ColumnName = "EntityId", DataLength = 450, AllowNull = true },
                new SqlColumn("FormatoExportacion", SqlDbType.NVarChar) { ColumnName = "ExportFormat", DataLength = 50, AllowNull = true },
                new SqlColumn("Mensaje", SqlDbType.NVarChar) { ColumnName = "DetailMessage", DataLength = -1, AllowNull = true },
                new SqlColumn("IdURL", SqlDbType.NVarChar) { ColumnName = "UrlId", DataLength = 450, AllowNull = true },
                new SqlColumn("IdModelo", SqlDbType.NVarChar) { ColumnName = "ModelId", DataLength = 450, AllowNull = true },
    
                // Columnas adicionales para filtros de permisos
                new SqlColumn("RolNombre", SqlDbType.NVarChar) { ColumnName = "RoleNameFilter", DataLength = 255, AllowNull = true },
                new SqlColumn("IdRolDB", SqlDbType.NVarChar) { ColumnName = "RoleIdFilter", DataLength = 450, AllowNull = true },
                new SqlColumn("CodigoVista", SqlDbType.NVarChar) { ColumnName = "ViewCodeFilter", DataLength = 50, AllowNull = true },
                new SqlColumn("PuedeVer", SqlDbType.Bit, true) { ColumnName = "CanViewFilter" },
                new SqlColumn("PuedeCrear", SqlDbType.Bit, true) { ColumnName = "CanCreateFilter" },
                new SqlColumn("PuedeEditar", SqlDbType.Bit, true) { ColumnName = "CanEditFilter" },
                new SqlColumn("TipoPermiso", SqlDbType.NVarChar) { ColumnName = "PermissionTypeFilter", DataLength = 50, AllowNull = true },
                new SqlColumn("RolesUsuario", SqlDbType.NVarChar) { ColumnName = "UserRolesFilter", DataLength = -1, AllowNull = true },
            }
        },
        restrictedToMinimumLevel: LogEventLevel.Information
    )
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Request starting")) // Filtra el inicio de solicitudes HTTP
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Request finished")) // Filtra el fin de solicitudes HTTP
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Route matched with")) // Mensajes de enrutamiento
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed DbCommand")) // Consultas SQL generadas por EF Core
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing ViewResult")) // Ejecución de vistas MVC
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed ViewResult"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed action")) // Detalle de acciones ejecutadas
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executed endpoint"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Start processing HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Sending HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Received HTTP response"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("End processing HTTP request"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Accessing expired session"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing RedirectResult"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("The query uses a row limiting operator"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("The file"))
    .Filter.ByExcluding(e => e.MessageTemplate.Text.Contains("Executing JsonResult"))
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. Configuración de Servicios ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.LogTo(Log.Information, LogLevel.Information);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataProtectionKeysStable")))
    .SetApplicationName("VCashApp");

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddResponseCompression(options =>
{
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/plain", "application/javascript", "text/css", "application/x-font-woff", "application/font-sfnt", "application/font-ttf", "application/font-otf", "image/svg+xml" });
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeLogService, EmployeeLogService>();
builder.Services.AddScoped<IRutaDiariaService, RutaDiariaService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICefTransactionService, CefTransactionService>();
builder.Services.AddScoped<ICefContainerService, CefContainerService>();
builder.Services.AddScoped<ICefIncidentService, CefIncidentService>();

builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Por favor ingrese 'Bearer ' + token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VCashApp API",
        Version = "v1",
        Description = "API de gestión de registros y rutas de empleados para VCashApp."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    else
    {
        Console.WriteLine($"WARNING: XML Documentation file not found at: {xmlPath}");
    }
    c.EnableAnnotations();
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return true;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VCashApp API V1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "VCashApp API Documentation";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseMiddleware<LogIpMiddleware>();


app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await IdentitySeedData.SeedRolesAsync(roleManager);
    await IdentitySeedData.SeedUsersAsync(userManager, roleManager, dbContext);
    await IdentitySeedData.SeedBranchPermissionsAsync(dbContext, userManager);
    await IdentitySeedData.SeedPermissionsAsync(dbContext, roleManager);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();