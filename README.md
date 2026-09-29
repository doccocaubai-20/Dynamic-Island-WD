# 🏝 Dynamic Island cho Windows

Ứng dụng mô phỏng thanh **Dynamic Island** của iPhone trên máy tính Windows, lấy cảm hứng từ chia sẻ của tác giả **Nhựt Tấn** trên **J2TEAM Community**.

---

## 🌟 Các tính năng chính

1. **Thanh Notch / Pill đa năng gắn trên đỉnh màn hình**:
   - **Chế độ Idle (Đồng hồ & Hệ thống)**: Hiển thị đồng hồ số (`HH:mm`) và đèn báo trạng thái. Khi di chuột hoặc click vào, Notch bung to mượt mà (Apple Spring Bounce) hiển thị giờ lớn, ngày tháng hiện tại, trạng thái media và nút tắt/mở âm lượng, tìm kiếm.
   - **Chế độ Media (Nghe nhạc)**:
     - Tự động bắt nhạc thật đang phát từ **Spotify, YouTube trên Chrome/Edge, Apple Music, VLC,...** thông qua Windows Media Session (GSMTC).
     - Thu nhỏ: hiển thị ảnh bìa album bo góc tròn, tên bài hát và sóng nhạc nhảy hoạt hình sống động.
     - Bung to: hiển thị đầy đủ ảnh bìa lớn, tên bài hát, nghệ sĩ, thanh tua tiến trình bài hát (seekbar), nút lùi bài, phát/tạm dừng, chuyển bài.
     - Tích hợp sẵn **Chế độ Demo** bài hát *"Khiem Phan - y2mate.com - Nhan Vi Tieng Chuong"* để bạn trải nghiệm ngay mà không cần mở nhạc ngoài.

2. **Cửa sổ Cài đặt (Settings)**:
   - Giao diện Dark theme bo góc hiện đại.
   - **APPEARANCE**: Tùy chọn chuyển đổi nhanh giữa `Floating pill` (viên thuốc lơ lửng) và `Full notch` (tai thỏ áp sát viền màn hình).
   - **ISLAND WIDTH**: Tinh chỉnh tỉ lệ độ rộng (`Scale: 100%`) bằng thanh trượt với phản hồi trực quan theo thời gian thực.
   - **SPRING BOUNCE**: Tinh chỉnh độ nảy đàn hồi (`Bounce: 100`) của hiệu ứng vật lý lò xo.
   - **STARTUP**: Bật/tắt khởi động cùng Windows (`Start with Windows`).
   - Các nút tương tác nhanh: `Home`, `Show/hide`, `Click pill`, `Expand`, `🎵 Demo Song`.

3. **Click-Through xuyên thấu thông minh**:
   - Vùng trong suốt xung quanh Notch hoàn toàn cho phép click chuột xuyên qua (Win32 `WM_NCHITTEST` / `HTTRANSPARENT`), không lo cản trở thao tác vào các ứng dụng bên dưới.

4. **Hiệu năng siêu nhẹ**:
   - Được viết bằng **C# .NET 9 WPF**, chiếm chỉ ~35-45MB RAM, CPU < 0.2%, cực kỳ mượt mà trên mọi cấu hình.

---

## 🚀 Hướng dẫn khởi chạy

- **Cách 1**: Nhấp đúp vào file `Start-DynamicIsland.bat`
- **Cách 2**: Chạy trực tiếp file thực thi tại:
  `DynamicIslandApp\bin\Release\net9.0-windows10.0.19041.0\DynamicIslandApp.exe`

### Thao tác nhanh:
- **Click chuột trái vào Notch**: Bung to / Thu nhỏ.
- **Click chuột phải vào Notch**: Mở menu chọn `Cài đặt`, `Đổi chế độ Full notch / Floating pill`, `Bật/Tắt nhạc Demo`, hoặc `Thoát`.
- **Khay hệ thống (System Tray)**: Nhấp đúp vào icon viên thuốc màu tím ở góc dưới màn hình để mở nhanh bảng Cài đặt.
"# Dynamic-Island-WD" 
