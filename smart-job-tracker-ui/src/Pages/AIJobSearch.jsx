import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Bot, Search, ChevronDown, ChevronUp, ExternalLink, Building2,
  CheckCircle, Clock, Sparkles, FileText, MessageSquare, Mail,
  Globe, AlertCircle, Star, Shield, Users, TrendingUp, Zap,
  Bookmark, BookmarkCheck, Filter, RefreshCw, SlidersHorizontal
} from 'lucide-react';
import toast from 'react-hot-toast';
import {
  aiJobSearch, trackExternalApplication, getExternalApplications,
  getProfile, saveExternalJob, getExternalSavedJobs, removeExternalSavedJob
} from '../services/api';

// ── Helpers ────────────────────────────────────────────────────────────────────

const formatDate = (dateStr) => {
  if (!dateStr) return 'Unknown';
  const d = new Date(dateStr);
  const diff = Math.floor((Date.now() - d) / 86400000);
  if (diff === 0) return 'Today';
  if (diff === 1) return 'Yesterday';
  if (diff <= 14) return `${diff}d ago`;
  return d.toLocaleDateString('en-SG', { day: 'numeric', month: 'short' });
};

const matchColor = (pct) => {
  if (pct >= 80) return 'bg-emerald-100 text-emerald-700 border border-emerald-200';
  if (pct >= 60) return 'bg-yellow-100 text-yellow-700 border border-yellow-200';
  return 'bg-red-100 text-red-700 border border-red-200';
};

const sponsorColor = (chance) => {
  if (chance === 'High') return 'bg-emerald-100 text-emerald-700';
  if (chance === 'Medium') return 'bg-amber-100 text-amber-700';
  return 'bg-red-100 text-red-700';
};

const sourceColor = (source) => {
  const s = (source || '').toLowerCase();
  if (s.includes('linkedin'))      return 'bg-blue-100 text-blue-700';
  if (s.includes('indeed'))        return 'bg-violet-100 text-violet-700';
  if (s.includes('glassdoor'))     return 'bg-green-100 text-green-700';
  if (s.includes('naukri'))        return 'bg-red-100 text-red-700';
  if (s.includes('shine'))         return 'bg-pink-100 text-pink-700';
  if (s.includes('instahyre'))     return 'bg-fuchsia-100 text-fuchsia-700';
  if (s.includes('jobstreet'))     return 'bg-cyan-100 text-cyan-700';
  if (s.includes('glints'))        return 'bg-teal-100 text-teal-700';
  if (s.includes('jobsdb'))        return 'bg-sky-100 text-sky-700';
  if (s.includes('nodeflair'))     return 'bg-indigo-100 text-indigo-700';
  if (s.includes('seek'))          return 'bg-amber-100 text-amber-700';
  if (s.includes('reed'))          return 'bg-rose-100 text-rose-700';
  if (s.includes('totaljobs'))     return 'bg-orange-100 text-orange-700';
  if (s.includes('ziprecruiter'))  return 'bg-lime-100 text-lime-700';
  if (s.includes('dice'))          return 'bg-purple-100 text-purple-700';
  if (s.includes('adzuna'))        return 'bg-orange-100 text-orange-700';
  if (s.includes('agency:'))       return 'bg-yellow-100 text-yellow-800';
  if (s.includes('company') || s === 'direct') return 'bg-emerald-100 text-emerald-700';
  return 'bg-slate-100 text-slate-600';
};

// Convert profile DTO from backend → AI search profile shape
const profileToSearchParams = (profileData) => ({
  candidateLocation: profileData.country || 'India',
  experienceYears: profileData.minExperienceYears || 3,
  targetRoles: profileData.preferredRoles
    ? profileData.preferredRoles.split(',').map(r => r.trim()).filter(Boolean)
    : ['Software Engineer', 'Backend Developer'],
  coreSkills: profileData.skills
    ? profileData.skills.split(',').map(s => s.trim()).filter(Boolean)
    : [],
  certifications: [],
  searchCountry: profileData.preferredLocation || 'Singapore',  // country to search jobs in
  searchLocation: '',                                            // city within that country (optional)
  maxJobs: 30,
  postedWithinDays: 14,
});

const COUNTRIES = [
  'Singapore', 'Australia', 'Canada', 'United Kingdom', 'United States',
  'Germany', 'France', 'Netherlands', 'UAE', 'New Zealand', 'India', 'Other'
];

const JOB_LOCATIONS = [
  'Singapore', 'London', 'New York', 'San Francisco', 'Toronto',
  'Sydney', 'Dubai', 'Amsterdam', 'Berlin', 'Remote'
];

