using CLinix;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Clinix
{
    public partial class Menu : Form
    {
        private Timer dongHo;
        public Menu()
        {
            InitializeComponent();
            CaiDatChaoMungVaDongHo();
        }
        private void CaiDatChaoMungVaDongHo()
        {
            CapNhatLoiChao();

            dongHo = new Timer();
            dongHo.Interval = 1000; 
            dongHo.Tick += DongHo_Tick;
            dongHo.Start();
        }
        private void DongHo_Tick(object sender, EventArgs e)
        {
            lblDongHo.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void CapNhatLoiChao()
        {
            int gio = DateTime.Now.Hour;

            if (gio >= 5 && gio < 12)
            {
                lblChaoMungg.Text = "Chào buổi sáng!";
            }
            else if (gio >= 12 && gio < 18)
            {
                lblChaoMungg.Text = "Chào buổi chiều!";
            }
            else
            {
                lblChaoMungg.Text = "Chào buổi tối!";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MonthCalendar lichThang = new MonthCalendar();
            lichThang.Dock = DockStyle.Fill;
            lichThang.MaxSelectionCount = 1;
            lichThang.CalendarDimensions = new System.Drawing.Size(1, 1);

            panel2.Controls.Add(lichThang);

            try
            {
                DataClasses2DataContext db = new DataClasses2DataContext();

                db.Connection.Open();
                int employeeCount = db.NguoiDungs.Count();
                lblNV.Text = $"{employeeCount}";

                db.Connection.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxIcon.Error);
            }
        }

        private void quảnLýDịchVụToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QuanLyDichVu quanLyDichVu = new QuanLyDichVu();
            ShowFormInPanel(quanLyDichVu);
        }
        private void ShowFormInPanel(Form form)
        {
            panel.Controls.Clear(); 
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.AutoScaleMode = AutoScaleMode.None; 
            form.Dock = DockStyle.Fill; 
            panel.Controls.Add(form);
            form.Show();
        }

        private void quảnLýThuốcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            QuanLyThuoc quanLyThuoc = new QuanLyThuoc();
            ShowFormInPanel(quanLyThuoc); 
        }

        private void phiếuChỉĐịnhToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChiDinhDichVu cd = new ChiDinhDichVu();
            ShowFormInPanel(cd);
        }

        private void phiếuKhâmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KhamBenh kb = new KhamBenh();
            ShowFormInPanel(kb);
        }

        private void toaThuốcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KeDonThuoc kdt = new KeDonThuoc();
            ShowFormInPanel(kdt);
        }

        private void quảnLýBệnhNhânToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmQLBenhNhan qLBenhNhan = new frmQLBenhNhan();
            ShowFormInPanel(qLBenhNhan);
        }

        private void trangChủToolStripMenuItem_Click(object sender, EventArgs e)
        {                    
            Menu trangChu = new Menu();
            trangChu.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            frmDanhSach dsnv = new frmDanhSach();
            ShowFormInPanel(dsnv);
        }

        private void quảnLýThanhToánToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmQLThanhToan qltt = new frmQLThanhToan();
            ShowFormInPanel(qltt);
        }

        private void quảnLýLịchHẹnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GiaoDienLichKham lk = new GiaoDienLichKham();
            ShowFormInPanel(lk);
        }

        private void ngườiDùngToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CapTaiKhoan formPatient = new CapTaiKhoan();
            ShowFormInPanel(formPatient);
        }

        private void quảnLýNhânViênToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmDanhSach danhSachNV = new frmDanhSach();
            ShowFormInPanel(danhSachNV);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
