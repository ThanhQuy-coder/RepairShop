"""Hybrid Intent Classifier: Rule-first + Supervised ML fallback (6 Intents).

Architecture:
1. Text normalization (Unicode NFC, lowercase, whitespace normalization).
2. Explicit out-of-scope & generic-word guards.
3. Word-boundary Rule-based scoring across DIAGNOSIS, QUOTATION, APPOINTMENT, WARRANTY, TRACKING.
4. Clear multi-intent priority & secondary intent extraction.
5. Supervised ML fallback (TF-IDF char_wb + LogisticRegression) when rule signal is weak/ambiguous.
6. Threshold gate (MIN_ML_CONFIDENCE = 0.55) returning UNKNOWN on insufficient evidence.
"""

from __future__ import annotations

import logging
import re
import unicodedata
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from app.schemas.agent_models import Intent

logger = logging.getLogger(__name__)

MODEL_PATH = Path(__file__).resolve().parent / "models" / "intent_clf.joblib"

MIN_RULE_CONFIDENCE = 0.85
MIN_ML_CONFIDENCE = 0.55

_BOUNDARY_CHARS = "0-9a-zA-Z\u00C0-\u1EF9\u0110\u0111"
_SPACE_RE = re.compile(r"\s+")

_WARRANTY_CODE_RE = re.compile(
    r"\bwr-[a-z0-9]{2,}(?:-[a-z0-9]{1,}){0,3}\b",
    re.IGNORECASE,
)
_TRACKING_CODE_RE = re.compile(
    r"\b(?:rs|rt|tk)-[a-z0-9]{2,}(?:-[a-z0-9]{1,}){0,3}\b",
    re.IGNORECASE,
)
_DATETIME_HINT_RE = re.compile(
    r"(?<![0-9a-zA-Z\u00C0-\u1EF9])("
    r"\d{1,2}h(?:\d{2})?|\d{1,2}:\d{2}|"
    r"ng[aà]y\s+\d{1,2}/\d{1,2}|h[oô]m\s+nay|ng[aà]y\s+mai|"
    r"chi[eề]u\s+mai|s[aá]ng\s+mai|s[aá]ng\s+nay|chi[eề]u\s+nay|"
    r"th[uứ]\s+[2-7]|ch[uủ]\s+nh[aậ]t|cu[oố]i\s+tu[aầ]n"
    r")(?![0-9a-zA-Z\u00C0-\u1EF9])",
    re.IGNORECASE,
)
_APPOINTMENT_ACTION_RE = re.compile(
    r"(?<![0-9a-zA-Z\u00C0-\u1EF9])("
    r"qua|gh[eé]|mang|đem|dem|l[iị]ch|h[eẹ]n|slot|tr[oố]ng|book|đ[aặ]t|dat|ch[oố]t|gi[uữ]|giu"
    r")(?![0-9a-zA-Z\u00C0-\u1EF9])",
    re.IGNORECASE,
)

# Out-of-scope patterns: only trigger UNKNOWN rule guard when no real shop repair context exists.
_OUT_OF_SCOPE_PHRASES: tuple[str, ...] = (
    "giá vàng", "gia vang",
    "giá xăng", "gia xang",
    "giá đô", "gia do", "tỷ giá", "ty gia",
    "giá bitcoin", "gia bitcoin", "tiền ảo", "tien ao", "crypto",
    "chứng khoán", "chung khoan", "cổ phiếu", "co phieu",
    "bất động sản", "bat dong san", "giá nhà đất", "gia nha dat",
    "dự báo thời tiết", "du bao thoi tiet", "thời tiết hôm nay", "thoi tiet hom nay",
    "bóng đá", "bong da", "tỉ số bóng đá", "kết quả xổ số", "ket qua xo so", "xổ số", "xo so",
    "sửa ống nước", "sua ong nuoc", "thợ điện nước", "tho dien nuoc",
    "sửa xe máy", "sua xe may", "sửa ô tô", "sua o to", "thay nhớt", "thay nhot", "xe máy", "xe may",
    "tủ lạnh", "tu lanh", "máy giặt", "may giat", "điều hòa", "dieu hoa", "máy lạnh", "may lanh",
    "nồi cơm điện", "noi com dien", "bếp từ", "bep tu", "lò vi sóng", "lo vi song",
    "nấu ăn", "nau an", "công thức nấu", "cong thuc nau", "món ăn", "mon an", "ship đồ ăn", "trà sữa",
    "làm thơ", "lam tho", "kể chuyện", "ke chuyen", "viết code", "viet code", "lập trình", "lap trinh",
    "dịch tiếng anh", "dich tieng anh", "bài tập toán", "bai tap toan", "giải phương trình",
    "đặt vé máy bay", "dat ve may bay", "đặt phòng khách sạn", "dat phong khach san",
    "pin con thỏ", "pin con tho", "pin tiểu aa", "pin tiểu aaa",
    "mua trả góp", "mua tra gop", "thu cũ đổi mới", "thu cu doi moi",
    "tuyển dụng", "tuyen dung", "xin việc", "xin viec",
)

