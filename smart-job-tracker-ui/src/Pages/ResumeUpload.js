import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  FileText, Upload, CheckCircle, AlertCircle, User, Briefcase,
  GraduationCap, Zap, ArrowRight, Edit3, Save
} from 'lucide-react';
import toast from 'react-hot-toast';
import { parseResume, updateProfile } from '../services/api';

// ── Manual Profile Form (fallback when PDF can't be parsed) ───────────────────

function ManualProfileForm({ onSaved }) {
  const [form, setForm] = useState({
    fullName: '', email: '', phone: '', country: '',
    preferredLocation: '', skills: '', preferredRoles: '',
    minExperienceYears: '', summary: ''
  });
  const [saving, setSaving] = useState(false);

  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));

  const handleSave = async () => {
    if (!form.fullName && !form.skills) {
      toast.error('Please fill in at least your name or skills');
      return;
    }
    setSaving(true);
    try {
      const payload = {};
      if (form.fullName) payload.fullName = form.fullName;
      if (form.email) payload.email = form.email;
      if (form.phone) payload.phone = form.phone;
      if (form.country) payload.country = form.country;
      if (form.preferredLocation) payload.preferredLocation = form.preferredLocation;
      if (form.skills) payload.skills = form.skills;
      if (form.preferredRoles) payload.preferredRoles = form.preferredRoles;
      if (form.minExperienceYears) payload.minExperienceYears = parseInt(form.minExperienceYears);
      if (form.summary) payload.summary = form.summary;

      await updateProfile(payload);
      toast.success('Profile saved! AI Job Search is now pre-filled with your data.');
      onSaved();
    } catch (err) {
      toast.error('Could not save profile. Make sure the backend is running.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="bg-white rounded-xl shadow-md p-6 space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Full Name *</label>
          <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. Sonaly Ganapathy"
            value={form.fullName} onChange={e => set('fullName', e.target.value)} />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Email</label>
          <input type="email" className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="you@example.com"
            value={form.email} onChange={e => set('email', e.target.value)} />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Phone</label>
          <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="+91 98765 43210"
            value={form.phone} onChange={e => set('phone', e.target.value)} />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Your Country</label>
          <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. India"
            value={form.country} onChange={e => set('country', e.target.value)} />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Target Job Location</label>
          <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. Singapore"
            value={form.preferredLocation} onChange={e => set('preferredLocation', e.target.value)} />
        </div>
        <div>
          <label className="text-xs font-medium text-gray-600 mb-1 block">Years of Experience</label>
          <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
            placeholder="e.g. 5"
            value={form.minExperienceYears} onChange={e => set('minExperienceYears', e.target.value)} />
        </div>
      </div>

      <div>
        <label className="text-xs font-medium text-gray-600 mb-1 block">
          Your Skills * <span className="text-gray-400">(comma-separated)</span>
        </label>
        <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
          placeholder="e.g. ASP.NET Core, C#, SQL Server, Angular, Azure"
          value={form.skills} onChange={e => set('skills', e.target.value)} />
      </div>

      <div>
        <label className="text-xs font-medium text-gray-600 mb-1 block">
          Target Job Roles <span className="text-gray-400">(comma-separated)</span>
        </label>
        <input className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
          placeholder="e.g. .NET Developer, Backend Engineer, Software Engineer"
          value={form.preferredRoles} onChange={e => set('preferredRoles', e.target.value)} />
      </div>

      <div>
        <label className="text-xs font-medium text-gray-600 mb-1 block">Professional Summary</label>
        <textarea rows={3} className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm resize-none"
          placeholder="Brief description of your experience and goals..."
          value={form.summary} onChange={e => set('summary', e.target.value)} />
      </div>

      <button
        onClick={handleSave}
        disabled={saving}
        className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
      >
        <Save size={16} />
        {saving ? 'Saving…' : 'Save Profile & Go to AI Job Search'}
      </button>
    </div>
  );
}

// ── Main Component ────────────────────────────────────────────────────────────

const ResumeUpload = () => {
  const navigate = useNavigate();
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [parsedData, setParsedData] = useState(null);
  const [profileUpdated, setProfileUpdated] = useState(false);
  const [unreadable, setUnreadable] = useState(false); // PDF can't be parsed
  const [showManual, setShowManual] = useState(false);
  const fileInputRef = useRef(null);

  const handleDrop = (e) => {
    e.preventDefault();
    e.stopPropagation();
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile && (droppedFile.type === 'application/pdf' || droppedFile.type.includes('word'))) {
      setFile(droppedFile);
    } else {
      toast.error('Please upload a PDF or Word document');
    }
  };

  const handleFileSelect = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) setFile(selectedFile);
  };

  const handleUpload = async () => {
    if (!file) { toast.error('Please select a file'); return; }

    setUploading(true);
    setParsedData(null);
    setProfileUpdated(false);
    setUnreadable(false);

    try {
      const parseRes = await parseResume(file);
      const parsed = parseRes.data;

      // Detect unreadable PDF
      if (parsed.summary === '__UNREADABLE_PDF__') {
        setUnreadable(true);
        setShowManual(true);
        toast('PDF could not be read as text — please fill in your details manually.', { icon: '⚠️' });
        return;
      }

      setParsedData(parsed);

      // Auto-update profile with extracted data
      const skills = (parsed.skills || []).join(', ');
      const hasUsefulData = skills || parsed.summary || parsed.fullName || parsed.email;

      if (hasUsefulData) {
        const payload = {};
        if (parsed.fullName) payload.fullName = parsed.fullName;
        if (parsed.email) payload.email = parsed.email;
        if (parsed.phone) payload.phone = parsed.phone;
        if (parsed.summary) payload.summary = parsed.summary;
        if (skills) payload.skills = skills;

        await updateProfile(payload);
        setProfileUpdated(true);
        toast.success('Resume parsed & profile updated!');
      } else {
        // Parsed OK but nothing useful extracted
        setShowManual(true);
        toast('Limited data extracted from your PDF. Please fill in your details below.', { icon: '⚠️' });
      }
    } catch (error) {
      const msg = error?.response?.data;
      toast.error(typeof msg === 'string' ? msg : 'Upload failed — make sure the backend is running.');
    } finally {
      setUploading(false);
    }
  };

  const resetUpload = () => {
    setFile(null);
    setParsedData(null);
    setProfileUpdated(false);
    setUnreadable(false);
    setShowManual(false);
  };

  // ── Render ──

  const showResults = parsedData || unreadable || showManual;

  return (
    <div>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-2 flex items-center gap-2">
          <FileText className="text-blue-600" /> Upload Your Resume
        </h1>
        <p className="text-gray-600">
          Upload your resume to auto-extract skills & experience — or fill in your details manually below.
        </p>
      </div>

      {/* Upload Zone (always shown if no result yet, or after reset) */}
      {!showResults && (
        <div className="space-y-6">
          <div
            onDrop={handleDrop}
            onDragOver={(e) => e.preventDefault()}
            className="bg-white rounded-xl shadow-md p-12 border-2 border-dashed border-gray-300 hover:border-blue-600 transition-colors cursor-pointer"
            onClick={() => fileInputRef.current?.click()}
          >
            <div className="text-center">
              <Upload className="mx-auto text-gray-400 mb-4" size={48} />
              <h3 className="text-xl font-semibold text-gray-800 mb-2">Drag and drop your resume</h3>
              <p className="text-gray-600 mb-4">or click to browse your files</p>
              <p className="text-sm text-gray-500">Supported: PDF, DOC, DOCX</p>
            </div>
            <input ref={fileInputRef} type="file" onChange={handleFileSelect} accept=".pdf,.doc,.docx" className="hidden" />
          </div>

          {file && (
            <div className="bg-blue-50 rounded-xl p-4 border border-blue-200 flex items-center gap-3">
              <FileText className="text-blue-600" size={28} />
              <div className="flex-1">
                <p className="font-semibold text-gray-800 text-sm">{file.name}</p>
                <p className="text-xs text-gray-500">{(file.size / 1024).toFixed(1)} KB</p>
              </div>
              <button onClick={() => setFile(null)} className="text-gray-400 hover:text-gray-600">✕</button>
            </div>
          )}

          <button
            onClick={handleUpload}
            disabled={!file || uploading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold py-3 rounded-lg transition-colors flex items-center justify-center gap-2"
          >
            <Upload size={18} />
            {uploading ? 'Parsing Resume…' : 'Upload & Parse Resume'}
          </button>

          {/* Manual entry option always available */}
          <div className="text-center">
            <p className="text-sm text-gray-500">
              Prefer to fill in manually?{' '}
              <button onClick={() => setShowManual(true)} className="text-blue-600 hover:underline font-medium">
                Enter profile details directly
              </button>
            </p>
          </div>
        </div>
      )}

      {/* Results / Manual Form */}
      {showResults && (
        <div className="space-y-6">
          {/* Unreadable PDF warning */}
          {unreadable && (
            <div className="bg-yellow-50 rounded-xl p-5 border-l-4 border-yellow-500 flex items-start gap-3">
              <AlertCircle className="text-yellow-600 flex-shrink-0 mt-0.5" size={22} />
              <div>
                <h3 className="font-bold text-yellow-800">PDF Could Not Be Read</h3>
                <p className="text-sm text-yellow-700 mt-1">
                  Your PDF appears to be <strong>scanned or image-based</strong>, so text could not be extracted automatically.
                  Please fill in your profile details below — it only takes 2 minutes.
                </p>
              </div>
            </div>
          )}

          {/* Partial extraction success */}
          {parsedData && !unreadable && (
            <div className={`rounded-xl shadow-md p-5 border-l-4 ${profileUpdated ? 'bg-green-50 border-green-600' : 'bg-blue-50 border-blue-600'}`}>
              <div className="flex items-center gap-3">
                <CheckCircle className={profileUpdated ? 'text-green-600' : 'text-blue-600'} size={26} />
                <div>
                  <h3 className="text-lg font-bold text-gray-800">
                    {profileUpdated ? 'Profile Updated from Resume!' : 'Resume Parsed'}
                  </h3>
                  <p className="text-sm text-gray-600">
                    {profileUpdated
                      ? 'Your skills and details have been saved. AI Job Search is now pre-filled.'
                      : `Parsed ${file?.name}`}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Extracted Data Display */}
          {parsedData && !unreadable && (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Basic Info */}
              <div className="bg-white rounded-xl shadow-sm border p-5">
                <h3 className="text-base font-bold text-gray-800 mb-3 flex items-center gap-2">
                  <User size={16} className="text-blue-600" /> Basic Information
                </h3>
                <div className="space-y-1.5 text-sm">
                  {parsedData.fullName && <p><span className="text-gray-500">Name:</span> <span className="font-medium">{parsedData.fullName}</span></p>}
                  {parsedData.email && <p><span className="text-gray-500">Email:</span> <span>{parsedData.email}</span></p>}
                  {parsedData.phone && <p><span className="text-gray-500">Phone:</span> <span>{parsedData.phone}</span></p>}
                  {parsedData.summary && (
                    <div className="mt-2 pt-2 border-t border-gray-100">
                      <p className="text-gray-500 mb-1">Summary</p>
                      <p className="text-gray-700">{parsedData.summary}</p>
                    </div>
                  )}
                  {!parsedData.fullName && !parsedData.email && <p className="text-gray-400 italic text-xs">No contact info extracted</p>}
                </div>
              </div>

              {/* Skills */}
              <div className="bg-white rounded-xl shadow-sm border p-5">
                <h3 className="text-base font-bold text-gray-800 mb-3 flex items-center gap-2">
                  <Zap size={16} className="text-blue-600" /> Extracted Skills
                  {(parsedData.skills || []).length > 0 && (
                    <span className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full">
                      {parsedData.skills.length} found
                    </span>
                  )}
                </h3>
                {parsedData.skills?.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {parsedData.skills.map((s, i) => (
                      <span key={i} className="bg-blue-100 text-blue-700 px-3 py-1 rounded-full text-sm font-medium">{s}</span>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-400 italic text-sm">No skills detected — fill in manually below</p>
                )}
              </div>

              {/* Experience */}
              {parsedData.experience?.length > 0 && (
                <div className="bg-white rounded-xl shadow-sm border p-5 lg:col-span-2">
                  <h3 className="text-base font-bold text-gray-800 mb-3 flex items-center gap-2">
                    <Briefcase size={16} className="text-blue-600" /> Work Experience
                  </h3>
                  <div className="space-y-3">
                    {parsedData.experience.map((exp, i) => (
                      <div key={i} className="flex justify-between items-start pb-3 border-b border-gray-100 last:border-b-0">
                        <div>
                          <p className="font-bold text-gray-800 text-sm">{exp.role}</p>
                          {exp.company && <p className="text-blue-600 text-xs">{exp.company}</p>}
                        </div>
                        {exp.duration && <span className="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded-full">{exp.duration}</span>}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Education */}
              {parsedData.education?.length > 0 && (
                <div className="bg-white rounded-xl shadow-sm border p-5 lg:col-span-2">
                  <h3 className="text-base font-bold text-gray-800 mb-3 flex items-center gap-2">
                    <GraduationCap size={16} className="text-blue-600" /> Education
                  </h3>
                  {parsedData.education.map((edu, i) => (
                    <div key={i} className="mb-2">
                      <p className="font-bold text-gray-800 text-sm">{edu.degree}</p>
                      <p className="text-gray-500 text-xs">
                        {edu.institution}{edu.year && ` · ${edu.year}`}
                      </p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Manual profile entry (shown when PDF unreadable OR skills missing) */}
          {showManual && (
            <div>
              <div className="flex items-center gap-2 mb-4">
                <Edit3 size={18} className="text-blue-600" />
                <h2 className="text-lg font-bold text-gray-800">
                  {unreadable ? 'Fill In Your Profile' : 'Add Missing Details'}
                </h2>
              </div>
              <ManualProfileForm onSaved={() => navigate('/ai-search')} />
            </div>
          )}

          {/* If profile was updated from PDF and nothing missing, show action buttons */}
          {profileUpdated && !showManual && (
            <div className="bg-white rounded-xl shadow-sm border p-5 flex flex-col sm:flex-row gap-3">
              <button onClick={() => navigate('/ai-search')}
                className="flex-1 bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 rounded-lg flex items-center justify-center gap-2">
                <ArrowRight size={16} /> Go to AI Job Search
              </button>
              <button onClick={() => setShowManual(true)}
                className="flex-1 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium py-3 rounded-lg flex items-center justify-center gap-2">
                <Edit3 size={15} /> Add More Details
              </button>
              <button onClick={resetUpload} className="px-5 py-3 border text-gray-600 rounded-lg hover:bg-gray-50">
                Upload Another
              </button>
            </div>
          )}

          {/* Reset link for unreadable case */}
          {unreadable && (
            <p className="text-center text-sm text-gray-500">
              Want to try again?{' '}
              <button onClick={resetUpload} className="text-blue-600 hover:underline">Upload a different file</button>
            </p>
          )}
        </div>
      )}
    </div>
  );
};

export default ResumeUpload;
