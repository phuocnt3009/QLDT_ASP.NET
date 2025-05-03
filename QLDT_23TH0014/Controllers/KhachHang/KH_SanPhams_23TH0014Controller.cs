using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLDT_23TH0014.Filters;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Controllers.KhachHang
{
    public class KH_SanPhams_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách sản phẩm
        [HttpGet]
        public ActionResult Index(string MaSP = "", string TenSP = "", string GBMin = "0", string GBMax = "70000000", string[] Ram = null, string[] DungLuong = null, string HDH = "", string MaHSX = "")
        {
            // Xử lý ViewBag để hiển thị lại trên giao diện tìm kiếm
            ViewBag.MaSP = MaSP;
            ViewBag.TenSP = TenSP;
            ViewBag.GBMin = GBMin;
            ViewBag.GBMax = GBMax;
            ViewBag.Ram = Ram ?? new string[0];
            ViewBag.DungLuong = DungLuong ?? new string[0];
            ViewBag.HDH = HDH;
            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", MaHSX);

            // Xử lý min/max giá bán
            int min = 0;
            int max = 70000000;

            // Bắt đầu truy vấn LINQ
            var query = db.ChiTietSanPhams
                          .Include(ct => ct.SanPham)
                          .Include(ct => ct.SanPham.HangSanXuat)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(MaSP))
                query = query.Where(ct => ct.MaSP.Contains(MaSP));

            if (!string.IsNullOrEmpty(TenSP))
                query = query.Where(ct => ct.SanPham.TenSP.Contains(TenSP));
            
            if (!string.IsNullOrEmpty(GBMin))
                int.TryParse(GBMin, out min);

            if (!string.IsNullOrEmpty(GBMax))
                int.TryParse(GBMax, out max);
            query = query.Where(ct => ct.SanPham.GiaBan >= min && ct.SanPham.GiaBan <= max);

            if (Ram != null)
            {
                var ram = Ram.Select(r => int.Parse(r)).ToList();
                query = query.Where(ct => ram.Contains(ct.SanPham.Ram));
            }

            if (DungLuong != null)
            {
                var dl = DungLuong.Select(d => int.Parse(d)).ToList();
                query = query.Where(ct => dl.Contains(ct.SanPham.DungLuong));
            }

            if (!string.IsNullOrEmpty(HDH))
                query = query.Where(ct => ct.SanPham.HeDieuHanh.Contains(HDH));

            if (!string.IsNullOrEmpty(MaHSX))
                query = query.Where(ct => ct.SanPham.MaHSX == MaHSX);

            var result = query.ToList();

            if (!result.Any())
                ViewBag.TB = "Không có sản phẩm tìm kiếm.";

            var danhMuc = result.GroupBy(ct => ct.SanPham.HangSanXuat).ToList();

            return View(danhMuc);
        }

        // Xem chi tiết sản phẩm
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var chiTietSanPhams = db.ChiTietSanPhams
                                    .Include(ct => ct.SanPham)
                                    .Include(ct => ct.SanPham.HangSanXuat)
                                    .Where(ct => ct.SanPham.MaSP == id)
                                    .ToList(); 
            if (chiTietSanPhams == null)
            {
                return HttpNotFound();
            }
            return View(chiTietSanPhams);
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