_SUPPORTED_REPAIR_DEVICE_PHRASES: tuple[str, ...] = (
    "điện thoại", "dien thoai", "smartphone", "iphone", "samsung", "galaxy", "xiaomi", "redmi",
    "oppo", "vivo", "realme", "nokia", "sony", "pixel", "oneplus",
    "laptop", "macbook", "mac book", "dell", "hp", "asus", "rog", "lenovo", "thinkpad", "legion", "acer", "msi",
    "tablet", "máy tính bảng", "may tinh bang", "ipad", "tab s",
    "smartwatch", "đồng hồ thông minh", "dong ho thong minh", "apple watch", "garmin", "fenix", "forerunner",
    "huawei watch", "marshall", "stanmore", "emberton", "nintendo", "switch", "playstation", "ps5", "dualsense",
    "ultragear", "xboom", "lg", "loa bluetooth", "tai nghe", "airpods", "máy ảnh", "may anh", "camera hành trình",
    "màn hình", "man hinh", "bàn phím", "ban phim", "mainboard", "bo mạch", "chân sạc", "chan sac",
    "cổng sạc", "cong sac", "cục pin điện thoại",
)


def normalize_text(text: str) -> str:
    """Unicode NFC + lowercase + collapse whitespace (keeps Vietnamese diacritics)."""
    if not text:
        return ""
    nfc = unicodedata.normalize("NFC", text)
    return _SPACE_RE.sub(" ", nfc.strip().lower())


def _compile_phrase_table(
    weighted_phrases: list[tuple[str, int]],
) -> list[tuple[re.Pattern[str], str, int]]:
    compiled: list[tuple[re.Pattern[str], str, int]] = []
    for phrase, weight in weighted_phrases:
        norm = normalize_text(phrase)
        pattern = re.compile(
            rf"(?<![{_BOUNDARY_CHARS}]){re.escape(norm)}(?![{_BOUNDARY_CHARS}])"
        )
        compiled.append((pattern, norm, weight))
    return compiled


