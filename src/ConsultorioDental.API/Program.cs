using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsultorioDental.API.Common;
using ConsultorioDental.API.Data;
using ConsultorioDental.API.Middleware;
using ConsultorioDental.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Base de datos ----------------
builder.Services.AddDbContext<ConsultorioDbContext>(opciones =>
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSqlServer"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// ---------------- Servicios de la aplicación ----------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IEspecialidadService, EspecialidadService>();
builder.Services.AddScoped<IDentistaService, DentistaService>();
builder.Services.AddScoped<IHorarioDentistaService, HorarioDentistaService>();
builder.Services.AddScoped<IMotivoService, MotivoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IConsultorioService, ConsultorioService>();
builder.Services.AddScoped<ICitaService, CitaService>();

// ---------------- Autenticación JWT ----------------
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");

if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("La clave JWT debe tener al menos 32 caracteres.");

builder.Services.AddAuthentication(opciones =>
{
    opciones.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opciones.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opciones =>
{
    opciones.RequireHttpsMetadata = false;
    opciones.SaveToken = true;
    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ClockSkew = TimeSpan.Zero
    };

    // Respuestas 401/403 con el mismo formato que el resto de la API.
    opciones.Events = new JwtBearerEvents
    {
        OnChallenge = async contexto =>
        {
            contexto.HandleResponse();
            if (contexto.Response.HasStarted) return;

            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            contexto.Response.ContentType = "application/json; charset=utf-8";

            var mensaje = contexto.AuthenticateFailure is null
                ? "Debe iniciar sesión y enviar el token JWT en el encabezado Authorization."
                : "El token enviado no es válido o ya expiró.";

            await contexto.Response.WriteAsync(SerializarError(mensaje));
        },
        OnForbidden = async contexto =>
        {
            if (contexto.Response.HasStarted) return;

            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            contexto.Response.ContentType = "application/json; charset=utf-8";
            await contexto.Response.WriteAsync(
                SerializarError("Su rol no tiene permiso para ejecutar esta operación."));
        }
    };
});

builder.Services.AddAuthorization();

// ---------------- Controladores y validación de modelos ----------------
builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Las validaciones de DataAnnotations se devuelven con el mismo contrato ApiResponse.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => TraducirErrorDeModelo(e.Key, x.ErrorMessage)))
            .Distinct()
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse.Error("Los datos enviados no son válidos. Corrija los errores indicados.", errores));
    };
});

builder.Services.AddEndpointsApiExplorer();

// ---------------- Swagger ----------------
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API - Gestión de Citas de Consultorio Dental",
        Version = "v1",
        Description =
            "Proyecto final INF-4318. API REST para administrar citas, pacientes, dentistas, " +
            "servicios, motivos, consultorios, especialidades y horarios. " +
            "El estado de la cita y el tiempo restante se calculan automáticamente.\n\n" +
            "Para probar los endpoints protegidos: ejecute POST /api/auth/login, copie el token " +
            "y regístrelo con el botón Authorize."
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue únicamente el token JWT devuelto por /api/auth/login."
    });

    // Todos los endpoints declaran el esquema Bearer, por eso el requisito se agrega de forma global.
    opciones.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = new List<string>()
    });

    // Comentarios XML: la documentación del código alimenta a Swagger.
    var archivoXml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var rutaXml = Path.Combine(AppContext.BaseDirectory, archivoXml);
    if (File.Exists(rutaXml)) opciones.IncludeXmlComments(rutaXml, includeControllerXmlComments: true);
});

builder.Services.AddCors(opciones =>
    opciones.AddDefaultPolicy(politica => politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---------------- Pipeline ----------------
// Va de primero para atrapar cualquier excepción del resto del pipeline.
app.UsarManejadorDeExcepciones();

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Consultorio Dental API v1");
    opciones.DocumentTitle = "Consultorio Dental API";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// La raíz lleva directo a la documentación interactiva.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ---------------- Migración y carga inicial ----------------
try
{
    await SeedData.InicializarAsync(app.Services);
}
catch (Exception ex)
{
    // Un fallo de base de datos no debe impedir que la API levante y responda con mensajes claros.
    app.Logger.LogError(ex, "No fue posible preparar la base de datos. Verifique la cadena de conexión.");
}

app.Run();

static string SerializarError(string mensaje) =>
    JsonSerializer.Serialize(ApiResponse.Error(mensaje), new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

// Los errores que genera el propio deserializador vienen en inglés y con detalle técnico
// (posiciones de bytes, tipos internos). Se sustituyen por un mensaje claro para el usuario.
static string TraducirErrorDeModelo(string clave, string mensaje)
{
    if (clave.StartsWith('$'))
        return "El cuerpo de la solicitud no tiene un formato JSON válido. Revise la sintaxis y el tipo de dato de cada campo.";

    if (string.IsNullOrWhiteSpace(mensaje))
        return $"El campo '{clave}' tiene un valor inválido.";

    if (mensaje.Contains("field is required", StringComparison.OrdinalIgnoreCase) ||
        mensaje.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase))
        return "Debe enviar el cuerpo de la solicitud con todos los campos obligatorios.";

    if (mensaje.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
        mensaje.Contains("is not valid for", StringComparison.OrdinalIgnoreCase))
        return $"El campo '{clave}' tiene un valor con formato incorrecto.";

    return $"{clave}: {mensaje}";
}
