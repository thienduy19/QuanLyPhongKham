using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Clinix
{
    public partial class frmQLBenhNhan : Form
    {
        private IMongoCollection<BenhNhan> collection;
        private IMongoCollection<HoSoBenhAn> collectionHoSo;

        public frmQLBenhNhan()
        {
            InitializeComponent();
            EnableControls(false);

            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("QuanLyPhongKhamDB");
            collection = database.GetCollection<BenhNhan>("benh_nhan");
            collectionHoSo = database.GetCollection<HoSoBenhAn>("ho_so_benh_an");
        }

        private void frmQLBenhNhan_Load(object sender, EventArgs e)
        {
            dgvDanhSachBN.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDanhSachBN.MultiSelect = false;
            dgvDanhSachBN.ReadOnly = true;
            dgvDanhSachBN.AllowUserToAddRows = false;
            dgvDanhSachBN.AllowUserToDeleteRows = false;

            btnLamMoi_Click(sender, e);
            EnableControls(false);
        }

        private void EnableControls(bool isEnabled)
        {
            txtHoTen.Enabled = isEnabled;
            txtDiaChi.Enabled = isEnabled;
            txtSDT.Enabled = isEnabled;
            radNam.Enabled = isEnabled;
            radNu.Enabled = isEnabled;
            dtpNgaySinh.Enabled = isEnabled;
            btnLuu.Enabled = isEnabled;
            txtMaBN.Enabled = isEnabled;
        }

        // Đổ danh sách lên lưới và định dạng cột theo TÊN cột (không dùng chỉ số)
        private void HienThiDanhSach(List<BenhNhan> ds)
        {
            dgvDanhSachBN.DataSource = ds;

            dgvDanhSachBN.Columns["Id"].Visible = false;
            dgvDanhSachBN.Columns["MaBenhNhan"].HeaderText = "Mã bệnh nhân";
            dgvDanhSachBN.Columns["TenBenhNhan"].HeaderText = "Tên bệnh nhân";
            dgvDanhSachBN.Columns["NgaySinh"].HeaderText = "Ngày sinh";
            dgvDanhSachBN.Columns["NgaySinh"].DefaultCellStyle.Format = "dd/MM/yyyy";
            dgvDanhSachBN.Columns["GioiTinh"].HeaderText = "Giới tính";
            dgvDanhSachBN.Columns["DiaChi"].HeaderText = "Địa chỉ";
            dgvDanhSachBN.Columns["SDT"].HeaderText = "Số điện thoại";

            dgvDanhSachBN.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDanhSachBN.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvDanhSachBN.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvDanhSachBN.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            try
            {
                HienThiDanhSach(collection.Find(new BsonDocument()).ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối MongoDB: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtMaBN.Clear();
            txtHoTen.Clear();
            txtDiaChi.Clear();
            txtSDT.Clear();
            txtTimKiem.Clear();
            radNam.Checked = false;
            radNu.Checked = false;
            dtpNgaySinh.CustomFormat = " ";
            dtpNgaySinh.Format = DateTimePickerFormat.Custom;
            dtpNgaySinh.Value = DateTime.Now;

            btnThem.Enabled = true;
            btnSua.Enabled = true;
            btnXoa.Enabled = true;
            btnLuu.Enabled = false;

            EnableControls(false);
        }

        private void btnThem_Click(object sender, EventArgs e)
        {
            EnableControls(true);
            txtMaBN.Clear();
            txtHoTen.Clear();
            txtDiaChi.Clear();
            txtSDT.Clear();
            radNam.Checked = false;
            radNu.Checked = false;
            dtpNgaySinh.Format = DateTimePickerFormat.Short;
            dtpNgaySinh.CustomFormat = "dd/MM/yyyy";
            dtpNgaySinh.Value = DateTime.Now;
            txtMaBN.Focus();

            btnThem.Enabled = false;
            btnSua.Enabled = false;
            btnXoa.Enabled = false;
            btnLuu.Enabled = true;
        }

        private void txtHoTen_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsLetter(e.KeyChar) && e.KeyChar != (char)Keys.Back && e.KeyChar != ' ') e.Handled = true;
        }

        private void txtSDT_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back) e.Handled = true;
        }

        // Kiểm tra dùng chung cho Lưu và Sửa
        private bool KiemTraHopLe()
        {
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtHoTen.Focus();
                return false;
            }
            if (!radNam.Checked && !radNu.Checked)
            {
                MessageBox.Show("Vui lòng chọn giới tính!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (txtSDT.Text.Trim().Length != 10)
            {
                MessageBox.Show("Số điện thoại phải đủ 10 chữ số!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSDT.Focus();
                return false;
            }
            if (dtpNgaySinh.Value.Date > DateTime.Today)
            {
                MessageBox.Show("Ngày sinh không được ở tương lai!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void btnLuu_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaBN.Text))
            {
                MessageBox.Show("Vui lòng nhập mã bệnh nhân!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMaBN.Focus();
                return;
            }
            if (!KiemTraHopLe()) return;

            string maBN = txtMaBN.Text.Trim();

            try
            {
                var benhNhanTonTai = collection.Find(b => b.MaBenhNhan == maBN).FirstOrDefault();
                if (benhNhanTonTai != null)
                {
                    MessageBox.Show("Mã bệnh nhân đã tồn tại! Vui lòng nhập mã khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMaBN.Focus();
                    return;
                }

                BenhNhan benhNhanMoi = new BenhNhan
                {
                    MaBenhNhan = maBN,
                    TenBenhNhan = txtHoTen.Text.Trim(),
                    NgaySinh = dtpNgaySinh.Value.Date,
                    GioiTinh = radNam.Checked ? "Nam" : "Nữ",
                    DiaChi = txtDiaChi.Text.Trim(),
                    SDT = txtSDT.Text.Trim()
                };

                collection.InsertOne(benhNhanMoi);

                MessageBox.Show("Thêm bệnh nhân thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnLamMoi_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            // Mã bệnh nhân lấy từ ô nhập (được điền khi click dòng trên lưới)
            string maBN = txtMaBN.Text.Trim();
            if (string.IsNullOrEmpty(maBN))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa bệnh nhân này không?\nToàn bộ hồ sơ bệnh án của bệnh nhân cũng sẽ bị xóa.",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var filter = Builders<BenhNhan>.Filter.Eq(bn => bn.MaBenhNhan, maBN);
                var deleteResult = collection.DeleteOne(filter);

                if (deleteResult.DeletedCount > 0)
                {
                    var deleteHoSoFilter = Builders<HoSoBenhAn>.Filter.Eq(hs => hs.MaBenhNhan, maBN);
                    collectionHoSo.DeleteMany(deleteHoSoFilter);
                    MessageBox.Show("Xóa bệnh nhân thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnLamMoi_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Bệnh nhân không tồn tại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvDanhSachBN_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvDanhSachBN.Rows[e.RowIndex];

            txtMaBN.Text = row.Cells["MaBenhNhan"].Value?.ToString();
            txtHoTen.Text = row.Cells["TenBenhNhan"].Value?.ToString();

            if (row.Cells["NgaySinh"].Value is DateTime ngaySinh
                && ngaySinh >= dtpNgaySinh.MinDate && ngaySinh <= dtpNgaySinh.MaxDate)
            {
                dtpNgaySinh.Format = DateTimePickerFormat.Short;
                dtpNgaySinh.Value = ngaySinh;
            }
            else
            {
                // Dữ liệu thiếu ngày sinh hợp lệ: đặt tạm hôm nay và nhắc người dùng chọn lại
                dtpNgaySinh.Format = DateTimePickerFormat.Short;
                dtpNgaySinh.Value = DateTime.Today;
                MessageBox.Show("Bệnh nhân này chưa có ngày sinh hợp lệ. Vui lòng chọn lại ngày sinh trước khi sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            bool nam = row.Cells["GioiTinh"].Value?.ToString() == "Nam";
            radNam.Checked = nam;
            radNu.Checked = !nam;

            txtDiaChi.Text = row.Cells["DiaChi"].Value?.ToString();
            txtSDT.Text = row.Cells["SDT"].Value?.ToString();

            EnableControls(true);
            btnLuu.Enabled = false;
            btnThem.Enabled = false;
            txtMaBN.Enabled = false;
        }

        private void btnSua_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaBN.Text))
            {
                MessageBox.Show("Vui lòng chọn bệnh nhân cần sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!KiemTraHopLe()) return;

            string maBN = txtMaBN.Text.Trim();

            try
            {
                string tenMoi = txtHoTen.Text.Trim();

                var filter = Builders<BenhNhan>.Filter.Eq(bn => bn.MaBenhNhan, maBN);
                var update = Builders<BenhNhan>.Update
                    .Set(bn => bn.TenBenhNhan, tenMoi)
                    .Set(bn => bn.NgaySinh, dtpNgaySinh.Value.Date)
                    .Set(bn => bn.GioiTinh, radNam.Checked ? "Nam" : "Nữ")
                    .Set(bn => bn.DiaChi, txtDiaChi.Text.Trim())
                    .Set(bn => bn.SDT, txtSDT.Text.Trim());

                var kq = collection.UpdateOne(filter, update);
                if (kq.MatchedCount == 0)
                {
                    MessageBox.Show("Bệnh nhân không tồn tại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Tên bệnh nhân được lưu trùng trong hồ sơ bệnh án nên phải cập nhật theo
                var hoSoFilter = Builders<HoSoBenhAn>.Filter.Eq(hs => hs.MaBenhNhan, maBN);
                var hoSoUpdate = Builders<HoSoBenhAn>.Update.Set(hs => hs.TenBenhNhan, tenMoi);
                collectionHoSo.UpdateMany(hoSoFilter, hoSoUpdate);

                MessageBox.Show("Cập nhật thông tin bệnh nhân thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnLamMoi_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTimKiem_Click(object sender, EventArgs e)
        {
            string searchTerm = txtTimKiem.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                MessageBox.Show("Vui lòng nhập mã hoặc tên để tìm kiếm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var builder = Builders<BenhNhan>.Filter;
                var filter = builder.Regex(bn => bn.MaBenhNhan, MongoHelper.Like(searchTerm)) |
                             builder.Regex(bn => bn.TenBenhNhan, MongoHelper.Like(searchTerm));

                var searchResults = collection.Find(filter).ToList();

                if (searchResults.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy bệnh nhân nào!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnLamMoi_Click(sender, e);
                }
                else
                {
                    HienThiDanhSach(searchResults);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tìm kiếm: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnXuatE_Click(object sender, EventArgs e)
        {
            if (dgvDanhSachBN.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Files|*.xlsx";
                sfd.Title = "Lưu file Excel";
                sfd.FileName = "DanhSachBenhNhan.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (ExcelPackage package = new ExcelPackage())
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Danh sách bệnh nhân");

                            // Chỉ xuất các cột đang hiện (bỏ cột Id ẩn)
                            var cols = dgvDanhSachBN.Columns.Cast<DataGridViewColumn>()
                                                    .Where(c => c.Visible)
                                                    .OrderBy(c => c.DisplayIndex)
                                                    .ToList();

                            for (int j = 0; j < cols.Count; j++)
                            {
                                var cell = worksheet.Cells[1, j + 1];
                                cell.Value = cols[j].HeaderText;
                                cell.Style.Font.Bold = true;
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            }

                            for (int i = 0; i < dgvDanhSachBN.Rows.Count; i++)
                            {
                                for (int j = 0; j < cols.Count; j++)
                                {
                                    worksheet.Cells[i + 2, j + 1].Value =
                                        dgvDanhSachBN.Rows[i].Cells[cols[j].Index].FormattedValue?.ToString();
                                }
                            }

                            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                            FileInfo fileInfo = new FileInfo(sfd.FileName);
                            package.SaveAs(fileInfo);
                        }
                        MessageBox.Show("Xuất file Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Có lỗi xảy ra: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        
        private void btnThoat_Click(object sender, EventArgs e)
        {
            Program.QuayVeMenu = true;
            this.Close();
        }
    }
}