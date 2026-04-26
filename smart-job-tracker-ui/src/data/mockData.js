// ─── Profile: Matches Sonaly Ganapathy's actual CV ───
export const mockProfile = {
  id: "1",
  fullName: "Sonaly Ganapathy",
  email: "iamsonaly@gmail.com",
  phone: "+91 96778 31277",
  summary:
    "Results-driven Full Stack Software Engineer with 3+ years of experience delivering scalable enterprise web applications using ASP.NET Core Web API, Angular, SQL Server, and Azure. Holds three Microsoft Azure certifications (AZ-400, AZ-305, AZ-104). Proven track record integrating Azure AI/Cognitive Services for real-world automation, implementing Clean Architecture microservice APIs, and configuring Azure CI/CD pipelines. Domain experience spans pharma compliance and enterprise document automation. Immediately available for relocation to Singapore.",
  country: "India",
  location: "India · Open to Relocation",
  locationType: "Hybrid",
  experienceRange: { min: 3, max: 5 },
  skills: [
    "C#", "TypeScript", "JavaScript", "HTML5", "CSS3",
    "ASP.NET Core Web API", "RESTful APIs", "Microservices",
    "Entity Framework Core", "JWT Auth", "Middleware",
    "Angular", "Angular Material", "RxJS",
    "Microsoft Azure", "Azure App Service", "Azure AI / Cognitive Services",
    "Azure CI/CD Pipelines", "Docker",
    "SQL Server", "Stored Procedures", "Performance Tuning",
    "Clean Architecture", "DDD", "SOLID Principles", "Agile / Scrum",
    "Git", "GitHub", "Visual Studio", "VS Code", "Postman", "SSMS"
  ],
  preferredRoles: [
    "Full Stack Developer",
    ".NET Developer",
    "Software Engineer",
    "Backend Developer",
    "Cloud Engineer"
  ],
  certifications: [
    "Microsoft Certified: DevOps Engineer Expert (AZ-400)",
    "Azure Solutions Architect Expert (AZ-305)",
    "Azure Administrator Associate (AZ-104)"
  ],
  education: [
    {
      school: "Anna University, India",
      degree: "B.E. Computer Science & Engineering",
      year: "2020"
    }
  ],
  resumeFile: "Sonaly_Ganapathy_CV_SG.pdf"
};

// ─── Parsed resume data (shown after upload) ───
export const mockParsedResume = {
  skills: [
    "C#", "TypeScript", "JavaScript", "HTML5", "CSS3",
    "ASP.NET Core Web API", "RESTful APIs", "Microservices",
    "Entity Framework Core", "JWT Auth",
    "Angular", "Angular Material", "RxJS",
    "Azure", "Azure App Service", "Azure AI / Cognitive Services",
    "Azure CI/CD Pipelines", "Docker",
    "SQL Server", "Stored Procedures",
    "Clean Architecture", "DDD", "SOLID",
    "Git", "Postman", "SSMS"
  ],
  experience: [
    {
      company: "Process Fusion — CapturePoint Platform",
      role: "Software Developer",
      duration: "Apr 2024 – Present",
      description:
        "Built scalable ASP.NET Core Web APIs for enterprise document processing. Integrated Azure Cognitive Services to automate invoice capture (–40% manual time). Designed Clean Architecture microservice APIs. Configured Azure CI/CD pipelines."
    },
    {
      company: "Navitas Life Sciences — PharmaReady",
      role: "Software Engineer",
      duration: "Jan 2022 – Mar 2024",
      description:
        "Designed RESTful APIs with ASP.NET Core for Angular frontend. Implemented JWT + RBAC (HIPAA-aligned). Optimized SQL Server queries (–35% execution time). Built responsive Angular components."
    }
  ],
  education: [
    {
      school: "Anna University, India",
      degree: "B.E. Computer Science & Engineering",
      year: "2020"
    }
  ],
  certifications: [
    "Microsoft Certified: DevOps Engineer Expert (AZ-400)",
    "Azure Solutions Architect Expert (AZ-305)",
    "Azure Administrator Associate (AZ-104)"
  ],
  projects: [
    {
      name: "ETMS – Employee Task Management System",
      tech: "ASP.NET Core · Angular · SQL Server · Azure · Clean Architecture",
      url: "https://github.com/SonalyGanapathy/ETMS-Employee-Task-Management-System"
    },
    {
      name: "SmartTaskManagement – AI-Assisted Task Platform",
      tech: "ASP.NET Core · SQL Server · Azure AI Services",
      url: "https://github.com/SonalyGanapathy/SmartTaskManagement"
    },
    {
      name: "CapturePoint – AI-Driven Document Automation",
      tech: "ASP.NET Core · Angular · Azure AI Services · SQL Server"
    }
  ],
  contact: {
    email: "iamsonaly@gmail.com",
    phone: "+91 96778 31277",
    location: "India · Open to Relocation · Singapore EP Sponsorship Required",
    linkedin: "linkedin.com/in/sonaly-ganapathy",
    github: "github.com/iamsonaly"
  }
};