_DIAGNOSIS_RULES = _compile_phrase_table([
    # High specificity symptoms & diagnostic phrases (+3)
    ("triệu chứng", 3), ("trieu chung", 3),
    ("chẩn đoán", 3), ("chan doan", 3), ("chuẩn đoán", 3), ("chuan doan", 3),
    ("bị gì", 3), ("bi gi", 3), ("bị sao", 3), ("bi sao", 3),
    ("lỗi gì", 3), ("loi gi", 3), ("hỏng gì", 3), ("hong gi", 3), ("hư gì", 3), ("hu gi", 3),
    ("nguyên nhân", 3), ("nguyen nhan", 3),
    ("bị làm sao", 3), ("bi lam sao", 3), ("vì sao", 3),
    ("sọc màn hình", 3), ("soc man hinh", 3), ("sọc xanh", 3), ("soc xanh", 3),
    ("vỡ màn hình", 3), ("vo man hinh", 3), ("bể màn hình", 3), ("be man hinh", 3),
    ("vỡ kính", 3), ("vo kinh", 3), ("nứt kính", 3), ("nut kinh", 3),
    ("liệt cảm ứng", 3), ("liet cam ung", 3), ("loạn cảm ứng", 3), ("loan cam ung", 3),
    ("chảy mực", 3), ("chay muc", 3), ("đốm đen màn", 3), ("dom den man", 3), ("màn hình đen", 3), ("man hinh den", 3),
    ("không lên nguồn", 3), ("khong len nguon", 3), ("ko len nguon", 3), ("k lên nguồn", 3), ("k len nguon", 3),
    ("sập nguồn", 3), ("sap nguon", 3), ("sụp nguồn", 3), ("sup nguon", 3),
    ("mất nguồn", 3), ("mat nguon", 3), ("chết nguồn", 3), ("chet nguon", 3),
    ("tự tắt nguồn", 3), ("tu tat nguon", 3), ("tự khởi động lại", 3), ("tu khoi dong lai", 3),
    ("treo logo", 3), ("treo táo", 3), ("treo tao", 3), ("bootloop", 3),
    ("không nhận sạc", 3), ("khong nhan sac", 3), ("ko nhan sac", 3),
    ("sạc không vào", 3), ("sac khong vao", 3), ("sạc ko vào", 3), ("sac ko vao", 3), ("k vào pin", 3),
    ("chai pin", 3), ("pin chai", 3), ("tụt pin", 3), ("tut pin", 3), ("hao pin", 3),
    ("phồng pin", 3), ("phong pin", 3), ("sưng pin", 3), ("bung nắp lưng", 3),
    ("pin tụt nhanh", 3), ("pin tut nhanh", 3), ("pin nhanh hết", 3),
    ("nóng máy", 3), ("nong may", 3), ("máy nóng", 3), ("may nong", 3),
    ("nóng khi sạc", 3), ("nong khi sac", 3), ("quá nhiệt", 3), ("qua nhiet", 3), ("nóng ran", 3),
    ("rơi xuống nước", 3), ("roi xuong nuoc", 3), ("rơi nước", 3), ("roi nuoc", 3), ("vào nước", 3), ("vao nuoc", 3),
    ("dính nước", 3), ("dinh nuoc", 3), ("vô nước", 3), ("vo nuoc", 3),
    ("rè loa", 3), ("re loa", 3), ("loa rè", 3), ("loa re", 3), ("mất tiếng", 3), ("mat tieng", 3), ("mất âm thanh", 3), ("mat am thanh", 3),
    ("mất wifi", 3), ("mat wifi", 3), ("không bắt được wifi", 3), ("khong bat duoc wifi", 3),
    ("mất sóng", 3), ("mat song", 3), ("không nhận sim", 3), ("khong nhan sim", 3),
    ("mất face id", 3), ("mat face id", 3), ("hỏng face id", 3), ("hong face id", 3),
    ("camera mờ", 3), ("camera mo", 3), ("mờ camera", 3), ("camera bị đen", 3), ("rung camera", 3),
    ("liệt phím", 3), ("liet phim", 3), ("kẹt phím", 3), ("ket phim", 3), ("lỗi bàn phím", 3), ("loi ban phim", 3),
    ("gãy bản lề", 3), ("gay ban le", 3), ("bung bản lề", 3), ("bung ban le", 3), ("kẹt bản lề", 3),
    ("màn hình xanh", 3), ("man hinh xanh", 3), ("blue screen", 3), ("bsod", 3),
    ("error code", 3), ("khi mở ứng dụng", 3), ("khi mo ung dung", 3),
    ("rạn chân chim", 3), ("ran chan chim", 3), ("nứt chân chim", 3), ("rạn kính", 3),
    ("quạt kêu to", 3), ("quat keu to", 3), ("kêu rè rè", 3), ("keu re re", 3), ("kêu cành cạch", 3),
    ("lỗi phần mềm", 3), ("loi phan mem", 3),
    ("sạc cả tiếng", 3), ("sac ca tieng", 3), ("không nhận vân tay", 3), ("khong nhan van tay", 3),
    ("lỏng chân sạc", 3), ("long chan sac", 3), ("gãy chân sạc", 3), ("gay chan sac", 3),
    ("sạc chập chờn", 3), ("sac chap chon", 3),
    ("mất bluetooth", 3), ("mat bluetooth", 3), ("không đo được nhịp tim", 3), ("hỏng nút vặn", 3),
    ("sửa ic", 2), ("ic nguồn", 2), ("ic nguon", 2), ("vệ sinh hay thay", 2),

    # Medium diagnostic phrases (+2)
    ("hiện lỗi", 2), ("hien loi", 2), ("báo lỗi", 2), ("bao loi", 2), ("bị lỗi", 2), ("bi loi", 2),
    ("bị đơ", 2), ("bi do", 2), ("treo máy", 2), ("treo may", 2), ("giật lag", 2), ("giat lag", 2),
    ("bị hỏng", 2), ("bi hong", 2), ("bị hư", 2), ("bi hu", 2), ("bị sọc", 2), ("bi soc", 2),
    ("bị mờ", 2), ("bi mo", 2), ("bị rè", 2), ("bi re", 2), ("bị nóng", 2), ("bi nong", 2),
    ("tụt nhanh", 2), ("tut nhanh", 2), ("nhanh hết pin", 2), ("nhanh het pin", 2),
    ("không lên hình", 2), ("khong len hinh", 2), ("ko lên màn", 2), ("không lên màn", 2),
    ("không nhận tai nghe", 2), ("không nhận micro", 2), ("hỏng mic", 2), ("hong mic", 2),
    ("cứu được không", 2), ("cuu duoc khong", 2), ("sửa được không", 2), ("sua duoc khong", 2),
    ("chập chờn", 2), ("chap chon", 2), ("lỗi main", 2), ("loi main", 2), ("chết main", 2), ("chet main", 2),
    ("kiểm tra lỗi", 2), ("kiem tra loi", 2), ("tư vấn lỗi", 2), ("tu van loi", 2),
    ("tại sao", 1), ("tai sao", 1), ("khắc phục", 2), ("khac phuc", 2),
    ("nháy màn hình", 2), ("nhay man hinh", 2), ("chớp màn hình", 2), ("ám vàng", 2), ("ám xanh", 2),

    # Weak hints (+1) — NEVER include standalone "pin", "sửa", "máy"
    ("màn hình", 1), ("man hinh", 1), ("cảm ứng", 1), ("cam ung", 1),
    ("mở không lên", 2), ("bật không lên", 2), ("bat khong len", 2), ("ko lên", 1),
])

