import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import './index.css';
import App from './App';
import PublicLayout from './layouts/PublicLayout';
import StaffLayout from './layouts/StaffLayout';
import AdminLayout from './layouts/AdminLayout';
import CustomerLayout from './layouts/CustomerLayout';
import RoleGuard from './routes/RoleGuard';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route element={<App />}>
          {/* Public — không cần đăng nhập */}
          <Route element={<PublicLayout />}>
            <Route path="/" element={<div>Home page (Task 5.11)</div>} />
            <Route path="/services" element={<div>Services page</div>} />
            <Route path="/articles" element={<div>Articles page</div>} />
            <Route path="/track" element={<div>Track page (Task 5.10)</div>} />
            <Route path="/login" element={<div>Login page (Task 5.5)</div>} />
          </Route>

          {/* Staff — Receptionist + Technician */}
          <Route element={<RoleGuard allowedRoles={['Receptionist', 'Technician']} />}>
            <Route element={<StaffLayout />}>
              <Route path="/staff/dashboard" element={<div>Staff Dashboard</div>} />
            </Route>
          </Route>

          {/* Admin */}
          <Route element={<RoleGuard allowedRoles={['Admin']} />}>
            <Route element={<AdminLayout />}>
              <Route path="/admin/dashboard" element={<div>Admin Dashboard</div>} />
            </Route>
          </Route>

          {/* Customer */}
          <Route element={<RoleGuard allowedRoles={['Customer']} />}>
            <Route element={<CustomerLayout />}>
              <Route path="/customer/home" element={<div>Customer Home</div>} />
            </Route>
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  </StrictMode>
);