// ─── Jobs: India / Singapore / Remote roles matching SG's .NET + Angular + Azure stack ───
export const mockJobs = [
  {
    id: "1",
    company: "Zoho Corporation",
    title: "Full Stack Developer (.NET + Angular)",
    location: "Chennai, India",
    jobType: "Full-time",
    salary: { min: 1200000, max: 1800000, currency: "INR" },
    description:
      "Build enterprise SaaS products using ASP.NET Core APIs and Angular front-end. Work on Clean Architecture microservices deployed on Azure.",
    matchScore: 95,
    source: "Naukri",
    postedDate: "2026-04-20",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "Angular", "SQL Server", "Azure"]
  },
  {
    id: "2",
    company: "Infosys",
    title: "Senior .NET Developer",
    location: "Bangalore, India",
    jobType: "Full-time",
    salary: { min: 1400000, max: 2000000, currency: "INR" },
    description:
      "Design and develop scalable REST APIs using ASP.NET Core. Implement CI/CD pipelines with Azure DevOps for global banking clients.",
    matchScore: 92,
    source: "Naukri",
    postedDate: "2026-04-19",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "Azure DevOps", "SQL Server", "Microservices"]
  },
  {
    id: "3",
    company: "DBS Bank",
    title: "Full Stack Engineer – .NET / Angular",
    location: "Singapore",
    jobType: "Full-time",
    salary: { min: 6000, max: 8500, currency: "SGD/mo" },
    description:
      "Modernize core banking systems using ASP.NET Core Web APIs and Angular. Azure-hosted microservices. EP sponsorship available.",
    matchScore: 93,
    source: "LinkedIn",
    postedDate: "2026-04-21",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "Angular", "Azure", "SQL Server"]
  },
  {
    id: "4",
    company: "Wipro",
    title: "Azure Cloud Engineer",
    location: "Hyderabad, India",
    jobType: "Full-time",
    salary: { min: 1000000, max: 1600000, currency: "INR" },
    description:
      "Architect and manage Azure infrastructure. Implement App Services, CI/CD pipelines, and Azure AI Cognitive Services integrations.",
    matchScore: 85,
    source: "Naukri",
    postedDate: "2026-04-18",
    isEasyApply: true,
    skills: ["Azure", "Azure DevOps", "Docker", "CI/CD", "ARM Templates"]
  },
  {
    id: "5",
    company: "TCS (Tata Consultancy Services)",
    title: "Software Engineer – .NET Core",
    location: "Chennai, India",
    jobType: "Full-time",
    salary: { min: 800000, max: 1200000, currency: "INR" },
    description:
      "Develop enterprise-grade applications using ASP.NET Core and Entity Framework Core. SQL Server optimization and Agile delivery.",
    matchScore: 88,
    source: "Naukri",
    postedDate: "2026-04-17",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "EF Core", "SQL Server", "Agile"]
  },
  {
    id: "6",
    company: "Grab",
    title: "Backend Engineer – C# / .NET",
    location: "Singapore",
    jobType: "Full-time",
    salary: { min: 7000, max: 10000, currency: "SGD/mo" },
    description:
      "Build high-throughput microservices for ride-hailing and delivery. .NET Core, Docker, Kubernetes. EP sponsorship provided.",
    matchScore: 82,
    source: "LinkedIn",
    postedDate: "2026-04-16",
    isEasyApply: false,
    skills: ["C#", ".NET Core", "Docker", "Kubernetes", "Microservices"]
  },
  {
    id: "7",
    company: "Flipkart",
    title: ".NET Developer",
    location: "Bangalore, India",
    jobType: "Full-time",
    salary: { min: 1500000, max: 2200000, currency: "INR" },
    description:
      "Build e-commerce backend services at scale using ASP.NET Core, SQL Server, and Redis. Clean Architecture patterns.",
    matchScore: 90,
    source: "Naukri",
    postedDate: "2026-04-20",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "SQL Server", "Redis", "Clean Architecture"]
  },
  {
    id: "8",
    company: "Shopee",
    title: "Full Stack Developer (Angular + .NET)",
    location: "Singapore",
    jobType: "Full-time",
    salary: { min: 6500, max: 9000, currency: "SGD/mo" },
    description:
      "Develop Angular front-ends and ASP.NET Core APIs for Southeast Asia's largest e-commerce platform. EP sponsorship available.",
    matchScore: 91,
    source: "LinkedIn",
    postedDate: "2026-04-15",
    isEasyApply: true,
    skills: ["Angular", "C#", "ASP.NET Core", "SQL Server", "RxJS"]
  },
  {
    id: "9",
    company: "Cognizant",
    title: "Senior Software Engineer – Azure & .NET",
    location: "Pune, India",
    jobType: "Full-time",
    salary: { min: 1300000, max: 1900000, currency: "INR" },
    description:
      "Lead Azure cloud migration projects. Build REST APIs with ASP.NET Core, configure Azure App Services and CI/CD pipelines.",
    matchScore: 87,
    source: "LinkedIn",
    postedDate: "2026-04-14",
    isEasyApply: false,
    skills: ["C#", "ASP.NET Core", "Azure", "CI/CD", "Microservices"]
  },
  {
    id: "10",
    company: "Tech Mahindra",
    title: "Angular + .NET Full Stack Developer",
    location: "Hyderabad, India",
    jobType: "Full-time",
    salary: { min: 1100000, max: 1700000, currency: "INR" },
    description:
      "Build responsive Angular UIs backed by ASP.NET Core Web APIs. Entity Framework Core, JWT authentication, Agile delivery.",
    matchScore: 94,
    source: "Naukri",
    postedDate: "2026-04-19",
    isEasyApply: true,
    skills: ["Angular", "C#", "ASP.NET Core", "EF Core", "JWT"]
  },
  {
    id: "11",
    company: "HCLTech",
    title: ".NET Microservices Developer",
    location: "Noida, India",
    jobType: "Full-time",
    salary: { min: 1000000, max: 1500000, currency: "INR" },
    description:
      "Design and implement microservices using ASP.NET Core. Docker containerization and Azure Kubernetes deployment.",
    matchScore: 83,
    source: "Naukri",
    postedDate: "2026-04-13",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "Docker", "Kubernetes", "Microservices"]
  },
  {
    id: "12",
    company: "GovTech Singapore",
    title: "Software Engineer (.NET / Azure)",
    location: "Singapore",
    jobType: "Full-time",
    salary: { min: 5500, max: 8000, currency: "SGD/mo" },
    description:
      "Build citizen-facing government digital services using .NET Core and Azure. Clean Architecture, CI/CD automation. EP sponsorship.",
    matchScore: 89,
    source: "LinkedIn",
    postedDate: "2026-04-12",
    isEasyApply: false,
    skills: ["C#", ".NET Core", "Azure", "SQL Server", "CI/CD"]
  },
  {
    id: "13",
    company: "Razorpay",
    title: "Backend Engineer – C#",
    location: "Bangalore, India",
    jobType: "Full-time",
    salary: { min: 1600000, max: 2400000, currency: "INR" },
    description:
      "Build payment infrastructure APIs. High-throughput C# services, SQL Server, Redis caching. Agile team.",
    matchScore: 80,
    source: "LinkedIn",
    postedDate: "2026-04-11",
    isEasyApply: true,
    skills: ["C#", "ASP.NET Core", "SQL Server", "Redis", "REST APIs"]
  },
  {
    id: "14",
    company: "Freshworks",
    title: "Full Stack Developer",
    location: "Chennai, India",
    jobType: "Full-time",
    salary: { min: 1400000, max: 2000000, currency: "INR" },
    description:
      "Build SaaS CRM features using Angular frontend and .NET Core backend. Azure-hosted, Clean Architecture.",
    matchScore: 93,
    source: "Naukri",
    postedDate: "2026-04-21",
    isEasyApply: true,
    skills: ["Angular", "C#", "ASP.NET Core", "Azure", "SQL Server"]
  },
  {
    id: "15",
    company: "OCBC Bank",
    title: "Application Developer (.NET)",
    location: "Singapore",
    jobType: "Full-time",
    salary: { min: 5800, max: 7500, currency: "SGD/mo" },
    description:
      "Develop and maintain banking applications using ASP.NET Core and Angular. SQL Server, stored procedures. EP sponsorship.",
    matchScore: 88,
    source: "Indeed",
    postedDate: "2026-04-10",
    isEasyApply: false,
    skills: ["C#", "ASP.NET Core", "Angular", "SQL Server"]
  },
  {
    id: "16",
    company: "Accenture",
    title: "Azure DevOps Engineer",
    location: "Mumbai, India",
    jobType: "Full-time",
    salary: { min: 1200000, max: 1800000, currency: "INR" },
    description:
      "Implement Azure CI/CD pipelines, ARM templates, and infrastructure-as-code. Docker + Kubernetes orchestration.",
    matchScore: 78,
    source: "Naukri",
    postedDate: "2026-04-09",
    isEasyApply: true,
    skills: ["Azure DevOps", "Docker", "Kubernetes", "CI/CD", "ARM"]
  },
  {
    id: "17",
    company: "Capgemini",
    title: "Software Developer – .NET & Angular",
    location: "Bangalore, India · Remote",
    jobType: "Remote",
    salary: { min: 1100000, max: 1600000, currency: "INR" },
    description:
      "Remote-first role building Angular + ASP.NET Core enterprise applications for European banking clients.",
    matchScore: 91,
    source: "LinkedIn",
    postedDate: "2026-04-20",
    isEasyApply: true,
    skills: ["Angular", "C#", "ASP.NET Core", "SQL Server", "Azure"]
  },
  {
    id: "18",
    company: "Thoughtworks",
    title: "Senior Consultant – .NET",
    location: "Pune, India",
    jobType: "Full-time",
    salary: { min: 1800000, max: 2600000, currency: "INR" },
    description:
      "Lead technical consulting engagements. Clean Architecture, TDD, microservices with .NET Core. Agile coaching.",
    matchScore: 84,
    source: "LinkedIn",
    postedDate: "2026-04-08",
    isEasyApply: false,
    skills: ["C#", ".NET Core", "Clean Architecture", "TDD", "Agile"]
  }
];