_QUOTATION_RULES = _compile_phrase_table([
    ("báo giá", 3), ("bao gia", 3), ("bảng giá", 3), ("bang gia", 3),
    ("giá bao nhiêu", 3), ("gia bao nhieu", 3),
    ("bao nhiêu tiền", 3), ("bao nhieu tien", 3), ("bao nhiu tiền", 3), ("bao nhiu tien", 3),
    ("hết bao nhiêu", 3), ("het bao nhieu", 3), ("tốn bao nhiêu", 3), ("ton bao nhieu", 3),
    ("khoảng bao nhiêu", 3), ("khoang bao nhieu", 3), ("tầm bao nhiêu", 3), ("tam bao nhieu", 3),
    ("chi phí", 3), ("chi phi", 3),
    ("giá thay", 3), ("gia thay", 3), ("giá sửa", 3), ("gia sua", 3), ("giá vệ sinh", 3), ("gia ve sinh", 3),
    ("phí thay", 3), ("phi thay", 3), ("phí sửa", 3), ("phi sua", 3), ("phí kiểm tra", 3), ("phi kiem tra", 3),
    ("tiền công", 3), ("tien cong", 3), ("công thay", 3), ("cong thay", 3),
    ("giá linh kiện", 3), ("gia linh kien", 3), ("giá dịch vụ", 3), ("gia dich vu", 3),
    ("giá ép kính", 3), ("gia ep kinh", 3), ("phí ép kính", 3), ("phi ep kinh", 3), ("ép kính bao nhiêu", 3),
    ("giá sao", 3), ("gia sao", 3), ("hết tầm", 3), ("het tam", 3), ("mấy trăm", 3), ("may tram", 3),
    ("bnh xeng", 3), ("bnh xiền", 3), ("phí ktra", 3), ("phi ktra", 3),
    ("mất tiền ko", 2), ("mat tien ko", 2), ("mất tiền không", 2), ("giá có cao không", 3),
    ("ước tính giá", 3), ("uoc tinh gia", 3), ("dự toán chi phí", 3), ("du toan chi phi", 3),
    ("bnhiu tiền", 3), ("bn tiền", 3), ("bn tien", 3),

    ("nhiêu tiền", 2), ("nhieu tien", 2), ("bao nhiu", 2), ("bnhiu", 2),
    ("đắt không", 2), ("dat khong", 2), ("mắc không", 2), ("mac khong", 2),
    ("rổ giá", 2), ("ro gia", 2), ("mức giá", 2), ("muc gia", 2), ("giá cả", 2), ("gia ca", 2),
    ("tính phí", 2), ("tinh phi", 2), ("báo phí", 2), ("bao phi", 2),
    ("giá tham khảo", 2), ("gia tham khao", 2), ("có tốn phí", 2), ("co ton phi", 2),
    ("tốn kém", 2), ("ton kem", 2), ("giá pin", 2), ("gia pin", 2), ("giá màn", 2), ("gia man", 2),
    ("tiền sửa", 2), ("tien sua", 2), ("tiền thay", 2), ("tien thay", 2), ("bg", 2),
])

