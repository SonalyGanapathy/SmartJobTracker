import { Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import ProtectedRoute from './components/Common/ProtectedRoute';
import Layout from './components/Layout/Layout';
import Login from './Pages/Login';
import Register from './Pages/Register';
import Dashboard from './Pages/Dashboard';
import AppliedJobs from './Pages/AppliedJobs';
import SavedJobs from './Pages/SavedJobs';
import Profile from './Pages/Profile';
import ResumeUpload from './Pages/ResumeUpload';
import AIJobSearch from './Pages/AIJobSearch';

function App() {
  return (
    <AuthProvider>
      <Routes>
        {/* Public routes */}
        <Route path="/login"    element={<Login />} />
        <Route path="/register" element={<Register />} />

        {/* Protected routes — wrapped in Layout */}
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <Layout>
                <Routes>
                  <Route path="/"          element={<Dashboard />} />
                  <Route path="/ai-search" element={<AIJobSearch />} />
                  <Route path="/applied"   element={<AppliedJobs />} />
                  <Route path="/saved"     element={<SavedJobs />} />
                  <Route path="/profile"   element={<Profile />} />
                  <Route path="/resume"    element={<ResumeUpload />} />
                  {/* Catch-all → dashboard */}
                  <Route path="*"          element={<Navigate to="/" replace />} />
                </Routes>
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </AuthProvider>
  );
}

export default App;
