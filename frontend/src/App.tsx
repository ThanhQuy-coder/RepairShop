import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import { ToastContainer } from './components/common';
import { notificationSignalRService } from './services/notificationSignalRService';

function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => {
    hydrate();
  }, [hydrate]);

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  useEffect(() => {
    if (isAuthenticated) notificationSignalRService.connect();
    return () => notificationSignalRService.disconnect();
  }, [isAuthenticated]);

  return (
    <>
      <Outlet />
      <ToastContainer />
    </>
  );
}

export default App;