_APPOINTMENT_RULES = _compile_phrase_table([
    ("đặt lịch", 3), ("dat lich", 3), ("hẹn lịch", 3), ("hen lich", 3), ("lịch hẹn", 3), ("lich hen", 3),
    ("book lịch", 3), ("book lich", 3), ("đặt hẹn", 3), ("dat hen", 3),
    ("giữ chỗ", 3), ("giu cho", 3), ("đặt chỗ", 3), ("dat cho", 3),
    ("đăng ký sửa", 3), ("dang ky sua", 3), ("đăng ký lịch", 3), ("dang ky lich", 3),
    ("hẹn mang máy", 3), ("hen mang may", 3), ("đổi lịch", 3), ("doi lich", 3), ("dời lịch", 3), ("hủy lịch", 3),
    ("khung giờ", 3), ("khung gio", 3), ("còn slot", 3), ("con slot", 3), ("slot trống", 3), ("slot trong", 3),
    ("lịch trống", 3), ("lich trong", 3), ("còn trống lịch", 3), ("con trong lich", 3),
    ("lên lịch", 3), ("len lich", 3), ("xếp lịch", 3), ("xep lich", 3), ("chốt lịch", 3), ("chot lich", 3),

    ("mang máy qua", 2), ("mang may qua", 2), ("đem máy qua", 2), ("dem may qua", 2),
    ("ghé tiệm", 2), ("ghe tiem", 2), ("ghé cửa hàng", 2), ("ghe cua hang", 2),
    ("qua cửa hàng", 2), ("qua cua hang", 2), ("ghé qua", 2), ("ghe qua", 2),
    ("hẹn giờ", 2), ("hen gio", 2), ("đặt trước", 2), ("dat truoc", 2),
    ("giờ mở cửa", 2), ("gio mo cua", 2), ("giờ làm việc", 2), ("gio lam viec", 2),
    ("nhận máy ngoài giờ", 2), ("làm việc cuối tuần", 2), ("tiếp nhận sửa", 2),
    ("đóng cửa", 2), ("dong cua", 2), ("mấy giờ đóng cửa", 3), ("nghỉ nhận máy", 2),
    ("qua sửa", 2), ("qua sua", 2), ("tới tiệm", 2), ("toi tiem", 2),
])

_WARRANTY_RULES = _compile_phrase_table([
    ("bảo hành", 3), ("bao hanh", 3),
    ("chính sách bảo hành", 4), ("chinh sach bao hanh", 4),
    ("phiếu bảo hành", 4), ("phieu bao hanh", 4),
    ("kiểm tra bảo hành", 4), ("kiem tra bao hanh", 4),
    ("tra cứu bảo hành", 4), ("tra cuu bao hanh", 4),
    ("thời hạn bảo hành", 4), ("thoi han bao hanh", 4),
    ("còn bảo hành", 4), ("con bao hanh", 4),
    ("hết bảo hành", 4), ("het bao hanh", 4),
    ("điều kiện bảo hành", 4), ("dieu kien bao hanh", 4),
    ("từ chối bảo hành", 4), ("tu choi bao hanh", 4),
    ("gói bảo hành", 4), ("goi bao hanh", 4),
    ("bảo hành bao lâu", 4), ("bao hanh bao lau", 4),
    ("yêu cầu bảo hành", 4), ("yeu cau bao hanh", 4),
    ("sửa lại miễn phí", 3), ("sua lai mien phi", 3),
    ("tái phát lỗi cũ", 3), ("tai phat loi cu", 3), ("lỗi tái phát", 3), ("loi tai phat", 3),
    ("hạn bảo hành", 3), ("han bao hanh", 3), ("thẻ bảo hành", 3), ("the bao hanh", 3),
    ("mã bảo hành", 4), ("ma bao hanh", 4),

    ("còn bh", 3), ("con bh", 3), ("hết bh", 3), ("het bh", 3),
    ("check bh", 3), ("tra bh", 3), ("phiếu bh", 3), ("phieu bh", 3),
    ("chế độ bh", 3), ("che do bh", 3), ("hạn bh", 3), ("han bh", 3),
    ("bh vàng", 3), ("bh bạc", 3), ("gói bh", 3), ("thời gian bh", 3),
    ("được bh", 3), ("duoc bh", 3), ("mất tem bh", 3), ("rách tem", 3), ("tem bảo hành", 3),
    ("hư lại", 2), ("hu lai", 2), ("hỏng lại", 2), ("hong lai", 2), ("bị lại", 2), ("bi lai", 2),
    ("sửa lại", 2), ("sua lai", 2), ("bh", 2), ("sau sửa chữa", 2), ("kiểm tra lại", 2),
    ("miễn phí", 1), ("mien phi", 1),
])

