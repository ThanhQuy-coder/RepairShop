import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import { ToastContainer } from './components/common';
import { connectNotificationHub, disconnectNotificationHub } from './services/signalrClient';

function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => {
    hydrate();
  }, [hydrate]);

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  useEffect(() => {
    if (isAuthenticated) connectNotificationHub();
    return () => disconnectNotificationHub();
  }, [isAuthenticated]);

  return (
    <>
      <Outlet />
      <ToastContainer />
    </>
  );
}

export default App;
