interface PlaceholderPageProps {
  title: string;
}

// Dùng tạm cho các trang chưa tới lượt code UI thật — giúp routing chạy được ngay,
// tránh phải chờ hoàn thiện tất cả trang mới test được luồng điều hướng.
export default function PlaceholderPage({ title }: PlaceholderPageProps) {
  return (
    <div style={{ padding: 24 }}>
      <h2>{title}</h2>
      <p>Trang đang được xây dựng.</p>
    </div>
  );
}
