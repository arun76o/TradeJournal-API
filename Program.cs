//using FirebaseAdmin;
//using Google.Apis.Auth.OAuth2;
//using Google.Cloud.Firestore;
//using TradeJournal.Api.Services;
//GoogleCredential credential;
//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

////var credentialPath = Path.Combine(
////    builder.Environment.ContentRootPath,
////    "Firebase",
////    "serviceAccountKey.json");

////FirebaseApp.Create(new AppOptions
////{
////    Credential = GoogleCredential.FromFile(credentialPath)
////});

////var projectId = "tradejournal-5d4cf";

////var firestoreDb = new FirestoreDbBuilder
////{
////    ProjectId = projectId,
////    Credential = GoogleCredential.FromFile(credentialPath)
////}.Build();


//var firebaseCredentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");

//if (!string.IsNullOrEmpty(firebaseCredentials))
//{
//    credential = GoogleCredential.FromJson(firebaseCredentials);
//}
//else
//{
//    var credentialPath = Path.Combine(
//        builder.Environment.ContentRootPath,
//        "Firebase",
//        "serviceAccountKey.json");

//    credential = GoogleCredential.FromFile(credentialPath);
//}

//FirebaseApp.Create(new AppOptions
//{
//    Credential = credential
//});

//var projectId = "tradejournal-5d4cf";

//var firestoreDb = new FirestoreDbBuilder
//{
//    ProjectId = projectId,
//    Credential = credential
//}.Build();

//builder.Services.AddSingleton(firestoreDb);
//builder.Services.AddScoped<FirestoreService>();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AngularPolicy", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

//builder.WebHost.UseUrls(
//    $"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}"
//);

//var app = builder.Build();
//app.UseCors("AngularPolicy");

//      app.UseSwagger();
//    app.UseSwaggerUI();



//app.UseAuthorization();

//app.MapControllers();

//app.Run();

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using TradeJournal.Api.Authentication;
using TradeJournal.Api.Authorization;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

GoogleCredential credential;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Firebase credentials
var firebaseCredentials =
    Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");

if (!string.IsNullOrEmpty(firebaseCredentials))
{
    // Production / Render
    credential = GoogleCredential.FromJson(firebaseCredentials);
}
else
{
    // Local development
    var credentialPath = Path.Combine(
        builder.Environment.ContentRootPath,
        "Firebase",
        "serviceAccountKey.json");

    credential = GoogleCredential.FromFile(credentialPath);
}

FirebaseApp.Create(new AppOptions
{
    Credential = credential
});

var projectId = "tradejournal-5d4cf";

var firestoreDb = new FirestoreDbBuilder
{
    ProjectId = projectId,
    Credential = credential
}.Build();

builder.Services.AddSingleton(firestoreDb);
builder.Services.AddScoped<FirestoreService>();

// Prop Firms workspace (separate Firestore collections: propFirms / propFirmTrades)
builder.Services.AddScoped<PropFirmService>();
builder.Services.AddScoped<PropFirmTradeService>();

// Two-Factor Authentication services
builder.Services.Configure<TwoFactorOptions>(
    builder.Configuration.GetSection(TwoFactorOptions.SectionName));

builder.Services.Configure<BrevoOptions>(
    builder.Configuration.GetSection(BrevoOptions.SectionName));

builder.Services.AddHttpClient<IBrevoEmailService, BrevoEmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Firebase Authentication scheme
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = FirebaseAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = FirebaseAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
        FirebaseAuthenticationHandler.SchemeName,
        options => { });

// Authorization for 2FA
builder.Services.AddScoped<IAuthorizationHandler, TwoFactorAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTwoFactor", policy =>
        policy.Requirements.Add(new TwoFactorRequirement()));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Render provides PORT.
// Don't override launchSettings.json when running locally.
var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

app.UseCors("AngularPolicy");

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();