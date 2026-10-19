using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Driver;

namespace Clinix
{
    // Form Lịch sử KCB, dựng hoàn toàn bằng code, không dùng Designer.
    // laFormChinh = true: Quay lại sẽ về menu chọn giao diện.
    // laFormChinh = false: Quay lại chỉ đóng form lịch sử.
    public class frmLichSuKCB : Form
    {
        private static readonly Color MauChu = Color.FromArgb(33, 99, 137);
        private static readonly Font FontThuong =
            new Font("Times New Roman", 11F, FontStyle.Regular);
        private static readonly Font FontDam =
            new Font("Times New Roman", 11F, FontStyle.Bold);

        private readonly IMongoCollection<HoSoBenhAn> collectionHoSo;
        private readonly LichSuKCBService service;
        private readonly string maBNCoDinh;
        private readonly bool laFormChinh;

        private SplitContainer split;
        private TextBox txtTimKiem;
        private DateTimePicker dtpTu;
        private DateTimePicker dtpDen;
        private ComboBox cbbTrangThai;

        private Button btnLoc;
        private Button btnLamMoi;
        private Button btnQuayLai;

        private DataGridView dgvLichSu;
        private DataGridView dgvThuoc;
        private DataGridView dgvDichVu;

        private TextBox txtLyDo;
        private TextBox txtChanDoan;
        private Label lblTongTien;
        private Label lblThanhToan;
        private ComboBox cbbHinhThuc;
        private Button btnThanhToan;

        public frmLichSuKCB(string maBN, bool laFormChinh)
        {
            maBNCoDinh = string.IsNullOrWhiteSpace(maBN) ? null : maBN.Trim();
            this.laFormChinh = laFormChinh;

            var database = new MongoClient("mongodb://localhost:27017")
                .GetDatabase("QuanLyPhongKhamDB");

            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
            service = new LichSuKCBService(database);

            Text = "Lịch sử khám chữa bệnh";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Snow;
            ClientSize = new Size(1200, 720);
            MinimumSize = new Size(1020, 620);
            Font = FontThuong;

            XayDungGiaoDien();

            Load += (s, e) =>
            {
                int khoangTrong = split.Height - split.Panel2MinSize;
                if (khoangTrong >= split.Panel1MinSize)
                {
                    int mongMuon = split.Height * 45 / 100;
                    split.SplitterDistance = Math.Min(
                        Math.Max(mongMuon, split.Panel1MinSize),
                        khoangTrong);
                }

                TaiDanhSach(null);
            };
        }

        // ---------- Dựng giao diện ----------

        private static Label TaoNhan(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = FontDam,
                ForeColor = MauChu,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
        }

        private static Button TaoNut(string text, int x, int y, int w)
        {
            return new Button
            {
                Text = text,
                Font = FontDam,
                ForeColor = MauChu,
                Location = new Point(x, y),
                Size = new Size(w, 34),
                UseVisualStyleBackColor = true
            };
        }

        private static void CauHinhLuoi(DataGridView g)
        {
            g.Dock = DockStyle.Fill;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.MultiSelect = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.BackgroundColor = Color.White;
            g.GridColor = Color.LightGray;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.AliceBlue;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            g.ColumnHeadersDefaultCellStyle.Font = FontDam;
            g.DefaultCellStyle.Font = FontThuong;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
        }

