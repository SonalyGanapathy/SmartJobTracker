import { useState } from 'react';
import {
  X, Copy, Check, ExternalLink, User, Mail, Phone,
  Briefcase, Zap, ChevronDown, ChevronUp, Globe, ShieldCheck, Link2, Search
} from 'lucide-react';

// Portals that require Singpass — India-based applicants can't apply directly
const SINGPASS_PORTALS = ['MyCareersFuture', 'Careers@Gov'];

const QuickApplyModal = ({ job, profile, onClose }) => {
  const [copied, setCopied] = useState({});
  const [showAllSkills, setShowAllSkills] = useState(false);
  const [launched, setLaunched] = useState(null); // which channel was launched

  const needsSingpass = SINGPASS_PORTALS.some(p =>
    (job.source || '').toLowerCase().includes(p.toLowerCase())
  );

  const copyField = (key, value) => {
    navigator.clipboard.writeText(value).then(() => {
      setCopied(prev => ({ ...prev, [key]: true }));
      setTimeout(() => setCopied(prev => ({ ...prev, [key]: false })), 2000);
    });
  };

  const topSkills = profile?.skills?.slice(0, 6) || [];
  const allSkills = profile?.skills || [];
  const skillsText = allSkills.join(', ');

  const coverSnippet = profile
    ? `Hi, I am ${profile.fullName}, a ${profile.preferredRoles?.[0] || 'Software Engineer'} with ${profile.experienceRange?.min}–${profile.experienceRange?.max}+ years of experience in ${topSkills.slice(0, 3).join(', ')}. I am very interested in the ${job.title} role at ${job.company} and believe my background aligns well with your requirements. I am based in India, immediately available, and would require Employment Pass (EP) sponsorship to relocate to Singapore.`
    : '';

  // Salary display
  const salaryLabel = () => {
    if (!job.salaryMin && !job.salaryMax) return null;
    const currency = job.currency === 'SGD' ? 'S$' : '$';
    if (job.salaryMin && job.salaryMax)
      return `${currency}${Number(job.salaryMin).toLocaleString()} – ${currency}${Number(job.salaryMax).toLocaleString()} /mo`;
    if (job.salaryMax) return `Up to ${currency}${Number(job.salaryMax).toLocaleString()} /mo`;
    return `From ${currency}${Number(job.salaryMin).toLocaleString()} /mo`;
  };

  // Alternative apply links for Singpass-required portals
  const linkedInSearchUrl = `https://www.linkedin.com/jobs/search/?keywords=${encodeURIComponent(job.title)}&location=Singapore&f_TPR=r604800`;
  const indeedSearchUrl = `https://sg.indeed.com/jobs?q=${encodeURIComponent(job.title)}&l=Singapore`;
  const googleJobsUrl = `https://www.google.com/search?q=${encodeURIComponent(`${job.title} ${job.company} Singapore job apply`)}`;
  const companyLinkedInUrl = `https://www.linkedin.com/company/${encodeURIComponent(job.company.toLowerCase().replace(/\s+/g, '-'))}/jobs/`;

  const openLink = (url, label) => {
    window.open(url, '_blank', 'noopener,noreferrer');
    setLaunched(label);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black bg-opacity-50 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">

        {/* Header */}
        <div className="sticky top-0 bg-gradient-to-r from-blue-600 to-indigo-600 text-white p-5 rounded-t-2xl">
          <button onClick={onClose} className="absolute top-4 right-4 text-white/80 hover:text-white transition-colors">
            <X size={20} />
          </button>
          <div className="flex items-center gap-2 mb-1">
            <Zap size={18} className="text-yellow-300" />
            <span className="text-sm font-semibold text-yellow-300 uppercase tracking-wide">Quick Apply</span>
          </div>
          <h2 className="text-lg font-bold leading-tight">{job.title}</h2>
          <p className="text-blue-100 text-sm mt-0.5">{job.company} · {job.location}</p>
          {salaryLabel() && <p className="text-green-300 text-sm font-semibold mt-1">{salaryLabel()}</p>}
          <div className="flex items-center gap-2 mt-2 flex-wrap">
            <span className="text-xs bg-white/20 px-2 py-0.5 rounded-full">{job.source}</span>
            {job.isEasyApply && <span className="text-xs bg-green-400/30 text-green-200 px-2 py-0.5 rounded-full font-medium">Easy Apply</span>}
            {needsSingpass && <span className="text-xs bg-yellow-400/30 text-yellow-200 px-2 py-0.5 rounded-full font-medium">⚠ Singpass Required</span>}
          </div>
        </div>

        <div className="p-5 space-y-4">

          {/* Singpass Warning + Alternative Apply */}
          {needsSingpass && (
            <div className="bg-amber-50 border border-amber-200 rounded-xl p-4">
              <div className="flex items-start gap-2 mb-3">
                <ShieldCheck size={16} className="text-amber-600 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="text-sm font-semibold text-amber-800">Direct apply requires Singpass 🇸🇬</p>
                  <p className="text-xs text-amber-700 mt-0.5">
                    {job.source} requires a Singapore Singpass account. As an India-based applicant,
                    use these alternatives to apply for the <strong>same job</strong>:
                  </p>
                </div>
              </div>

              {/* Alternative Apply Buttons */}
              <div className="space-y-2">
                <button
                  onClick={() => openLink(linkedInSearchUrl, 'LinkedIn')}
                  className="w-full flex items-center gap-3 bg-[#0A66C2] hover:bg-[#0855a3] text-white px-4 py-2.5 rounded-lg text-sm font-semibold transition-colors"
                >
                  <Link2 size={16} />
                  <span className="flex-1 text-left">Search on LinkedIn Jobs</span>
                  {launched === 'LinkedIn' && <Check size={14} />}
                </button>

                <button
                  onClick={() => openLink(indeedSearchUrl, 'Indeed')}
                  className="w-full flex items-center gap-3 bg-[#2164f3] hover:bg-[#1a52d1] text-white px-4 py-2.5 rounded-lg text-sm font-semibold transition-colors"
                >
                  <Search size={16} />
                  <span className="flex-1 text-left">Search on Indeed Singapore</span>
                  {launched === 'Indeed' && <Check size={14} />}
                </button>

                <button
                  onClick={() => openLink(companyLinkedInUrl, 'CompanyLI')}
                  className="w-full flex items-center gap-3 bg-white border border-gray-300 hover:bg-gray-50 text-gray-700 px-4 py-2.5 rounded-lg text-sm font-semibold transition-colors"
                >
                  <Briefcase size={16} className="text-gray-500" />
                  <span className="flex-1 text-left">View {job.company} Jobs on LinkedIn</span>
                  {launched === 'CompanyLI' && <Check size={14} className="text-green-600" />}
                </button>

                <button
                  onClick={() => openLink(googleJobsUrl, 'Google')}
                  className="w-full flex items-center gap-3 bg-white border border-gray-300 hover:bg-gray-50 text-gray-700 px-4 py-2.5 rounded-lg text-sm font-semibold transition-colors"
                >
                  <Globe size={16} className="text-gray-500" />
                  <span className="flex-1 text-left">Find via Google Jobs</span>
                  {launched === 'Google' && <Check size={14} className="text-green-600" />}
                </button>
              </div>

              {launched && (
                <p className="text-xs text-green-700 font-medium mt-2 text-center">
                  ✅ Opened! Copy your details below and paste into the application form.
                </p>
              )}
            </div>
          )}

          {/* Direct apply (non-Singpass portals) */}
          {!needsSingpass && (
            <div className="bg-blue-50 border border-blue-200 rounded-xl p-3 text-sm text-blue-800">
              <p className="font-semibold mb-1">Apply in 30 seconds:</p>
              <p>1. Copy each field below → 2. Click <strong>Open Job Page</strong> → 3. Paste & submit ✅</p>
            </div>
          )}

          {/* Your Details */}
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">Your Details — Click to Copy</h3>
            <CopyField icon={<User size={15} />} label="Full Name" value={profile?.fullName || ''} copied={copied['name']} onCopy={() => copyField('name', profile?.fullName || '')} />
            <CopyField icon={<Mail size={15} />} label="Email" value={profile?.email || ''} copied={copied['email']} onCopy={() => copyField('email', profile?.email || '')} />
            <CopyField icon={<Phone size={15} />} label="Phone" value={profile?.phone || ''} copied={copied['phone']} onCopy={() => copyField('phone', profile?.phone || '')} />
            <CopyField icon={<Briefcase size={15} />} label="Current Role" value={profile?.preferredRoles?.[0] || 'Software Engineer'} copied={copied['role']} onCopy={() => copyField('role', profile?.preferredRoles?.[0] || '')} />
            <CopyField icon={<Globe size={15} />} label="Location / Availability" value="India — Open to relocation Singapore · EP sponsorship needed · Immediate joiner" copied={copied['location']} onCopy={() => copyField('location', 'India — Open to relocation Singapore · EP sponsorship needed · Immediate joiner')} />
          </div>

          {/* Skills */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">Key Skills</h3>
              <button
                onClick={() => copyField('skills', skillsText)}
                className={`text-xs flex items-center gap-1 px-2 py-1 rounded-lg transition-colors font-medium ${copied['skills'] ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600 hover:bg-blue-100 hover:text-blue-700'}`}
              >
                {copied['skills'] ? <Check size={12} /> : <Copy size={12} />}
                {copied['skills'] ? 'Copied!' : 'Copy All'}
              </button>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {(showAllSkills ? allSkills : topSkills).map((skill, i) => (
                <button
                  key={i}
                  onClick={() => copyField(`skill_${i}`, skill)}
                  className={`text-xs px-2.5 py-1 rounded-full border font-medium transition-colors ${copied[`skill_${i}`] ? 'bg-green-100 text-green-700 border-green-300' : 'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100'}`}
                >
                  {copied[`skill_${i}`] ? '✓ ' : ''}{skill}
                </button>
              ))}
              {allSkills.length > 6 && (
                <button onClick={() => setShowAllSkills(!showAllSkills)} className="text-xs text-gray-500 hover:text-gray-700 flex items-center gap-1 px-2">
                  {showAllSkills ? <><ChevronUp size={12} /> Less</> : <><ChevronDown size={12} /> +{allSkills.length - 6} more</>}
                </button>
              )}
            </div>
          </div>

          {/* Work Pass */}
          <div className="rounded-xl border border-amber-200 bg-amber-50 p-3">
            <div className="flex items-center gap-2 mb-2">
              <ShieldCheck size={15} className="text-amber-600" />
              <h3 className="text-sm font-semibold text-amber-800">Work Pass Info — Foreign Applicant 🇮🇳</h3>
            </div>
            {job.passInfo ? (
              <p className="text-xs text-amber-700 mb-1">
                Based on salary: <span className={`font-semibold px-1.5 py-0.5 rounded ${job.passInfo.color}`}>{job.passInfo.type}</span>
                <br />{job.passInfo.tip}
              </p>
            ) : (
              <p className="text-xs text-amber-700 mb-1">Salary not disclosed — verify pass eligibility before applying.</p>
            )}
            <p className="text-xs text-amber-700">💡 Mention: <em>"I require Employment Pass sponsorship."</em></p>
            <a href="https://www.mom.gov.sg/passes-and-permits/employment-pass/eligibility" target="_blank" rel="noopener noreferrer" className="text-xs text-blue-600 hover:underline font-medium">
              → Check EP eligibility on MOM.gov.sg ↗
            </a>
          </div>

          {/* Cover Note */}
          {coverSnippet && (
            <div>
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">Cover Note</h3>
                <button
                  onClick={() => copyField('cover', coverSnippet)}
                  className={`text-xs flex items-center gap-1 px-2 py-1 rounded-lg transition-colors font-medium ${copied['cover'] ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600 hover:bg-blue-100 hover:text-blue-700'}`}
                >
                  {copied['cover'] ? <Check size={12} /> : <Copy size={12} />}
                  {copied['cover'] ? 'Copied!' : 'Copy'}
                </button>
              </div>
              <p className="text-xs text-gray-600 bg-gray-50 rounded-xl p-3 leading-relaxed border border-gray-200">{coverSnippet}</p>
            </div>
          )}

          {/* Direct apply CTA (non-Singpass only) */}
          {!needsSingpass && (
            <div className="space-y-2 pt-1">
              <button
                onClick={() => openLink(job.applyUrl, 'Direct')}
                className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 px-6 rounded-xl transition-colors flex items-center justify-center gap-2 text-base"
              >
                <ExternalLink size={18} />
                {launched === 'Direct' ? 'Job Page Opened — Go Apply! 🚀' : 'Open Job Page & Apply'}
              </button>
              {launched === 'Direct' && (
                <div className="text-center text-sm text-green-600 font-medium">✅ Tab opened! Paste your details and submit.</div>
              )}
            </div>
          )}

          <button onClick={onClose} className="w-full text-gray-500 hover:text-gray-700 py-2 text-sm transition-colors">Close</button>
        </div>
      </div>
    </div>
  );
};

const CopyField = ({ icon, label, value, copied, onCopy }) => (
  <button
    onClick={onCopy}
    disabled={!value}
    className={`w-full flex items-center gap-3 p-3 rounded-xl border text-left transition-all
      ${copied ? 'bg-green-50 border-green-300 text-green-700' : 'bg-gray-50 border-gray-200 hover:bg-blue-50 hover:border-blue-300 text-gray-700'}
      ${!value ? 'opacity-40 cursor-default' : 'cursor-pointer'}`}
  >
    <span className={copied ? 'text-green-500' : 'text-gray-400'}>{icon}</span>
    <div className="flex-1 min-w-0">
      <p className="text-xs text-gray-400 font-medium">{label}</p>
      <p className="text-sm font-medium truncate">{value || 'Not set'}</p>
    </div>
    <span className={`flex-shrink-0 ${copied ? 'text-green-500' : 'text-gray-300'}`}>
      {copied ? <Check size={16} /> : <Copy size={16} />}
    </span>
  </button>
);

export default QuickApplyModal;
