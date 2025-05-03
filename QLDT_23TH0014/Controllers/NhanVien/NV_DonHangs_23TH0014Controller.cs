using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using QLDT_23TH0014.Filters;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Controllers.NhanVien
{
    public class NV_DonHangs_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách đơn hàng
        [HttpGet]
        [AuthenticationFilter("AD", "NV")]
        public ActionResult Index(string MaDDH = "", DateTime? TuNgay = null, DateTime? DenNgay = null, int? TinhTrang = null, int? ThanhToan = null,
                                  string DiaChi = "", string TongTienMin = "0", string TongTienMax = "500000000", string MaKH = "", string MaNV = "", int? page = 1)
        {
            var use = Session["use"] as NguoiDung;

            // ViewBag để giữ giá trị tìm kiếm
            ViewBag.MaDDH = MaDDH;
            ViewBag.TuNgay = TuNgay;
            ViewBag.DenNgay = DenNgay;
            ViewBag.TinhTrang = TinhTrang;
            ViewBag.DiaChi = DiaChi;
            ViewBag.ThanhToan = ThanhToan;
            ViewBag.TongTienMin = TongTienMin;
            ViewBag.TongTienMax = TongTienMax;
            ViewBag.MaKH = MaKH;
            ViewBag.MaNV_Duyet = MaNV;

            // Lấy dữ liệu từ cơ sở dữ liệu dưới dạng IQueryable
            IQueryable<ChiTietDDH> query;
            if (use.VaiTro == "AD")
            {
                query = db.ChiTietDDHs
                                .Include(ddh => ddh.DonDatHang)
                                .Include(ddh => ddh.SanPham)
                                .AsQueryable();
            }
            else
            {
                query = db.ChiTietDDHs
                                .Include(ddh => ddh.DonDatHang)
                                .Include(ddh => ddh.SanPham)
                                .Where(ddh => ddh.DonDatHang.MaNhanVien == use.MaNguoiDung)
                                .AsQueryable();
            }

            // Tính tổng tiền theo từng đơn và nhóm lại theo đơn đặt hàng
            var danhSach = query
                .GroupBy(ct => ct.DonDatHang)
                .Select(group => new
                {
                    DonHang = group.Key,
                    TongTien = group.Sum(ct => ct.SoLuong * ct.SanPham.GiaBan)
                });

            // Lọc theo các tiêu chí
            if (!string.IsNullOrEmpty(MaDDH))
                danhSach = danhSach.Where(d => d.DonHang.MaDDH.Contains(MaDDH));

            if (TuNgay.HasValue)
                danhSach = danhSach.Where(d => d.DonHang.Ngay >= TuNgay.Value);

            if (DenNgay.HasValue)
                danhSach = danhSach.Where(d => d.DonHang.Ngay <= DenNgay.Value);

            if (TinhTrang != null)
                danhSach = danhSach.Where(d => d.DonHang.TinhTrang == TinhTrang);

            if (ThanhToan != null)
                danhSach = danhSach.Where(d => d.DonHang.ThanhToan == ThanhToan);

            if (!string.IsNullOrEmpty(DiaChi))
                danhSach = danhSach.Where(d => d.DonHang.DiaChiNhanHang.Contains(DiaChi));

            if (decimal.TryParse(TongTienMin, out decimal minTT) && decimal.TryParse(TongTienMax, out decimal maxTT))
                danhSach = danhSach.Where(d => d.TongTien >= minTT && d.TongTien <= maxTT);

            if (!string.IsNullOrEmpty(MaKH))
                danhSach = danhSach.Where(d => d.DonHang.MaKhachHang.Contains(MaKH));

            if (!string.IsNullOrEmpty(MaNV))
                danhSach = danhSach.Where(d => d.DonHang.MaNhanVien.Contains(MaNV));

            // Sắp xếp
            danhSach = danhSach.OrderBy(d => d.DonHang.TinhTrang)
                               .ThenByDescending(d => d.DonHang.MaDDH);

            // Phân trang
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Trả về kết quả phân trang
            return View(danhSach.ToPagedList(pageNumber, pageSize));
        }

        // Xem chi tiết đơn hàng
        [AuthenticationFilter("AD", "NV")]
        public ActionResult Details(string id)
        {
            var use = Session["use"] as NguoiDung;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang == null)
            {
                return HttpNotFound();
            }
            var chitiet = db.ChiTietDDHs
                .Include(d => d.SanPham)
                .Include(d => d.MauSac)
                .Where(d => d.MaDDH == id)
                .Select(d => new
                {
                    d.MaSP,
                    d.MaMau,
                    d.SanPham.TenSP,
                    d.MauSac.TenMau,
                    d.SoLuong,
                    d.SanPham.GiaBan,
                    ThanhTien = d.SoLuong * d.SanPham.GiaBan,
                    AnhBia = db.ChiTietSanPhams
                              .Where(ct => ct.MaSP == d.MaSP && ct.MaMau == d.MaMau)
                              .Select(ct => ct.AnhBia)
                              .FirstOrDefault(),
                    d.DonDatHang.MaDDH,
                    d.DonDatHang.DiaChiNhanHang,
                    d.DonDatHang.TinhTrang,
                    d.DonDatHang.ThanhToan,
                    d.DonDatHang.MaNhanVien,
                    TenKH = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                         .Select(nd => nd.HoTen)
                                         .FirstOrDefault(),
                    SDT = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                       .Select(nd => nd.SDT)
                                       .FirstOrDefault()
                })
                .ToList();
            if (use.VaiTro == "NV")
                if (use.MaNguoiDung != chitiet.FirstOrDefault().MaNhanVien)
                    return HttpNotFound();
            return View(chitiet);
        }

        //Duyệt đơn hàng
        [AuthenticationFilter("AD")]
        public ActionResult Accept(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang == null)
            {
                return HttpNotFound();
            }

            // Lấy thôn tin đơn hàng
            var chitiet = db.ChiTietDDHs
                .Include(d => d.SanPham)
                .Include(d => d.MauSac)
                .Where(d => d.MaDDH == id)
                .Select(d => new
                {
                    d.MaSP,
                    d.MaMau,
                    d.SanPham.TenSP,
                    d.MauSac.TenMau,
                    d.SoLuong,
                    d.SanPham.GiaBan,
                    ThanhTien = d.SoLuong * d.SanPham.GiaBan,
                    AnhBia = db.ChiTietSanPhams
                              .Where(ct => ct.MaSP == d.MaSP && ct.MaMau == d.MaMau)
                              .Select(ct => ct.AnhBia)
                              .FirstOrDefault(),
                    d.DonDatHang.MaDDH,
                    d.DonDatHang.DiaChiNhanHang,
                    d.DonDatHang.TinhTrang,
                    d.DonDatHang.ThanhToan,
                    TenKH = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                         .Select(nd => nd.HoTen)
                                         .FirstOrDefault(),
                    SDT = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                       .Select(nd => nd.SDT)
                                       .FirstOrDefault()
                })
                .ToList();

            // Lấy danh sách nhân viên
            var nhanViens = db.NguoiDungs
                      .Where(nd => nd.VaiTro == "NV")
                      .Select(nd => new { nd.MaNguoiDung, nd.HoTen })
                      .ToList();
            ViewBag.MaNhanVien = new SelectList(nhanViens, "MaNguoiDung", "HoTen");

            return View(chitiet);
        }
        [HttpPost, ActionName("Accept")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult AcceptConfirmed(string id, string maNV)
        {
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang.TinhTrang == 0)
            {              
                if (string.IsNullOrEmpty(maNV))
                {
                    TempData["Error"] = "Vui lòng phân công nhân viên cho đơn hàng!";
                    return RedirectToAction("Accept", new { id });
                }
                else
                {
                    donDatHang.TinhTrang = 1; // 1: Đang chờ giao hàng
                    donDatHang.MaNhanVien = maNV; // Phân công nhân viên xử lý đơn hàng
                    db.Entry(donDatHang).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index", "NV_DonHangs_23TH0014");
                }
            }
            else
            {
                return RedirectToAction("Index", "NV_DonHangs_23TH0014");
            }
        }

        // Hủy đơn hàng
        [AuthenticationFilter("AD")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang == null)
            {
                return HttpNotFound();
            }
            var chitiet = db.ChiTietDDHs
                .Include(d => d.SanPham)
                .Include(d => d.MauSac)
                .Where(d => d.MaDDH == id)
                .Select(d => new
                {
                    d.MaSP,
                    d.MaMau,
                    d.SanPham.TenSP,
                    d.MauSac.TenMau,
                    d.SoLuong,
                    d.SanPham.GiaBan,
                    ThanhTien = d.SoLuong * d.SanPham.GiaBan,
                    AnhBia = db.ChiTietSanPhams
                              .Where(ct => ct.MaSP == d.MaSP && ct.MaMau == d.MaMau)
                              .Select(ct => ct.AnhBia)
                              .FirstOrDefault(),
                    d.DonDatHang.MaDDH,
                    d.DonDatHang.DiaChiNhanHang,
                    d.DonDatHang.TinhTrang,
                    d.DonDatHang.ThanhToan,
                    TenKH = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                         .Select(nd => nd.HoTen)
                                         .FirstOrDefault(),
                    SDT = db.NguoiDungs.Where(nd => nd.MaNguoiDung == d.DonDatHang.MaKhachHang)
                                       .Select(nd => nd.SDT)
                                       .FirstOrDefault()
                })
                .ToList();
            return View(chitiet);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteConfirmed(string id)
        {
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang.TinhTrang < 2)
            {   
                donDatHang.TinhTrang = 3; // 3: Đã hủy
                db.Entry(donDatHang).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "NV_DonHangs_23TH0014");
            }
            else
            {
                return RedirectToAction("Index", "NV_DonHangs_23TH0014");
            }
        }

        // Hoàn thành đơn hàng
        [AuthenticationFilter("NV")]
        public ActionResult Complete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang == null)
            {
                return HttpNotFound();
            }           
            if (donDatHang.TinhTrang == 1)
            {
                donDatHang.ThanhToan = 1; // 1: Đã thanh toán
                donDatHang.TinhTrang = 2; // 2: Đã thành công
                db.Entry(donDatHang).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "NV_DonHangs_23TH0014");
            }
            else
            {
                return RedirectToAction("Index", "NV_DonHangs_23TH0014");
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
