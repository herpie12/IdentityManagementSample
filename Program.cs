using IdentityManagementSample;
using IdentityManagementSample.Helpers;
using IdentityManagementSample.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Filestore>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddDataProtection();

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

app.MapGet("/start-password-reset", async (string username, Filestore db, IDataProtectionProvider provider) =>
{
    //Generate hash and this can be sent out to the user through email, for verification.
    var protector = provider.CreateProtector("PasswordReset");
    var user = await db.GetUserAsync(username);
    //protect more than the username, could be guid+username which is saved in the db. Due to a hacker can get the username.
    return protector.Protect(user.Name);
});

app.MapGet("/end-password-reset", async (string username, string password, string hash, Filestore db, IDataProtectionProvider provider, IPasswordHasher<User> hasher) =>
{
    //User will provide the hash which has been sent to them, (the endpoint start pw reset), and the other required fields.
    var protector = provider.CreateProtector("PasswordReset");

    var hashUserName = protector.Unprotect(hash);
    if (hashUserName != username)
    {
        return "bad hash";
    }

    var user = await db.GetUserAsync(username);
    user.PasswordHash = hasher.HashPassword(user, password);
    await db.PutAsync(user);

    return "password reset";
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
