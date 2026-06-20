using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using TradeJournal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var credentialPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "Firebase",
    "serviceAccountKey.json");

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(credentialPath)
});

var projectId = "tradejournal-5d4cf";

var firestoreDb = new FirestoreDbBuilder
{
    ProjectId = projectId,
    Credential = GoogleCredential.FromFile(credentialPath)
}.Build();

builder.Services.AddSingleton(firestoreDb);
builder.Services.AddScoped<FirestoreService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.WebHost.UseUrls(
    $"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}"
);

var app = builder.Build();
app.UseCors("AngularPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();