using JWTMinimalAPI.DTO;
using JWTMinimalAPI.Helper;
using JWTMinimalAPI.Migrations;
using JWTMinimalAPI.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var securityScheme = new OpenApiSecurityScheme()
{
    Name = "Authorization",
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "JSON Web Token based security",
};


var securityReq = new OpenApiSecurityRequirement()
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        new string[] {}
    }
};

var contactInfo = new OpenApiContact()
{
    Name = "Hiren Shekhda",
    Email = "admin@hiren.com",
    Url = new Uri("https://geekhiren.com")
};

var license = new OpenApiLicense()
{
    Name = "Free License",
};

var info = new OpenApiInfo()
{
    Version = "V1",
    Title = "Employee Api with JWT Authentication",
    Description = "Employee Api with JWT Authentication",
    Contact = contactInfo,
    License = license
};

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", info);
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(securityReq);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateAudience = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateLifetime = false, // In any other application other then demo this needs to be true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

#region Endpoint Token

app.MapPost("/Accounts/Register", [Authorize] async (DBContext db, User user) =>
{
    if (await db.Users.FirstOrDefaultAsync(x => x.Id == user.Id) != null)
    {
        return Results.BadRequest();
    }
    var keyNew = PasswordHelper.GeneratePassword(10);
    var password = PasswordHelper.EncodePassword(user.Password, keyNew);
    user.Password = password;
    user.VCode = keyNew;
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/Accounts/{user.Id}", user);
});

app.MapGet("/Accounts/{id}", [Authorize] async (DBContext db, int id) =>
{
    var item = await db.Users.FirstOrDefaultAsync(x => x.Id == id);

    return item == null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/Accounts/login", [AllowAnonymous] async (DBContext db, UserDto user) =>
{
    var getAllUser = await db.Users.ToListAsync();
    var hashCode = getAllUser.Where(x => x.UserName == user.UserName).FirstOrDefault();
    if (getAllUser.Any(x => x.UserName == user.UserName) && getAllUser.Any(x => x.Password == PasswordHelper.EncodePassword(user.Password, hashCode.VCode)))
    {

        var secureKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var securityKey = new SymmetricSecurityKey(secureKey);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim("Id", "1"),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.Now.AddMinutes(5),
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = credentials
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);
        return Results.Ok(jwtToken);
    }
    return Results.Unauthorized();
});

#endregion Endpoint Token


#region CRUD EMPLOYEE

app.MapGet("/Employees", [Authorize] async (DBContext db) =>
{
    return await db.Employees.Where(x => x.IsActive).ToListAsync();
});

app.MapPost("/Employees/Insert", [Authorize] async (DBContext db, Employee employee) =>
{
    if (await db.Employees.FirstOrDefaultAsync(x => x.Id == employee.Id && x.IsActive) != null)
    {
        return Results.BadRequest();
    }

    db.Employees.Add(employee);
    await db.SaveChangesAsync();
    return Results.Created($"/Employees/{employee.Id}", employee);
});

app.MapGet("/Employees/Get/{id}", [Authorize] async (DBContext db, int id) =>
{
    var item = await db.Employees.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

    return item == null ? Results.NotFound() : Results.Ok(item);
});

app.MapPut("/Employees/Update/{id}", [Authorize] async (DBContext db, int id, Employee employee) =>
{
    var existItem = await db.Employees.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
    if (existItem == null)
    {
        return Results.BadRequest();
    }

    existItem.Name = employee.Name;
    existItem.Salary = employee.Salary;
    existItem.PhoneNumber = employee.PhoneNumber;
    existItem.IsActive = employee.IsActive;

    await db.SaveChangesAsync();
    return Results.Ok(employee);
});

app.MapPut("/Employees/Delete/{id}", [Authorize] async (DBContext db, int id) =>
{
    var existItem = await db.Employees.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
    if (existItem == null)
    {
        return Results.BadRequest();
    }
    existItem.IsActive = false;
    await db.SaveChangesAsync();
    return Results.Ok(existItem);
});



#endregion CRUD EMPLOYEE


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
