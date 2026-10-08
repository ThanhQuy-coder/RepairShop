import { useEffect, useState } from 'react';
import { appointmentService, type AppointmentResponse } from '../../services/appointmentService';
import { Button, ErrorMessage } from '../../components/common';
import { extractApiError } from '../../utils/apiError';

export default function MyAppointmentsPage() {
  const [appointments, setAppointments] = useState<AppointmentResponse[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    try {
      setAppointments(await appointmentService.getMine());
      setError(null);
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const cancel = async (id: string) => {
    try {
      await appointmentService.cancel(id);
      await load();
    } catch (err) {
      setError(extractApiError(err).message);
    }
  };

  return (
    <section style={{ maxWidth: 900, margin: '0 auto', padding: 24 }}>
      <h1>Lịch hẹn của tôi</h1>
      {error && <ErrorMessage message={error} onRetry={() => void load()} />}
      {appointments.length === 0 && !error && <p>Chưa có lịch hẹn.</p>}
      {appointments.map((appointment) => (
        <article key={appointment.id} style={{ border: '1px solid #e5e7eb', borderRadius: 12, padding: 16, marginBottom: 12 }}>
          <strong>{appointment.appointmentDate}</strong>
          <p>Số điện thoại: {appointment.phone}</p>
          <p>Trạng thái: {appointment.status}</p>
          {(appointment.status === 'Pending' || appointment.status === 'Confirmed') && (
            <Button variant="ghost" size="sm" onClick={() => void cancel(appointment.id)}>
              Hủy lịch hẹn
            </Button>
          )}
        </article>
      ))}
    </section>
  );
}
