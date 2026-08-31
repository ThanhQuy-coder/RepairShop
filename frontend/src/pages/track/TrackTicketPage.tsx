import { useParams } from 'react-router-dom';

export default function TrackTicketPage() {
  const { ticketCode } = useParams();
  return (
    <div style={{ padding: 24 }}>
      <h2>Tra cứu phiếu: {ticketCode ?? '(chưa nhập mã)'}</h2>
    </div>
  );
}