// Country → portals people actually use to search jobs there
const PORTAL_MAP = {
  'Singapore':      ['LinkedIn', 'Indeed', 'Glassdoor', 'JobStreet', 'Glints', 'JobsDB', 'NodeFlair', 'Adzuna'],
  'India':          ['LinkedIn', 'Indeed', 'Naukri', 'Glassdoor', 'Shine', 'Instahyre', 'Monster India', 'Adzuna'],
  'United Kingdom': ['LinkedIn', 'Indeed', 'Glassdoor', 'Reed', 'Totaljobs', 'Adzuna'],
  'United States':  ['LinkedIn', 'Indeed', 'Glassdoor', 'ZipRecruiter', 'Dice', 'CareerBuilder', 'Adzuna'],
  'Australia':      ['LinkedIn', 'Indeed', 'SEEK', 'Glassdoor', 'Adzuna'],
  'Canada':         ['LinkedIn', 'Indeed', 'Glassdoor', 'Adzuna'],
  'Germany':        ['LinkedIn', 'Indeed', 'Glassdoor', 'StepStone', 'Adzuna'],
  'France':         ['LinkedIn', 'Indeed', 'Glassdoor', 'Adzuna'],
  'Netherlands':    ['LinkedIn', 'Indeed', 'Glassdoor', 'Adzuna'],
  'UAE':            ['LinkedIn', 'Indeed', 'Glassdoor', 'Bayt', 'GulfTalent', 'Adzuna'],
  'New Zealand':    ['LinkedIn', 'Indeed', 'SEEK', 'Glassdoor', 'Adzuna'],
};
const getPortalsForCountry = (country) => PORTAL_MAP[country] ?? ['LinkedIn', 'Indeed', 'Glassdoor', 'Adzuna'];

const MATCH_CONDITIONS = [
  { label: 'Any match', value: 0 },
  { label: '50%+ match', value: 50 },
  { label: '60%+ match', value: 60 },
  { label: '70%+ match', value: 70 },
  { label: '80%+ match (best fit)', value: 80 },
];

// ── Loading Panel ──────────────────────────────────────────────────────────────

function LoadingPanel({ step, roles }) {
  const primaryRole = (roles && roles.length > 0) ? roles[0] : 'Software Engineer';
  const steps = [
    `Connecting to LinkedIn · Indeed · Glassdoor · JobStreet…`,
    `Fetching real-time job listings for ${primaryRole}…`,
    `Filtering jobs matching your skills and experience…`,
    `Scoring visa/work authorisation sponsorship likelihood…`,
    `Generating tailored resume summaries per job…`,
    `Generating recruiter messages & cover notes…`,
    `Building company profiles and live openings…`,
  ];

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-8 max-w-lg mx-auto">
      <div className="flex items-center gap-3 mb-6">
        <div className="bg-blue-100 p-3 rounded-full">
          <Sparkles size={24} className="text-blue-600 animate-pulse" />
        </div>
        <div>
          <h3 className="font-bold text-slate-800">AI Job Search Running…</h3>
          <p className="text-sm text-slate-500">Scanning multiple job portals in real-time</p>
        </div>
      </div>
      <div className="space-y-1">
        {steps.map((s, i) => (
          <div
            key={i}
            className={`flex items-center gap-2 text-sm py-1.5 transition-all ${
              i < step ? 'text-emerald-600' : i === step ? 'text-blue-700 font-medium' : 'text-slate-400'
            }`}
          >
            {i < step ? (
              <CheckCircle size={16} className="text-emerald-500 flex-shrink-0" />
            ) : i === step ? (
              <div className="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin flex-shrink-0" />
            ) : (
              <div className="w-4 h-4 rounded-full border-2 border-slate-300 flex-shrink-0" />
            )}
            {s}
          </div>
        ))}
      </div>
    </div>
  );
}

// ── AI Content Panel ───────────────────────────────────────────────────────────

function AIContentPanel({ job }) {
  const copyText = (text, label) => {
    navigator.clipboard.writeText(text).then(() => toast.success(`${label} copied!`));
  };

  return (
    <div className="bg-slate-50 border-t border-slate-100 p-4 grid md:grid-cols-3 gap-4">
      <div className="bg-white rounded-xl p-4 border border-slate-200">
        <div className="flex items-center gap-2 mb-2 text-blue-700">
          <FileText size={15} />
          <span className="text-xs font-semibold uppercase tracking-wide">Tailored Resume Summary</span>
        </div>
        <p className="text-xs text-slate-700 leading-relaxed">{job.tailoredResumeSummary}</p>
        <button onClick={() => copyText(job.tailoredResumeSummary, 'Resume summary')} className="mt-2 text-xs text-blue-600 hover:underline">Copy</button>
      </div>

      <div className="bg-white rounded-xl p-4 border border-slate-200">
        <div className="flex items-center gap-2 mb-2 text-indigo-700">
          <MessageSquare size={15} />
          <span className="text-xs font-semibold uppercase tracking-wide">LinkedIn Recruiter Message</span>
        </div>
        <p className="text-xs text-slate-700 leading-relaxed">{job.recruiterMessage}</p>
        <button onClick={() => copyText(job.recruiterMessage, 'Recruiter message')} className="mt-2 text-xs text-indigo-600 hover:underline">Copy</button>
      </div>

      <div className="bg-white rounded-xl p-4 border border-slate-200">
        <div className="flex items-center gap-2 mb-2 text-emerald-700">
          <Mail size={15} />
          <span className="text-xs font-semibold uppercase tracking-wide">Quick Cover Note</span>
        </div>
        <p className="text-xs text-slate-700 leading-relaxed">{job.coverNote}</p>
        <button onClick={() => copyText(job.coverNote, 'Cover note')} className="mt-2 text-xs text-emerald-600 hover:underline">Copy</button>
      </div>
    </div>
  );
}

