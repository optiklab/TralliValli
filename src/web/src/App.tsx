import { BrowserRouter, Routes, Route, Navigate, useSearchParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { RegisterPage } from './components/RegisterPage';
import { LoginPage } from './components/LoginPage';
import { MagicLinkSent } from './components/MagicLinkSent';
import { VerifyMagicLink } from './components/VerifyMagicLink';
import { ChatLayout } from './components/ChatLayout';
import { useAuthStore } from '@stores/useAuthStore';
import { api } from '@services/index';
import './App.css';

// Wrapper component to handle magic link verification with URL params
function VerifyMagicLinkWrapper() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') || '';
  
  return (
    <VerifyMagicLink 
      token={token} 
      onSuccess={() => window.location.href = '/'}
      onError={(error) => console.error('Magic link verification failed:', error)}
    />
  );
}

// Main content with routing logic
function AppContent() {
  const { isAuthenticated } = useAuthStore();
  const [isBootstrapped, setIsBootstrapped] = useState<boolean | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const checkSystemStatus = async () => {
      try {
        const status = await api.getSystemStatus();
        setIsBootstrapped(status.isBootstrapped);
      } catch (error) {
        console.error('Failed to check system status:', error);
        // Assume bootstrapped if we can't check
        setIsBootstrapped(true);
      } finally {
        setIsLoading(false);
      }
    };
    checkSystemStatus();
  }, []);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
          <p className="mt-2 text-sm text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  // If authenticated, show chat interface
  if (isAuthenticated) {
    return (
      <Routes>
        <Route path="/" element={<ChatLayout />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    );
  }

  // Not authenticated
  return (
    <Routes>
      {/* Magic link verification */}
      <Route path="/auth/verify" element={<VerifyMagicLinkWrapper />} />
      <Route path="/magic-link-sent" element={<MagicLinkSent />} />
      
      {/* Registration - for first admin or with invite */}
      <Route path="/register" element={
        <RegisterPage 
          onSuccess={() => window.location.href = '/'}
        />
      } />
      
      {/* Login page */}
      <Route path="/login" element={
        <LoginPage 
          onMagicLinkSent={() => window.location.href = '/magic-link-sent'}
        />
      } />
      
      {/* Default route - show register for first admin, login for bootstrapped system */}
      <Route path="/" element={
        isBootstrapped 
          ? <Navigate to="/login" replace /> 
          : <Navigate to="/register" replace />
      } />
      
      {/* Catch all */}
      <Route path="*" element={
        isBootstrapped 
          ? <Navigate to="/login" replace /> 
          : <Navigate to="/register" replace />
      } />
    </Routes>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AppContent />
    </BrowserRouter>
  );
}

export default App;
