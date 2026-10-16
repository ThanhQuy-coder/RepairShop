import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, ErrorMessage, Input } from '../../components/common';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';

const storageKey = 'appointment-lookup-token';

export default function AppointmentLookupPage() {
  const [token, setToken] = useState('');
  const [phone, setPhone] = useState('');
  const [result, setResult] = useState<AppointmentResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    try {
      const stored = JSON.parse(localStorage.getItem(storageKey) || 'null') as { token?: string; expiresAt?: number } | null;
      if (stored?.token && stored.expiresAt && stored.expiresAt > Date.now()) setToken(stored.token);
      else localStorage.removeItem(storageKey);
    } catch { localStorage.removeItem(storageKey); }
  }, []);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      setResult(await appointmentService.lookup(token, phone));
    } catch { setError('Không tìm thấy lịch hẹn với thông tin đã cung cấp.'); }
  };

  return <main><h1>Tra cứu lịch hẹn</h1><form onSubmit={submit}>
    {error && <ErrorMessage message={error} />}
    <Input label="Mã tra cứu" value={token} onChange={(event) => setToken(event.target.value)} required />
    <Input label="Số điện thoại" type="tel" value={phone} onChange={(event) => setPhone(event.target.value)} required />
    <Button type="submit">Tra cứu</Button>
  </form>
  {result && <section><h2>Lịch hẹn {result.id}</h2><p>Ngày: {result.appointmentDate}</p><p>Trạng thái: {result.status}</p></section>}
  </main>;
}