_TRACKING_RULES = _compile_phrase_table([
    ("tiến độ sửa", 4), ("tien do sua", 4),
    ("tiến độ đơn", 4), ("tien do don", 4),
    ("tiến độ", 3), ("tien do", 3),
    ("trạng thái đơn", 4), ("trang thai don", 4),
    ("trạng thái phiếu", 4), ("trang thai phieu", 4),
    ("tình trạng đơn", 4), ("tinh trang don", 4),
    ("tình trạng phiếu", 4), ("tinh trang phieu", 4),
    ("mã phiếu", 3), ("ma phieu", 3), ("mã đơn", 3), ("ma don", 3),
    ("mã tra cứu", 4), ("ma tra cuu", 4), ("mã ticket", 4), ("ma ticket", 4),
    ("sửa xong chưa", 4), ("sua xong chua", 4), ("đã xong chưa", 3), ("da xong chua", 3),
    ("xong chưa", 3), ("xong chua", 3),
    ("lấy máy được chưa", 4), ("lay may duoc chua", 4),
    ("khi nào lấy máy", 3), ("khi nao lay may", 3),
    ("bao giờ lấy máy", 3), ("bao gio lay may", 3), ("bao giờ xong", 3), ("bao gio xong", 3),
    ("kiểm tra tiến độ", 4), ("kiem tra tien do", 4),
    ("kiểm tra đơn", 4), ("kiem tra don", 4), ("kiểm tra phiếu", 4), ("kiem tra phieu", 4),
    ("tra cứu đơn", 4), ("tra cuu don", 4), ("tra cứu phiếu", 4), ("tra cuu phieu", 4),
    ("theo dõi đơn", 4), ("theo doi don", 4), ("theo dõi phiếu", 4),
    ("check đơn", 3), ("check don", 3), ("check phiếu", 3), ("check phieu", 3),
    ("đang ở bước nào", 3), ("dang o buoc nao", 3),
    ("đơn sửa", 3), ("don sua", 3), ("phiếu sửa", 3), ("phieu sua", 3),

    ("tra cứu", 2), ("tra cuu", 2), ("trạng thái", 2), ("trang thai", 2),
    ("chờ linh kiện", 2), ("cho linh kien", 2), ("đã sửa tới đâu", 3), ("tới đâu rồi", 2),
    ("kiểm thử cuối", 2), ("chưa thấy alo", 2), ("đem sửa", 2), ("dem sua", 2),
    ("gửi sửa hôm", 3), ("gui sua hom", 3), ("nhận máy chưa", 2), ("đã nhận lại máy chưa", 3),
])

_OUT_OF_SCOPE_RULES = _compile_phrase_table([(p, 1) for p in _OUT_OF_SCOPE_PHRASES])
_DEVICE_CONTEXT_RULES = _compile_phrase_table([(p, 1) for p in _SUPPORTED_REPAIR_DEVICE_PHRASES])


@dataclass
class IntentSignals:
    intent: Intent
    confidence: float
    scores: dict[str, int] = field(default_factory=dict)
    matched_terms: list[str] = field(default_factory=list)
    secondary: list[Intent] = field(default_factory=list)
    source: str = "rule"


def _score_table(
    normalized: str,
    rules: list[tuple[re.Pattern[str], str, int]],
) -> tuple[int, list[str]]:
    score = 0
    matched: list[str] = []
    for pattern, term, weight in rules:
        if pattern.search(normalized):
            score += weight
            matched.append(term)
    return score, matched


def _collect_secondary(
    primary: Intent,
    scores: dict[Intent, int],
) -> list[Intent]:
    order = (
        Intent.WARRANTY,
        Intent.TRACKING,
        Intent.DIAGNOSIS,
        Intent.QUOTATION,
        Intent.APPOINTMENT,
    )
    return [
        candidate
        for candidate in order
        if candidate != primary and scores.get(candidate, 0) >= 2
    ]


