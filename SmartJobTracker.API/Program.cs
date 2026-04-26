using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SmartJobTracker.API.Data;
using SmartJobTracker.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Business services ────────────────────────────────────────────────────────
builder.Services.AddScoped<IResumeParserService, ResumeParserService>();
builder.Services.AddScoped<IJobMatchingService, JobMatchingService>();
builder.Services.AddScoped<IJobSearchService, JobSearchService>();
builder.Services.AddScoped<IAIJobSearchService, AIJobSearchService>();

// External job aggregation (Adzuna + LinkedIn + Naukri + JSearch + NodeFlair)
builder.Services.AddHttpClient<AdzunaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<CareersGovService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
});
builder.Services.AddHttpClient<LinkedInJobsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<NaukriJobsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IExternalJobService, ExternalJobService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(50);
});

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"]  ?? "SmartJobTracker",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "SmartJobTrackerUI",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero   // no grace period on expiry
        };
    });

builder.Services.AddAuthorization();

// ── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// ── Auto-create / migrate DB tables ─────────────────────────────────────────
// Safe raw-SQL pattern: IF OBJECT_ID IS NULL → CREATE TABLE / IF COL IS NULL → ALTER.
// Never drops or destructively modifies existing data.
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // ── 1. UserProfiles ──────────────────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[UserProfiles]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserProfiles] (
                    [Id]                INT           IDENTITY(1,1) NOT NULL,
                    [FullName]          NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_UP_FullName] DEFAULT N'',
                    [Email]             NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_UP_Email]    DEFAULT N'',
                    [Phone]             NVARCHAR(MAX) NULL,
                    [Country]           NVARCHAR(MAX) NULL,
                    [PreferredLocation] NVARCHAR(MAX) NULL,
                    [LocationType]      NVARCHAR(MAX) NULL,
                    [MinExperienceYears] INT          NULL,
                    [MaxExperienceYears] INT          NULL,
                    [PreferredRoles]    NVARCHAR(MAX) NULL,
                    [Skills]            NVARCHAR(MAX) NULL,
                    [Education]         NVARCHAR(MAX) NULL,
                    [Summary]           NVARCHAR(MAX) NULL,
                    [ResumeFileName]    NVARCHAR(MAX) NULL,
                    [PasswordHash]      NVARCHAR(MAX) NULL,
                    [CreatedAt]         DATETIME2     NOT NULL CONSTRAINT [DF_UP_CreatedAt] DEFAULT GETUTCDATE(),
                    [UpdatedAt]         DATETIME2     NOT NULL CONSTRAINT [DF_UP_UpdatedAt] DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_UserProfiles] PRIMARY KEY ([Id])
                );
            END
        ");
        log.LogInformation("UserProfiles table ready.");
    }
    catch (Exception ex) { log.LogWarning("UserProfiles setup skipped: {Msg}", ex.Message); }

    // ── 1b. Add PasswordHash column to existing UserProfiles table ───────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'[dbo].[UserProfiles]')
                  AND name = N'PasswordHash'
            )
            BEGIN
                ALTER TABLE [dbo].[UserProfiles]
                ADD [PasswordHash] NVARCHAR(MAX) NULL;
            END
        ");
        log.LogInformation("PasswordHash column ready.");
    }
    catch (Exception ex) { log.LogWarning("PasswordHash column migration skipped: {Msg}", ex.Message); }

    // ── 2. JobListings ───────────────────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[JobListings]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[JobListings] (
                    [Id]          INT            IDENTITY(1,1) NOT NULL,
                    [Title]       NVARCHAR(MAX)  NOT NULL CONSTRAINT [DF_JL_Title]   DEFAULT N'',
                    [Company]     NVARCHAR(MAX)  NOT NULL CONSTRAINT [DF_JL_Company] DEFAULT N'',
                    [Location]    NVARCHAR(MAX)  NULL,
                    [JobType]     NVARCHAR(MAX)  NULL,
                    [Description] NVARCHAR(MAX)  NULL,
                    [Requirements] NVARCHAR(MAX) NULL,
                    [SalaryMin]   DECIMAL(18,2)  NULL,
                    [SalaryMax]   DECIMAL(18,2)  NULL,
                    [Currency]    NVARCHAR(MAX)  NULL,
                    [Source]      NVARCHAR(MAX)  NULL,
                    [SourceUrl]   NVARCHAR(MAX)  NULL,
                    [PostedDate]  DATETIME2      NOT NULL CONSTRAINT [DF_JL_PostedDate] DEFAULT GETUTCDATE(),
                    [IsEasyApply] BIT            NOT NULL CONSTRAINT [DF_JL_EasyApply]  DEFAULT 0,
                    [MatchScore]  INT            NULL,
                    [Tags]        NVARCHAR(MAX)  NULL,
                    [CreatedAt]   DATETIME2      NOT NULL CONSTRAINT [DF_JL_CreatedAt]  DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_JobListings] PRIMARY KEY ([Id])
                );
                -- Seed sample job listings
                SET IDENTITY_INSERT [dbo].[JobListings] ON;
                INSERT INTO [dbo].[JobListings] ([Id],[Title],[Company],[Location],[JobType],[Description],[Requirements],[SalaryMin],[SalaryMax],[Currency],[Source],[SourceUrl],[PostedDate],[IsEasyApply],[MatchScore],[Tags],[CreatedAt]) VALUES
                (1,N'Senior .NET Developer',N'Microsoft',N'Remote',N'FullTime',N'Join our cloud infrastructure team.',N'.NET 6+, C#, Azure, SQL Server, REST APIs',120000,160000,N'USD',N'LinkedIn',N'https://linkedin.com/jobs/1','2026-04-16',1,85,N'Backend,.NET,Azure,Senior,Remote','2026-04-21'),
                (2,N'Full Stack Engineer',N'Google',N'Mountain View, CA',N'FullTime',N'Join Google''s Search team.',N'JavaScript, TypeScript, React, Node.js, GCP',140000,180000,N'USD',N'Indeed',N'https://indeed.com/jobs/2','2026-04-18',0,72,N'FullStack,JavaScript,React','2026-04-21'),
                (3,N'React Developer',N'Spotify',N'Remote',N'FullTime',N'Build amazing user experiences.',N'React, JavaScript/TypeScript, CSS, REST APIs',100000,140000,N'USD',N'LinkedIn',N'https://linkedin.com/jobs/3','2026-04-19',1,78,N'Frontend,React,JavaScript,Remote','2026-04-21'),
                (4,N'Cloud Infrastructure Engineer',N'AWS',N'Seattle, WA',N'FullTime',N'Design and maintain cloud infrastructure.',N'AWS, Terraform, Kubernetes, Docker',130000,170000,N'USD',N'Glassdoor',N'https://glassdoor.com/jobs/4','2026-04-17',0,68,N'DevOps,AWS,Kubernetes,Cloud','2026-04-21'),
                (5,N'Backend Engineer',N'Uber',N'San Francisco, CA',N'FullTime',N'Build scalable backend systems.',N'Java 11+, Spring Boot, Kafka, MySQL',125000,165000,N'USD',N'LinkedIn',N'https://linkedin.com/jobs/5','2026-04-20',1,55,N'Backend,Java,Microservices','2026-04-21');
                SET IDENTITY_INSERT [dbo].[JobListings] OFF;
            END
        ");
        log.LogInformation("JobListings table ready.");
    }
    catch (Exception ex) { log.LogWarning("JobListings setup skipped: {Msg}", ex.Message); }

    // ── 3. JobApplications ───────────────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[JobApplications]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[JobApplications] (
                    [Id]            INT           IDENTITY(1,1) NOT NULL,
                    [JobListingId]  INT           NOT NULL,
                    [UserProfileId] INT           NOT NULL,
                    [Status]        NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_JA_Status] DEFAULT N'Applied',
                    [AppliedDate]   DATETIME2     NOT NULL CONSTRAINT [DF_JA_AppliedDate] DEFAULT GETUTCDATE(),
                    [Notes]         NVARCHAR(MAX) NULL,
                    [CoverLetter]   NVARCHAR(MAX) NULL,
                    [LastUpdatedAt] DATETIME2     NOT NULL CONSTRAINT [DF_JA_UpdatedAt] DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_JobApplications] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_JobApplications_JobListings]  FOREIGN KEY ([JobListingId])  REFERENCES [dbo].[JobListings]([Id])  ON DELETE CASCADE,
                    CONSTRAINT [FK_JobApplications_UserProfiles] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles]([Id]) ON DELETE CASCADE
                );
            END
        ");
        log.LogInformation("JobApplications table ready.");
    }
    catch (Exception ex) { log.LogWarning("JobApplications setup skipped: {Msg}", ex.Message); }

    // ── 4. SavedJobs ─────────────────────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[SavedJobs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SavedJobs] (
                    [Id]            INT           IDENTITY(1,1) NOT NULL,
                    [JobListingId]  INT           NOT NULL,
                    [UserProfileId] INT           NOT NULL,
                    [SavedDate]     DATETIME2     NOT NULL CONSTRAINT [DF_SJ_SavedDate] DEFAULT GETUTCDATE(),
                    [Notes]         NVARCHAR(MAX) NULL,
                    CONSTRAINT [PK_SavedJobs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SavedJobs_JobListings]  FOREIGN KEY ([JobListingId])  REFERENCES [dbo].[JobListings]([Id])  ON DELETE CASCADE,
                    CONSTRAINT [FK_SavedJobs_UserProfiles] FOREIGN KEY ([UserProfileId]) REFERENCES [dbo].[UserProfiles]([Id]) ON DELETE CASCADE
                );
            END
        ");
        log.LogInformation("SavedJobs table ready.");
    }
    catch (Exception ex) { log.LogWarning("SavedJobs setup skipped: {Msg}", ex.Message); }

    // ── 5. ExternalJobApplications ───────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[ExternalJobApplications]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ExternalJobApplications] (
                    [Id]                    INT             IDENTITY(1,1) NOT NULL,
                    [UserProfileId]         INT             NOT NULL,
                    [Title]                 NVARCHAR(500)   NOT NULL CONSTRAINT [DF_ExtJobApp_Title] DEFAULT N'',
                    [Company]               NVARCHAR(500)   NOT NULL CONSTRAINT [DF_ExtJobApp_Company] DEFAULT N'',
                    [Location]              NVARCHAR(500)   NULL,
                    [Source]                NVARCHAR(200)   NULL,
                    [ApplyUrl]              NVARCHAR(2000)  NULL,
                    [JobType]               NVARCHAR(100)   NULL,
                    [SalaryMin]             DECIMAL(18,2)   NULL,
                    [SalaryMax]             DECIMAL(18,2)   NULL,
                    [Currency]              NVARCHAR(10)    NULL,
                    [Skills]                NVARCHAR(MAX)   NULL,
                    [MatchScore]            INT             NULL,
                    [AiConfidenceScore]     INT             NULL,
                    [VisaSponsorshipChance] NVARCHAR(50)    NULL,
                    [JobPostedDate]         DATETIME2       NULL,
                    [Status]                NVARCHAR(50)    NOT NULL CONSTRAINT [DF_ExtJobApp_Status] DEFAULT N'Applied',
                    [AppliedAt]             DATETIME2       NOT NULL CONSTRAINT [DF_ExtJobApp_AppliedAt] DEFAULT GETUTCDATE(),
                    [LastUpdatedAt]         DATETIME2       NOT NULL CONSTRAINT [DF_ExtJobApp_UpdatedAt] DEFAULT GETUTCDATE(),
                    [CoverNote]             NVARCHAR(MAX)   NULL,
                    [RecruiterMessage]      NVARCHAR(MAX)   NULL,
                    [Notes]                 NVARCHAR(MAX)   NULL,
                    CONSTRAINT [PK_ExternalJobApplications] PRIMARY KEY ([Id])
                );
            END
        ");
        log.LogInformation("ExternalJobApplications table ready.");
    }
    catch (Exception ex) { log.LogWarning("ExternalJobApplications setup skipped: {Msg}", ex.Message); }

    // ── 6. ExternalSavedJobs ─────────────────────────────────────────────────
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'[dbo].[ExternalSavedJobs]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ExternalSavedJobs] (
                    [Id]                    INT             IDENTITY(1,1) NOT NULL,
                    [UserProfileId]         INT             NOT NULL,
                    [ExternalJobId]         NVARCHAR(500)   NOT NULL CONSTRAINT [DF_ESJ_ExtJobId] DEFAULT N'',
                    [Title]                 NVARCHAR(500)   NOT NULL CONSTRAINT [DF_ESJ_Title] DEFAULT N'',
                    [Company]               NVARCHAR(500)   NOT NULL CONSTRAINT [DF_ESJ_Company] DEFAULT N'',
                    [Location]              NVARCHAR(500)   NULL,
                    [Source]                NVARCHAR(200)   NULL,
                    [ApplyUrl]              NVARCHAR(2000)  NULL,
                    [JobType]               NVARCHAR(100)   NULL,
                    [Salary]                NVARCHAR(200)   NULL,
                    [SalaryMin]             DECIMAL(18,2)   NULL,
                    [SalaryMax]             DECIMAL(18,2)   NULL,
                    [Currency]              NVARCHAR(10)    NULL,
                    [Skills]                NVARCHAR(MAX)   NULL,
                    [MatchPercent]          INT             NOT NULL CONSTRAINT [DF_ESJ_Match] DEFAULT 0,
                    [VisaSponsorshipChance] NVARCHAR(50)    NULL,
                    [PostedDate]            DATETIME2       NULL,
                    [Description]           NVARCHAR(MAX)   NULL,
                    [SavedDate]             DATETIME2       NOT NULL CONSTRAINT [DF_ESJ_SavedDate] DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_ExternalSavedJobs] PRIMARY KEY ([Id])
                );
            END
        ");
        log.LogInformation("ExternalSavedJobs table ready.");
    }
    catch (Exception ex) { log.LogWarning("ExternalSavedJobs setup skipped: {Msg}", ex.Message); }
}

app.Run();