// ─── Applications: realistic entries tied to the jobs above ───
export const mockApplications = [
  {
    id: "1",
    jobId: "3",
    company: "DBS Bank",
    role: "Full Stack Engineer – .NET / Angular",
    appliedDate: "2026-04-18",
    status: "Interviewing",
    nextStep: "Technical Interview on April 25",
    source: "LinkedIn"
  },
  {
    id: "2",
    jobId: "7",
    company: "Flipkart",
    role: ".NET Developer",
    appliedDate: "2026-04-15",
    status: "Screening",
    nextStep: "HR screening call scheduled",
    source: "Naukri"
  },
  {
    id: "3",
    jobId: "1",
    company: "Zoho Corporation",
    role: "Full Stack Developer (.NET + Angular)",
    appliedDate: "2026-04-14",
    status: "Applied",
    nextStep: "Under review",
    source: "Naukri"
  },
  {
    id: "4",
    jobId: "14",
    company: "Freshworks",
    role: "Full Stack Developer",
    appliedDate: "2026-04-12",
    status: "Offered",
    nextStep: "Offer letter valid until April 28",
    source: "Naukri"
  },
  {
    id: "5",
    jobId: "5",
    company: "TCS",
    role: "Software Engineer – .NET Core",
    appliedDate: "2026-04-05",
    status: "Rejected",
    nextStep: "Consider other TCS openings",
    source: "Naukri"
  },
  {
    id: "6",
    jobId: "8",
    company: "Shopee",
    role: "Full Stack Developer (Angular + .NET)",
    appliedDate: "2026-04-16",
    status: "Interviewing",
    nextStep: "System Design round on April 23",
    source: "LinkedIn"
  }
];

// ─── Saved Jobs ───
export const mockSavedJobs = [
  { id: "1", jobId: "3",  company: "DBS Bank",    role: "Full Stack Engineer – .NET / Angular",  savedDate: "2026-04-20" },
  { id: "2", jobId: "6",  company: "Grab",        role: "Backend Engineer – C# / .NET",          savedDate: "2026-04-19" },
  { id: "3", jobId: "12", company: "GovTech Singapore", role: "Software Engineer (.NET / Azure)", savedDate: "2026-04-18" },
  { id: "4", jobId: "15", company: "OCBC Bank",   role: "Application Developer (.NET)",          savedDate: "2026-04-17" },
  { id: "5", jobId: "2",  company: "Infosys",     role: "Senior .NET Developer",                 savedDate: "2026-04-16" }
];

// ─── Dashboard stats: derived from the 6 applications above ───
export const mockDashboardStats = {
  totalApplied: 6,
  totalSaved: 5,
  totalInterviews: 2,
  totalOffers: 1,
  totalRejected: 1,
  matchRate: 88
};