// ── Job Row ────────────────────────────────────────────────────────────────────

function JobRow({ job, index, expanded, onToggle, isApplied, onApply, applying, isSaved, onSave, saving }) {
  return (
    <div className={`border rounded-xl overflow-hidden mb-3 bg-white hover:shadow-md transition-shadow ${isApplied ? 'border-green-300' : isSaved ? 'border-blue-300' : 'border-slate-200'}`}>
      <div className="grid grid-cols-12 gap-2 items-center p-4 cursor-pointer" onClick={onToggle}>
        {/* # */}
        <div className="col-span-1 text-xs text-slate-400 font-mono text-center">
          {isApplied ? <CheckCircle size={14} className="text-green-500 mx-auto" /> : index + 1}
        </div>

        {/* Job Title + Company */}
        <div className="col-span-3">
          <p className="font-semibold text-slate-800 text-sm leading-tight">{job.title}</p>
          <p className="text-xs text-slate-500 mt-0.5 flex items-center gap-1">
            <Building2 size={11} />{job.company}
          </p>
          <div className="flex flex-wrap gap-1 mt-1">
            {(job.skills || []).slice(0, 3).map((s) => (
              <span key={s} className="text-xs bg-blue-50 text-blue-600 px-1.5 py-0.5 rounded">{s}</span>
            ))}
          </div>
        </div>

        {/* Experience */}
        <div className="col-span-1 text-xs text-slate-600">{job.experience}</div>

        {/* Salary */}
        <div className="col-span-2 text-xs font-medium text-slate-700">{job.salary || 'Not disclosed'}</div>

        {/* Match % */}
        <div className="col-span-1 flex justify-center">
          <span className={`text-xs font-bold px-2 py-1 rounded-full ${matchColor(job.matchPercent)}`}>
            {job.matchPercent}%
          </span>
        </div>

        {/* EP Chance + Source */}
        <div className="col-span-2">
          <span className={`text-xs font-semibold px-2 py-1 rounded-full ${sponsorColor(job.visaSponsorshipChance)}`}>
            {job.visaSponsorshipChance === 'High' ? <Shield size={10} className="inline mr-0.5" /> :
             job.visaSponsorshipChance === 'Medium' ? <AlertCircle size={10} className="inline mr-0.5" /> :
             <Clock size={10} className="inline mr-0.5" />}
            {job.visaSponsorshipChance}
          </span>
          {job.source && (
            <p className="mt-1">
              <span className={`px-1.5 py-0.5 rounded text-xs ${sourceColor(job.source)}`}>{job.source}</span>
            </p>
          )}
        </div>

        {/* Actions */}
        <div className="col-span-2 flex flex-col items-end gap-1">
          <div className="flex items-center gap-1">
            {/* Save button */}
            <button
              onClick={(e) => { e.stopPropagation(); onSave(job); }}
              disabled={saving}
              className={`p-1.5 rounded-lg transition-colors ${isSaved ? 'bg-blue-100 text-blue-600' : 'bg-gray-100 text-gray-500 hover:bg-blue-50 hover:text-blue-600'}`}
              title={isSaved ? 'Saved' : 'Save for later'}
            >
              {isSaved ? <BookmarkCheck size={14} /> : <Bookmark size={14} />}
            </button>

            {/* Apply button */}
            {isApplied ? (
              <span className="flex items-center gap-1 bg-green-100 text-green-700 text-xs px-3 py-1.5 rounded-lg font-medium border border-green-200">
                <CheckCircle size={11} /> Applied ✓
              </span>
            ) : (
              <button
                onClick={(e) => { e.stopPropagation(); onApply(job); }}
                disabled={applying}
                className="flex items-center gap-1 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-xs px-3 py-1.5 rounded-lg transition-colors font-medium"
              >
                {applying ? 'Opening…' : 'Apply'} <ExternalLink size={11} />
              </button>
            )}
          </div>
          <span className="text-xs text-slate-400">{formatDate(job.postedDate)}</span>
          {expanded ? <ChevronUp size={14} className="text-slate-400" /> : <ChevronDown size={14} className="text-slate-400" />}
        </div>
      </div>

      {expanded && <AIContentPanel job={job} />}
    </div>
  );
}

// ── Company Card ───────────────────────────────────────────────────────────────

