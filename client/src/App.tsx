import type { ReactElement } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './auth/AuthContext';
import AppShell from './app/AppShell';
import LoginPage from './pages/LoginPage';
import HomePage from './pages/HomePage';
import CatalogPage from './pages/CatalogPage';
import ProductDetailPage from './pages/ProductDetailPage';
import RfqsPage from './pages/RfqsPage';
import RfqCreatePage from './pages/RfqCreatePage';
import RfqDetailPage from './pages/RfqDetailPage';
import SamplesPage from './pages/SamplesPage';
import ProfilePage from './pages/ProfilePage';

function RequireAuth({ children }: { children: ReactElement }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <RequireAuth>
                <AppShell />
              </RequireAuth>
            }
          >
            <Route path="/" element={<HomePage />} />
            <Route path="/catalog" element={<CatalogPage />} />
            <Route path="/catalog/:productId" element={<ProductDetailPage />} />
            <Route path="/rfqs" element={<RfqsPage />} />
            <Route path="/rfqs/new" element={<RfqCreatePage />} />
            <Route path="/rfqs/:rfqId" element={<RfqDetailPage />} />
            <Route path="/samples" element={<SamplesPage />} />
            <Route path="/profile" element={<ProfilePage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
