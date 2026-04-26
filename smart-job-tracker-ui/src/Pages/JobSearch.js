import { useEffect, useState, useCallback } from 'react';
import {
  Search, Globe, Loader, AlertCircle, ChevronRight, ChevronLeft,
  ShieldCheck, Info, CheckCircle
} from 'lucide-react';
import JobCard from '../components/Jobs/JobCard';
import JobFilters from '../components/Jobs/JobFilters';
import {
  searchExternalJobs,
  trackExternalApplication,
  getExternalApplications,
  getSavedJobs,
  saveJob,
  getProfile,
} from '../services/api';

// EP eligibility based on MOM guidelines (2024)
const getPassType = (salaryMin, salaryMax) => {
  const salary = salaryMin || salaryMax;
  if (!salary) return null;
  if (salary >= 5000)
    return { type: 'EP Eligible', color: 'text-green-700 bg-green-50 border-green-200', tip: 'Employment Pass — for professionals earning S$5,000+/mo' };
  if (salary >= 3150)
    return { type: 'S Pass Range', color: 'text-yellow-700 bg-yellow-50 border-yellow-200', tip: 'S Pass — mid-skilled workers S$3,150–S$4,999/mo' };
  return { type: 'Check Pass', color: 'text-gray-600 bg-gray-50 border-gray-200', tip: 'Verify pass eligibility with MOM before applying' };
};

const isSingapore = (loc) => (loc || '').toLowerCase().includes('singapore');