function CompanyCard({ company }) {
  return (
    <div className="bg-white border border-slate-200 rounded-xl p-4 hover:shadow-md transition-shadow">
      <div className="flex items-start gap-3 mb-3">
        <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center text-white font-bold text-lg flex-shrink-0">
          {company.logoInitial}
        </div>
        <div className="flex-1 min-w-0">
          <h4 className="font-semibold text-slate-800 text-sm truncate">{company.company}</h4>
          <p className="text-xs text-slate-500">{company.industry}</p>
        </div>
        {company.sponsorEP && (
          <span className="text-xs bg-emerald-100 text-emerald-700 px-2 py-0.5 rounded-full font-medium flex-shrink-0">
            <Shield size={10} className="inline mr-0.5" />EP Sponsor
          </span>
        )}
      </div>

      <p className="text-xs text-slate-600 mb-3 leading-relaxed">{company.epNotes}</p>

      {company.matchingJobTitles?.length > 0 ? (
        <div>
          <p className="text-xs font-semibold text-slate-500 mb-2 uppercase tracking-wide flex items-center gap-1">
            <Zap size={11} className="text-yellow-500" /> Live Openings
          </p>
          <div className="space-y-1.5">
            {company.matchingJobTitles.map((title, i) => (
              <a key={i} href={company.matchingJobLinks[i]} target="_blank" rel="noopener noreferrer"
                className="flex items-center justify-between text-xs bg-blue-50 text-blue-700 hover:bg-blue-100 px-2.5 py-1.5 rounded-lg transition-colors">
                <span className="truncate">{title}</span>
                <ExternalLink size={10} className="flex-shrink-0 ml-1" />
              </a>
            ))}
          </div>
        </div>
      ) : (
        <div className="text-center py-2">
          {company.careersUrl ? (
            <a href={company.careersUrl} target="_blank" rel="noopener noreferrer"
              className="text-xs text-blue-600 hover:underline flex items-center justify-center gap-1">
              <Globe size={11} /> Check Careers Page
            </a>
          ) : (
            <p className="text-xs text-slate-400">No matching live openings found</p>
          )}
        </div>
      )}
    </div>
  );
}

// ── Profile Editor Modal ───────────────────────────────────────────────────────