        private void XayDungGiaoDien()
        {
            // Thanh lọc có hai hàng để các nút không bị che khi thu nhỏ form.
            var pnlLoc = new Panel
            {
                Dock = DockStyle.Top,
                Height = 112,
                BackColor = Color.AliceBlue
            };

            txtTimKiem = new TextBox
            {
                Location = new Point(15, 38),
                Width = 230,
                Font = FontThuong
            };

            dtpTu = new DateTimePicker
            {
                Location = new Point(265, 38),
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowCheckBox = true,
                Checked = false,
                Font = FontThuong
            };

            dtpDen = new DateTimePicker
            {
                Location = new Point(435, 38),
                Width = 150,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowCheckBox = true,
                Checked = false,
                Font = FontThuong
            };

            cbbTrangThai = new ComboBox
            {
                Location = new Point(605, 38),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FontThuong
            };

            cbbTrangThai.Items.AddRange(new object[]
            {
                "Tất cả",
                "Chờ khám",
                "Đang khám",
                "Đã khám"
            });
            cbbTrangThai.SelectedIndex = 0;

            btnLoc = TaoNut("Lọc", 0, 70, 90);
            btnLamMoi = TaoNut("Làm mới", 0, 70, 100);
            btnQuayLai = TaoNut("Quay lại", 0, 70, 100);

            btnLoc.Click += (s, e) => TaiDanhSach(null);
            btnLamMoi.Click += btnLamMoi_Click;
            btnQuayLai.Click += btnQuayLai_Click;

            // Chọn trạng thái sẽ tự lọc lại danh sách.
            cbbTrangThai.SelectedIndexChanged += (s, e) => TaiDanhSach(null);

            // Nếu form được mở từ một bệnh nhân, điền sẵn mã nhưng vẫn cho phép xóa/sửa.
            if (maBNCoDinh != null)
                txtTimKiem.Text = maBNCoDinh;

            pnlLoc.Controls.Add(TaoNhan("Mã hoặc tên bệnh nhân", 15, 12));
            pnlLoc.Controls.Add(TaoNhan("Từ ngày", 265, 12));
            pnlLoc.Controls.Add(TaoNhan("Đến ngày", 435, 12));
            pnlLoc.Controls.Add(TaoNhan("Trạng thái", 605, 12));

            pnlLoc.Controls.Add(txtTimKiem);
            pnlLoc.Controls.Add(dtpTu);
            pnlLoc.Controls.Add(dtpDen);
            pnlLoc.Controls.Add(cbbTrangThai);

            pnlLoc.Controls.Add(btnLoc);
            pnlLoc.Controls.Add(btnLamMoi);
            pnlLoc.Controls.Add(btnQuayLai);

            // Xếp ba nút sát bên phải, tự điều chỉnh theo chiều rộng thanh lọc.
            Action sapXepNut = () =>
            {
                int benPhai = pnlLoc.ClientSize.Width - 15;

                btnQuayLai.Location = new Point(
                    benPhai - btnQuayLai.Width, 70);

                btnLamMoi.Location = new Point(
                    btnQuayLai.Left - 8 - btnLamMoi.Width, 70);

                btnLoc.Location = new Point(
                    btnLamMoi.Left - 8 - btnLoc.Width, 70);
            };

            pnlLoc.Resize += (s, e) => sapXepNut();
            sapXepNut();

            AcceptButton = btnLoc;

            // Lưới lịch sử ở phía trên.
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.Snow,
                
            };

            dgvLichSu = new DataGridView();
            CauHinhLuoi(dgvLichSu);
            dgvLichSu.SelectionChanged += (s, e) => HienChiTiet();
            dgvLichSu.CellFormatting += dgvLichSu_CellFormatting;
            split.Panel1.Controls.Add(dgvLichSu);

            // Khu vực chi tiết bên dưới: thông tin, toa thuốc, dịch vụ.
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(6)
            };

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Cột thông tin hồ sơ và thanh toán.
            var gbThongTin = new GroupBox
            {
                Text = "Thông tin hồ sơ",
                Dock = DockStyle.Fill,
                Font = FontDam,
                ForeColor = MauChu
            };

