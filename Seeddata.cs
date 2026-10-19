using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Clinix
{
    // Sinh dữ liệu mẫu cho bệnh nhân và hồ sơ bệnh án.
    // Dữ liệu mẫu có mã bệnh nhân bắt đầu bằng "BNS" nên xóa được riêng, không đụng dữ liệu thật.
    // Hồ sơ mẫu đều ở trạng thái da_kham nên không ảnh hưởng hàng đợi Redis.
    internal static class SeedData
    {
        private const string TienTo = "BNS";
        private const int CoLo = 2000;

        private static readonly string[] Ho =
        {
            "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ",
            "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý"
        };
        private static readonly string[] DemNam =
            { "Văn", "Hữu", "Minh", "Quốc", "Đức", "Thanh", "Gia", "Anh" };
        private static readonly string[] DemNu =
            { "Thị", "Ngọc", "Thu", "Thanh", "Kim", "Bảo", "Mai", "Phương" };
        private static readonly string[] TenNam =
        {
            "Long", "Hùng", "Dũng", "Nam", "Tuấn", "Khoa", "Phúc", "Hải",
            "Bảo", "Huy", "Khang", "Minh", "Thịnh", "Đạt", "Quân"
        };
        private static readonly string[] TenNu =
        {
            "Lan", "Hoa", "Linh", "Trang", "Hương", "Thảo", "Ngân", "Vy",
            "Nhi", "Yến", "Châu", "Trâm", "Uyên", "Mai", "Anh"
        };
        private static readonly string[] Duong =
        {
            "Lê Lợi", "Nguyễn Huệ", "Trần Hưng Đạo", "Lý Thường Kiệt",
            "Cách Mạng Tháng 8", "Nguyễn Trãi", "Hai Bà Trưng", "Võ Văn Tần",
            "Điện Biên Phủ", "Phan Đăng Lưu", "Quang Trung", "Lê Văn Việt"
        };
        private static readonly string[] Quan =
        {
            "Quận 1", "Quận 3", "Quận 5", "Quận 7", "Quận 10", "Bình Thạnh",
            "Gò Vấp", "Tân Bình", "TP. Thủ Đức", "Phú Nhuận"
        };
        private static readonly string[] DauSo =
        {
            "90", "91", "93", "97", "98", "32", "33", "35", "37", "38",
            "70", "76", "77", "78", "79", "81", "82", "83", "84", "85"
        };
        private static readonly string[] LyDo =
        {
            "Nổi mụn nhiều ở mặt", "Ngứa da kéo dài", "Nổi mẩn đỏ", "Rụng tóc nhiều",
            "Da khô bong tróc", "Nám da", "Dị ứng da", "Viêm da cơ địa",
            "Nấm da", "Sẹo mụn", "Khám da định kỳ"
        };
        private static readonly string[] ChanDoan =
        {
            "Mụn trứng cá", "Viêm da cơ địa", "Nấm da", "Mày đay",
            "Viêm da tiếp xúc", "Nám da", "Vảy nến", "Rụng tóc", "Chàm"
        };
        private static readonly string[] CachDung =
        {
            "Sáng 1 viên, tối 1 viên", "Bôi ngày 2 lần", "Uống sau ăn",
            "Bôi buổi tối trước khi ngủ", "Ngày 3 lần, mỗi lần 1 viên"
        };
        private static readonly string[] HinhThuc = { "Tiền mặt", "Chuyển khoản", "Thẻ" };

        private static T Chon<T>(Random r, T[] a)
        {
            return a[r.Next(a.Length)];
        }

        private static void ChenTheoLo<T>(IMongoCollection<T> col, List<T> ds)
        {
            for (int i = 0; i < ds.Count; i += CoLo)
            {
                col.InsertMany(ds.Skip(i).Take(CoLo).ToList(),
                               new InsertManyOptions { IsOrdered = false });
            }
        }

        public static string Tao(int soBenhNhan)
        {
            var rnd = new Random();
            var db = new MongoClient("mongodb://localhost:27017").GetDatabase("QuanLyPhongKhamDB");
            var colBN = db.GetCollection<BenhNhan>("benh_nhan");
            var colHS = db.GetCollection<HoSoBenhAn>("ho_so_benh_an");
            var colBS = db.GetCollection<BacSi>("bac_si");
            var colDV = db.GetCollection<DichVu>("dich_vu");

            // Bác sĩ: chỉ thêm mẫu khi collection đang trống
            if (colBS.CountDocuments(FilterDefinition<BacSi>.Empty) == 0)
            {
                string[] ck = { "Da liễu", "Thẩm mỹ da", "Da liễu nhi" };
                var dsMau = new List<BacSi>();
                for (int i = 1; i <= 8; i++)
                {
                    dsMau.Add(new BacSi
                    {
                        MaBacSi = "BS" + i.ToString("D2"),
                        TenBacSi = "Bác sĩ " + Chon(rnd, Ho) + " " + Chon(rnd, TenNam),
                        ChuyenKhoa = Chon(rnd, ck),
                        SDT = "09" + rnd.Next(10000000, 99999999)
                    });
                }
                colBS.InsertMany(dsMau);
            }
            var dsMaBS = colBS.Find(FilterDefinition<BacSi>.Empty).ToList()
                              .Select(b => b.MaBacSi)
                              .Where(m => !string.IsNullOrWhiteSpace(m))
                              .ToList();

            // Thuốc và dịch vụ: chỉ thêm mẫu nếu thiếu loại tương ứng
            var dsThuoc = colDV.Find(dv => dv.Loai == "Thuoc").ToList();
            if (dsThuoc.Count == 0)
            {
                string[] tenThuoc =
                {
                    "Isotretinoin 20mg", "Doxycycline 100mg", "Cetirizine 10mg", "Loratadine 10mg",
                    "Clotrimazole 1%", "Betamethasone 0.05%", "Adapalene 0.1%", "Vitamin E 400IU"
                };
                for (int i = 0; i < tenThuoc.Length; i++)
                {
                    dsThuoc.Add(new DichVu
                    {
                        MaDichVu = "TS" + (i + 1).ToString("D3"),
                        TenDichVu = tenThuoc[i],
                        DonGia = rnd.Next(5, 120) * 1000m,
                        Loai = "Thuoc"
                    });
                }
                colDV.InsertMany(dsThuoc);
            }
            var dsDichVu = colDV.Find(dv => dv.Loai != "Thuoc").ToList();
            if (dsDichVu.Count == 0)
            {
                string[] tenDV = { "Khám da liễu", "Soi da", "Laser trị nám", "Lấy nhân mụn", "Điều trị sẹo rỗ", "Xét nghiệm nấm" };
                decimal[] gia = { 150000m, 200000m, 800000m, 300000m, 1200000m, 180000m };
                for (int i = 0; i < tenDV.Length; i++)
                {
                    dsDichVu.Add(new DichVu
                    {
                        MaDichVu = "DS" + (i + 1).ToString("D3"),
                        TenDichVu = tenDV[i],
                        DonGia = gia[i],
                        Loai = "DichVu"
                    });
                }
                colDV.InsertMany(dsDichVu);
            }

            // Chỉ mục để truy vấn nhanh khi dữ liệu lớn
            colBN.Indexes.CreateOne(new CreateIndexModel<BenhNhan>(
                Builders<BenhNhan>.IndexKeys.Ascending(b => b.MaBenhNhan)));
            colHS.Indexes.CreateOne(new CreateIndexModel<HoSoBenhAn>(
                Builders<HoSoBenhAn>.IndexKeys.Ascending(h => h.MaBenhNhan)));
            colHS.Indexes.CreateOne(new CreateIndexModel<HoSoBenhAn>(
                Builders<HoSoBenhAn>.IndexKeys.Ascending(h => h.MaPhieu)));
            colHS.Indexes.CreateOne(new CreateIndexModel<HoSoBenhAn>(
                Builders<HoSoBenhAn>.IndexKeys.Descending(h => h.NgayKham)));

            // Bệnh nhân: mã nối tiếp mã BNS lớn nhất đang có
            int batDauBN = 0;
            var cuoiBN = colBN.Find(Builders<BenhNhan>.Filter.Regex(
                                b => b.MaBenhNhan, new BsonRegularExpression("^" + TienTo + "\\d{6}$")))
                              .SortByDescending(b => b.MaBenhNhan)
                              .Limit(1)
                              .FirstOrDefault();
            if (cuoiBN != null)
                int.TryParse(cuoiBN.MaBenhNhan.Substring(TienTo.Length), out batDauBN);

            var dsBN = new List<BenhNhan>(soBenhNhan);
            for (int i = 1; i <= soBenhNhan; i++)
            {
                bool nam = rnd.Next(2) == 0;
                string ten = Chon(rnd, Ho) + " "
                           + Chon(rnd, nam ? DemNam : DemNu) + " "
                           + Chon(rnd, nam ? TenNam : TenNu);

                dsBN.Add(new BenhNhan
                {
                    MaBenhNhan = TienTo + (batDauBN + i).ToString("D6"),
                    TenBenhNhan = ten,
                    NgaySinh = DateTime.Today.AddDays(-rnd.Next(365, 365 * 85)),
                    GioiTinh = nam ? "Nam" : "Nữ",
                    SDT = "0" + Chon(rnd, DauSo) + rnd.Next(0, 10000000).ToString("D7"),
                    DiaChi = rnd.Next(1, 300) + " " + Chon(rnd, Duong) + ", " + Chon(rnd, Quan) + ", TP.HCM"
                });
            }
            ChenTheoLo(colBN, dsBN);

            // Hồ sơ bệnh án: khoảng 70% bệnh nhân có 1 đến 3 hồ sơ đã khám xong.
            // 30% còn lại không có hồ sơ để còn dùng thử chức năng cấp số.
            string phieuTiepTheo = MaPhieuHelper.TaoMaPhieuTiepTheo(colHS);
            int so = int.Parse(phieuTiepTheo.Substring(2)) - 1;
            int tongHS = 0;
            bool hetMaPhieu = false;
            var dsHS = new List<HoSoBenhAn>();

            foreach (var bn in dsBN)
            {
                if (hetMaPhieu) break;
                if (rnd.NextDouble() >= 0.7) continue;

                int soLan = rnd.Next(1, 4);
                for (int k = 0; k < soLan; k++)
                {
                    // Mã phiếu hiện chỉ có 5 chữ số (PK00001 đến PK99999)
                    if (so >= 99999) { hetMaPhieu = true; break; }
                    so++;

                    var thuoc = dsThuoc.OrderBy(x => rnd.Next()).Take(rnd.Next(1, 5))
                        .Select(t => new ChiTietThuoc
                        {
                            MaThuoc = t.MaDichVu,
                            TenThuoc = t.TenDichVu,
                            DonGia = t.DonGia,
                            SoLuong = rnd.Next(1, 6),
                            CachDung = Chon(rnd, CachDung)
                        }).ToList();

                    var dichVu = dsDichVu.OrderBy(x => rnd.Next()).Take(rnd.Next(0, 3))
                        .Select(d => new ChiTietDichVu
                        {
                            MaDichVu = d.MaDichVu,
                            TenDichVu = d.TenDichVu,
                            DonGia = d.DonGia,
                            SoLuong = 1
                        }).ToList();

                    decimal tong = thuoc.Sum(t => t.DonGia * t.SoLuong)
                                 + dichVu.Sum(d => d.DonGia * d.SoLuong);
                    bool daTT = rnd.NextDouble() < 0.7;

                    dsHS.Add(new HoSoBenhAn
                    {
                        MaPhieu = "PK" + so.ToString("D5"),
                        MaBenhNhan = bn.MaBenhNhan,
                        TenBenhNhan = bn.TenBenhNhan,
                        MaBacSi = dsMaBS.Count > 0 ? Chon(rnd, dsMaBS.ToArray()) : "",
                        SoThuTu = 0,
                        NgayKham = DateTime.Now.AddDays(-rnd.Next(0, 730)).AddMinutes(-rnd.Next(0, 1440)),
                        TrangThai = "da_kham",
                        LyDoKham = Chon(rnd, LyDo),
                        ChanDoan = Chon(rnd, ChanDoan),
                        DonThuoc = thuoc,
                        DichVuChiDinh = dichVu,
                        ThanhToan = new ThongTinThanhToan
                        {
                            TongTien = tong,
                            HinhThuc = daTT ? Chon(rnd, HinhThuc) : "",
                            DaThanhToan = daTT
                        }
                    });
                    tongHS++;
                }

                if (dsHS.Count >= CoLo)
                {
                    ChenTheoLo(colHS, dsHS);
                    dsHS.Clear();
                }
            }
            if (dsHS.Count > 0) ChenTheoLo(colHS, dsHS);

            string ghiChu = hetMaPhieu
                ? "\nĐã dừng tạo hồ sơ vì mã phiếu đạt giới hạn PK99999."
                : "";
            return "Đã tạo " + dsBN.Count.ToString("N0") + " bệnh nhân và "
                 + tongHS.ToString("N0") + " hồ sơ bệnh án." + ghiChu;
        }

        public static string Xoa()
        {
            var db = new MongoClient("mongodb://localhost:27017").GetDatabase("QuanLyPhongKhamDB");
            var colBN = db.GetCollection<BenhNhan>("benh_nhan");
            var colHS = db.GetCollection<HoSoBenhAn>("ho_so_benh_an");
            var re = new BsonRegularExpression("^" + TienTo);

            long hs = colHS.DeleteMany(Builders<HoSoBenhAn>.Filter.Regex(h => h.MaBenhNhan, re)).DeletedCount;
            long bn = colBN.DeleteMany(Builders<BenhNhan>.Filter.Regex(b => b.MaBenhNhan, re)).DeletedCount;

            return "Đã xóa " + bn.ToString("N0") + " bệnh nhân và " + hs.ToString("N0") + " hồ sơ mẫu.";
        }
    }
}