function ProfileEditor({ profile, onSave, onCancel }) {
  const [draft, setDraft] = useState({ ...profile });
  const update = (key, val) => setDraft(d => ({ ...d, [key]: val }));
  const updateList = (key, val) => setDraft(d => ({
    ...d, [key]: val.split(',').map(s => s.trim()).filter(Boolean)
  }));

  return (
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto shadow-2xl">
        <h3 className="font-bold text-slate-800 text-lg mb-4 flex items-center gap-2">
          <SlidersHorizontal size={18} className="text-blue-600" /> Edit Search Profile
        </h3>

        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">Your Country</label>
              <select className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                value={draft.candidateLocation} onChange={e => update('candidateLocation', e.target.value)}>
                {COUNTRIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">Experience (years)</label>
              <input type="number" className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                value={draft.experienceYears} onChange={e => update('experienceYears', parseInt(e.target.value) || 0)} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">Job Search Country</label>
              <select className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                value={draft.searchCountry} onChange={e => update('searchCountry', e.target.value)}>
                {COUNTRIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">City / Area <span className="text-slate-400">(optional)</span></label>
              <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                placeholder="e.g. Sydney, London…"
                value={draft.searchLocation} onChange={e => update('searchLocation', e.target.value)} />
            </div>
          </div>

          <div>
            <label className="text-xs font-medium text-slate-600 mb-1 block">Target Roles (comma-separated)</label>
            <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
              value={draft.targetRoles.join(', ')} onChange={e => updateList('targetRoles', e.target.value)} />
          </div>

          <div>
            <label className="text-xs font-medium text-slate-600 mb-1 block">Core Skills (comma-separated)</label>
            <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
              value={draft.coreSkills.join(', ')} onChange={e => updateList('coreSkills', e.target.value)} />
          </div>

          <div>
            <label className="text-xs font-medium text-slate-600 mb-1 block">Certifications (comma-separated)</label>
            <input className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
              value={draft.certifications.join(', ')} onChange={e => updateList('certifications', e.target.value)} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">Max Jobs (20–40)</label>
              <input type="number" min="20" max="40" className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                value={draft.maxJobs} onChange={e => update('maxJobs', Math.min(40, Math.max(20, parseInt(e.target.value) || 30)))} />
            </div>
            <div>
              <label className="text-xs font-medium text-slate-600 mb-1 block">Posted within</label>
              <select className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm"
                value={draft.postedWithinDays} onChange={e => update('postedWithinDays', parseInt(e.target.value))}>
                <option value={7}>7 days</option>
                <option value={14}>14 days</option>
                <option value={30}>30 days</option>
              </select>
            </div>
          </div>
        </div>

        <div className="flex gap-3 mt-6">
          <button onClick={() => onSave(draft)}
            className="flex-1 bg-blue-600 hover:bg-blue-700 text-white py-2 rounded-xl text-sm font-medium transition-colors">
            Save & Search
          </button>
          <button onClick={onCancel}
            className="px-4 py-2 border border-slate-200 rounded-xl text-sm text-slate-600 hover:bg-slate-50 transition-colors">
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function AIJobSearch() {
  const initRan = useRef(false); // guard against React StrictMode double-invoke
  const [profile, setProfile] = useState(null); // null = not yet loaded
  const [editMode, setEditMode] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadStep, setLoadStep] = useState(0);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [expandedId, setExpandedId] = useState(null);
  const [activeTab, setActiveTab] = useState('jobs');
  const [appliedKeys, setAppliedKeys] = useState(new Set()); // "title_company" set
  const [applyingId, setApplyingId] = useState(null);
  const [savedJobIds, setSavedJobIds] = useState(new Set()); // DB id set
  const [savedJobMap, setSavedJobMap] = useState({}); // externalJobId → DB id
  const [savingId, setSavingId] = useState(null);

  // Filters
  const [filters, setFilters] = useState({
    postedWithinDays: 14,
    minMatchPercent: 0,
    portals: [],            // empty = all
    showFilters: false,
  });

  // Displayed jobs after frontend filter (min match %)
  const [displayedJobs, setDisplayedJobs] = useState([]);

  // Load profile + existing saved/applied jobs on mount
  // useRef guard prevents React 18 StrictMode from firing this twice in dev
  useEffect(() => {
    if (initRan.current) return;
    initRan.current = true;

    const init = async () => {
      try {
        const [profileRes, appliedRes, savedRes] = await Promise.allSettled([
          getProfile(),
          getExternalApplications(),
          getExternalSavedJobs(),
        ]);

        if (profileRes.status === 'fulfilled') {
          const p = profileToSearchParams(profileRes.value.data);
          setProfile(p);
          // Auto-run search if profile has skills
          if (p.coreSkills.length > 0 || p.targetRoles.length > 0) {
            runSearch(p, { postedWithinDays: 14, minMatchPercent: 0, portals: [] });
          }
        }

        if (appliedRes.status === 'fulfilled') {
          const keys = new Set((appliedRes.value.data || []).map(a => `${a.title}_${a.company}`));
          setAppliedKeys(keys);
        }

        if (savedRes.status === 'fulfilled') {
          const savedList = savedRes.value.data || [];
          const idSet = new Set(savedList.map(j => j.externalJobId));
          const idMap = {};
          savedList.forEach(j => { idMap[j.externalJobId] = j.id; });
          setSavedJobIds(idSet);
          setSavedJobMap(idMap);
        }
      } catch (e) {
        console.warn('Init error:', e);
      }
    };
    init();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Re-filter displayed jobs when result or minMatchPercent changes
  useEffect(() => {
    if (!result) return;
    const min = filters.minMatchPercent;
    const portals = filters.portals;
    let jobs = result.jobs;
    if (min > 0) jobs = jobs.filter(j => j.matchPercent >= min);
    if (portals.length > 0) {
      jobs = jobs.filter(j => portals.some(p => (j.source || '').toLowerCase().includes(p.toLowerCase())));
    }
    setDisplayedJobs(jobs);
  }, [result, filters.minMatchPercent, filters.portals]);

  const runSearch = useCallback(async (prof, filt) => {
    const searchProfile = prof || profile;
    const searchFilters = filt || filters;
    if (!searchProfile) return;

    setLoading(true);
    setError(null);
    setResult(null);
    setLoadStep(0);

    const stepTimers = [600, 1200, 2000, 3000, 4000, 5000, 6000].map((ms, i) =>
      setTimeout(() => setLoadStep(i + 1), ms)
    );

    try {
      const data = await aiJobSearch({
        candidateLocation: searchProfile.candidateLocation,
        experienceYears: searchProfile.experienceYears,
        targetRoles: searchProfile.targetRoles,
        coreSkills: searchProfile.coreSkills,
        certifications: searchProfile.certifications,
        searchCountry: searchProfile.searchCountry,    // country to search jobs in
        searchLocation: searchProfile.searchLocation,  // city within that country (optional)
        maxJobs: searchProfile.maxJobs,
        postedWithinDays: searchFilters.postedWithinDays ?? searchProfile.postedWithinDays,
        jobPortals: searchFilters.portals || [],
        minMatchPercent: 0,
      });
      setResult(data);
    } catch (err) {
      setError(
        err?.response?.data?.message ||
        'Backend is offline. Start SmartJobTracker.API and try again.'
      );
    } finally {
      stepTimers.forEach(clearTimeout);
      setLoading(false);
    }
  }, [profile, filters]);

  const handleApply = async (job) => {
    const key = `${job.title}_${job.company}`;
    if (appliedKeys.has(key)) {
      if (job.applyUrl) window.open(job.applyUrl, '_blank', 'noopener,noreferrer');
      return;
    }
    setApplyingId(job.id);
    try {
      if (job.applyUrl) window.open(job.applyUrl, '_blank', 'noopener,noreferrer');
      await trackExternalApplication({
        title: job.title, company: job.company, location: job.location,
        source: job.source, applyUrl: job.applyUrl, jobType: job.jobType,
        skills: (job.skills || []).join(','), matchScore: job.matchPercent,
        aiConfidenceScore: job.sponsorshipScore, visaSponsorshipChance: job.visaSponsorshipChance,
        coverNote: job.coverNote, recruiterMessage: job.recruiterMessage,
      });
      setAppliedKeys(prev => new Set([...prev, key]));
      toast.success('Application tracked in your dashboard!');
    } catch (err) {
      console.warn('Track apply failed:', err.message);
    } finally {
      setApplyingId(null);
    }
  };

  const handleSave = async (job) => {
    const extId = job.id;
    setSavingId(extId);
    try {
      if (savedJobIds.has(extId)) {
        // Unsave
        const dbId = savedJobMap[extId];
        if (dbId) {
          await removeExternalSavedJob(dbId);
          setSavedJobIds(prev => { const n = new Set(prev); n.delete(extId); return n; });
          setSavedJobMap(prev => { const n = { ...prev }; delete n[extId]; return n; });
          toast.success('Removed from saved jobs');
        }
      } else {
        // Save
        const res = await saveExternalJob({
          externalJobId: extId,
          title: job.title, company: job.company, location: job.location,
          source: job.source, applyUrl: job.applyUrl, jobType: job.jobType,
          salary: job.salary, salaryMin: job.salaryMin, salaryMax: job.salaryMax,
          currency: job.currency, skills: (job.skills || []).join(','),
          matchPercent: job.matchPercent, visaSponsorshipChance: job.visaSponsorshipChance,
          postedDate: job.postedDate, description: job.description,
        });
        const newId = res.data.id;
        setSavedJobIds(prev => new Set([...prev, extId]));
        setSavedJobMap(prev => ({ ...prev, [extId]: newId }));
        toast.success('Saved! View in Saved Jobs');
      }
    } catch (err) {
      if (err?.response?.status === 400 && err.response.data?.includes?.('already saved')) {
        toast('Already saved', { icon: 'ℹ️' });
      } else {
        toast.error('Could not save job');
      }
    } finally {
      setSavingId(null);
    }
  };

  const handleSaveProfile = (draft) => {
    setProfile(draft);
    setEditMode(false);
    runSearch(draft, filters);
  };

  const updateFilter = (key, val) => {
    const newFilters = { ...filters, [key]: val };
    setFilters(newFilters);
  };

  const togglePortal = (portal) => {
    const newPortals = filters.portals.includes(portal)
      ? filters.portals.filter(p => p !== portal)
      : [...filters.portals, portal];
    updateFilter('portals', newPortals);
  };

  if (!profile) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="text-center">
          <div className="w-10 h-10 border-4 border-blue-600 border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-slate-500">Loading your profile…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {editMode && (
        <ProfileEditor profile={profile} onSave={handleSaveProfile} onCancel={() => setEditMode(false)} />
      )}

      {/* ── Header Banner ── */}
      <div className="bg-gradient-to-r from-blue-600 to-indigo-700 text-white rounded-2xl p-5 mb-5">
        <div className="flex items-start justify-between">
          <div>
            <h2 className="text-xl font-bold mb-1 flex items-center gap-2">
              <Bot size={22} /> AI Job Search — Real-time Multi-portal Scanner
            </h2>
            <p className="text-blue-100 text-sm">
              Finds live jobs across LinkedIn · Indeed · Glassdoor · Naukri · JobStreet · Adzuna · and more
            </p>
          </div>
          <button onClick={() => setEditMode(true)}
            className="text-xs bg-white/20 hover:bg-white/30 px-3 py-1.5 rounded-lg transition-colors flex-shrink-0">
            Edit Profile
          </button>
        </div>

        {/* Profile quick view */}
        <div className="mt-4 grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
          <div className="bg-white/10 rounded-lg p-3">
            <p className="text-blue-200 text-xs mb-0.5">From → To</p>
            <p className="font-semibold text-sm">
              {profile.candidateLocation} → {profile.searchCountry}
              {profile.searchLocation ? `, ${profile.searchLocation}` : ''}
            </p>
          </div>
          <div className="bg-white/10 rounded-lg p-3">
            <p className="text-blue-200 text-xs mb-0.5">Experience</p>
            <p className="font-semibold">{profile.experienceYears}+ years</p>
          </div>
          <div className="bg-white/10 rounded-lg p-3">
            <p className="text-blue-200 text-xs mb-0.5">Top Skills</p>
            <p className="font-semibold text-xs truncate">
              {profile.coreSkills.length > 0 ? profile.coreSkills.slice(0, 3).join(', ') : 'Not set'}
            </p>
          </div>
          <div className="bg-white/10 rounded-lg p-3">
            <p className="text-blue-200 text-xs mb-0.5">Target Roles</p>
            <p className="font-semibold text-xs truncate">
              {profile.targetRoles.length > 0 ? profile.targetRoles[0] : 'Not set'}
            </p>
          </div>
        </div>

        {profile.coreSkills.length === 0 && (
          <div className="mt-3 bg-yellow-400/20 border border-yellow-400/30 rounded-lg p-3 text-sm text-yellow-100 flex items-center gap-2">
            <AlertCircle size={16} />
            No skills found in your profile. <a href="/resume" className="underline font-medium">Upload your resume</a> to auto-fill.
          </div>
        )}
      </div>

      {/* ── Filter Bar ── */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm mb-5">
        <div className="p-4">
          <div className="flex flex-wrap items-end gap-3">
            {/* Your Country */}
            <div className="flex-1 min-w-32">
              <label className="text-xs font-medium text-slate-500 mb-1 block">Your Country</label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none"
                value={profile.candidateLocation}
                onChange={e => setProfile(p => ({ ...p, candidateLocation: e.target.value }))}
              >
                {COUNTRIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>

            {/* Job Search Country */}
            <div className="flex-1 min-w-36">
              <label className="text-xs font-medium text-slate-500 mb-1 block">Job Search Country</label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none"
                value={profile.searchCountry}
                onChange={e => {
                  setProfile(p => ({ ...p, searchCountry: e.target.value }));
                  // Reset portal filter — different country = different portals
                  updateFilter('portals', []);
                }}
              >
                {COUNTRIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>

            {/* Job Search City (optional) */}
            <div className="flex-1 min-w-32">
              <label className="text-xs font-medium text-slate-500 mb-1 block">
                City <span className="text-slate-400 font-normal">(optional)</span>
              </label>
              <input
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none"
                value={profile.searchLocation}
                onChange={e => setProfile(p => ({ ...p, searchLocation: e.target.value }))}
                placeholder="e.g. London, Sydney…"
              />
            </div>

            {/* Date Posted */}
            <div className="flex-1 min-w-32">
              <label className="text-xs font-medium text-slate-500 mb-1 block">Date Posted</label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none"
                value={filters.postedWithinDays}
                onChange={e => updateFilter('postedWithinDays', parseInt(e.target.value))}
              >
                <option value={7}>Last 7 days</option>
                <option value={14}>Last 14 days</option>
                <option value={30}>Last 30 days</option>
              </select>
            </div>

            {/* Match Condition */}
            <div className="flex-1 min-w-36">
              <label className="text-xs font-medium text-slate-500 mb-1 block">Match Condition</label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:outline-none"
                value={filters.minMatchPercent}
                onChange={e => updateFilter('minMatchPercent', parseInt(e.target.value))}
              >
                {MATCH_CONDITIONS.map(m => (
                  <option key={m.value} value={m.value}>{m.label}</option>
                ))}
              </select>
            </div>

            {/* Toggle more filters */}
            <button
              onClick={() => updateFilter('showFilters', !filters.showFilters)}
              className={`flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium border transition-colors ${
                filters.portals.length > 0 ? 'bg-blue-50 border-blue-200 text-blue-700' : 'border-slate-200 text-slate-600 hover:bg-slate-50'
              }`}
            >
              <Filter size={14} />
              Portals
              {filters.portals.length > 0 && (
                <span className="bg-blue-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {filters.portals.length}
                </span>
              )}
            </button>

            {/* Search button */}
            <button
              onClick={() => runSearch(profile, filters)}
              disabled={loading}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white px-5 py-2 rounded-lg font-semibold text-sm transition-colors"
            >
              {loading ? <RefreshCw size={14} className="animate-spin" /> : <Search size={14} />}
              {loading ? 'Searching…' : 'Search Jobs'}
            </button>
          </div>

          {/* Portal Checkboxes — dynamic based on selected search country */}
          {filters.showFilters && (
            <div className="mt-3 pt-3 border-t border-slate-100">
              <p className="text-xs font-medium text-slate-500 mb-2">
                Job Portals for <span className="text-slate-700 font-semibold">{profile.searchCountry}</span>
                <span className="text-slate-400 font-normal ml-1">(empty = all portals)</span>
              </p>
              <div className="flex flex-wrap gap-2">
                {getPortalsForCountry(profile.searchCountry).map(p => (
                  <label key={p} className="flex items-center gap-1.5 cursor-pointer">
                    <input
                      type="checkbox"
                      className="rounded text-blue-600"
                      checked={filters.portals.includes(p)}
                      onChange={() => togglePortal(p)}
                    />
                    <span className={`text-xs px-2 py-1 rounded-full font-medium ${sourceColor(p)}`}>{p}</span>
                  </label>
                ))}
                {filters.portals.length > 0 && (
                  <button onClick={() => updateFilter('portals', [])}
                    className="text-xs text-red-500 hover:text-red-700 ml-2">
                    Clear
                  </button>
                )}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ── Loading ── */}
      {loading && (
        <div className="flex justify-center py-10">
          <LoadingPanel step={loadStep} roles={profile.targetRoles} />
        </div>
      )}

      {/* ── Error ── */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-5 text-red-700 flex gap-3">
          <AlertCircle size={20} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold">Search failed</p>
            <p className="text-sm mt-1">{error}</p>
            <button onClick={() => runSearch()} className="mt-2 text-sm underline">Try again</button>
          </div>
        </div>
      )}

      {/* ── No result yet ── */}
      {!loading && !result && !error && (
        <div className="text-center py-16">
          <div className="bg-gradient-to-br from-blue-50 to-indigo-50 rounded-2xl p-10 border border-blue-100 inline-block">
            <Bot size={48} className="text-blue-400 mx-auto mb-4" />
            <h3 className="text-xl font-bold text-slate-700 mb-2">Ready to find your next job</h3>
            <p className="text-slate-500 text-sm mb-6 max-w-md">
              Scans LinkedIn · Indeed · Glassdoor · Naukri · JobStreet · Adzuna in real-time.<br />
              Portals change automatically based on the country you're searching.
            </p>
            <button
              onClick={() => runSearch()}
              className="bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-xl font-semibold flex items-center gap-2 mx-auto transition-colors"
            >
              <Search size={18} /> Start AI Job Search
            </button>
          </div>
        </div>
      )}

      {/* ── Results ── */}
      {result && !loading && (
        <div>
          {/* Summary Bar */}
          <div className="flex items-center justify-between mb-4 flex-wrap gap-3">
            <div>
              <h2 className="text-xl font-bold text-slate-800">
                {displayedJobs.length} Jobs Found
                {displayedJobs.length !== result.totalFound && (
                  <span className="text-sm font-normal text-slate-500 ml-2">
                    (filtered from {result.totalFound})
                  </span>
                )}
              </h2>
              <p className="text-sm text-slate-500">{result.searchSummary}</p>
              {appliedKeys.size > 0 && (
                <p className="text-xs text-green-700 mt-0.5 flex items-center gap-1">
                  <CheckCircle size={12} /> {appliedKeys.size} tracked in dashboard
                </p>
              )}
            </div>
            <div className="flex gap-2 items-center flex-wrap">
              {[...new Set(result.sourcesUsed || [])].map(s => (
                <span key={s} className={`text-xs px-2 py-1 rounded-full ${sourceColor(s)}`}>{s}</span>
              ))}
              <button onClick={() => runSearch()}
                className="flex items-center gap-1.5 bg-blue-50 hover:bg-blue-100 text-blue-700 px-4 py-2 rounded-lg text-sm font-medium transition-colors">
                <RefreshCw size={13} /> Refresh
              </button>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex gap-2 mb-5 border-b border-slate-200">
            <button
              onClick={() => setActiveTab('jobs')}
              className={`pb-2 px-1 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'jobs' ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              <TrendingUp size={14} className="inline mr-1" /> Job Listings ({displayedJobs.length})
            </button>
            <button
              onClick={() => setActiveTab('companies')}
              className={`pb-2 px-1 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'companies' ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              <Users size={14} className="inline mr-1" /> Companies ({result.companiesHiringFromIndia?.length || 0})
            </button>
          </div>

          {/* Jobs Tab */}
          {activeTab === 'jobs' && (
            <div>
              {/* Column Headers */}
              <div className="grid grid-cols-12 gap-2 text-xs font-semibold text-slate-500 uppercase tracking-wide px-4 pb-2 border-b border-slate-100 mb-3">
                <div className="col-span-1 text-center">#</div>
                <div className="col-span-3">Job / Company</div>
                <div className="col-span-1">Exp.</div>
                <div className="col-span-2">Salary</div>
                <div className="col-span-1 text-center">Match</div>
                <div className="col-span-2">EP / Source</div>
                <div className="col-span-2 text-right">Save · Apply</div>
              </div>

              {displayedJobs.length === 0 ? (
                <div className="text-center py-10 text-slate-500">
                  <Bot size={40} className="mx-auto mb-3 text-slate-300" />
                  <p className="font-medium">No jobs match your current filters</p>
                  <p className="text-sm">Try lowering the match threshold or selecting all portals.</p>
                </div>
              ) : (
                displayedJobs.map((job, i) => (
                  <JobRow
                    key={job.id}
                    job={job}
                    index={i}
                    expanded={expandedId === job.id}
                    onToggle={() => setExpandedId(prev => prev === job.id ? null : job.id)}
                    isApplied={appliedKeys.has(`${job.title}_${job.company}`)}
                    onApply={handleApply}
                    applying={applyingId === job.id}
                    isSaved={savedJobIds.has(job.id)}
                    onSave={handleSave}
                    saving={savingId === job.id}
                  />
                ))
              )}

              {displayedJobs.length > 0 && (
                <p className="text-center text-xs text-slate-400 mt-4">
                  Click any row to expand AI-tailored resume summary, recruiter message & cover note.
                </p>
              )}
            </div>
          )}

          {/* Companies Tab */}
          {activeTab === 'companies' && (
            <div>
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 mb-5 flex gap-3">
                <Star size={18} className="text-amber-600 flex-shrink-0 mt-0.5" />
                <div className="text-sm text-amber-800">
                  <strong>These companies are known to actively hire candidates and sponsor work authorisation.</strong> Live openings matching your profile are shown where available.
                </div>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {(result.companiesHiringFromIndia || []).map((company, i) => (
                  <CompanyCard key={i} company={company} />
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
