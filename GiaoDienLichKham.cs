using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clinix;

namespace CLinix
{
    public partial class GiaoDienLichKham : Form

    {
        private DataClasses2DataContext db = new DataClasses2DataContext();

        public GiaoDienLichKham()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
            LoadTrangThai();
            if (dgvPatients.Columns["Edit"] == null)
            {
                if (dgvPatients.Columns["Edit"] == null)
                {
                    DataGridViewButtonColumn btnEdit = new DataGridViewButtonColumn
                    {
                        Name = "Edit",
                        HeaderText = "Edit",
                        Text = "Chỉnh sửa",
                        UseColumnTextForButtonValue = true
                    };

                    dgvPatients.Columns.Add(btnEdit);
                }
            }

            dtpFGioBatDau.Format = DateTimePickerFormat.Custom;
            dtpFGioBatDau.CustomFormat = "HH:mm";
            dtpFGioBatDau.ShowUpDown = true;

            dtpFGioKetThuc.Format = DateTimePickerFormat.Custom;
            dtpFGioKetThuc.CustomFormat = "HH:mm";
            dtpFGioKetThuc.ShowUpDown = true;

            txtSearch.Text = "Vui lòng nhập tên bệnh nhân hoặc số điện thoại";
            txtSearch.ForeColor = Color.Gray;

            txtSearch.Enter += txtSearch_MouseEnter;
            txtSearch.Leave += txtSearch_MouseLeave;

            CustomizeDataGridView();
        }

