import { useEffect, useState } from 'react';
import { Send, ExternalLink, Trash2, ChevronDown } from 'lucide-react';
import LoadingSpinner from '../components/Common/LoadingSpinner';
import {
  getExternalApplications,
  getApplications,
  updateExternalApplicationStatus,
  deleteExternalApplication,
} from '../services/api';

const formatDate = (d) => {
  if (!d) return '—';
  return new Date(d).toLocaleDateString('en-SG', { day: 'numeric', month: 'short', year: 'numeric' });
};

const STATUS_COLORS = {
  Applied:       'bg-blue-100 text-blue-700',
  Screening:     'bg-yellow-100 text-yellow-700',
  Interviewing:  'bg-purple-100 text-purple-700',
  Offered:       'bg-green-100 text-green-700',
  Rejected:      'bg-red-100 text-red-600',
  Withdrawn:     'bg-gray-100 text-gray-600',
};

const SOURCE_COLORS = {
  LinkedIn:         'bg-blue-50 text-blue-700',
  Indeed:           'bg-violet-50 text-violet-700',
  Glassdoor:        'bg-green-50 text-green-700',
  MyCareersFuture:  'bg-red-50 text-red-700',
  Company:          'bg-emerald-50 text-emerald-700',
};

const VALID_STATUSES = ['Applied', 'Screening', 'Interviewing', 'Offered', 'Rejected', 'Withdrawn'];