def classify_by_rules(message: str) -> IntentSignals:
    """Classify an input message strictly using word-boundary rules and regexes."""
    normalized = normalize_text(message)
    if not normalized:
        return IntentSignals(
            intent=Intent.UNKNOWN,
            confidence=0.95,
            scores={},
            matched_terms=[],
            secondary=[],
            source="rule",
        )

    diag_score, diag_terms = _score_table(normalized, _DIAGNOSIS_RULES)
    quote_score, quote_terms = _score_table(normalized, _QUOTATION_RULES)
    appt_score, appt_terms = _score_table(normalized, _APPOINTMENT_RULES)
    warr_score, warr_terms = _score_table(normalized, _WARRANTY_RULES)
    track_score, track_terms = _score_table(normalized, _TRACKING_RULES)

    has_warranty_code = bool(_WARRANTY_CODE_RE.search(normalized))
    has_tracking_code = bool(_TRACKING_CODE_RE.search(normalized))

    if has_warranty_code:
        warr_score += 4
        warr_terms.append("wr-code")
    if has_tracking_code:
        track_score += 4
        track_terms.append("ticket-code")

    # Disambiguate "tra cứu bảo hành" / "kiểm tra bảo hành" from accidentally hitting TRACKING.
    if warr_score >= 3 and not has_tracking_code and "đơn" not in normalized and "phiếu sửa" not in normalized:
        if "tra cứu" in track_terms or "tra cuu" in track_terms:
            track_score = max(0, track_score - 2)
            track_terms = [t for t in track_terms if t not in ("tra cứu", "tra cuu")]

    # Datetime expression supports APPOINTMENT only alongside scheduling/visit clues.
    if _DATETIME_HINT_RE.search(normalized) and _APPOINTMENT_ACTION_RE.search(normalized):
        if appt_score >= 1 or (warr_score == 0 and track_score == 0):
            appt_score += 1
            appt_terms.append("datetime-hint")

    # Check explicit out-of-scope domains when there is no repair device context or explicit codes.
    oos_score, oos_terms = _score_table(normalized, _OUT_OF_SCOPE_RULES)
    dev_score, _ = _score_table(normalized, _DEVICE_CONTEXT_RULES)
    if (
        oos_score > 0
        and dev_score == 0
        and not has_warranty_code
        and not has_tracking_code
        and diag_score < 3
        and warr_score < 3
        and track_score < 3
    ):
        return IntentSignals(
            intent=Intent.UNKNOWN,
            confidence=0.92,
            scores={
                "DIAGNOSIS": diag_score,
                "QUOTATION": quote_score,
                "APPOINTMENT": appt_score,
                "WARRANTY": warr_score,
                "TRACKING": track_score,
            },
            matched_terms=oos_terms,
            secondary=[],
            source="rule",
        )

    score_map: dict[Intent, int] = {
        Intent.DIAGNOSIS: diag_score,
        Intent.QUOTATION: quote_score,
        Intent.APPOINTMENT: appt_score,
        Intent.WARRANTY: warr_score,
        Intent.TRACKING: track_score,
    }
    term_map: dict[Intent, list[str]] = {
        Intent.DIAGNOSIS: diag_terms,
        Intent.QUOTATION: quote_terms,
        Intent.APPOINTMENT: appt_terms,
        Intent.WARRANTY: warr_terms,
        Intent.TRACKING: track_terms,
    }
    score_dict = {k.value: v for k, v in score_map.items()}

    # Clear priority rules for multi-intent and explicit repair operations:
    intent = Intent.UNKNOWN
    if has_warranty_code and not has_tracking_code:
        intent = Intent.WARRANTY
    elif has_tracking_code and not has_warranty_code:
        intent = Intent.TRACKING
    elif warr_score >= 3 and warr_score >= track_score and warr_score >= quote_score and warr_score >= appt_score and diag_score < 4:
        intent = Intent.WARRANTY
    elif track_score >= 3 and track_score >= warr_score and track_score >= quote_score and track_score >= appt_score:
        intent = Intent.TRACKING
    elif diag_score >= 3:
        # Concrete symptom complaints always prioritize DIAGNOSIS so multi-intent flows (C19) run diagnosis->quotation->appointment.
        intent = Intent.DIAGNOSIS
    elif quote_score >= 3 and quote_score >= appt_score:
        intent = Intent.QUOTATION
    elif appt_score >= 3 and appt_score > quote_score:
        intent = Intent.APPOINTMENT
    else:
        # Pick highest positive score (tie break: WARRANTY > TRACKING > QUOTATION > APPOINTMENT > DIAGNOSIS)
        ranked = sorted(
            score_map.items(),
            key=lambda kv: (
                kv[1],
                {
                    Intent.WARRANTY: 5,
                    Intent.TRACKING: 4,
                    Intent.QUOTATION: 3,
                    Intent.APPOINTMENT: 2,
                    Intent.DIAGNOSIS: 1,
                }.get(kv[0], 0),
            ),
            reverse=True,
        )
        if ranked[0][1] > 0:
            intent = ranked[0][0]

    if intent == Intent.UNKNOWN:
        return IntentSignals(
            intent=Intent.UNKNOWN,
            confidence=0.0,
            scores=score_dict,
            matched_terms=[],
            secondary=[],
            source="rule",
        )

    primary_score = score_map[intent]
    # Rule confidence only reaches >= MIN_RULE_CONFIDENCE (0.85) on clear evidence (score >= 3).
    if primary_score >= 5:
        confidence = min(0.98, 0.88 + 0.02 * (primary_score - 5))
    elif primary_score >= 3:
        confidence = 0.85 + 0.02 * (primary_score - 3)
    elif primary_score == 2:
        confidence = 0.68
    else:
        confidence = 0.50

    return IntentSignals(
        intent=intent,
        confidence=round(confidence, 4),
        scores=score_dict,
        matched_terms=term_map[intent],
        secondary=_collect_secondary(intent, score_map),
        source="rule",
    )


