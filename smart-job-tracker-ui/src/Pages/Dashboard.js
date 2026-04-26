import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LayoutDashboard, Briefcase, Bookmark, Award, Zap, TrendingUp,
  Clock, ExternalLink, AlertCircle
} from 'lucide-react';
import StatsCard from '../components/Dashboard/StatsCard';
import LoadingSpinner from '../components/Common/LoadingSpinner';
import {
  getDashboardStats,
  getExternalApplications,
} from '../services/api';

const formatDate = (d) => {
  if (!d) return '—';
  const dt = new Date(d);
  return dt.toLocaleDateString('en-SG', { day: 'numeric', month: 'short', year: 'numeric' });
};

const statusColor = (s) => {
  if (!s) return 'bg-gray-100 text-gray-600';
  switch (s.toLowerCase()) {
    case 'applied': return 'bg-blue-100 text-blue-700';
    case 'screening': return 'bg-yellow-100 text-yellow-700';
    case 'interviewing': return 'bg-purple-100 text-purple-700';
    case 'offered': return 'bg-green-100 text-green-700';
    case 'rejected': return 'bg-red-100 text-red-600';
    case 'withdrawn': return 'bg-gray-100 text-gray-600';
    default: return 'bg-gray-100 text-gray-600';
  }
};

const Dashboard = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState(null);
  const [recentApps, setRecentApps] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [statsRes, externalRes] = await Promise.all([
          getDashboardStats(),
          getExternalApplications().catch(() => ({ data: [] })),
        ]);

        setStats(statsRes.data);

        // Merge external applications into recent list
        const externalApps = Array.isArray(externalRes.data) ? externalRes.data : [];
        // Also pull from stats.recentApplications (internal DB apps)
        const internalRecent = statsRes.data?.recentApplications || [];

        // Normalise both to same shape
        const normalised = [
          ...externalApps.slice(0, 8).map(a => ({
            id: `ext_${a.id}`,
            company: a.company,
            role: a.title,
            appliedDate: a.appliedAt,
            status: a.status,
            source: a.source,
            applyUrl: a.applyUrl,
            isExternal: true,
          })),
          ...internalRecent.map(a => ({
            id: a.id,
            company: a.jobListing?.company || '—',
            role: a.jobListing?.title || '—',
            appliedDate: a.appliedDate,
            status: a.status,
            source: a.jobListing?.source || '—',
            applyUrl: a.jobListing?.sourceUrl || null,
            isExternal: false,
          })),
        ]
          .sort((a, b) => new Date(b.appliedDate) - new Date(a.appliedDate))
          .slice(0, 8);

        setRecentApps(normalised);
      } catch (err) {
        console.error('Dashboard load failed:', err);
        setError('Could not connect to backend. Make sure the API is running.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <LoadingSpinner />;

  return (
    <div className="space-y-8">
      {/* Welcome Section */}
      <div className="bg-gradient-to-r from-blue-600 to-blue-800 text-white rounded-xl p-8 shadow-lg">
        <h1 className="text-3xl font-bold mb-2">Welcome back, Sonaly! 👋</h1>
        <p className="text-blue-100">
          Your real-time job application dashboard — all data live from the backend.
        </p>
      </div>

      {/* Backend Offline Notice */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl px-5 py-4 flex items-start gap-3">
          <AlertCircle size={18} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold text-sm">Backend not reachable</p>
            <p className="text-sm mt-0.5">{error}</p>
          </div>
        </div>
      )}

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-6">
          <StatsCard title="Total Applied" value={stats.totalApplied ?? 0} icon={TrendingUp} color="blue" />
          <StatsCard title="Saved Jobs" value={stats.totalSaved ?? 0} icon={Bookmark} color="purple" />
          <StatsCard title="Interviews" value={stats.totalInterviews ?? 0} icon={Award} color="yellow" />
          <StatsCard title="Offers" value={stats.totalOffers ?? 0} icon={Briefcase} color="green" />
          <StatsCard title="Rejected" value={stats.totalRejected ?? 0} icon={Zap} color="red" />
        </div>
      )}

      {/* Recent Applications Table */}
      <div className="bg-white rounded-xl shadow-md p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-800 flex items-center gap-2">
            <LayoutDashboard className="text-blue-600" />
            Recent Applications
            {recentApps.length > 0 && (
              <span className="text-sm font-normal text-gray-500 ml-1">({recentApps.length} tracked)</span>
            )}
          </h2>
          <button
            onClick={() => navigate('/applied')}
            className="text-blue-600 hover:text-blue-800 font-medium text-sm"
          >
            View All →
          </button>
        </div>

        {recentApps.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b-2 border-gray-200">
                  <th className="text-left py-3 px-4 font-semibold text-gray-700">Company</th>
                  <th className="text-left py-3 px-4 font-semibold text-gray-700">Role</th>
                  <th className="text-left py-3 px-4 font-semibold text-gray-700">Source</th>
                  <th className="text-left py-3 px-4 font-semibold text-gray-700">Applied</th>
                  <th className="text-left py-3 px-4 font-semibold text-gray-700">Status</th>
                </tr>
              </thead>
              <tbody>
                {recentApps.map((app) => (
                  <tr key={app.id} className="border-b border-gray-100 hover:bg-gray-50">
                    <td className="py-3 px-4 font-medium text-gray-800">{app.company}</td>
                    <td className="py-3 px-4 text-gray-700 max-w-xs truncate">
                      {app.applyUrl ? (
                        <a
                          href={app.applyUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="hover:text-blue-600 hover:underline flex items-center gap-1"
                        >
                          {app.role}
                          <ExternalLink size={11} className="flex-shrink-0 text-gray-400" />
                        </a>
                      ) : app.role}
                    </td>
                    <td className="py-3 px-4 text-xs">
                      <span className="bg-slate-100 text-slate-600 px-2 py-0.5 rounded-full">
                        {app.source || '—'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-gray-600 text-sm">{formatDate(app.appliedDate)}</td>
                    <td className="py-3 px-4">
                      <span className={`text-xs font-semibold px-2.5 py-1 rounded-full ${statusColor(app.status)}`}>
                        {app.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-10 text-gray-500">
            <Clock size={40} className="mx-auto mb-3 text-gray-300" />
            <p className="font-medium">No applications tracked yet.</p>
            <p className="text-sm mt-1">
              Click <strong>Apply</strong> on any job in the{' '}
              <button onClick={() => navigate('/jobs')} className="text-blue-600 hover:underline">
                Job Search
              </button>{' '}
              or{' '}
              <button onClick={() => navigate('/ai-jobs')} className="text-blue-600 hover:underline">
                AI Search
              </button>{' '}
              page to start tracking.
            </p>
          </div>
        )}
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-xl shadow-md p-6">
        <h2 className="text-2xl font-bold text-gray-800 mb-6">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <button
            onClick={() => navigate('/ai-jobs')}
            className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-3 px-6 rounded-lg transition-colors flex items-center justify-center gap-2"
          >
            🤖 AI Job Search
          </button>
          <button
            onClick={() => navigate('/jobs')}
            className="bg-indigo-100 hover:bg-indigo-200 text-indigo-700 font-medium py-3 px-6 rounded-lg transition-colors"
          >
            🔍 Browse Live Jobs
          </button>
          <button
            onClick={() => navigate('/applied')}
            className="bg-blue-100 hover:bg-blue-200 text-blue-700 font-medium py-3 px-6 rounded-lg transition-colors"
          >
            📋 My Applications
          </button>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
