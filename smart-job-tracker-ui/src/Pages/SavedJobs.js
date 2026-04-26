import { useEffect, useState } from 'react';
import { Bookmark, ExternalLink, Building2, MapPin, Clock, Star, Shield, Trash2, Bot } from 'lucide-react';
import LoadingSpinner from '../components/Common/LoadingSpinner';
import toast from 'react-hot-toast';
import {
  getSavedJobs, unsaveJob,
  getExternalSavedJobs, removeExternalSavedJob
} from '../services/api';

// ── Helpers ────────────────────────────────────────────────────────────────────

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  const diff = Math.floor((Date.now() - d) / 86400000);
  if (diff === 0) return 'Saved today';
  if (diff === 1) return 'Saved yesterday';
  return `Saved ${diff} days ago`;
};

const matchColor = (pct) => {
  if (!pct) return 'bg-slate-100 text-slate-600';
  if (pct >= 80) return 'bg-emerald-100 text-emerald-700';
  if (pct >= 60) return 'bg-yellow-100 text-yellow-700';
  return 'bg-red-100 text-red-700';
};

const sponsorColor = (chance) => {
  if (chance === 'High') return 'bg-emerald-100 text-emerald-700';
  if (chance === 'Medium') return 'bg-amber-100 text-amber-700';
  return 'bg-slate-100 text-slate-500';
};

// ── External Saved Job Card ────────────────────────────────────────────────────

function ExternalSavedCard({ job, onRemove }) {
  const skills = job.skills ? job.skills.split(',').map(s => s.trim()).filter(Boolean) : [];

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-200 hover:shadow-md transition-shadow p-5">
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-start gap-3 flex-1 min-w-0">
          <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-lg flex items-center justify-center text-white font-bold flex-shrink-0">
            {(job.company || '?')[0].toUpperCase()}
          </div>
          <div className="min-w-0">
            <h3 className="font-bold text-gray-800 text-sm leading-tight truncate">{job.title}</h3>
            <p className="text-blue-600 text-xs font-medium mt-0.5 flex items-center gap-1">
              <Building2 size={11} /> {job.company}
            </p>
          </div>
        </div>
        <button onClick={() => onRemove(job.id)} title="Remove from saved"
          className="text-gray-400 hover:text-red-500 transition-colors ml-2 flex-shrink-0">
          <Trash2 size={15} />
        </button>
      </div>

      {/* Meta */}
      <div className="flex flex-wrap gap-2 mb-3 text-xs text-slate-600">
        {job.location && (
          <span className="flex items-center gap-1"><MapPin size={11} />{job.location}</span>
        )}
        {job.source && (
          <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded-full">{job.source}</span>
        )}
        {job.jobType && (
          <span className="bg-slate-100 px-2 py-0.5 rounded-full">{job.jobType}</span>
        )}
      </div>

      {/* Salary */}
      {(job.salary || (job.salaryMin || job.salaryMax)) && (
        <p className="text-sm font-semibold text-gray-700 mb-2">
          {job.salary ||
            `${job.currency || 'SGD'} ${job.salaryMin ? job.salaryMin.toLocaleString() : ''}${job.salaryMax ? ` – ${job.salaryMax.toLocaleString()}` : ''}/mo`}
        </p>
      )}

      {/* Skills */}
      {skills.length > 0 && (
        <div className="flex flex-wrap gap-1 mb-3">
          {skills.slice(0, 5).map((s, i) => (
            <span key={i} className="text-xs bg-blue-50 text-blue-700 px-2 py-0.5 rounded-full">{s}</span>
          ))}
          {skills.length > 5 && <span className="text-xs text-slate-400">+{skills.length - 5} more</span>}
        </div>
      )}

      {/* Badges */}
      <div className="flex items-center justify-between">
        <div className="flex gap-2 flex-wrap">
          {job.matchPercent > 0 && (
            <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${matchColor(job.matchPercent)}`}>
              {job.matchPercent}% match
            </span>
          )}
          {job.visaSponsorshipChance && job.visaSponsorshipChance !== 'Low' && (
            <span className={`text-xs font-medium px-2 py-0.5 rounded-full flex items-center gap-0.5 ${sponsorColor(job.visaSponsorshipChance)}`}>
              <Shield size={10} /> {job.visaSponsorshipChance} EP chance
            </span>
          )}
        </div>
        <span className="text-xs text-slate-400 flex items-center gap-1">
          <Clock size={10} /> {formatDate(job.savedDate)}
        </span>
      </div>

      {/* Apply Button */}
      {job.applyUrl && (
        <a href={job.applyUrl} target="_blank" rel="noopener noreferrer"
          className="mt-3 w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold py-2 px-4 rounded-lg transition-colors">
          Apply Now <ExternalLink size={13} />
        </a>
      )}
    </div>
  );
}

// ── Internal Saved Job Card ───────────────────────────────────────────────────

function InternalSavedCard({ savedJob, onRemove }) {
  const job = savedJob.jobListing || savedJob;
  if (!job || !job.title) return null;

  return (
    <div className="bg-white rounded-xl shadow-sm border border-slate-200 hover:shadow-md transition-shadow p-5">
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="font-bold text-gray-800 text-sm">{job.title}</h3>
          <p className="text-blue-600 text-xs font-medium mt-0.5">{job.company}</p>
        </div>
        <button onClick={() => onRemove(savedJob.jobListingId)} title="Remove from saved"
          className="text-gray-400 hover:text-red-500 transition-colors">
          <Trash2 size={15} />
        </button>
      </div>

      <div className="flex flex-wrap gap-2 text-xs text-slate-600 mb-2">
        {job.location && <span className="flex items-center gap-1"><MapPin size={11} />{job.location}</span>}
        {job.jobType && <span className="bg-slate-100 px-2 py-0.5 rounded-full">{job.jobType}</span>}
        {job.source && <span className="bg-blue-50 text-blue-700 px-2 py-0.5 rounded-full">{job.source}</span>}
      </div>

      {(job.salaryMin || job.salaryMax) && (
        <p className="text-sm font-semibold text-gray-700 mb-2">
          {job.currency} {job.salaryMin?.toLocaleString()} – {job.salaryMax?.toLocaleString()}
        </p>
      )}

      {job.matchScore > 0 && (
        <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${matchColor(job.matchScore)}`}>
          {job.matchScore}% match
        </span>
      )}
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