class MlIntentClassifier:
    """Lazy-loading scikit-learn TF-IDF + LogisticRegression classifier wrapper."""

    def __init__(self, model_path: Path = MODEL_PATH) -> None:
        self._model_path = model_path
        self._pipeline: Any | None = None
        self._load_attempted = False

    @property
    def is_available(self) -> bool:
        return self._ensure_loaded() is not None

    def _ensure_loaded(self) -> Any | None:
        if self._load_attempted:
            return self._pipeline
        self._load_attempted = True
        if not self._model_path.exists():
            logger.warning("Intent ML model not found at %s; using rules only.", self._model_path)
            return None
        try:
            import joblib

            self._pipeline = joblib.load(self._model_path)
        except Exception as exc:  # pragma: no cover - corrupt artifact guard
            logger.warning("Failed to load intent ML model %s: %s", self._model_path, exc)
            self._pipeline = None
        return self._pipeline

    def predict(self, message: str) -> tuple[Intent, float, dict[str, float]]:
        pipeline = self._ensure_loaded()
        if pipeline is None:
            return Intent.UNKNOWN, 0.0, {}

        normalized = normalize_text(message)
        if not normalized:
            return Intent.UNKNOWN, 1.0, {Intent.UNKNOWN.value: 1.0}

        proba = pipeline.predict_proba([normalized])[0]
        classes = list(pipeline.classes_)
        prob_map = {str(c): round(float(p), 4) for c, p in zip(classes, proba)}
        best_idx = max(range(len(classes)), key=lambda i: float(proba[i]))
        best_label = str(classes[best_idx])
        best_prob = float(proba[best_idx])
        try:
            best_intent = Intent(best_label)
        except ValueError:
            best_intent = Intent.UNKNOWN
        return best_intent, round(best_prob, 4), prob_map


_ML_CLASSIFIER: MlIntentClassifier | None = None


def get_ml_classifier() -> MlIntentClassifier:
    global _ML_CLASSIFIER
    if _ML_CLASSIFIER is None:
        _ML_CLASSIFIER = MlIntentClassifier()
    return _ML_CLASSIFIER


def classify_intent(message: str) -> IntentSignals:
    """Hybrid classifier: Rule-first with clear-signal threshold, ML fallback, threshold gate."""
    rule_signals = classify_by_rules(message)

    # Clear out-of-scope or strong rule match returns directly without ML override.
    if rule_signals.confidence >= MIN_RULE_CONFIDENCE:
        return rule_signals

    ml = get_ml_classifier()
    if not ml.is_available:
        if rule_signals.intent != Intent.UNKNOWN and rule_signals.confidence >= 0.68:
            return rule_signals
        return IntentSignals(
            intent=Intent.UNKNOWN,
            confidence=round(1.0 - rule_signals.confidence, 4),
            scores=rule_signals.scores,
            matched_terms=rule_signals.matched_terms,
            secondary=rule_signals.secondary,
            source="threshold",
        )

    ml_intent, ml_prob, prob_map = ml.predict(message)
    score_map_for_secondary = {
        Intent.WARRANTY: rule_signals.scores.get(Intent.WARRANTY.value, 0),
        Intent.TRACKING: rule_signals.scores.get(Intent.TRACKING.value, 0),
        Intent.DIAGNOSIS: rule_signals.scores.get(Intent.DIAGNOSIS.value, 0),
        Intent.QUOTATION: rule_signals.scores.get(Intent.QUOTATION.value, 0),
        Intent.APPOINTMENT: rule_signals.scores.get(Intent.APPOINTMENT.value, 0),
    }

    if ml_prob >= MIN_ML_CONFIDENCE:
        return IntentSignals(
            intent=ml_intent,
            confidence=ml_prob,
            scores=rule_signals.scores,
            matched_terms=rule_signals.matched_terms,
            secondary=_collect_secondary(ml_intent, score_map_for_secondary),
            source="ml",
        )

    # Rule+ML consensus above 0.35 on the same active intent prevents false UNKNOWN on slang/short inputs.
    if (
        ml_intent != Intent.UNKNOWN
        and ml_prob >= 0.35
        and rule_signals.intent == ml_intent
    ):
        return IntentSignals(
            intent=ml_intent,
            confidence=round(max(rule_signals.confidence, ml_prob), 4),
            scores=rule_signals.scores,
            matched_terms=rule_signals.matched_terms,
            secondary=_collect_secondary(ml_intent, score_map_for_secondary),
            source="ml",
        )

    if rule_signals.intent != Intent.UNKNOWN and rule_signals.confidence >= 0.68 and ml_intent == rule_signals.intent:
        return IntentSignals(
            intent=rule_signals.intent,
            confidence=max(rule_signals.confidence, ml_prob),
            scores=rule_signals.scores,
            matched_terms=rule_signals.matched_terms,
            secondary=rule_signals.secondary,
            source="rule",
        )

    return IntentSignals(
        intent=Intent.UNKNOWN,
        confidence=round(prob_map.get(Intent.UNKNOWN.value, ml_prob), 4),
        scores=rule_signals.scores,
        matched_terms=rule_signals.matched_terms,
        secondary=[],
        source="threshold",
    )
