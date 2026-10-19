using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;

namespace Clinix
{
    public partial class KhamBenh : Form
    {
        private MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<BenhNhan> collectionBN;
        private IMongoCollection<HoSoBenhAn> collectionHoSo;
        private IMongoCollection<BacSi> collectionBacSi;
        private string hoSoHienTaiId;

        // Tạo bằng code, không nằm trong Designer
        private Button btnHoanTat;
        private TextBox txtLyDoKham;
        private ComboBox cbbLocChanDoan;

        private static readonly Lazy<ConnectionMultiplexer> redisLazy =
            new Lazy<ConnectionMultiplexer>(() =>
                ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false,connectTimeout=5000"));

        private static IDatabase RedisDb => redisLazy.Value.GetDatabase(0);

        public KhamBenh()
        {
            InitializeComponent();

            client = new MongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("QuanLyPhongKhamDB");
            collectionBN = database.GetCollection<BenhNhan>("benh_nhan");
            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
            collectionBacSi = database.GetCollection<BacSi>("bac_si");
        }

        private void HienThiBenhNhan(List<BenhNhan> ds)
        {
            dgvBN.DataSource = ds.Select(bn => new
            {
                bn.MaBenhNhan,
                bn.TenBenhNhan,
                bn.SDT,
                bn.GioiTinh,
                bn.NgaySinh,
                bn.DiaChi
            }).ToList();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TaoBoLocChanDoan();

            LoadDanhSachBenhNhan();
            LoadDanhSachBacSi();

            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd/MM/yyyy";

            txtMaPhieu.Text = MaPhieuMoi();

            TaoNutHoanTat();
            TaoOLyDoKham();
        }

        // Mã phiếu kế tiếp theo số thứ tự (chỉ để hiển thị); lỗi kết nối thì để trống,
        // lúc lưu hồ sơ mới sẽ tính lại
        private string MaPhieuMoi()
        {
            try { return MaPhieuHelper.TaoMaPhieuTiepTheo(collectionHoSo); }
            catch { return ""; }
        }

        // Nút Hoàn tất khám đặt bên trái nút Làm mới, cùng hàng với Lưu / Quay Lại
        private void TaoNutHoanTat()
        {
            btnHoanTat = new Button
            {
                Text = "Hoàn tất khám",
                Font = btnLuu.Font,
                ForeColor = btnLuu.ForeColor,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(130, 31),
                Location = new Point(btnNhapMoi.Left - 145, btnNhapMoi.Top),
                UseVisualStyleBackColor = true
            };
            btnHoanTat.Click += btnHoanTat_Click;
            Controls.Add(btnHoanTat);
        }

        // Ô chỉ đọc hiển thị lý do khám (lễ tân nhập lúc cấp số) ngay trong khung "Khám bệnh".
        // Ô Chẩn đoán được thu nhỏ lại một chút để nhường chỗ.
        private void TaoOLyDoKham()
        {
            var lblLyDo = new Label
            {
                Text = "Lý do khám:",
                AutoSize = true,
                Font = label6.Font,
                ForeColor = label6.ForeColor,
                BackColor = label6.BackColor,
                Location = new Point(25, 77)
            };

            txtLyDoKham = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = txtChanDoan.Font,
                BackColor = Color.WhiteSmoke,
                Location = new Point(129, 74),
                Size = new Size(498, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            label6.Top = 125;
            txtChanDoan.Top = 122;
            txtChanDoan.Height = 46;

            groupBox3.Controls.Add(lblLyDo);
            groupBox3.Controls.Add(txtLyDoKham);
        }

        // Ô lọc theo chẩn đoán đặt dưới ô tìm kiếm, lưới bệnh nhân được đẩy xuống một chút
        private void TaoBoLocChanDoan()
        {
            var lbl = new Label
            {
                Text = "Lọc chẩn đoán:",
                AutoSize = true,
                Font = new Font("Times New Roman", 10.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 99, 137),
                BackColor = Color.Transparent,
                Location = new Point(24, 88)
            };

            cbbLocChanDoan = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = txtTim.Font,
                Location = new Point(150, 84),
                Width = 220
            };

            cbbLocChanDoan.Items.AddRange(new object[]
            {
                "Tất cả",
                "Chưa chẩn đoán",
                "Đã chẩn đoán"
            });
            cbbLocChanDoan.SelectedIndex = 0;

            // Gắn sự kiện sau khi đặt giá trị đầu để không tải danh sách sớm
            cbbLocChanDoan.SelectedIndexChanged += (s, e) => LoadDanhSachBenhNhan();

            dgvBN.Top = 122;
            dgvBN.Height = 554;

            groupBox1.Controls.Add(lbl);
            groupBox1.Controls.Add(cbbLocChanDoan);
        }

        // TẢI DANH SÁCH BÁC SĨ TỪ MONGODB (collection bac_si)
        private void LoadDanhSachBacSi()
        {
            try
            {
                var ds = collectionBacSi.Find(FilterDefinition<BacSi>.Empty)
                                        .SortBy(b => b.MaBacSi)
                                        .ToList();
                if (ds.Count == 0)
                {
                    MessageBox.Show("Chưa có bác sĩ nào trong collection bac_si.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cbbBSi.DisplayMember = "TenBacSi";
                cbbBSi.ValueMember = "MaBacSi";
                cbbBSi.DataSource = ds;
                cbbBSi.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách bác sĩ: " + ex.Message);
            }
        }

        // Chọn bác sĩ trong combobox theo mã. So khớp không phân biệt hoa thường, bỏ khoảng trắng thừa.
        private void ChonBacSi(string maBacSi)
        {
            var ds = cbbBSi.DataSource as List<BacSi>;
            if (ds == null || ds.Count == 0) return;

            if (string.IsNullOrWhiteSpace(maBacSi))
            {
                cbbBSi.SelectedIndex = 0;
                return;
            }

            int idx = ds.FindIndex(b => string.Equals(
                (b.MaBacSi ?? "").Trim(), maBacSi.Trim(), StringComparison.OrdinalIgnoreCase));

            if (idx >= 0)
            {
                cbbBSi.SelectedIndex = idx;
            }
            else
            {
                cbbBSi.SelectedIndex = -1;
                MessageBox.Show(
                    "Hồ sơ ghi mã bác sĩ \"" + maBacSi + "\" nhưng mã này không có trong collection bac_si.",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Mã các bệnh nhân đang có hồ sơ chờ/đang khám (chưa thanh toán) đã có chẩn đoán.
        // Cùng điều kiện với hồ sơ mà TaiHoSoDangKham nạp lên form. Danh sách này nhỏ vì chỉ gồm hồ sơ đang xử lý.
        private List<string> LayMaDaChanDoan()
        {
            var filter = Builders<HoSoBenhAn>.Filter.Where(h =>
                (h.TrangThai == "cho_kham" || h.TrangThai == "dang_kham")
                && h.ChanDoan != null && h.ChanDoan != ""
                && (h.ThanhToan == null || h.ThanhToan.DaThanhToan == false));

            return collectionHoSo.Distinct(h => h.MaBenhNhan, filter).ToList();
        }

        // Bộ lọc bệnh nhân gồm từ khóa tìm kiếm (mã hoặc tên) và lọc chẩn đoán đang chọn
        private FilterDefinition<BenhNhan> TaoBoLocBenhNhan()
        {
            var b = Builders<BenhNhan>.Filter;
            var filter = b.Empty;

            string tuKhoa = txtTim.Text.Trim();
            if (tuKhoa.Length > 0)
            {
                filter &= b.Regex(bn => bn.TenBenhNhan, MongoHelper.Like(tuKhoa))
                        | b.Regex(bn => bn.MaBenhNhan, MongoHelper.Like(tuKhoa));
            }

            int loc = cbbLocChanDoan == null ? 0 : cbbLocChanDoan.SelectedIndex;
            if (loc == 1)
            {
                filter &= b.Nin(bn => bn.MaBenhNhan, LayMaDaChanDoan());
            }
            else if (loc == 2)
            {
                filter &= b.In(bn => bn.MaBenhNhan, LayMaDaChanDoan());
            }

            return filter;
        }

        // TẢI DANH SÁCH BỆNH NHÂN TỪ MONGODB (theo từ khóa và bộ lọc chẩn đoán hiện tại)
        private void LoadDanhSachBenhNhan()
        {
            try
            {
                HienThiBenhNhan(collectionBN.Find(TaoBoLocBenhNhan()).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối MongoDB: " + ex.Message);
            }
        }

        // Tính tuổi đúng: trừ 1 nếu chưa qua sinh nhật năm nay
        private void CapNhatTuoi()
        {
            DateTime ns = dateTimeNgaySinh.Value.Date;
            DateTime homNay = DateTime.Today;
            int tuoi = homNay.Year - ns.Year;
            if (ns > homNay.AddYears(-tuoi)) tuoi--;
            txtTuoi.Text = Math.Max(tuoi, 0).ToString();
        }

        // Nạp hồ sơ đang chờ khám (cho_kham, do ThemLichKham tạo lúc cấp số) hoặc đang khám (dang_kham),
        // chưa thanh toán, mới nhất của bệnh nhân lên form
        private void TaiHoSoDangKham(string maBN)
        {
            if (string.IsNullOrEmpty(maBN)) return;

            try
            {
                var hoSo = collectionHoSo
                    .Find(h => h.MaBenhNhan == maBN
                            && (h.TrangThai == "cho_kham" || h.TrangThai == "dang_kham")
                            && (h.ThanhToan == null || h.ThanhToan.DaThanhToan == false))
                    .SortByDescending(h => h.NgayKham)
                    .FirstOrDefault();

                if (hoSo == null) return; // chưa có hồ sơ chờ/đang khám: giữ form trống, phiếu mới

                hoSoHienTaiId = hoSo.Id;
                txtChanDoan.Text = hoSo.ChanDoan ?? "";
                txtLyDoKham.Text = hoSo.LyDoKham ?? "";

                if (!string.IsNullOrEmpty(hoSo.MaPhieu))
                    txtMaPhieu.Text = hoSo.MaPhieu;

                ChonBacSi(hoSo.MaBacSi);

                if (hoSo.NgayKham >= dateTimePicker1.MinDate && hoSo.NgayKham <= dateTimePicker1.MaxDate)
                    dateTimePicker1.Value = hoSo.NgayKham;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải hồ sơ khám: " + ex.Message);
            }
        }

        // CLICK VÀO BẢNG ĐỂ ĐỔ DỮ LIỆU LÊN TEXTBOX
        private void dataGridViewBN_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvBN.Rows[e.RowIndex];

            txtMaBN.Text = row.Cells["MaBenhNhan"].Value?.ToString();
            txtHoTen.Text = row.Cells["TenBenhNhan"].Value?.ToString();
            txtSDT.Text = row.Cells["SDT"].Value?.ToString();
            cbbGioiTinh.Text = row.Cells["GioiTinh"].Value?.ToString();
            txtDiaChi.Text = row.Cells["DiaChi"].Value?.ToString() ?? "";

            if (row.Cells["NgaySinh"].Value is DateTime ns
                && ns >= dateTimeNgaySinh.MinDate && ns <= dateTimeNgaySinh.MaxDate)
            {
                dateTimeNgaySinh.Value = ns;
                CapNhatTuoi();
            }
            else
            {
                txtTuoi.Text = "";
            }

            // Đặt về trạng thái phiếu mới (kể cả bác sĩ về mặc định), rồi nạp hồ sơ đang khám nếu có
            txtChanDoan.Text = "";
            txtLyDoKham.Text = "";
            txtMaPhieu.Text = MaPhieuMoi();
            hoSoHienTaiId = null;
            ChonBacSi(null);

            TaiHoSoDangKham(txtMaBN.Text);
        }

        // Lưu hoặc cập nhật hồ sơ (upsert). Dùng chung cho nút Lưu, Hoàn tất khám và menu Kê toa.
        // Hồ sơ cho_kham chuyển sang dang_kham ngay khi bác sĩ lưu lần đầu.
        private bool LuuHoSo(bool batBuocChanDoan)
        {
            if (string.IsNullOrEmpty(txtMaBN.Text))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (batBuocChanDoan && string.IsNullOrWhiteSpace(txtChanDoan.Text))
            {
                MessageBox.Show("Vui lòng nhập Chẩn đoán trước khi lưu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string maBacSi = cbbBSi.SelectedValue?.ToString();
            if (string.IsNullOrEmpty(maBacSi))
            {
                MessageBox.Show("Vui lòng chọn bác sĩ khám!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(hoSoHienTaiId))
                {
                    var hoSoMoi = new HoSoBenhAn
                    {
                        MaPhieu = MaPhieuHelper.TaoMaPhieuTiepTheo(collectionHoSo),
                        MaBenhNhan = txtMaBN.Text,
                        TenBenhNhan = txtHoTen.Text,
                        NgayKham = dateTimePicker1.Value,
                        ChanDoan = txtChanDoan.Text.Trim(),
                        LyDoKham = "",
                        MaBacSi = maBacSi,
                        TrangThai = "dang_kham",
                        SoThuTu = 0,
                        ThanhToan = new ThongTinThanhToan { TongTien = 0, HinhThuc = "", DaThanhToan = false }
                    };
                    collectionHoSo.InsertOne(hoSoMoi);
                    hoSoHienTaiId = hoSoMoi.Id;
                    txtMaPhieu.Text = hoSoMoi.MaPhieu;
                }
                else
                {
                    var update = Builders<HoSoBenhAn>.Update
                        .Set(h => h.ChanDoan, txtChanDoan.Text.Trim())
                        .Set(h => h.MaBacSi, maBacSi)
                        .Set(h => h.NgayKham, dateTimePicker1.Value)
                        .Set(h => h.TrangThai, "dang_kham");
                    collectionHoSo.UpdateOne(Builders<HoSoBenhAn>.Filter.Eq(h => h.Id, hoSoHienTaiId), update);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu vào MongoDB: " + ex.Message);
                return false;
            }
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }
        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (LuuHoSo(true))
            {
                MessageBox.Show(
                    "Lưu hồ sơ khám thành công!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Chẩn đoán vừa lưu có thể làm bệnh nhân đổi nhóm lọc, nên tải lại danh sách.
                // Các ô nhập trên form giữ nguyên nên vẫn kê toa được.
                LoadDanhSachBenhNhan();
            }
        }

        // Khám xong thì bệnh nhân rời hàng đợi Redis của ngày, để lần khám sau trong ngày cấp số lại được.
        // Mongo là nguồn đúng; lỗi Redis chỉ cảnh báo, không hoàn tác việc hoàn tất khám.
        private void GoKhoiHangDoi(string maBN)
        {
            try
            {
                var keys = new[] { DateTime.Now, dateTimePicker1.Value }
                    .Select(d => "phongkham:hangdoi:" + d.ToString("yyyyMMdd"))
                    .Distinct();

                foreach (string key in keys)
                    RedisDb.SortedSetRemove(key, maBN);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Đã hoàn tất khám nhưng chưa gỡ được bệnh nhân khỏi hàng đợi Redis: " + ex.Message,
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // HOÀN TẤT KHÁM: lưu hồ sơ rồi đặt trạng thái da_kham, form về trạng thái trống
        private void btnHoanTat_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtMaBN.Text))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult xacNhan = MessageBox.Show(
                "Hoàn tất khám cho bệnh nhân này? Hồ sơ sẽ chuyển sang trạng thái đã khám.",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (xacNhan != DialogResult.Yes) return;

            if (!LuuHoSo(true)) return;

            string maBN = txtMaBN.Text.Trim();

            try
            {
                var filter = Builders<HoSoBenhAn>.Filter.Eq(h => h.Id, hoSoHienTaiId);
                var update = Builders<HoSoBenhAn>.Update.Set(h => h.TrangThai, "da_kham");
                collectionHoSo.UpdateOne(filter, update);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật trạng thái: " + ex.Message);
                return;
            }

            GoKhoiHangDoi(maBN);

            MessageBox.Show("Đã hoàn tất khám!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnNhapMoi_Click(sender, e);
        }

        // NÚT LÀM MỚI
        private void btnNhapMoi_Click(object sender, EventArgs e)
        {
            txtMaBN.Text = "";
            txtHoTen.Text = "";
            txtSDT.Text = "";
            cbbGioiTinh.Text = "";
            txtDiaChi.Text = "";
            txtTuoi.Text = "";
            txtChanDoan.Text = "";
            txtLyDoKham.Text = "";
            txtMaPhieu.Text = MaPhieuMoi();
            hoSoHienTaiId = null;
            ChonBacSi(null);
            LoadDanhSachBenhNhan();
        }

        private void btnTim_Click(object sender, EventArgs e)
        {
            LoadDanhSachBenhNhan();
        }

        private void kêToaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!LuuHoSo(false)) return; // tự tạo hồ sơ nếu chưa có, chẩn đoán có thể điền sau
            KeToa frmToa = new KeToa(txtMaBN.Text, hoSoHienTaiId);
            frmToa.ShowDialog();
        }

        // Xem (chỉ đọc) dịch vụ chỉ định lễ tân đã thêm lúc cấp số cho hồ sơ đang mở
        private void chỉĐịnhToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hoSoHienTaiId))
            {
                MessageBox.Show("Bệnh nhân này chưa có hồ sơ chờ khám hoặc đang khám.", "Chỉ định", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var hoSo = collectionHoSo.Find(h => h.Id == hoSoHienTaiId).FirstOrDefault();
                var dsDichVu = hoSo?.DichVuChiDinh ?? new List<ChiTietDichVu>();

                if (dsDichVu.Count == 0)
                {
                    MessageBox.Show("Hồ sơ này chưa có dịch vụ chỉ định.", "Chỉ định", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var sb = new StringBuilder();
                foreach (var dv in dsDichVu)
                    sb.AppendLine(dv.TenDichVu + "  x" + dv.SoLuong + "  =  " + (dv.DonGia * dv.SoLuong).ToString("N0") + " đ");
                sb.AppendLine();
                sb.AppendLine("Cộng dịch vụ: " + dsDichVu.Sum(d => d.DonGia * d.SoLuong).ToString("N0") + " đ");

                MessageBox.Show(sb.ToString(), "Dịch vụ chỉ định", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dịch vụ chỉ định: " + ex.Message);
            }
        }

        // Mở form Lịch sử KCB dạng hộp thoại; có chọn bệnh nhân thì chỉ xem bệnh nhân đó
        private void lịchSửKCBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string maBN = string.IsNullOrWhiteSpace(txtMaBN.Text) ? null : txtMaBN.Text.Trim();
            using (var f = new frmLichSuKCB(maBN, false))
            {
                f.ShowDialog(this);
            }
        }

        private void dateTimeNgaySinh_ValueChanged(object sender, EventArgs e)
        {
            CapNhatTuoi();
        }
    }
}