const SavedJobs = () => {
  const [externalSaved, setExternalSaved] = useState([]);
  const [internalSaved, setInternalSaved] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('external');

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const [extRes, intRes] = await Promise.allSettled([
          getExternalSavedJobs(),
          getSavedJobs(),
        ]);
        if (extRes.status === 'fulfilled') setExternalSaved(extRes.value.data || []);
        if (intRes.status === 'fulfilled') setInternalSaved(intRes.value.data || []);
      } catch (error) {
        console.error('Failed to fetch saved jobs:', error);
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, []);

  const handleRemoveExternal = async (id) => {
    try {
      await removeExternalSavedJob(id);
      setExternalSaved(prev => prev.filter(j => j.id !== id));
      toast.success('Removed from saved jobs');
    } catch (err) {
      toast.error('Failed to remove');
    }
  };

  const handleRemoveInternal = async (jobListingId) => {
    try {
      await unsaveJob(jobListingId);
      setInternalSaved(prev => prev.filter(j => j.jobListingId !== jobListingId));
      toast.success('Removed from saved jobs');
    } catch (err) {
      toast.error('Failed to remove');
    }
  };

  if (loading) return <LoadingSpinner />;

  const totalSaved = externalSaved.length + internalSaved.length;

  return (
    <div>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-2 flex items-center gap-2">
          <Bookmark className="text-blue-600" /> Saved Jobs
        </h1>
        <p className="text-gray-600">
          {totalSaved > 0
            ? `${totalSaved} saved job${totalSaved !== 1 ? 's' : ''} — review and apply when ready`
            : 'Save jobs from AI Job Search to review them later'}
        </p>
      </div>

      {totalSaved === 0 ? (
        <div className="bg-white rounded-xl shadow-md p-12 text-center">
          <Bookmark size={48} className="mx-auto text-gray-300 mb-4" />
          <h3 className="text-xl font-semibold text-gray-700 mb-2">No saved jobs yet</h3>
          <p className="text-gray-500 mb-6 text-sm">
            Use the bookmark icon on any job in AI Job Search to save it here.
          </p>
          <a href="/ai-search"
            className="inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg transition-colors">
            <Bot size={16} /> Go to AI Job Search
          </a>
        </div>
      ) : (
        <>
          {/* Tabs */}
          <div className="flex gap-2 mb-6 border-b border-slate-200">
            <button
              onClick={() => setActiveTab('external')}
              className={`pb-2 px-2 text-sm font-medium border-b-2 transition-colors ${
                activeTab === 'external' ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              <Bot size={14} className="inline mr-1" />
              AI Search Jobs ({externalSaved.length})
            </button>
            {internalSaved.length > 0 && (
              <button
                onClick={() => setActiveTab('internal')}
                className={`pb-2 px-2 text-sm font-medium border-b-2 transition-colors ${
                  activeTab === 'internal' ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-700'
                }`}
              >
                <Star size={14} className="inline mr-1" />
                Pinned Jobs ({internalSaved.length})
              </button>
            )}
          </div>

          {/* External Saved Jobs */}
          {activeTab === 'external' && (
            externalSaved.length === 0 ? (
              <div className="text-center py-10 text-slate-500">
                <Bookmark size={36} className="mx-auto mb-3 text-slate-300" />
                <p>No AI Search jobs saved yet. Use the bookmark icon on job rows.</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
                {externalSaved.map(job => (
                  <ExternalSavedCard key={job.id} job={job} onRemove={handleRemoveExternal} />
                ))}
              </div>
            )
          )}

          {/* Internal Saved Jobs */}
          {activeTab === 'internal' && (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
              {internalSaved.map(savedJob => (
                <InternalSavedCard
                  key={savedJob.id}
                  savedJob={savedJob}
                  onRemove={handleRemoveInternal}
                />
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
};

export default SavedJobs;