const JobSearch = () => {
  const [jobs, setJobs] = useState([]);
  const [appliedKeys, setAppliedKeys] = useState(new Set()); // "Title_Company" keys
  const [savedJobs, setSavedJobs] = useState([]);
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [applying, setApplying] = useState(null); // job id currently being applied
  const [filters, setFilters] = useState({ location: 'Singapore' });
  const [sourcesUsed, setSourcesUsed] = useState([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [error, setError] = useState(null);
  const [showPassInfo, setShowPassInfo] = useState(false);

  // ── Load profile, saved jobs, and already-applied external jobs ──────────
  useEffect(() => {
    getProfile().then(res => setProfile(res.data)).catch(() => {});
    getSavedJobs().then(res => setSavedJobs(res.data.map(j => j.jobId || j.id))).catch(() => {});
    getExternalApplications().then(res => {
      const keys = new Set((res.data || []).map(a => `${a.title}_${a.company}`));
      setAppliedKeys(keys);
    }).catch(() => {});
  }, []);

  // ── Fetch jobs whenever filters or page change ────────────────────────────
  const fetchJobs = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const userSkills = profile?.skills?.join(',') || '';
      const autoKeyword =
        filters.keyword ||
        profile?.preferredRoles?.[0] ||
        'software engineer';

      const res = await searchExternalJobs({
        location: filters.location || 'Singapore',
        keyword: autoKeyword,
        jobType: filters.jobType || '',
        page,
        userSkills,
      });

      const jobsWithPass = res.data.map(job => ({
        ...job,
        passInfo: isSingapore(filters.location)
          ? getPassType(job.salary?.min, job.salary?.max)
          : null,
      }));

      setJobs(jobsWithPass);
      setSourcesUsed(res.meta?.sourcesUsed || []);
      setTotalCount(res.meta?.totalCount || res.data.length);
      setHasMore(res.meta?.hasMore || false);
    } catch (err) {
      console.error('Failed to fetch jobs:', err);
      setError(
        'Could not connect to the job service. Make sure the backend is running: ' +
        'open a terminal in SmartJobTracker.API and run `dotnet run`, then refresh.'
      );
    } finally {
      setLoading(false);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters, page, profile]);

  useEffect(() => { fetchJobs(); }, [fetchJobs]);

  // ── Apply to external job ────────────────────────────────────────────────
  const handleApply = async (job) => {
    const key = `${job.title}_${job.company}`;
    if (appliedKeys.has(key)) {
      // Already applied — open the URL again
      if (job.applyUrl) window.open(job.applyUrl, '_blank', 'noopener,noreferrer');
      return;
    }

    setApplying(job.id);
    try {
      // 1. Open apply URL in new tab
      if (job.applyUrl) window.open(job.applyUrl, '_blank', 'noopener,noreferrer');

      // 2. Track the application in our backend
      await trackExternalApplication({
        title: job.title,
        company: job.company,
        location: job.location,
        source: job.source,
        applyUrl: job.applyUrl,
        jobType: job.jobType,
        salaryMin: job.salary?.min,
        salaryMax: job.salary?.max,
        currency: job.salary?.currency,
        skills: (job.skills || []).join(','),
        matchScore: job.matchScore,
        jobPostedDate: job.postedDate,
      });

      // 3. Mark as applied in UI
      setAppliedKeys(prev => new Set([...prev, key]));
    } catch (err) {
      console.warn('Could not track application:', err.message);
      // Application was still opened; silently ignore tracking error
    } finally {
      setApplying(null);
    }
  };

  const handleSave = async (jobId) => {
    try {
      await saveJob(jobId);
      setSavedJobs(prev =>
        prev.includes(jobId) ? prev.filter(id => id !== jobId) : [...prev, jobId]
      );
    } catch (err) {
      console.error('Failed to save job:', err);
    }
  };

  const handleFilterChange = (newFilters) => { setPage(1); setFilters(newFilters); };
  const handleReset = () => { setPage(1); setFilters({ location: 'Singapore' }); };

  const resumeRole = profile?.preferredRoles?.[0] || 'Software Engineer';
  const resumeSkillCount = profile?.skills?.length || 0;
  const searchLocation = filters.location || 'Singapore';
  const sgMode = isSingapore(searchLocation);

  return (
    <div>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-800 mb-1 flex items-center gap-2">
          <Search className="text-blue-600" />
          Find Your Next Job
        </h1>
        <p className="text-gray-500 text-sm">
          Searching as{' '}
          <span className="font-semibold text-blue-600">{resumeRole}</span>
          {resumeSkillCount > 0 && ` · ${resumeSkillCount} skills from your resume`}
          {' · '}Real-time from LinkedIn, Indeed, Glassdoor, JobStreet &amp; company portals
        </p>
      </div>

      {/* Filters */}
      <JobFilters onFilterChange={handleFilterChange} onReset={handleReset} />

      {/* Singapore EP Pass Info */}
      {sgMode && !loading && (
        <div className="mb-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
          <div className="flex items-start gap-3">
            <ShieldCheck size={18} className="text-amber-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <p className="text-sm font-semibold text-amber-800">
                  Work Pass Required — Applying from India 🇮🇳
                </p>
                <button onClick={() => setShowPassInfo(!showPassInfo)} className="text-amber-600 hover:text-amber-800">
                  <Info size={14} />
                </button>
              </div>
              {showPassInfo && (
                <div className="mt-2 text-xs text-amber-700 space-y-1">
                  <p><span className="font-semibold">🟢 Employment Pass (EP)</span> — Min salary S$5,000/mo. Most common for tech roles.</p>
                  <p><span className="font-semibold">🟡 S Pass</span> — Min salary S$3,150/mo. Quota-limited per company.</p>
                  <p><span className="font-semibold">💡 Tip:</span> Filter for salary &gt; S$5,000/mo for best EP chances.</p>
                  <a
                    href="https://www.mom.gov.sg/passes-and-permits/employment-pass/eligibility"
                    target="_blank" rel="noopener noreferrer"
                    className="inline-block mt-1 text-blue-600 hover:underline font-medium"
                  >
                    → Check EP Eligibility on MOM website ↗
                  </a>
                </div>
              )}
              {!showPassInfo && (
                <p className="text-xs text-amber-600 mt-0.5">
                  Each card shows EP/S Pass eligibility based on salary.{' '}
                  <button onClick={() => setShowPassInfo(true)} className="underline">Learn more</button>
                </p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Live Mode Banner */}
      {!loading && !error && (
        <div className="mb-4 bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-xl px-5 py-3 flex items-center gap-3 shadow">
          <Globe size={20} className="flex-shrink-0 animate-pulse" />
          <div className="flex-1">
            <span className="font-semibold">
              {sgMode ? '🇸🇬 Live Singapore Jobs' : `🌐 Live Jobs — ${searchLocation}`}
            </span>
            <span className="text-blue-100 text-sm ml-2">
              {totalCount} jobs · &quot;{filters.keyword || resumeRole}&quot;
              {sourcesUsed.length > 0 && ` · ${sourcesUsed.join(' + ')}`}
            </span>
          </div>
          <span className="text-xs bg-white/20 px-2 py-1 rounded-full font-medium">Real-time</span>
        </div>
      )}

      {/* Error Banner */}
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-3 flex items-start gap-3">
          <AlertCircle size={18} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-semibold">Could not load live jobs</p>
            <p className="text-sm mt-0.5">{error}</p>
          </div>
        </div>
      )}

      {/* Results Count */}
      {!loading && !error && (
        <div className="mb-4 flex items-center justify-between">
          <p className="text-gray-600 font-medium">
            Showing {jobs.length} live jobs{page > 1 && ` · Page ${page}`}
          </p>
          {appliedKeys.size > 0 && (
            <span className="text-sm text-green-700 bg-green-50 border border-green-200 rounded-full px-3 py-1 flex items-center gap-1">
              <CheckCircle size={14} />
              {appliedKeys.size} applied this session
            </span>
          )}
        </div>
      )}

      {/* Loading */}
      {loading && (
        <div className="flex flex-col items-center justify-center py-20 gap-3 text-gray-500">
          <Loader size={32} className="animate-spin text-blue-600" />
          <p className="text-sm font-medium text-center max-w-sm">
            Searching LinkedIn · Indeed · Glassdoor · JobStreet · Company portals
            <br />
            <span className="text-blue-600">"{filters.keyword || resumeRole}"</span>
            {' '}in <span className="text-blue-600">{searchLocation}</span>…
          </p>
        </div>
      )}

      {/* Job Grid */}
      {!loading && !error && jobs.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {jobs.map((job) => {
            const key = `${job.title}_${job.company}`;
            const isApplied = appliedKeys.has(key);
            const isApplying = applying === job.id;

            return (
              <div key={job.id} className="relative">
                {/* Applied overlay badge */}
                {isApplied && (
                  <div className="absolute top-3 right-3 z-10 bg-green-600 text-white text-xs font-bold px-2.5 py-1 rounded-full flex items-center gap-1 shadow">
                    <CheckCircle size={11} /> Applied
                  </div>
                )}
                <div className={isApplied ? 'opacity-80' : ''}>
                  <JobCard
                    job={job}
                    onApply={() => handleApply(job)}
                    onSave={handleSave}
                    isSaved={savedJobs.includes(job.id)}
                    profile={profile}
                    customApplyLabel={
                      isApplying
                        ? 'Opening…'
                        : isApplied
                        ? 'Applied ✓'
                        : job.isEasyApply
                        ? 'Easy Apply'
                        : 'Apply Now'
                    }
                    applyDisabled={isApplying}
                  />
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Empty State */}
      {!loading && !error && jobs.length === 0 && (
        <div className="bg-white rounded-xl shadow-md p-12 text-center">
          <Search size={48} className="mx-auto text-gray-300 mb-4" />
          <h3 className="text-xl font-semibold text-gray-700 mb-2">No jobs found</h3>
          <p className="text-gray-500 text-sm">
            Try a different keyword or location. Make sure the backend is running.
          </p>
        </div>
      )}

      {/* Pagination */}
      {!loading && jobs.length > 0 && (
        <div className="flex items-center justify-center gap-4 py-6">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="flex items-center gap-1 px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors shadow-sm"
          >
            <ChevronLeft size={16} /> Previous
          </button>
          <span className="text-sm text-gray-600 font-medium">Page {page}</span>
          <button
            onClick={() => setPage(p => p + 1)}
            disabled={!hasMore}
            className="flex items-center gap-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium disabled:opacity-40 disabled:cursor-not-allowed transition-colors shadow-sm"
          >
            Next <ChevronRight size={16} />
          </button>
        </div>
      )}
    </div>
  );
};

export default JobSearch;
