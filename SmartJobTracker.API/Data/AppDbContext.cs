using Microsoft.EntityFrameworkCore;
using SmartJobTracker.API.Entities;

namespace SmartJobTracker.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<JobListing> JobListings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<ExternalJobApplication> ExternalJobApplications { get; set; }
        public DbSet<ExternalSavedJob> ExternalSavedJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.JobListing)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(a => a.JobListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.UserProfile)
                .WithMany(u => u.JobApplications)
                .HasForeignKey(a => a.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedJob>()
                .HasOne(s => s.JobListing)
                .WithMany(j => j.SavedJobs)
                .HasForeignKey(s => s.JobListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedJob>()
                .HasOne(s => s.UserProfile)
                .WithMany(u => u.SavedJobs)
                .HasForeignKey(s => s.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed realistic job listings
            var seedDate = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc);
            var jobs = new List<JobListing>
            {
                new JobListing
                {
                    Id = 1,
                    Title = "Senior .NET Developer",
                    Company = "Microsoft",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "We are looking for a Senior .NET Developer with 5+ years of experience to join our cloud infrastructure team. You will work on high-scale distributed systems using C# and .NET.",
                    Requirements = ".NET 6+, C#, Azure, SQL Server, REST APIs, Microservices, Docker, Kubernetes",
                    SalaryMin = 120000,
                    SalaryMax = 160000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/1",
                    PostedDate = seedDate.AddDays(-5),
                    IsEasyApply = true,
                    MatchScore = 85,
                    Tags = "Backend,.NET,Azure,Senior,Remote",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 2,
                    Title = "Full Stack Engineer",
                    Company = "Google",
                    Location = "Mountain View, CA",
                    JobType = "FullTime",
                    Description = "Join Google's Search team as a Full Stack Engineer. You'll work with React on the frontend and Node.js on the backend, serving millions of users daily.",
                    Requirements = "JavaScript, TypeScript, React, Node.js, GCP, MongoDB, REST APIs",
                    SalaryMin = 140000,
                    SalaryMax = 180000,
                    Currency = "USD",
                    Source = "Indeed",
                    SourceUrl = "https://indeed.com/jobs/2",
                    PostedDate = seedDate.AddDays(-3),
                    IsEasyApply = false,
                    MatchScore = 72,
                    Tags = "FullStack,JavaScript,React,Node.js,Google",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 3,
                    Title = "React Developer",
                    Company = "Spotify",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build amazing user experiences for our music streaming platform. You'll work with React, Redux, and modern frontend tooling.",
                    Requirements = "React, JavaScript/TypeScript, CSS, REST APIs, Redux, Testing",
                    SalaryMin = 100000,
                    SalaryMax = 140000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/3",
                    PostedDate = seedDate.AddDays(-2),
                    IsEasyApply = true,
                    MatchScore = 78,
                    Tags = "Frontend,React,JavaScript,Remote",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 4,
                    Title = "Cloud Infrastructure Engineer",
                    Company = "AWS",
                    Location = "Seattle, WA",
                    JobType = "FullTime",
                    Description = "Design and maintain cloud infrastructure for AWS services. Work with Terraform, Kubernetes, and cutting-edge DevOps tools.",
                    Requirements = "AWS, Terraform, Kubernetes, Docker, CI/CD, Linux, Go/Python",
                    SalaryMin = 130000,
                    SalaryMax = 170000,
                    Currency = "USD",
                    Source = "Glassdoor",
                    SourceUrl = "https://glassdoor.com/jobs/4",
                    PostedDate = seedDate.AddDays(-4),
                    IsEasyApply = false,
                    MatchScore = 68,
                    Tags = "DevOps,AWS,Kubernetes,Cloud,Infrastructure",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 5,
                    Title = "Backend Engineer - Java",
                    Company = "Uber",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Build scalable backend systems serving millions of rides daily. Use Java, Spring Boot, and distributed systems patterns.",
                    Requirements = "Java 11+, Spring Boot, Kafka, MySQL, Redis, Microservices",
                    SalaryMin = 125000,
                    SalaryMax = 165000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/5",
                    PostedDate = seedDate.AddDays(-1),
                    IsEasyApply = true,
                    MatchScore = 55,
                    Tags = "Backend,Java,Microservices,Kafka",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 6,
                    Title = "Machine Learning Engineer",
                    Company = "OpenAI",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Work on cutting-edge AI models and deploy them to production. Use Python, PyTorch, and large-scale distributed computing.",
                    Requirements = "Python, PyTorch, TensorFlow, Machine Learning, Statistics, CUDA",
                    SalaryMin = 150000,
                    SalaryMax = 200000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/6",
                    PostedDate = seedDate.AddDays(-6),
                    IsEasyApply = false,
                    MatchScore = 45,
                    Tags = "ML,AI,Python,PyTorch,Research",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 7,
                    Title = "QA Engineer",
                    Company = "Apple",
                    Location = "Cupertino, CA",
                    JobType = "FullTime",
                    Description = "Ensure quality of iOS and macOS applications. Write automated tests, manage test infrastructure, and collaborate with developers.",
                    Requirements = "Selenium, Test Automation, Python/JavaScript, CI/CD, Agile",
                    SalaryMin = 90000,
                    SalaryMax = 130000,
                    Currency = "USD",
                    Source = "Glassdoor",
                    SourceUrl = "https://glassdoor.com/jobs/7",
                    PostedDate = seedDate.AddDays(-7),
                    IsEasyApply = true,
                    MatchScore = 60,
                    Tags = "QA,Testing,Automation,Apple",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 8,
                    Title = "DevOps Engineer",
                    Company = "Netflix",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build and maintain the infrastructure that serves millions of streams daily. Use Kubernetes, Docker, and monitoring tools.",
                    Requirements = "Kubernetes, Docker, CI/CD, Monitoring (Prometheus), Linux, Go/Python",
                    SalaryMin = 120000,
                    SalaryMax = 160000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/8",
                    PostedDate = seedDate.AddDays(-8),
                    IsEasyApply = true,
                    MatchScore = 70,
                    Tags = "DevOps,Kubernetes,Docker,Remote,Netflix",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 9,
                    Title = "Senior Software Architect",
                    Company = "IBM",
                    Location = "New York, NY",
                    JobType = "FullTime",
                    Description = "Lead architectural decisions for enterprise systems. 10+ years of experience designing large-scale distributed applications.",
                    Requirements = ".NET, Java, C++, System Design, Enterprise Architecture, Cloud",
                    SalaryMin = 150000,
                    SalaryMax = 200000,
                    Currency = "USD",
                    Source = "Indeed",
                    SourceUrl = "https://indeed.com/jobs/9",
                    PostedDate = seedDate.AddDays(-9),
                    IsEasyApply = false,
                    MatchScore = 82,
                    Tags = "Architecture,Senior,Enterprise,.NET,Java",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 10,
                    Title = "Data Engineer",
                    Company = "Facebook",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build data pipelines and warehouses to support analytics and machine learning. Use Spark, Hadoop, and SQL at massive scale.",
                    Requirements = "Python, SQL, Spark, Hadoop, Kafka, BigQuery, Airflow",
                    SalaryMin = 130000,
                    SalaryMax = 170000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/10",
                    PostedDate = seedDate.AddDays(-2),
                    IsEasyApply = true,
                    MatchScore = 65,
                    Tags = "Data,BigData,Spark,Python,Remote",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 11,
                    Title = "Senior React Developer",
                    Company = "Airbnb",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Build and optimize the user-facing platform. Lead frontend architecture decisions and mentor junior developers.",
                    Requirements = "React, TypeScript, GraphQL, Jest, CSS-in-JS, Performance Optimization",
                    SalaryMin = 140000,
                    SalaryMax = 180000,
                    Currency = "USD",
                    Source = "Glassdoor",
                    SourceUrl = "https://glassdoor.com/jobs/11",
                    PostedDate = seedDate.AddDays(-10),
                    IsEasyApply = false,
                    MatchScore = 88,
                    Tags = "Frontend,React,TypeScript,Senior,GraphQL",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 12,
                    Title = "Backend Software Engineer",
                    Company = "Amazon",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build backend services for AWS or retail platform. Use Java or Python to handle millions of requests per second.",
                    Requirements = "Java/Python, DynamoDB, Lambda, S3, RDS, Microservices",
                    SalaryMin = 110000,
                    SalaryMax = 150000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/12",
                    PostedDate = seedDate.AddDays(-3),
                    IsEasyApply = true,
                    MatchScore = 75,
                    Tags = "Backend,Java,AWS,Remote,Amazon",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 13,
                    Title = "Product Engineer",
                    Company = "Stripe",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Build payment infrastructure used by millions. Work across backend, frontend, and DevOps to ship features rapidly.",
                    Requirements = "Fullstack, JavaScript/TypeScript, React, Node.js, PostgreSQL",
                    SalaryMin = 140000,
                    SalaryMax = 180000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/13",
                    PostedDate = seedDate.AddDays(-4),
                    IsEasyApply = false,
                    MatchScore = 79,
                    Tags = "Fullstack,JavaScript,React,Stripe,Payments",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 14,
                    Title = "Solutions Architect",
                    Company = "Salesforce",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Design solutions for enterprise customers using Salesforce platform. Collaborate with sales, customers, and engineers.",
                    Requirements = "Salesforce Platform, Apex, Lightning, SOQL, Enterprise Architecture",
                    SalaryMin = 120000,
                    SalaryMax = 160000,
                    Currency = "USD",
                    Source = "Indeed",
                    SourceUrl = "https://indeed.com/jobs/14",
                    PostedDate = seedDate.AddDays(-5),
                    IsEasyApply = true,
                    MatchScore = 50,
                    Tags = "Architecture,Salesforce,Enterprise,Remote",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 15,
                    Title = "iOS Developer",
                    Company = "Discord",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Build iOS app serving millions of gamers. Use Swift, SwiftUI, and native iOS frameworks.",
                    Requirements = "Swift, SwiftUI, Objective-C, iOS SDK, Xcode, CocoaPods",
                    SalaryMin = 110000,
                    SalaryMax = 150000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/15",
                    PostedDate = seedDate.AddDays(-6),
                    IsEasyApply = false,
                    MatchScore = 52,
                    Tags = "Mobile,iOS,Swift,Discord",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 16,
                    Title = "Database Administrator",
                    Company = "Oracle",
                    Location = "Austin, TX",
                    JobType = "FullTime",
                    Description = "Manage and optimize large-scale database infrastructure. Ensure high availability and performance.",
                    Requirements = "SQL, Oracle Database, PostgreSQL, Backup/Recovery, Tuning, Monitoring",
                    SalaryMin = 100000,
                    SalaryMax = 140000,
                    Currency = "USD",
                    Source = "Glassdoor",
                    SourceUrl = "https://glassdoor.com/jobs/16",
                    PostedDate = seedDate.AddDays(-7),
                    IsEasyApply = true,
                    MatchScore = 58,
                    Tags = "Database,SQL,Oracle,DBA",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 17,
                    Title = "Security Engineer",
                    Company = "Microsoft",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build security tools and infrastructure for Microsoft products. Work on threat detection, vulnerability management, and incident response.",
                    Requirements = "Security, C++, Python, Cryptography, Network Security, Incident Response",
                    SalaryMin = 130000,
                    SalaryMax = 170000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/17",
                    PostedDate = seedDate.AddDays(-8),
                    IsEasyApply = false,
                    MatchScore = 62,
                    Tags = "Security,Cybersecurity,Microsoft,Remote",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 18,
                    Title = "Golang Developer",
                    Company = "Dropbox",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Build distributed systems and tools in Go. Work on file sync, storage, and infrastructure.",
                    Requirements = "Go, Distributed Systems, gRPC, Protocol Buffers, Testing",
                    SalaryMin = 120000,
                    SalaryMax = 160000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/18",
                    PostedDate = seedDate.AddDays(-9),
                    IsEasyApply = true,
                    MatchScore = 73,
                    Tags = "Backend,Go,Distributed Systems,Remote,Dropbox",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 19,
                    Title = "Technical Writer",
                    Company = "GitHub",
                    Location = "Remote",
                    JobType = "FullTime",
                    Description = "Create clear, comprehensive documentation for developers. Write guides, API docs, and tutorials.",
                    Requirements = "Technical Writing, Markdown, API Documentation, Git, Developer Experience",
                    SalaryMin = 80000,
                    SalaryMax = 120000,
                    Currency = "USD",
                    Source = "Indeed",
                    SourceUrl = "https://indeed.com/jobs/19",
                    PostedDate = seedDate.AddDays(-10),
                    IsEasyApply = true,
                    MatchScore = 40,
                    Tags = "Technical Writing,Documentation,Remote,GitHub",
                    CreatedAt = seedDate
                },
                new JobListing
                {
                    Id = 20,
                    Title = "API Platform Engineer",
                    Company = "Twilio",
                    Location = "San Francisco, CA",
                    JobType = "FullTime",
                    Description = "Build APIs and platforms used by thousands of developers. Design for scale, reliability, and developer experience.",
                    Requirements = "API Design, Node.js/Python, PostgreSQL, Redis, REST, gRPC",
                    SalaryMin = 125000,
                    SalaryMax = 165000,
                    Currency = "USD",
                    Source = "LinkedIn",
                    SourceUrl = "https://linkedin.com/jobs/20",
                    PostedDate = seedDate.AddDays(-11),
                    IsEasyApply = false,
                    MatchScore = 76,
                    Tags = "Backend,APIs,Platform,Twilio,Node.js",
                    CreatedAt = seedDate
                }
            };

            modelBuilder.Entity<JobListing>().HasData(jobs);
        }
    }
}
