import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Send,
  Bookmark,
  User,
  FileText,
  Briefcase,
  Bot
} from 'lucide-react';

const Sidebar = ({ userProfile }) => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/ai-search', label: 'AI Job Search', icon: Bot, badge: 'LIVE' },
    { path: '/applied', label: 'Applied Jobs', icon: Send },
    { path: '/saved', label: 'Saved Jobs', icon: Bookmark },
    { path: '/profile', label: 'My Profile', icon: User },
    { path: '/resume', label: 'Upload Resume', icon: FileText }
  ];

  return (
    <div className="w-64 bg-slate-900 text-white h-screen flex flex-col fixed left-0 top-0 shadow-lg">
      {/* Logo */}
      <div className="p-6 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <div className="bg-blue-600 p-2 rounded-lg">
            <Briefcase size={24} />
          </div>
          <div>
            <h1 className="text-xl font-bold">SmartJobTracker</h1>
            <p className="text-xs text-gray-400">AI-powered job tracker</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-6 px-4">
        <ul className="space-y-2">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;
            return (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={`flex items-center gap-3 px-4 py-3 rounded-lg font-medium transition-all ${
                    isActive
                      ? 'bg-blue-600 text-white shadow-lg'
                      : 'text-gray-300 hover:bg-slate-800 hover:text-white'
                  }`}
                >
                  <Icon size={20} />
                  <span className="flex-1">{item.label}</span>
                  {item.badge && (
                    <span className="text-xs bg-yellow-400 text-yellow-900 px-1.5 py-0.5 rounded font-bold">
                      {item.badge}
                    </span>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* User Info */}
      {userProfile && (
        <div className="p-6 border-t border-slate-800">
          <div className="bg-slate-800 rounded-lg p-4">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center font-bold">
                {userProfile.fullName.charAt(0)}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{userProfile.fullName}</p>
                <p className="text-xs text-gray-400 truncate">{userProfile.email}</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Sidebar;