function StatusDropdown({ appId, current, onUpdate }) {
  const [open, setOpen] = useState(false);
  const [updating, setUpdating] = useState(false);

  const handleSelect = async (status) => {
    if (status === current) { setOpen(false); return; }
    setUpdating(true);
    try {
      await onUpdate(appId, status);
    } finally {
      setUpdating(false);
      setOpen(false);
    }
  };

  return (
    <div className="relative inline-block">
      <button
        onClick={() => setOpen(!open)}
        disabled={updating}
        className={`flex items-center gap-1 text-xs font-semibold px-2.5 py-1 rounded-full transition-colors cursor-pointer ${STATUS_COLORS[current] || 'bg-gray-100 text-gray-600'}`}
      >
        {updating ? 'Saving…' : current}
        <ChevronDown size={11} />
      </button>
      {open && (
        <div className="absolute left-0 top-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-20 min-w-[130px]">
          {VALID_STATUSES.map(s => (
            <button
              key={s}
              onClick={() => handleSelect(s)}
              className={`w-full text-left px-3 py-2 text-xs hover:bg-gray-50 ${s === current ? 'font-bold' : ''}`}
            >
              {s}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

const AppliedJobs = () => {
  const [allApps, setAllApps] = useState([]);
  const [activeFilter, setActiveFilter] = useState('All');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      const [extRes, intRes] = await Promise.all([
        getExternalApplications().catch(() => ({ data: [] })),
        getApplications().catch(() => ({ data: [] })),
      ]);

      const external = (extRes.data || []).map(a => ({
        id: `ext_${a.id}`,
        rawId: a.id,
        isExternal: true,
        title: a.title,
        company: a.company,
        location: a.location || '—',
        source: a.source || '—',
        applyUrl: a.applyUrl,
        jobType: a.jobType,
        salaryMin: a.salaryMin,
        salaryMax: a.salaryMax,
        currency: a.currency,
        skills: a.skills ? a.skills.split(',').filter(Boolean) : [],
        matchScore: a.matchScore,
        status: a.status,
        appliedAt: a.appliedAt,
        coverNote: a.coverNote,
        recruiterMessage: a.recruiterMessage,
      }));

      const internal = (intRes.data || []).map(a => ({
        id: `int_${a.id}`,
        rawId: a.id,
        isExternal: false,
        title: a.jobListing?.title || 'Unknown Role',
        company: a.jobListing?.company || '—',
        location: a.jobListing?.location || '—',
        source: a.jobListing?.source || '—',
        applyUrl: a.jobListing?.sourceUrl,
        jobType: a.jobListing?.jobType,
        status: a.status,
        appliedAt: a.appliedDate,
      }));

      const merged = [...external, ...internal].sort(
        (a, b) => new Date(b.appliedAt) - new Date(a.appliedAt)
      );
      setAllApps(merged);
    } catch (err) {
      setError('Could not load applications. Make sure the API backend is running.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const handleStatusUpdate = async (appId, newStatus) => {
    const app = allApps.find(a => a.id === `ext_${appId}`);
    if (!app) return;
    await updateExternalApplicationStatus(appId, newStatus);
    setAllApps(prev => prev.map(a => a.id === `ext_${appId}` ? { ...a, status: newStatus } : a));
  };

  const handleDelete = async (appId) => {
    if (!window.confirm('Remove this application from tracking?')) return;
    await deleteExternalApplication(appId);
    setAllApps(prev => prev.filter(a => a.id !== `ext_${appId}`));
  };

  const STATUS_OPTIONS = ['All', ...VALID_STATUSES];

  const statusCounts = allApps.reduce((acc, a) => {
    acc[a.status] = (acc[a.status] || 0) + 1;
    return acc;
  }, {});

  const filtered = activeFilter === 'All'
    ? allApps
    : allApps.filter(a => a.status === activeFilter);

  if (loading) return <LoadingSpinner />;

  return (
    <div>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-2 flex items-center gap-2">
          <Send className="text-blue-600" />
          My Applications
        </h1>
        <p className="text-gray-600">
          {allApps.length} application{allApps.length !== 1 ? 's' : ''} tracked —
          click the status badge to update progress
        </p>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-4 mb-6 text-sm">
          {error}
        </div>
      )}

      {/* Status Filter Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-4 mb-6 overflow-x-auto">
        <div className="flex gap-2">
          {STATUS_OPTIONS.map(status => (
            <button
              key={status}
              onClick={() => setActiveFilter(status)}
              className={`px-4 py-2 rounded-lg font-medium whitespace-nowrap text-sm transition-colors ${
                activeFilter === status
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {status}
              {status !== 'All' && statusCounts[status] > 0 && (
                <span className="ml-1.5 text-xs opacity-80">({statusCounts[status]})</span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Summary Stats */}
      {activeFilter === 'All' && allApps.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          {[
            { label: 'Applied', key: 'Applied', color: 'blue' },
            { label: 'Interviewing', key: 'Interviewing', color: 'purple' },
            { label: 'Offers', key: 'Offered', color: 'green' },
            { label: 'Rejected', key: 'Rejected', color: 'red' },
          ].map(({ label, key, color }) => (
            <div key={key} className={`bg-${color}-50 rounded-lg p-4`}>
              <p className={`text-${color}-600 text-sm font-medium`}>{label}</p>
              <p className={`text-2xl font-bold text-${color}-700`}>{statusCounts[key] || 0}</p>
            </div>
          ))}
        </div>
      )}

      {/* Applications List */}
      {filtered.length > 0 ? (
        <div className="space-y-3">
          {filtered.map((app) => (
            <div key={app.id} className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 hover:shadow-md transition-shadow">
              <div className="flex items-start justify-between gap-4">
                {/* Left: Job info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <h3 className="font-bold text-gray-800 text-base truncate">
                      {app.applyUrl ? (
                        <a
                          href={app.applyUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="hover:text-blue-600 hover:underline flex items-center gap-1"
                        >
                          {app.title}
                          <ExternalLink size={12} className="flex-shrink-0 text-gray-400" />
                        </a>
                      ) : app.title}
                    </h3>
                    {app.matchScore && (
                      <span className="text-xs font-bold bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded-full">
                        {app.matchScore}% match
                      </span>
                    )}
                  </div>
                  <p className="text-gray-600 font-medium text-sm">{app.company}</p>
                  <div className="flex flex-wrap gap-2 mt-2 text-xs text-gray-500">
                    <span>{app.location}</span>
                    {app.jobType && <span>· {app.jobType}</span>}
                    {(app.salaryMin || app.salaryMax) && (
                      <span className="text-green-700 font-medium">
                        · S${app.salaryMin?.toLocaleString()}
                        {app.salaryMax && `–${app.salaryMax?.toLocaleString()}`}/mo
                      </span>
                    )}
                  </div>
                  {app.skills && app.skills.length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-2">
                      {app.skills.slice(0, 5).map(s => (
                        <span key={s} className="text-xs bg-blue-50 text-blue-600 px-1.5 py-0.5 rounded">{s}</span>
                      ))}
                    </div>
                  )}
                </div>

                {/* Right: source, date, status */}
                <div className="flex flex-col items-end gap-2 flex-shrink-0">
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${SOURCE_COLORS[app.source] || 'bg-gray-100 text-gray-600'}`}>
                    {app.source}
                  </span>
                  <span className="text-xs text-gray-500">{formatDate(app.appliedAt)}</span>

                  {/* Status — interactive dropdown for external apps */}
                  {app.isExternal ? (
                    <StatusDropdown appId={app.rawId} current={app.status} onUpdate={handleStatusUpdate} />
                  ) : (
                    <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${STATUS_COLORS[app.status] || 'bg-gray-100 text-gray-600'}`}>
                      {app.status}
                    </span>
                  )}

                  {/* Delete (external only) */}
                  {app.isExternal && (
                    <button
                      onClick={() => handleDelete(app.rawId)}
                      className="text-gray-300 hover:text-red-400 transition-colors"
                      title="Remove from tracking"
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>
              </div>

              {/* AI Cover Note preview */}
              {app.coverNote && (
                <div className="mt-3 pt-3 border-t border-gray-100">
                  <p className="text-xs text-gray-500 font-semibold uppercase tracking-wide mb-1">AI Cover Note (generated)</p>
                  <p className="text-xs text-gray-600 leading-relaxed line-clamp-2">{app.coverNote}</p>
                </div>
              )}
            </div>
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-md p-12 text-center">
          <Send size={48} className="mx-auto text-gray-300 mb-4" />
          <h3 className="text-xl font-semibold text-gray-700 mb-2">No applications yet</h3>
          <p className="text-gray-600 text-sm">
            When you click <strong>Apply</strong> on any job in Job Search or AI Job Search,
            it will appear here automatically.
          </p>
        </div>
      )}
    </div>
  );
};

export default AppliedJobs;
