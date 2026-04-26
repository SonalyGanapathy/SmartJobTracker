import { Bookmark, MapPin, Banknote, Clock, Zap, ExternalLink, Send } from 'lucide-react';
import { useState } from 'react';
import toast from 'react-hot-toast';
import QuickApplyModal from './QuickApplyModal';

// Format salary with currency
const formatSalary = (salary) => {
  if (!salary) return '';
  const { min, max, currency } = salary;
  if (currency === 'INR') {
    const fmtLPA = (v) => (v / 100000).toFixed(1);
    return `₹${fmtLPA(min)} – ${fmtLPA(max)} LPA`;
  }
  if (currency && currency.includes('SGD')) {
    if (min && max) return `S$${Number(min).toLocaleString()} – S$${Number(max).toLocaleString()} /mo`;
    if (max) return `Up to S$${Number(max).toLocaleString()} /mo`;
    if (min) return `From S$${Number(min).toLocaleString()} /mo`;
  }
  if (min && max) return `$${Number(min).toLocaleString()} – $${Number(max).toLocaleString()}`;
  return '';
};

// Relative time from ISO date
const timeAgo = (dateStr) => {
  if (!dateStr) return 'Recently posted';
  const posted = new Date(dateStr);
  const now = new Date();
  const diffMs = now - posted;
  const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  if (days === 0) return 'Today';
  if (days === 1) return '1 day ago';
  if (days < 7) return `${days} days ago`;
  if (days < 30) return `${Math.floor(days / 7)} week${Math.floor(days / 7) > 1 ? 's' : ''} ago`;
  return `${Math.floor(days / 30)} month${Math.floor(days / 30) > 1 ? 's' : ''} ago`;
};

// Source → color + priority label
const sourceConfig = {
  'Company':       { color: 'bg-emerald-100 text-emerald-800', label: '🏢 Direct' },
  'LinkedIn':      { color: 'bg-blue-100 text-blue-700',       label: 'LinkedIn' },
  'Indeed':        { color: 'bg-purple-100 text-purple-700',   label: 'Indeed' },
  'Glassdoor':     { color: 'bg-green-100 text-green-700',     label: 'Glassdoor' },
  'JobStreet':     { color: 'bg-orange-100 text-orange-700',   label: 'JobStreet' },
  'Glints':        { color: 'bg-pink-100 text-pink-700',       label: 'Glints' },
  'JobsDB':        { color: 'bg-yellow-100 text-yellow-800',   label: 'JobsDB' },
  'Careers@Gov':   { color: 'bg-red-100 text-red-700',         label: '🇸🇬 Gov' },
  'Adzuna':        { color: 'bg-gray-100 text-gray-600',       label: 'Adzuna' },
  'default':       { color: 'bg-gray-100 text-gray-600',       label: null },
};

