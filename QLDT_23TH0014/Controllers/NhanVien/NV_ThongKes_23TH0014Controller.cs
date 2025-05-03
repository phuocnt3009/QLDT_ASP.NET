using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLDT_23TH0014.Filters;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Controllers.NhanVien
{
    public class NV_ThongKes_23TH0014Controller : Controller
    {
         readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Thống kê số lượng và giá trị đơn hàng theo trạng thái
        [AuthenticationFilter("AD", "NV")]
        public void ThongKeDonHang()
        {
            var use = Session["use"] as NguoiDung;
            var donDatHangs = db.DonDatHangs.AsQueryable();
            var chiTietDDHs = db.ChiTietDDHs.AsQueryable();
            if (use.VaiTro == "NV")
            {
                donDatHangs = donDatHangs.Where(d => d.MaNhanVien == use.MaNguoiDung);
                chiTietDDHs = chiTietDDHs.Where(ct => ct.DonDatHang.MaNhanVien == use.MaNguoiDung);
            }
            ViewBag.SLDonChoXacNhan = donDatHangs.Count(d => d.TinhTrang == 0);
            ViewBag.SLDonDangGiao = donDatHangs.Count(d => d.TinhTrang == 1);
            ViewBag.SLDonThanhCong = donDatHangs.Count(d => d.TinhTrang == 2);
            ViewBag.SLDonDaHuy = donDatHangs.Count(d => d.TinhTrang == 3);
            ViewBag.GTDonChoXacNhan = chiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang == 0)
                                                    .Sum(ct => (decimal?)ct.SoLuong * ct.SanPham.GiaBan) ?? 0;
            ViewBag.GTDonDangGiao = chiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang == 1)
                                                  .Sum(ct => (decimal?)ct.SoLuong * ct.SanPham.GiaBan) ?? 0;
            ViewBag.GTDonThanhCong = chiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang == 2)
                                                   .Sum(ct => (decimal?)ct.SoLuong * ct.SanPham.GiaBan) ?? 0;
            ViewBag.GTDonDaHuy = chiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang == 3)
                                               .Sum(ct => (decimal?)ct.SoLuong * ct.SanPham.GiaBan) ?? 0;
        }
        // Thống kê sản phẩm bán chạy
        [AuthenticationFilter("AD", "NV")]
        public void SanPhamBanChay()
        {
            var top5SanPham = db.ChiTietDDHs
                .Where(ct => ct.DonDatHang.TinhTrang < 3) //Không tính đơn hàng đã hủy
                .GroupBy(c => c.MaSP)
                .Select(g => new
                {
                    g.FirstOrDefault().SanPham.TenSP,
                    SoLuong = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(5)
                .ToList();

            // Chuẩn bị dữ liệu cho view
            ViewBag.TopSanPhamLabels = top5SanPham.Select(x => x.TenSP).ToList();
            ViewBag.TopSanPhamData = top5SanPham.Select(x => x.SoLuong).ToList();
        }

        // Thống kê sản phẩm bán chạy nâng cao
        [AuthenticationFilter("AD", "NV")]
        [HttpGet]
        public ActionResult SanPhamBanChay_NC(DateTime? tuNgay, DateTime? denNgay, bool kieuThongKe = false, string maHSX = "", int sl = 5)
        {
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;  
            ViewBag.KieuThongKe = kieuThongKe;
            ViewBag.HangSanXuat = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", maHSX);
            ViewBag.SoLuong = sl;

            var query = db.ChiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang < 3).AsQueryable();  // Chỉ tính đơn chưa hủy

            if (tuNgay.HasValue)
                query = query.Where(ct => ct.DonDatHang.Ngay >= tuNgay.Value);

            if (denNgay.HasValue)
                query = query.Where(ct => ct.DonDatHang.Ngay <= denNgay.Value);

            if (!string.IsNullOrEmpty(maHSX))
                query = query.Where(ct => ct.SanPham.MaHSX == maHSX);

            // Thống kê theo số lượng hoặc doanh thu
            var topSanPham = query
                .GroupBy(c => c.MaSP)
                .Select(g => new
                {
                    g.FirstOrDefault().SanPham.TenSP,
                    SoLuong = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.SoLuong * x.SanPham.GiaBan)
                })
                .OrderByDescending(x => kieuThongKe ? x.DoanhThu : x.SoLuong)
                .Take(sl)
                .ToList();

            ViewBag.TopSanPhamLabels = topSanPham.Select(x => x.TenSP).ToList();
            ViewBag.TopSanPhamData = kieuThongKe ? topSanPham.Select(x => x.DoanhThu).ToList() : topSanPham.Select(x => x.SoLuong).ToList();

            return View();
        }
        [AuthenticationFilter("AD", "NV")]
        public void DoanhThuTheoKy()
        {
            // Tạo danh sách 6 tháng gần nhất
            var last5Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Today.AddMonths(-i))
                .Select(d => new { Thang = d.Month, Nam = d.Year })
                .OrderBy(x => x.Nam).ThenBy(x => x.Thang)
                .ToList();

            // Lấy doanh thu của đơn đã thành công (TinhTrang == 2)
            var doanhThu = db.DonDatHangs
                .Where(d => d.TinhTrang == 2)
                .GroupBy(d => new { d.Ngay.Year, d.Ngay.Month })
                .Select(g => new
                {
                    Thang = g.Key.Month,
                    Nam = g.Key.Year,
                    DoanhThu = g.SelectMany(d => d.ChiTietDDHs).Sum(ct => ct.SoLuong * ct.SanPham.GiaBan)
                })
                .ToList();

            // Ghép với danh sách 5 tháng gần nhất, nếu không có doanh thu thì gán 0
            var doanhThuTheoThang = last5Months
                .Select(m => new
                {
                    m.Thang,
                    m.Nam,
                    DoanhThu = doanhThu.FirstOrDefault(d => d.Thang == m.Thang && d.Nam == m.Nam)?.DoanhThu ?? 0
                })
                .ToList();

            // Chuẩn bị dữ liệu cho view
            ViewBag.ThangLabels = doanhThuTheoThang.Select(t => $"{t.Thang}/{t.Nam}").ToList();
            ViewBag.DoanhThuData = doanhThuTheoThang.Select(t => t.DoanhThu).ToList();
        }
        [AuthenticationFilter("AD", "NV")]
        public ActionResult DoanhThuTheoKy_NC(string kieuKy = "Thang", int soKy = 6)
        {
            ViewBag.KieuKy = kieuKy;
            ViewBag.SoKy = soKy;

            // Ngày hôm nay
            var today = DateTime.Today;

            // Danh sách kỳ cần thống kê
            var danhSachKy = new List<(string Label, DateTime Start, DateTime End)>();

            for (int i = 0; i < soKy; i++)
            {
                DateTime start, end;
                string label = "";

                switch (kieuKy)
                {
                    case "Nam":
                        var year = today.AddYears(-i).Year;
                        start = new DateTime(year, 1, 1);
                        end = new DateTime(year, 12, 31);
                        label = $"Năm {year}";
                        break;

                    case "Quy":
                        var currentQ = (today.Month - 1) / 3 + 1;
                        var qDate = today.AddMonths(-3 * i);
                        var q = (qDate.Month - 1) / 3 + 1;
                        var qYear = qDate.Year;
                        start = new DateTime(qYear, (q - 1) * 3 + 1, 1);
                        end = start.AddMonths(3).AddDays(-1);
                        label = $"Q{q}/{qYear}";
                        break;

                    case "Tuan":
                        var startOfWeek = today.AddDays(-7 * i - (int)today.DayOfWeek + (int)DayOfWeek.Monday);
                        start = startOfWeek;
                        end = start.AddDays(6);
                        label = $"Tuần {start:dd/MM} - {end:dd/MM}";
                        break;

                    case "Ngay":
                        var day = today.AddDays(-i);
                        start = day;
                        end = day;
                        label = day.ToString("dd/MM");
                        break;

                    default: // "Thang"
                        var monthDate = today.AddMonths(-i);
                        start = new DateTime(monthDate.Year, monthDate.Month, 1);
                        end = start.AddMonths(1).AddDays(-1);
                        label = $"{monthDate.Month}/{monthDate.Year}";
                        break;
                }
                danhSachKy.Add((label, start, end));
            }

            // Đảo ngược để hiển thị theo thứ tự tăng dần
            danhSachKy.Reverse();

            // Lấy dữ liệu đơn hàng
            var donHoanThanh = db.DonDatHangs
                .Where(d => d.TinhTrang == 2)
                .Select(d => new
                {
                    d.Ngay,
                    TongTien = d.ChiTietDDHs.Sum(ct => ct.SoLuong * ct.SanPham.GiaBan)
                })
                .ToList();

            // Tính doanh thu theo từng kỳ
            var ketQua = danhSachKy
                .Select(k => new
                {
                    k.Label,
                    DoanhThu = donHoanThanh
                        .Where(d => d.Ngay >= k.Start && d.Ngay <= k.End)
                        .Sum(d => d.TongTien)
                })
                .ToList();

            // Chuẩn bị dữ liệu cho view
            ViewBag.ThangLabels = ketQua.Select(x => x.Label).ToList();
            ViewBag.DoanhThuData = ketQua.Select(x => x.DoanhThu).ToList();

            return View();
        }

        [AuthenticationFilter("AD", "NV")]
        [HttpGet]         
        public ActionResult Index(DateTime? tuNgay, DateTime? denNgay, string[] HSX = null)
        {
            // 1. Thống kê số lượng và giá trị đơn hàng theo trạng thái
            ThongKeDonHang();

            // 2. Thống kê top 5 sản phẩm bán chạy nhất
            SanPhamBanChay();

            // 3. Thống kê doanh thu 6 tháng gần nhất
            DoanhThuTheoKy();

            // 4. Thống kê doanh thu theo sản phẩm và hãng sản xuất
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;
            ViewBag.HSXList = db.HangSanXuats.Select(h => new SelectListItem
            {
                Value = h.MaHSX,
                Text = h.TenHSX
            }).ToList();
            ViewBag.selectedHSX = HSX ?? new string[] { };

            var query = db.ChiTietDDHs.Where(ct => ct.DonDatHang.TinhTrang == 2).AsQueryable();

            if (tuNgay.HasValue)
                query = query.Where(ct => ct.DonDatHang.Ngay >= tuNgay.Value);

            if (denNgay.HasValue)
                query = query.Where(ct => ct.DonDatHang.Ngay <= denNgay.Value);

            if (HSX?.Any() == true)
            {
                var listHSX = HSX.ToList();
                query = query.Where(ct => listHSX.Contains(ct.SanPham.MaHSX));
            }
            var data = query.GroupBy(ct => new
                            {
                                ct.SanPham.MaSP,
                                ct.SanPham.TenSP,
                                ct.SanPham.GiaBan,
                                ct.SanPham.HangSanXuat.TenHSX
                            })
                            .Select(g => new
                            {
                                g.Key.TenHSX,
                                g.Key.MaSP,
                                g.Key.TenSP,
                                g.Key.GiaBan,
                                TongSoLuong = g.Sum(x => x.SoLuong),
                                TongTien = g.Sum(x => x.SoLuong * g.Key.GiaBan)
                            })
                            .ToList()
                            .GroupBy(x => x.TenHSX)
                            .Select(g => new
                            {
                                TenHSX = g.Key,
                                TongTien = g.Sum(x => x.TongTien),
                                SanPhams = g.Select(sp => new
                                {
                                    sp.MaSP,
                                    sp.TenSP,
                                    sp.TongSoLuong,
                                    sp.GiaBan,
                                    sp.TongTien
                                }).ToList()
                            }).ToList();
            return View(data);
        }
    }
}
