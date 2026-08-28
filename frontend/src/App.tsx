import { useEffect, useState } from "react";
import apiClient from "./services/apiClient";

function App() {
  const [status, setStatus] = useState("Đang kiểm tra kết nối Backend...");

  useEffect(() => {
    apiClient
      .get("/customers") // gọi 1 endpoint yêu cầu auth để test — kỳ vọng 401 nếu Backend chạy đúng
      .catch((err) => {
        if (err.response) {
          setStatus(
            `==> Kết nối Backend thành công (HTTP ${err.response.status} — đúng vì chưa đăng nhập)`,
          );
        } else {
          setStatus(
            "!!==> Không kết nối được Backend — kiểm tra lại VITE_API_BASE_URL và Backend có đang chạy không.",
          );
        }
      });
  }, []);

  return <div style={{ padding: 24 }}>{status}</div>;
}

export default App;
