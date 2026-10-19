using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Clinix
{
    public partial class ThemLichKham : Form
    {
        private IMongoCollection<BenhNhan> collectionBN;
        private IMongoCollection<DichVu> collectionDichVu;
        private IMongoCollection<HoSoBenhAn> collectionHoSo;
        private BindingList<ChiTietDichVu> dsDichVuChiDinh = new BindingList<ChiTietDichVu>();
        private string selectedBNId;
        private bool dangDoDuLieu = false;

        // Bệnh nhân có hồ sơ ở một trong các trạng thái này coi như đã lập phiếu, không hiện để cấp số nữa.
        // Muốn cho bệnh nhân đã khám xong (da_kham) quay lại khám lần mới thì bỏ "da_kham" khỏi mảng này.
        private static readonly string[] TrangThaiDaCoPhieu = { "cho_kham", "dang_kham", "da_kham" };

        private static readonly Lazy<ConnectionMultiplexer> redisLazy =
            new Lazy<ConnectionMultiplexer>(() =>
                ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false,connectTimeout=5000"));

        private static IDatabase RedisDb => redisLazy.Value.GetDatabase(0);

        public ThemLichKham()
        {
            InitializeComponent();
            var database = new MongoClient("mongodb://localhost:27017").GetDatabase("QuanLyPhongKhamDB");
            collectionBN = database.GetCollection<BenhNhan>("benh_nhan");
            collectionDichVu = database.GetCollection<DichVu>("dich_vu");
            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
        }

        private void frmThemLichKham_Load(object sender, EventArgs e)
        {
            LoadBenhNhanData();
            LoadDanhSachDichVu();
            CauHinhLuoiDichVuChiDinh();
            txtSoLuong.Text = "1";
            CapNhatTongTien();
        }

        private void HienThiBenhNhan(List<BenhNhan> ds)
        {
            dgvBenhNhan.DataSource = ds.Select(bn => new
            {
                bn.MaBenhNhan,
                bn.TenBenhNhan,
                bn.SDT,
                bn.DiaChi,
                bn.GioiTinh
            }).ToList();
            CustomizeDgvBenhNhan();
        }

        // Chỉ giữ bệnh nhân chưa có hồ sơ nào ở các trạng thái trong TrangThaiDaCoPhieu
        private FilterDefinition<BenhNhan> LocChuaCoPhieu()
        {
            var locHoSo = Builders<HoSoBenhAn>.Filter.In(h => h.TrangThai, TrangThaiDaCoPhieu);
            var daCoPhieu = collectionHoSo.Distinct(h => h.MaBenhNhan, locHoSo).ToList();
            return Builders<BenhNhan>.Filter.Nin(bn => bn.MaBenhNhan, daCoPhieu);
        }

        private void LoadBenhNhanData()
        {
            try
            {
                HienThiBenhNhan(collectionBN.Find(LocChuaCoPhieu()).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối MongoDB: " + ex.Message);
            }
        }

        // Chỉ lấy dịch vụ, không lẫn thuốc (cùng cách lọc với QuanLyDichVu)
        private void LoadDanhSachDichVu()
        {
            try
            {
                var filter = Builders<DichVu>.Filter.Ne(dv => dv.Loai, "Thuoc");
                var ds = collectionDichVu.Find(filter).SortBy(dv => dv.TenDichVu).ToList();

                if (ds.Count == 0)
                {
                    MessageBox.Show("Chưa có dịch vụ nào trong collection dich_vu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                cbbDichVu.DisplayMember = "TenDichVu";
                cbbDichVu.ValueMember = "MaDichVu";
                cbbDichVu.DataSource = ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dịch vụ: " + ex.Message);
            }
        }

        private void CauHinhLuoiDichVuChiDinh()
        {
            dgvDichVuChiDinh.DataSource = dsDichVuChiDinh;
            dgvDichVuChiDinh.ReadOnly = true;
            dgvDichVuChiDinh.AllowUserToAddRows = false;
            dgvDichVuChiDinh.AllowUserToDeleteRows = false;
            dgvDichVuChiDinh.MultiSelect = false;
            dgvDichVuChiDinh.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDichVuChiDinh.RowHeadersVisible = false;
            dgvDichVuChiDinh.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvDichVuChiDinh.Columns["MaDichVu"].HeaderText = "Mã";
            dgvDichVuChiDinh.Columns["TenDichVu"].HeaderText = "Tên dịch vụ";
            dgvDichVuChiDinh.Columns["DonGia"].HeaderText = "Đơn giá";
            dgvDichVuChiDinh.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgvDichVuChiDinh.Columns["SoLuong"].HeaderText = "SL";
        }

        private void CapNhatTongTien()
        {
            decimal tong = dsDichVuChiDinh.Sum(d => d.DonGia * d.SoLuong);
            lblTongTien.Text = "Tạm tính: " + tong.ToString("N0") + " đ";
        }

        private void btnThemDV_Click(object sender, EventArgs e)
        {
            var dv = cbbDichVu.SelectedItem as DichVu;
            if (dv == null)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(txtSoLuong.Text.Trim(), out int soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên lớn hơn 0!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoLuong.Focus();
                return;
            }

            var daCo = dsDichVuChiDinh.FirstOrDefault(d => d.MaDichVu == dv.MaDichVu);
            if (daCo != null)
            {
                daCo.SoLuong += soLuong;
                dgvDichVuChiDinh.Refresh();
            }
            else
            {
                dsDichVuChiDinh.Add(new ChiTietDichVu
                {
                    MaDichVu = dv.MaDichVu,
                    TenDichVu = dv.TenDichVu,
                    DonGia = dv.DonGia,
                    SoLuong = soLuong
                });
            }

            txtSoLuong.Text = "1";
            CapNhatTongTien();
        }

        private void btnXoaDV_Click(object sender, EventArgs e)
        {
            var dv = dgvDichVuChiDinh.CurrentRow?.DataBoundItem as ChiTietDichVu;
            if (dv == null)
            {
                MessageBox.Show("Vui lòng chọn dịch vụ cần bỏ trong danh sách chỉ định!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dsDichVuChiDinh.Remove(dv);
            CapNhatTongTien();
        }

        private void dgvBenhNhan_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvBenhNhan.Rows[e.RowIndex];

            string ma = row.Cells["MaBenhNhan"].Value?.ToString() ?? "";
            string ten = row.Cells["TenBenhNhan"].Value?.ToString() ?? "";
            string sdt = row.Cells["SDT"].Value?.ToString() ?? "";
            string diaChi = row.Cells["DiaChi"].Value?.ToString() ?? "";
            string gioiTinh = row.Cells["GioiTinh"].Value?.ToString() ?? "";

            selectedBNId = ma;

            dangDoDuLieu = true;
            try
            {
                txtMaBenhNhan.Text = ma;
                txtTenBenhNhan.Text = ten;
                txtSDT.Text = sdt;
                lblDiaChi.Text = diaChi;
                lblGioiTinh.Text = gioiTinh;
            }
            finally { dangDoDuLieu = false; }
        }

        private void txtTenBenhNhan_TextChanged(object sender, EventArgs e)
        {
            if (dangDoDuLieu) return;
            XoaLuaChon();
            ThucHienTimKiem();
        }

        private void txtSDT_TextChanged(object sender, EventArgs e)
        {
            if (dangDoDuLieu) return;
            XoaLuaChon();
            ThucHienTimKiem();
        }

        // Người dùng gõ tìm kiếm nghĩa là đã bỏ bệnh nhân đang chọn
        private void XoaLuaChon()
        {
            selectedBNId = null;
            txtMaBenhNhan.Text = string.Empty;
            lblDiaChi.Text = string.Empty;
            lblGioiTinh.Text = string.Empty;
        }

        private void ThucHienTimKiem()
        {
            string tenBenhNhan = txtTenBenhNhan.Text.Trim();
            string sdt = txtSDT.Text.Trim();

            try
            {
                var builder = Builders<BenhNhan>.Filter;
                var filter = LocChuaCoPhieu();

                if (!string.IsNullOrEmpty(tenBenhNhan))
                    filter &= builder.Regex(bn => bn.TenBenhNhan, MongoHelper.Like(tenBenhNhan));

                if (!string.IsNullOrEmpty(sdt))
                    filter &= builder.Regex(bn => bn.SDT, MongoHelper.Like(sdt));

                HienThiBenhNhan(collectionBN.Find(filter).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tìm kiếm bệnh nhân: " + ex.Message);
            }
        }

        // Cấp số cho hôm nay: ZADD NX (score = thời điểm cấp số, unix ms);
        // số thứ tự hiển thị = ZRANK + 1, không lưu số thứ tự riêng.
        // Cấp số xong tạo luôn hồ sơ trạng thái "cho_kham" kèm lý do khám, dịch vụ chỉ định và tổng tiền.
        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedBNId))
            {
                MessageBox.Show("Vui lòng chọn một bệnh nhân từ danh sách để cấp số!", "Chưa chọn bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string todayStr = DateTime.Now.ToString("yyyyMMdd");
                string keyHangDoi = $"phongkham:hangdoi:{todayStr}";

                // Member chỉ là mã bệnh nhân; tên tra lại từ MongoDB khi cần
                double score = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                bool daThem = RedisDb.SortedSetAdd(keyHangDoi, selectedBNId, score, When.NotExists);

                long? rank = RedisDb.SortedSetRank(keyHangDoi, selectedBNId, Order.Ascending);
                string soThuTu = rank.HasValue ? (rank.Value + 1).ToString() : "?";

                if (!daThem)
                {
                    MessageBox.Show($"Bệnh nhân này đã có trong hàng đợi hôm nay.\nSố thứ tự hiện tại: {soThuTu}", "Đã cấp số", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var dichVuChiDinh = dsDichVuChiDinh.ToList();
                decimal tong = dichVuChiDinh.Sum(d => d.DonGia * d.SoLuong);

                var hoSo = new HoSoBenhAn
                {
                    MaPhieu = "", // gán số thứ tự ngay trước khi insert, bên dưới
                    MaBenhNhan = selectedBNId,
                    TenBenhNhan = txtTenBenhNhan.Text.Trim(),
                    MaBacSi = "", // bác sĩ được chọn ở màn hình Khám Bệnh
                    NgayKham = DateTime.Now,
                    TrangThai = "cho_kham",
                    LyDoKham = txtLyDoKham.Text.Trim(),
                    ChanDoan = "",
                    SoThuTu = 0,
                    DichVuChiDinh = dichVuChiDinh,
                    ThanhToan = new ThongTinThanhToan { TongTien = tong, HinhThuc = "", DaThanhToan = false }
                };

                try
                {
                    hoSo.MaPhieu = MaPhieuHelper.TaoMaPhieuTiepTheo(collectionHoSo);
                    collectionHoSo.InsertOne(hoSo);
                }
                catch
                {
                    // Không tạo được hồ sơ thì gỡ khỏi hàng đợi để hai nơi không lệch nhau
                    RedisDb.SortedSetRemove(keyHangDoi, selectedBNId);
                    throw;
                }

                RedisDb.KeyExpire(keyHangDoi, TimeSpan.FromHours(24));

                MessageBox.Show(
                    $"Cấp số thành công!\n\nBệnh nhân: {txtTenBenhNhan.Text}\nSỐ THỨ TỰ: {soThuTu}\nMã phiếu: {hoSo.MaPhieu}\nTạm tính: {tong:N0} đ",
                    "Hàng đợi Redis", MessageBoxButtons.OK, MessageBoxIcon.Information);

                btnReset_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cấp số. Hãy chắc chắn đã bật Redis Server và MongoDB!\nChi tiết: {ex.Message}");
            }
        }

        private void btnIn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chức năng in ấn báo cáo đã được lược bỏ để tập trung vào NoSQL.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CustomizeDgvBenhNhan()
        {
            dgvBenhNhan.Columns["MaBenhNhan"].HeaderText = "Mã Bệnh Nhân";
            dgvBenhNhan.Columns["TenBenhNhan"].HeaderText = "Tên Bệnh Nhân";
            dgvBenhNhan.Columns["SDT"].HeaderText = "Số Điện Thoại";
            dgvBenhNhan.Columns["DiaChi"].HeaderText = "Địa Chỉ";
            dgvBenhNhan.Columns["GioiTinh"].HeaderText = "Giới Tính";

            dgvBenhNhan.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvBenhNhan.EnableHeadersVisualStyles = false;
            dgvBenhNhan.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvBenhNhan.ColumnHeadersDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvBenhNhan.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvBenhNhan.ColumnHeadersDefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Bold);

            foreach (DataGridViewColumn column in dgvBenhNhan.Columns)
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgvBenhNhan.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvBenhNhan.DefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Regular);
            dgvBenhNhan.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvBenhNhan.RowHeadersVisible = false;
            dgvBenhNhan.GridColor = Color.LightGray;
            dgvBenhNhan.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvBenhNhan.BackgroundColor = Color.White;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dangDoDuLieu = true;
            try
            {
                txtMaBenhNhan.Text = string.Empty;
                txtTenBenhNhan.Text = string.Empty;
                txtSDT.Text = string.Empty;
                lblDiaChi.Text = string.Empty;
                lblGioiTinh.Text = string.Empty;
                selectedBNId = null;
            }
            finally { dangDoDuLieu = false; }

            txtLyDoKham.Text = string.Empty;
            txtSoLuong.Text = "1";
            dsDichVuChiDinh.Clear();
            CapNhatTongTien();

            LoadBenhNhanData();
            dgvBenhNhan.ClearSelection();
        }
        private void btnThoat_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }

        private void btnQuayLai_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }
    }
}