import { useEffect, useState } from 'react';
import Sidebar from './Sidebar';
import { getProfile } from '../../services/api';
import LoadingSpinner from '../Common/LoadingSpinner';
import { Toaster } from 'react-hot-toast';
import { useAuth } from '../../contexts/AuthContext';
import { logout } from '../../services/authService';

const Layout = ({ children }) => {
  const { user } = useAuth();
  const [userProfile, setUserProfile] = useState(null);
  const [loading, setLoading]         = useState(true);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await getProfile();
        setUserProfile(response.data);
      } catch (error) {
        console.error('Failed to fetch profile:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, []);

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar userProfile={userProfile} />

      <div className="flex-1 ml-64 overflow-y-auto">
        {/* Top Bar */}
        <div className="bg-white shadow-sm sticky top-0 z-10">
          <div className="px-8 py-4 flex justify-between items-center">
            <h2 className="text-2xl font-bold text-gray-800">SmartJobTracker</h2>
            <div className="flex items-center gap-4">
              {(userProfile || user) && (
                <div className="text-right">
                  <p className="text-sm font-medium text-gray-800">
                    {userProfile?.fullName || user?.fullName}
                  </p>
                  <p className="text-xs text-gray-500">
                    {userProfile?.email || user?.email}
                  </p>
                </div>
              )}
              <button
                onClick={logout}
                style={{
                  padding: '6px 14px',
                  fontSize: '13px',
                  fontWeight: 600,
                  borderRadius: '8px',
                  border: '1.5px solid #e5e7eb',
                  background: '#fff',
                  color: '#374151',
                  cursor: 'pointer',
                  transition: 'background 0.15s',
                }}
                onMouseEnter={(e) => (e.target.style.background = '#f3f4f6')}
                onMouseLeave={(e) => (e.target.style.background = '#fff')}
              >
                Sign out
              </button>
            </div>
          </div>
        </div>

        {/* Main Content */}
        <main className="p-8">
          {loading ? <LoadingSpinner /> : children}
        </main>
      </div>

      <Toaster position="top-right" />
    </div>
  );
};

export default Layout;