            var info = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8
            };

            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            txtLyDo = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = FontThuong,
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                ScrollBars = ScrollBars.Vertical
            };

            txtChanDoan = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = FontThuong,
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                ScrollBars = ScrollBars.Vertical
            };

            lblTongTien = new Label
            {
                AutoSize = true,
                Font = FontDam,
                ForeColor = MauChu,
                Text = "Tổng tiền: 0 đ",
                Margin = new Padding(3, 6, 3, 3)
            };

            lblThanhToan = new Label
            {
                AutoSize = true,
                Font = FontThuong,
                ForeColor = Color.Black
            };

            cbbHinhThuc = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = FontThuong
            };
            cbbHinhThuc.Items.AddRange(new object[]
            {
                "Tiền mặt",
                "Chuyển khoản",
                "Thẻ"
            });
            cbbHinhThuc.SelectedIndex = 0;

            btnThanhToan = new Button
            {
                Text = "Thanh toán",
                Dock = DockStyle.Fill,
                Font = FontDam,
                ForeColor = Color.DarkRed,
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnThanhToan.Click += btnThanhToan_Click;

            info.Controls.Add(new Label
            {
                Text = "Lý do khám",
                AutoSize = true,
                Font = FontDam,
                ForeColor = MauChu
            }, 0, 0);

            info.Controls.Add(txtLyDo, 0, 1);

            info.Controls.Add(new Label
            {
                Text = "Chẩn đoán",
                AutoSize = true,
                Font = FontDam,
                ForeColor = MauChu
            }, 0, 2);

            info.Controls.Add(txtChanDoan, 0, 3);
            info.Controls.Add(lblTongTien, 0, 4);
            info.Controls.Add(lblThanhToan, 0, 5);
            info.Controls.Add(cbbHinhThuc, 0, 6);
            info.Controls.Add(btnThanhToan, 0, 7);
            gbThongTin.Controls.Add(info);

            // Cột toa thuốc.
            var gbThuoc = new GroupBox
            {
                Text = "Toa thuốc",
                Dock = DockStyle.Fill,
                Font = FontDam,
                ForeColor = MauChu
            };

            dgvThuoc = new DataGridView();
            CauHinhLuoi(dgvThuoc);
            gbThuoc.Controls.Add(dgvThuoc);

            // Cột dịch vụ chỉ định.
            var gbDichVu = new GroupBox
            {
                Text = "Dịch vụ chỉ định",
                Dock = DockStyle.Fill,
                Font = FontDam,
                ForeColor = MauChu
            };

            dgvDichVu = new DataGridView();
            CauHinhLuoi(dgvDichVu);
            gbDichVu.Controls.Add(dgvDichVu);

            tlp.Controls.Add(gbThongTin, 0, 0);
            tlp.Controls.Add(gbThuoc, 1, 0);
            tlp.Controls.Add(gbDichVu, 2, 0);
            split.Panel2.Controls.Add(tlp);

            // Thêm Split trước, thanh lọc sau để thanh lọc nằm phía trên.
            Controls.Add(split);
            Controls.Add(pnlLoc);
        }

        // ---------- Tải và hiển thị dữ liệu ----------

        private string MaTrangThaiDangChon()
        {
            switch (cbbTrangThai.SelectedIndex)
            {
                case 1: return "cho_kham";
                case 2: return "dang_kham";
                case 3: return "da_kham";
                default: return null;
            }
        }

        private static string TenTrangThai(string ma)
        {
            switch ((ma ?? "").Trim().ToLowerInvariant())
            {
                case "cho_kham": return "Chờ khám";
                case "dang_kham": return "Đang khám";
                case "da_kham": return "Đã khám";
                default: return ma;
            }
        }

        private void TaiDanhSach(string idChon)
        {
            try
            {
                DateTime? tu = dtpTu.Checked
                    ? dtpTu.Value.Date
                    : (DateTime?)null;

                DateTime? den = dtpDen.Checked
                    ? dtpDen.Value.Date
                    : (DateTime?)null;

                if (tu.HasValue && den.HasValue && tu.Value > den.Value)
                {
                    MessageBox.Show(
                        "Từ ngày phải trước hoặc bằng Đến ngày.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Luôn tải tất cả trước; ô tìm kiếm và trạng thái sẽ lọc kết quả.
                var ds = service.LayLichSu(null, tu, den);

                string tk = (txtTimKiem.Text ?? "").Trim();
                if (tk.Length > 0)
                {
                    ds = ds.Where(x =>
                        (x.MaBenhNhan ?? "").IndexOf(
                            tk, StringComparison.OrdinalIgnoreCase) >= 0
                        ||
                        (x.TenBenhNhan ?? "").IndexOf(
                            tk, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();
                }

                string trangThai = MaTrangThaiDangChon();
                if (trangThai != null)
                {
                    ds = ds.Where(x => string.Equals(
                        (x.TrangThai ?? "").Trim(),
                        trangThai,
                        StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                dgvLichSu.DataSource = ds;
                DinhDangLuoiLichSu();

                if (idChon != null)
                {
                    foreach (DataGridViewRow row in dgvLichSu.Rows)
                    {
                        var item = row.DataBoundItem as LichSuKCBItem;
                        if (item != null && item.Id == idChon)
                        {
                            if (row.Cells["MaPhieu"] != null)
                                dgvLichSu.CurrentCell = row.Cells["MaPhieu"];

                            break;
                        }
                    }
                }

                HienChiTiet();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi tải lịch sử KCB: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void DatTieuDe(
            DataGridView g,
            string ten,
            string tieuDe,
            string dinhDang = null)
        {
            var col = g.Columns[ten];
            if (col == null)
                return;

            col.HeaderText = tieuDe;

            if (dinhDang != null)
                col.DefaultCellStyle.Format = dinhDang;
        }

        private void DinhDangLuoiLichSu()
        {
            if (dgvLichSu.Columns["Id"] != null)
                dgvLichSu.Columns["Id"].Visible = false;

            if (dgvLichSu.Columns["MaBacSi"] != null)
                dgvLichSu.Columns["MaBacSi"].Visible = false;

            DatTieuDe(dgvLichSu, "MaPhieu", "Mã phiếu");
            DatTieuDe(dgvLichSu, "MaBenhNhan", "Mã BN");
            DatTieuDe(dgvLichSu, "TenBenhNhan", "Tên bệnh nhân");
            DatTieuDe(dgvLichSu, "NgayKham", "Ngày khám", "dd/MM/yyyy HH:mm");
            DatTieuDe(dgvLichSu, "TenBacSi", "Bác sĩ");
            DatTieuDe(dgvLichSu, "TrangThai", "Trạng thái");
            DatTieuDe(dgvLichSu, "ChanDoan", "Chẩn đoán");
            DatTieuDe(dgvLichSu, "SoLoaiThuoc", "Số loại thuốc");
            DatTieuDe(dgvLichSu, "SoDichVu", "Số dịch vụ");
            DatTieuDe(dgvLichSu, "TongTien", "Tổng tiền", "N0");
            DatTieuDe(dgvLichSu, "DaThanhToan", "Đã thanh toán");
        }

        private void dgvLichSu_CellFormatting(
            object sender,
            DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null)
                return;

            if (dgvLichSu.Columns[e.ColumnIndex].Name != "TrangThai")
                return;

            e.Value = TenTrangThai(e.Value.ToString());
            e.FormattingApplied = true;
        }

        private LichSuKCBItem ItemHienTai
        {
            get
            {
                return dgvLichSu.CurrentRow == null
                    ? null
                    : dgvLichSu.CurrentRow.DataBoundItem as LichSuKCBItem;
            }
        }

        private static decimal TinhTong(HoSoBenhAn hs)
        {
            var thuoc = hs.DonThuoc ?? new List<ChiTietThuoc>();
            var dichVu = hs.DichVuChiDinh ?? new List<ChiTietDichVu>();

            return thuoc.Sum(t => t.DonGia * t.SoLuong)
                 + dichVu.Sum(d => d.DonGia * d.SoLuong);
        }

        private void XoaChiTiet()
        {
            txtLyDo.Text = "";
            txtChanDoan.Text = "";

            dgvThuoc.DataSource = new List<ChiTietThuoc>();
            dgvDichVu.DataSource = new List<ChiTietDichVu>();
            DinhDangLuoiChiTiet();

            lblTongTien.Text = "Tổng tiền: 0 đ";
            lblThanhToan.Text = "";
            cbbHinhThuc.Enabled = false;
            btnThanhToan.Enabled = false;
        }

        private void DinhDangLuoiChiTiet()
        {
            DatTieuDe(dgvThuoc, "MaThuoc", "Mã");
            DatTieuDe(dgvThuoc, "TenThuoc", "Tên thuốc");
            DatTieuDe(dgvThuoc, "DonGia", "Đơn giá", "N0");
            DatTieuDe(dgvThuoc, "SoLuong", "SL");
            DatTieuDe(dgvThuoc, "CachDung", "Cách dùng");

            DatTieuDe(dgvDichVu, "MaDichVu", "Mã");
            DatTieuDe(dgvDichVu, "TenDichVu", "Tên dịch vụ");
            DatTieuDe(dgvDichVu, "DonGia", "Đơn giá", "N0");
            DatTieuDe(dgvDichVu, "SoLuong", "SL");
        }

        private void HienChiTiet()
        {
            var item = ItemHienTai;

            if (item == null || item.HoSo == null)
            {
                XoaChiTiet();
                return;
            }

            var hs = item.HoSo;

            txtLyDo.Text = hs.LyDoKham ?? "";
            txtChanDoan.Text = hs.ChanDoan ?? "";

            dgvThuoc.DataSource =
                (hs.DonThuoc ?? new List<ChiTietThuoc>()).ToList();

            dgvDichVu.DataSource =
                (hs.DichVuChiDinh ?? new List<ChiTietDichVu>()).ToList();

            DinhDangLuoiChiTiet();

            // Dùng cùng số tiền với bảng lịch sử.
            lblTongTien.Text =
                "Tổng tiền: " + item.TongTien.ToString("N0") + " đ";

            bool daThanhToan =
                hs.ThanhToan != null && hs.ThanhToan.DaThanhToan;

            if (daThanhToan)
            {
                lblThanhToan.Text = "Đã thanh toán";

                int idx = cbbHinhThuc.Items.IndexOf(
                    hs.ThanhToan.HinhThuc ?? "");

                cbbHinhThuc.SelectedIndex = idx >= 0 ? idx : 0;
                cbbHinhThuc.Enabled = false;
                btnThanhToan.Enabled = false;
            }
            else
            {
                bool daKham = hs.TrangThai == "da_kham";

                lblThanhToan.Text = daKham
                    ? "Chưa thanh toán"
                    : "Chưa thanh toán (hồ sơ chưa hoàn tất khám)";

                cbbHinhThuc.Enabled = daKham;
                btnThanhToan.Enabled = daKham;
            }
        }

        // ---------- Sự kiện ----------

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            txtTimKiem.Clear();
            dtpTu.Checked = false;
            dtpDen.Checked = false;
            cbbTrangThai.SelectedIndex = 0;

            // Chạy trực tiếp để chắc chắn danh sách được làm mới,
            // kể cả khi trạng thái vốn đã là "Tất cả".
            TaiDanhSach(null);
        }

        private void btnThanhToan_Click(object sender, EventArgs e)
        {
            var item = ItemHienTai;

            if (item == null || item.HoSo == null)
            {
                MessageBox.Show(
                    "Vui lòng chọn một hồ sơ!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (item.HoSo.TrangThai != "da_kham")
            {
                MessageBox.Show(
                    "Chỉ thanh toán được hồ sơ đã khám xong.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (item.DaThanhToan)
            {
                MessageBox.Show(
                    "Hồ sơ này đã được thanh toán.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                TaiDanhSach(item.Id);
                return;
            }

            decimal tong = item.TongTien;
            string hinhThuc = cbbHinhThuc.Text;
            string id = item.Id;

            DialogResult xacNhan = MessageBox.Show(
                "Xác nhận thanh toán " + tong.ToString("N0")
                + " đ cho phiếu " + item.MaPhieu
                + " bằng hình thức \"" + hinhThuc + "\"?",
                "Xác nhận thanh toán",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (xacNhan != DialogResult.Yes)
                return;

            try
            {
                var filter = Builders<HoSoBenhAn>.Filter.Where(h =>
                    h.Id == id
                    && h.TrangThai == "da_kham"
                    && (h.ThanhToan == null
                        || h.ThanhToan.DaThanhToan == false));

                var update = Builders<HoSoBenhAn>.Update.Set(
                    h => h.ThanhToan,
                    new ThongTinThanhToan
                    {
                        TongTien = tong,
                        HinhThuc = hinhThuc,
                        DaThanhToan = true
                    });

                var ketQua = collectionHoSo.UpdateOne(filter, update);

                if (ketQua.MatchedCount == 0)
                {
                    MessageBox.Show(
                        "Hồ sơ đã thay đổi hoặc đã được thanh toán. Danh sách sẽ được tải lại.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Thanh toán thành công!",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // Tải lại để checkbox Đã thanh toán phản ánh giá trị MongoDB.
                TaiDanhSach(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi thanh toán: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnQuayLai_Click(object sender, EventArgs e)
        {
            if (laFormChinh)
                Program.QuayVeMenu = true;

            Close();
        }
    }
}