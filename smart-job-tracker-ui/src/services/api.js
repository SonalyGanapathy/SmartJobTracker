import axios from 'axios';
import { getToken, clearSession } from './authService';

// ── Base URL ──────────────────────────────────────────────────────────────────
const BASE_URL = 'https://localhost:7217/api';

const api = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30000,
});

// ── Request interceptor: attach JWT Bearer token ──────────────────────────────
api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ── Response interceptor: redirect to /login on 401 ──────────────────────────
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      clearSession();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// ── Profile ───────────────────────────────────────────────────────────────────
export const getProfile = () => api.get('/profile');

export const updateProfile = (profileData) => api.put('/profile', profileData);

// Parse resume — returns full extracted data (skills, experience, education, summary)
export const parseResume = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/resume/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 30000,
  });
};

// Upload resume AND auto-update user profile in one call
export const uploadResume = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/resume/upload-and-create-profile', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 30000,
  });
};

// ── Internal Job Search (DB listings) ────────────────────────────────────────
export const searchJobs = (filters = {}) => api.get('/jobs/search', { params: filters });

export const getJobById = (id) => api.get(`/jobs/${id}`);

// ── External Real-Time Jobs ───────────────────────────────────────────────────
// Aggregates: JSearch (LinkedIn, Indeed, Glassdoor, company portals) +
//             Adzuna + NodeFlair + Careers@Gov
export const searchExternalJobs = async ({
  location = 'Singapore',
  keyword = '',
  jobType = '',
  page = 1,
  userSkills = '',
} = {}) => {
  const normalizeJobs = (jobs) =>
    (jobs || []).map((j) => {
      const rawSource = j.source || '';
      const isAgency = rawSource.startsWith('Agency:');
      const displaySource = isAgency ? rawSource.replace('Agency:', '') : rawSource;

      return {
        id: j.id,
        title: j.title,
        company: j.company,
        companyLogo: j.companyLogo,
        location: j.location,
        jobType: j.jobType,
        description: j.description,
        salary:
          j.salaryMin || j.salaryMax
            ? { min: j.salaryMin, max: j.salaryMax, currency: j.currency }
            : null,
        source: displaySource,
        sourcePriority: j.sourcePriority || 5,
        isTrustedAgency: j.isTrustedAgency || isAgency,
        applyUrl: j.applyUrl,
        postedDate: j.postedDate,
        isEasyApply: j.isEasyApply,
        matchScore: j.matchScore || 50,
        skills: j.skills || [],
        isExternal: true,
      };
    });

  const params = { location, page };
  if (keyword) params.keyword = keyword;
  if (jobType) params.jobType = jobType;
  if (userSkills) params.userSkills = userSkills;

  // 50s — backend runs 8 keywords × 2 pages in parallel
  const response = await api.get('/externaljobs', { params, timeout: 50000 });
  return {
    data: normalizeJobs(response.data.jobs),
    meta: {
      totalCount: response.data.totalCount,
      page: response.data.page,
      hasMore: response.data.hasMore,
      sourcesUsed: response.data.sourcesUsed || [],
    },
  };
};

// ── Internal Applications (jobs that exist in our DB) ────────────────────────
export const getApplications = () => api.get('/applications');

export const applyToJob = (jobId) =>
  api.post('/applications', { jobListingId: jobId });

export const updateApplicationStatus = (applicationId, status, notes) =>
  api.put(`/applications/${applicationId}/status`, { status, notes });

export const deleteApplication = (applicationId) =>
  api.delete(`/applications/${applicationId}`);

// ── External Job Application Tracking ────────────────────────────────────────
// When user clicks Apply on an external job (LinkedIn, Indeed, etc.),
// we record it here so they can track progress in the dashboard.
export const trackExternalApplication = (jobData) =>
  api.post('/external-applications', jobData, { timeout: 10000 });

export const getExternalApplications = () => api.get('/external-applications');

export const updateExternalApplicationStatus = (id, status, notes) =>
  api.put(`/external-applications/${id}/status`, { status, notes });

export const deleteExternalApplication = (id) =>
  api.delete(`/external-applications/${id}`);

export const checkExternalApplied = (title, company) =>
  api.get('/external-applications/check', { params: { title, company } });

// ── Saved Jobs (internal DB jobs) ─────────────────────────────────────────────
export const getSavedJobs = () => api.get('/saved-jobs');

export const saveJob = (jobId) =>
  api.post('/saved-jobs', { jobListingId: jobId });

export const unsaveJob = (jobId) => api.delete(`/saved-jobs/${jobId}`);

// ── External Saved Jobs (AI Job Search bookmarks) ─────────────────────────────
export const getExternalSavedJobs = () => api.get('/external-saved-jobs');

export const saveExternalJob = (jobData) =>
  api.post('/external-saved-jobs', jobData);

export const removeExternalSavedJob = (id) => api.delete(`/external-saved-jobs/${id}`);