const JobCard = ({ job, onApply, onSave, isSaved = false, profile = null }) => {
  const [isApplying, setIsApplying] = useState(false);
  const [showQuickApply, setShowQuickApply] = useState(false);

  const getMatchColor = (score) => {
    if (score >= 80) return 'bg-green-100 text-green-700 border-green-300';
    if (score >= 60) return 'bg-yellow-100 text-yellow-700 border-yellow-300';
    return 'bg-red-100 text-red-700 border-red-300';
  };

  const srcCfg = sourceConfig[job.source] || sourceConfig['default'];
  const sourceLabel = srcCfg.label || job.source;
  const sourceBadge = srcCfg.color;

  const handleApply = async () => {
    // External real jobs → open QuickApply modal
    if (job.isExternal || job.applyUrl) {
      setShowQuickApply(true);
      return;
    }
    // Internal mock/DB jobs → old behavior
    setIsApplying(true);
    try {
      await onApply(job.id);
      toast.success(`Applied to ${job.company}!`);
    } catch {
      toast.error('Failed to apply.');
    } finally {
      setIsApplying(false);
    }
  };

  const handleSave = async () => {
    try {
      await onSave(job.id);
      toast.success(isSaved ? 'Removed from saved' : 'Job saved!');
    } catch {
      toast.error('Failed to save job.');
    }
  };

  const salary = formatSalary(job.salary);

  return (
    <>
      <div className={`bg-white rounded-xl shadow-md hover:shadow-lg transition-shadow p-6 border-l-4 flex flex-col
        ${job.source === 'Company' ? 'border-emerald-500' :
          job.isTrustedAgency ? 'border-violet-500' :
          job.source === 'LinkedIn' ? 'border-blue-600' :
          job.source === 'Indeed' ? 'border-purple-500' :
          job.source === 'Glassdoor' ? 'border-green-500' :
          job.source === 'JobStreet' ? 'border-orange-400' :
          'border-gray-300'}`}>
        {/* Header */}
        <div className="flex justify-between items-start mb-4">
          <div className="flex-1 min-w-0 pr-3">
            <div className="flex items-center gap-2 mb-1">
              {job.companyLogo && (
                <img src={job.companyLogo} alt={job.company} className="w-6 h-6 rounded object-contain" onError={e => e.target.style.display='none'} />
              )}
              <h3 className="text-lg font-bold text-gray-800 leading-tight line-clamp-2">{job.title}</h3>
            </div>
            <p className="text-blue-600 font-semibold">{job.company}</p>
          </div>
          <div className={`flex-shrink-0 flex items-center justify-center w-14 h-14 rounded-lg font-bold text-sm border ${getMatchColor(job.matchScore)}`}>
            {job.matchScore}%
          </div>
        </div>

        {/* Meta */}
        <div className="space-y-1.5 mb-4 text-sm text-gray-600">
          <div className="flex items-center gap-2 flex-wrap">
            <MapPin size={15} className="text-gray-400 flex-shrink-0" />
            <span>{job.location}</span>
            {job.jobType && <span className="text-xs bg-gray-100 px-2 py-0.5 rounded">{job.jobType}</span>}
            {job.isEasyApply && (
              <span className="text-xs bg-green-100 text-green-700 px-2 py-0.5 rounded font-medium flex items-center gap-1">
                <Zap size={10} /> Easy Apply
              </span>
            )}
          </div>
          {salary && (
            <div className="flex items-center gap-2">
              <Banknote size={15} className="text-gray-400" />
              <span className="font-semibold text-green-700">{salary}</span>
              {/* Pass eligibility badge */}
              {job.passInfo && (
                <span
                  className={`text-xs px-2 py-0.5 rounded-full border font-medium ${job.passInfo.color}`}
                  title={job.passInfo.tip}
                >
                  🛂 {job.passInfo.type}
                </span>
              )}
            </div>
          )}
          {/* Pass badge when no salary shown */}
          {!salary && job.isExternal && (
            <div className="flex items-center gap-2">
              <Banknote size={15} className="text-gray-400" />
              <span className="text-xs text-gray-400 italic">Salary not disclosed</span>
              <span className="text-xs px-2 py-0.5 rounded-full border font-medium text-gray-500 bg-gray-50 border-gray-200" title="Verify pass eligibility with MOM before applying">
                🛂 Check Pass
              </span>
            </div>
          )}
          <div className="flex items-center gap-2 flex-wrap">
            <Clock size={15} className="text-gray-400" />
            <span>{timeAgo(job.postedDate)}</span>
            <span className={`text-xs px-2 py-0.5 rounded font-medium ${sourceBadge}`}>{sourceLabel}</span>
            {job.isTrustedAgency && (
              <span className="text-xs bg-violet-100 text-violet-700 px-2 py-0.5 rounded font-medium border border-violet-200">
                ⭐ Trusted Agency
              </span>
            )}
            {job.source === 'Company' && (
              <span className="text-xs bg-emerald-50 text-emerald-700 px-2 py-0.5 rounded font-medium border border-emerald-200">
                Direct Post
              </span>
            )}
            {job.isExternal && (
              <span className="text-xs bg-indigo-50 text-indigo-600 px-2 py-0.5 rounded font-medium">Live</span>
            )}
          </div>
        </div>

        {/* Description */}
        {job.description && (
          <p className="text-gray-700 text-sm mb-4 line-clamp-2 flex-grow">{job.description}</p>
        )}

        {/* Skills */}
        {job.skills && job.skills.length > 0 && (
          <div className="flex flex-wrap gap-1.5 mb-4">
            {job.skills.slice(0, 5).map((skill, idx) => (
              <span key={idx} className="text-xs bg-blue-50 text-blue-700 px-2 py-1 rounded-full font-medium">{skill}</span>
            ))}
            {job.skills.length > 5 && (
              <span className="text-xs text-gray-500 self-center">+{job.skills.length - 5} more</span>
            )}
          </div>
        )}

        {/* Actions */}
        <div className="flex gap-2 pt-4 border-t border-gray-200 mt-auto">
          <button
            onClick={handleApply}
            disabled={isApplying}
            className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-lg transition-colors flex items-center justify-center gap-2"
          >
            {job.isExternal || job.applyUrl
              ? <><Zap size={15} /> Quick Apply</>
              : isApplying
                ? <><Send size={15} /> Applying…</>
                : <><Send size={15} /> Apply Now</>
            }
          </button>
          {(job.isExternal || job.applyUrl) && (
            <a
              href={job.applyUrl}
              target="_blank"
              rel="noopener noreferrer"
              title="Open job page"
              className="px-3 py-2 bg-gray-100 hover:bg-gray-200 text-gray-600 rounded-lg transition-colors flex items-center"
            >
              <ExternalLink size={16} />
            </a>
          )}
          <button
            onClick={handleSave}
            className={`px-4 py-2 rounded-lg font-medium transition-colors ${
              isSaved ? 'bg-blue-100 text-blue-600 hover:bg-blue-200' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            <Bookmark size={18} fill={isSaved ? 'currentColor' : 'none'} />
          </button>
        </div>
      </div>

      {/* Quick Apply Modal */}
      {showQuickApply && (
        <QuickApplyModal
          job={job}
          profile={profile}
          onClose={() => setShowQuickApply(false)}
        />
      )}
    </>
  );
};

export default JobCard;
