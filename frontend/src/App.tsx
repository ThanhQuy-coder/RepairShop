import { useEffect } from 'react';
import { useAuthStore } from './store/authStore';
import { Outlet } from 'react-router-dom';

function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => {
    hydrate();
  }, [hydrate]);

  return <Outlet />;
}

export default App;