// ── Dashboard ─────────────────────────────────────────────────────────────────
export const getDashboardStats = async () => {
  try {
    const [internalRes, externalRes] = await Promise.all([
      api.get('/dashboard/stats'),
      api.get('/external-applications').catch(() => ({ data: [] })),
    ]);

    const internal = internalRes.data;
    const externalApps = Array.isArray(externalRes.data) ? externalRes.data : [];

    const externalByStatus = externalApps.reduce((acc, app) => {
      acc[app.status] = (acc[app.status] || 0) + 1;
      return acc;
    }, {});

    return {
      data: {
        ...internal,
        totalApplied: (internal.totalApplied || 0) + externalApps.length,
        totalInterviews:
          (internal.totalInterviews || 0) + (externalByStatus['Interviewing'] || 0),
        totalOffers: (internal.totalOffers || 0) + (externalByStatus['Offered'] || 0),
        totalRejected:
          (internal.totalRejected || 0) + (externalByStatus['Rejected'] || 0),
        externalApplications: externalApps.slice(0, 5),
      },
    };
  } catch (err) {
    console.warn('Dashboard stats fetch failed:', err.message);
    return {
      data: {
        totalApplied: 0,
        totalInterviews: 0,
        totalOffers: 0,
        totalRejected: 0,
        totalSaved: 0,
        recentApplications: [],
        topMatchedJobs: [],
        externalApplications: [],
      },
    };
  }
};

// ── AI Job Search ─────────────────────────────────────────────────────────────
export const aiJobSearch = async (requestDto) => {
  const response = await api.post('/aijobsearch', requestDto, { timeout: 60000 });
  const data = response.data;

  const jobs = (data.jobs || []).map((j) => ({
    id: j.id,
    title: j.title,
    company: j.company,
    companyLogo: j.companyLogo,
    location: j.location,
    experience: j.experience,
    salary: j.salary,
    salaryMin: j.salaryMin,
    salaryMax: j.salaryMax,
    currency: j.currency,
    matchPercent: j.matchPercent,
    visaSponsorshipChance: j.visaSponsorshipChance,
    sponsorshipScore: j.sponsorshipScore,
    applyUrl: j.applyUrl,
    source: j.source,
    sourcePriority: j.sourcePriority,
    isTrustedAgency: j.isTrustedAgency,
    isEasyApply: j.isEasyApply,
    postedDate: j.postedDate,
    skills: j.skills || [],
    jobType: j.jobType,
    description: j.description,
    tailoredResumeSummary: j.tailoredResumeSummary,
    recruiterMessage: j.recruiterMessage,
    coverNote: j.coverNote,
  }));

  const companies = (data.companiesHiringFromIndia || []).map((c) => ({
    company: c.company,
    industry: c.industry,
    logoInitial: c.logoInitial,
    hiresFromIndia: c.hiresFromIndia,
    sponsorEP: c.sponsorEP,
    epNotes: c.epNotes,
    matchingJobTitles: c.matchingJobTitles || [],
    matchingJobLinks: c.matchingJobLinks || [],
    careersUrl: c.careersUrl,
  }));

  return {
    jobs,
    companiesHiringFromIndia: companies,
    totalFound: data.totalFound,
    totalSearched: data.totalSearched,
    sourcesUsed: data.sourcesUsed || [],
    generatedAt: data.generatedAt,
    searchSummary: data.searchSummary,
  };
};

// ── Claude AI Job Search ──────────────────────────────────────────────────────
export const claudeJobSearch = async (requestDto) => {
  const response = await api.post('/claudejobsearch', requestDto, { timeout: 120000 });
  const data = response.data;

  const jobs = (data.jobs || []).map((j) => ({
    id: j.id,
    title: j.title,
    company: j.company,
    companyLogo: j.companyLogo,
    location: j.location,
    experience: j.experience,
    salary: j.salary,
    salaryMin: j.salaryMin,
    salaryMax: j.salaryMax,
    currency: j.currency,
    matchPercent: j.matchPercent,
    matchAnalysis: j.matchAnalysis,
    visaSponsorshipChance: j.visaSponsorshipChance,
    sponsorshipScore: j.sponsorshipScore,
    applyUrl: j.applyUrl,
    source: j.source,
    sourcePriority: j.sourcePriority,
    isTrustedAgency: j.isTrustedAgency,
    isEasyApply: j.isEasyApply,
    postedDate: j.postedDate,
    skills: j.skills || [],
    jobType: j.jobType,
    description: j.description,
    tailoredResumeSummary: j.tailoredResumeSummary,
    recruiterMessage: j.recruiterMessage,
    coverNote: j.coverNote,
  }));

  return {
    jobs,
    totalFound: data.totalFound,
    totalSearched: data.totalSearched,
    sourcesUsed: data.sourcesUsed || [],
    generatedAt: data.generatedAt,
    searchSummary: data.searchSummary,
    model: data.model,
    generatedQueries: data.generatedQueries || [],
  };
};

export default api;