        private void LoadData()
        {
            using (DataClasses2DataContext db = new DataClasses2DataContext())
            {
                var appointments = from lichhen in db.LichHens
                                   join benhnhan in db.BenhNhans
                                   on lichhen.MaBenhNhan equals benhnhan.MaBenhNhan
                                   orderby lichhen.NgayHen descending
                                   select new
                                   {
                                       MaLichHen = lichhen.MaLichHen,
                                       TenBenhNhan = benhnhan.TenBenhNhan,
                                       SDT = benhnhan.SDT,
                                       NgayHen = lichhen.NgayHen,
                                       KhungGio = lichhen.KhungGio,
                                       TrangThai = lichhen.TrangThai
                                   };

                dgvPatients.DataSource = appointments.ToList();
            }
        }
        private void LoadTrangThai()
        {
            using (DataClasses2DataContext db = new DataClasses2DataContext())
            {
                var statuses = db.LichHens.Select(lh => lh.TrangThai).Distinct().ToList();
                clbStatus.Items.AddRange(statuses.ToArray());
            }
        }
        private void btnFilter_Click(object sender, EventArgs e)
        {
            DateTime startDateTime = dtpFNgayBatDau.Value.Date + dtpFGioBatDau.Value.TimeOfDay;
            DateTime endDateTime = dtpFNgayKetThuc.Value.Date + dtpFGioKetThuc.Value.TimeOfDay;

            if (startDateTime > endDateTime)
            {
                MessageBox.Show("Thời gian bắt đầu phải trước thời gian kết thúc",
                                "Khung thời gian không họp lệ",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            List<string> selectedStatuses = clbStatus.CheckedItems.Cast<string>().ToList();
            using (DataClasses2DataContext db = new DataClasses2DataContext())
            {
                var filteredResults = from lichhen in db.LichHens
                                      join benhnhan in db.BenhNhans
                                      on lichhen.MaBenhNhan equals benhnhan.MaBenhNhan
                                      where lichhen.NgayHen >= startDateTime
                                            && lichhen.NgayHen <= endDateTime
                                            && (selectedStatuses.Count == 0 || selectedStatuses.Contains(lichhen.TrangThai))
                                      orderby lichhen.NgayHen descending
                                      select new
                                      {
                                          MaLichHen = lichhen.MaLichHen,
                                          TenBenhNhan = benhnhan.TenBenhNhan,
                                          SDT = benhnhan.SDT,
                                          NgayHen = lichhen.NgayHen,
                                          KhungGio = lichhen.KhungGio,
                                          TrangThai = lichhen.TrangThai
                                      };
                dgvPatients.DataSource = filteredResults.ToList();
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            for (int i = 0; i < clbStatus.Items.Count; i++)
            {
                clbStatus.SetItemChecked(i, false);
            }
            dtpFNgayBatDau.Value = DateTime.Today;
            dtpFNgayKetThuc.Value = DateTime.Today;
            dtpFGioBatDau.Value = DateTime.Today;
            dtpFGioKetThuc.Value = DateTime.Today.AddHours(23).AddMinutes(59); 
            LoadData();
        }

        private void dgvPatients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvPatients.Columns[e.ColumnIndex].Name == "Edit")
            {
                var row = dgvPatients.Rows[e.RowIndex];
                string trangThai = row.Cells["TrangThai"].Value?.ToString();
                DateTime ngayHen = Convert.ToDateTime(row.Cells["NgayHen"].Value);

                if ((trangThai == "Đã hoàn thành" || trangThai == "Đã hoàn thành, tái khám" || trangThai == "Bị hủy")
                    && ngayHen < DateTime.Today)
                {
                    MessageBox.Show("Lịch hẹn này đã bị khóa và không thể chỉnh sửa.",
                                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maLichHen = row.Cells["MaLichHen"].Value.ToString();
                string tenBenhNhan = row.Cells["TenBenhNhan"].Value.ToString();
                string sdt = row.Cells["SDT"].Value.ToString();
                DateTime ngayKham = Convert.ToDateTime(row.Cells["NgayHen"].Value);
                string khungGio = row.Cells["KhungGio"].Value.ToString();
                string trangThaiCurrent = row.Cells["TrangThai"].Value.ToString();

                ChinhSuaLichKham editForm = new ChinhSuaLichKham(
                    maLichHen, tenBenhNhan, sdt, ngayKham, khungGio, trangThaiCurrent
                );
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadData(); 
                }
            }
        }
        private void txtSearch_MouseEnter(object sender, EventArgs e)
        {

            if (txtSearch.Text == "Vui lòng nhập tên bệnh nhân hoặc số điện thoại")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Black; 
            }
        }
        private void txtSearch_MouseLeave(object sender, EventArgs e)
        {
            if (txtSearch.Text == "Vui lòng nhập tên bệnh nhân hoặc số điện thoại")
            {
                txtSearch.Text = "";
                txtSearch.ForeColor = Color.Gray; 
            }
        }
        private void CustomizeDataGridView()
        {
            dgvPatients.Columns[0].HeaderText = "Mã Lịch Hẹn";
            dgvPatients.Columns[1].HeaderText = "Tên Bệnh Nhân";
            dgvPatients.Columns[2].HeaderText = "Số Điện Thoại";
            dgvPatients.Columns[3].HeaderText = "Ngày Hẹn";
            dgvPatients.Columns[4].HeaderText = "Khung Giờ";
            dgvPatients.Columns[5].HeaderText = "Trạng Thái";
            dgvPatients.Columns[6].HeaderText = "Edit";
            dgvPatients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPatients.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            foreach (DataGridViewColumn column in dgvPatients.Columns)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
            dgvPatients.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvPatients.EnableHeadersVisualStyles = false; // Cho phép tùy chỉnh header
            dgvPatients.ColumnHeadersDefaultCellStyle.BackColor = Color.AliceBlue;
            dgvPatients.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvPatients.ColumnHeadersDefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Bold);
            dgvPatients.DefaultCellStyle.Font = new Font("Times New Roman", 12, FontStyle.Regular);
            dgvPatients.BackgroundColor = Color.White;
            dgvPatients.GridColor = Color.LightGray;
            dgvPatients.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPatients.RowHeadersVisible = false; 
            dgvPatients.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }
        private void btnTim_Click(object sender, EventArgs e)
        {
            string searchText = txtSearch.Text.Trim();

            using (DataClasses2DataContext db = new DataClasses2DataContext())
            {
                var searchResults = from lichhen in db.LichHens
                                    join benhnhan in db.BenhNhans
                                    on lichhen.MaBenhNhan equals benhnhan.MaBenhNhan
                                    where benhnhan.TenBenhNhan.Contains(searchText) ||
                                          benhnhan.SDT.Contains(searchText)
                                    orderby lichhen.NgayHen descending
                                    select new
                                    {
                                        MaLichHen = lichhen.MaLichHen,
                                        TenBenhNhan = benhnhan.TenBenhNhan,
                                        SDT = benhnhan.SDT,
                                        NgayHen = lichhen.NgayHen,
                                        KhungGio = lichhen.KhungGio,
                                        TrangThai = lichhen.TrangThai
                                    };
                dgvPatients.DataSource = searchResults.ToList();
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {

            ThemLichKham fAdd = new ThemLichKham();
            fAdd.Show();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            frmQuanLyThongKeLichKham fTK = new frmQuanLyThongKeLichKham();
            fTK.Show();
        }
    }
}








