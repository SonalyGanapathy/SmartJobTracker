import { useEffect, useState } from 'react';
import { User, X } from 'lucide-react';
import LoadingSpinner from '../components/Common/LoadingSpinner';
import { getProfile, updateProfile } from '../services/api';
import toast from 'react-hot-toast';

const Profile = () => {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [newSkill, setNewSkill] = useState('');
  const [newRole, setNewRole] = useState('');

  // Transform backend DTO → frontend shape
  const normalizeProfile = (raw) => ({
    ...raw,
    // backend: preferredLocation (string) → frontend: location
    location: raw.preferredLocation || '',
    // backend: skills/preferredRoles are comma-separated strings → frontend: arrays
    skills: raw.skills
      ? raw.skills.split(',').map(s => s.trim()).filter(Boolean)
      : [],
    preferredRoles: raw.preferredRoles
      ? raw.preferredRoles.split(',').map(s => s.trim()).filter(Boolean)
      : [],
    // backend: minExperienceYears / maxExperienceYears → frontend: experienceRange
    experienceRange: {
      min: raw.minExperienceYears ?? 0,
      max: raw.maxExperienceYears ?? 0,
    },
  });

  // Transform frontend shape → backend DTO for save
  const denormalizeProfile = (p) => ({
    ...p,
    preferredLocation: p.location,
    skills: Array.isArray(p.skills) ? p.skills.join(', ') : p.skills,
    preferredRoles: Array.isArray(p.preferredRoles) ? p.preferredRoles.join(', ') : p.preferredRoles,
    minExperienceYears: p.experienceRange?.min ?? 0,
    maxExperienceYears: p.experienceRange?.max ?? 0,
  });

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await getProfile();
        setProfile(normalizeProfile(response.data));
      } catch (error) {
        console.error('Failed to fetch profile:', error);
        toast.error('Failed to load profile');
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, []);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setProfile(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleAddSkill = () => {
    if (newSkill.trim()) {
      setProfile(prev => ({
        ...prev,
        skills: [...prev.skills, newSkill.trim()]
      }));
      setNewSkill('');
    }
  };

  const handleRemoveSkill = (skill) => {
    setProfile(prev => ({
      ...prev,
      skills: prev.skills.filter(s => s !== skill)
    }));
  };

  const handleAddRole = () => {
    if (newRole.trim()) {
      setProfile(prev => ({
        ...prev,
        preferredRoles: [...prev.preferredRoles, newRole.trim()]
      }));
      setNewRole('');
    }
  };

  const handleRemoveRole = (role) => {
    setProfile(prev => ({
      ...prev,
      preferredRoles: prev.preferredRoles.filter(r => r !== role)
    }));
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await updateProfile(denormalizeProfile(profile));
      toast.success('Profile updated successfully!');
    } catch (error) {
      console.error('Failed to update profile:', error);
      toast.error('Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSpinner />;
  if (!profile) return <div>Failed to load profile</div>;

  return (
    <div>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-800 mb-2 flex items-center gap-2">
          <User className="text-blue-600" />
          My Profile
        </h1>
        <p className="text-gray-600">Update your profile to improve job matching</p>
      </div>

      {/* Profile Form */}
      <div className="space-y-6">
        {/* Personal Info Section */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-6">Personal Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Full Name</label>
              <input
                type="text"
                name="fullName"
                value={profile.fullName}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
              <input
                type="email"
                name="email"
                value={profile.email}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Phone</label>
              <input
                type="tel"
                name="phone"
                value={profile.phone}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Country</label>
              <input
                type="text"
                name="country"
                value={profile.country}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              />
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-2">Location</label>
              <input
                type="text"
                name="location"
                value={profile.location}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              />
            </div>
          </div>
        </div>

        {/* Preferences Section */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-6">Job Preferences</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Location Type</label>
              <select
                name="locationType"
                value={profile.locationType}
                onChange={handleInputChange}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
              >
                <option value="Remote">Remote</option>
                <option value="Hybrid">Hybrid</option>
                <option value="OnSite">On-Site</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Experience Range (years)</label>
              <div className="flex gap-2">
                <input
                  type="number"
                  value={profile.experienceRange.min}
                  onChange={(e) => setProfile(prev => ({
                    ...prev,
                    experienceRange: { ...prev.experienceRange, min: parseInt(e.target.value) }
                  }))}
                  placeholder="Min"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
                />
                <input
                  type="number"
                  value={profile.experienceRange.max}
                  onChange={(e) => setProfile(prev => ({
                    ...prev,
                    experienceRange: { ...prev.experienceRange, max: parseInt(e.target.value) }
                  }))}
                  placeholder="Max"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Summary Section */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-6">Professional Summary</h2>
          <textarea
            name="summary"
            value={profile.summary}
            onChange={handleInputChange}
            rows="4"
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
            placeholder="Tell us about yourself..."
          />
        </div>

        {/* Skills Section */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-6">Technical Skills</h2>
          <div className="flex gap-2 mb-4">
            <input
              type="text"
              value={newSkill}
              onChange={(e) => setNewSkill(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleAddSkill()}
              placeholder="Add a skill..."
              className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
            />
            <button
              onClick={handleAddSkill}
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg transition-colors"
            >
              Add
            </button>
          </div>
          <div className="flex flex-wrap gap-2">
            {profile.skills.map((skill, idx) => (
              <div
                key={idx}
                className="bg-blue-100 text-blue-700 px-3 py-1 rounded-full text-sm font-medium flex items-center gap-2"
              >
                {skill}
                <button onClick={() => handleRemoveSkill(skill)} className="hover:text-blue-900">
                  <X size={14} />
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Preferred Roles Section */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <h2 className="text-xl font-bold text-gray-800 mb-6">Preferred Job Roles</h2>
          <div className="flex gap-2 mb-4">
            <input
              type="text"
              value={newRole}
              onChange={(e) => setNewRole(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleAddRole()}
              placeholder="Add a preferred role..."
              className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-600"
            />
            <button
              onClick={handleAddRole}
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg transition-colors"
            >
              Add
            </button>
          </div>
          <div className="flex flex-wrap gap-2">
            {profile.preferredRoles.map((role, idx) => (
              <div
                key={idx}
                className="bg-purple-100 text-purple-700 px-3 py-1 rounded-full text-sm font-medium flex items-center gap-2"
              >
                {role}
                <button onClick={() => handleRemoveRole(role)} className="hover:text-purple-900">
                  <X size={14} />
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Save Button */}
        <div className="bg-white rounded-xl shadow-md p-6">
          <button
            onClick={handleSave}
            disabled={saving}
            className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold py-3 px-8 rounded-lg transition-colors"
          >
            {saving ? 'Saving...' : 'Save Profile'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default Profile;
