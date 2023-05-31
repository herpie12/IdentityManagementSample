using IdentityManagementSample;
using IdentityManagementSample.Helpers;
using IdentityManagementSample.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Filestore>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication().AddCookie();

builder.Services.AddAuthorization(builder =>
{
    builder.AddPolicy("manager", pb =>
    {
        pb.RequireAuthenticatedUser()
        .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireClaim("role", "manager");
    });
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");


app.MapGet("/register", async (string username, string password, IPasswordHasher<User> hasher, Filestore db, HttpContext ctx) =>
{
    var user = new User() { Name = username };
    user.PasswordHash = hasher.HashPassword(user, password);
    await db.PutAsync(user);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserHelper.Convert(user));

    return user;
});

app.MapGet("/login", async (string username, string password, IPasswordHasher<User> hasher, Filestore db, HttpContext ctx) =>
{
    var user = await db.GetUserAsync(username);

    var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

    if (result == PasswordVerificationResult.Failed)
    {
        return "bad credentials";
    }

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserHelper.Convert(user));

    return "ok";
});

app.MapGet("/promote", async (string username, Filestore db) =>
{
    var user = await db.GetUserAsync(username);
    user.Claims.Add(new UserClaim() { Type = "role", Value = "manager" });

    await db.PutAsync(user);

    return "promoted!";
});

app.MapGet("/protected", () =>
{
    return "user has the role: manager";

}).RequireAuthorization("manager");


app.MapGet("/loginSetCookie", (HttpContext ctx) =>
{
    ctx.Response.Headers["set-cookie"] = "auth=usr:xxx";
    return "ok";
});

app.MapGet("/usernameReadFromCookie", (HttpContext ctx) =>
{
    var authCookie = ctx.Request.Headers.Cookie.FirstOrDefault(x => x.StartsWith("auth="));
    return authCookie;
});


app.Run();
