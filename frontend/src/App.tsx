import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import { ToastContainer } from './components/common';

function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => {
    hydrate();
  }, [hydrate]);

  return (
    <>
      <Outlet />
      <ToastContainer />
    </>
  );
}

export default App;
