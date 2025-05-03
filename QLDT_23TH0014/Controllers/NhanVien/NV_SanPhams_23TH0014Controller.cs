using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLDT_23TH0014.Models;
using PagedList;
using PagedList.Mvc;
using QLDT_23TH0014.Filters;

namespace QLDT_23TH0014.Controllers.NhanVien
{
    public class NV_SanPhams_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách sản phẩm
        [HttpGet]
        [AuthenticationFilter("AD", "NV")]
        public ActionResult Index(string MaSP = "", string TenSP = "", string GBMin = "0", string GBMax = "70000000", string[] Ram = null, string[] DungLuong = null, string HDH = "", string MaHSX = "", int? page = 1)
        {
            // ViewBag để giữ lại các giá trị tìm kiếm
            ViewBag.MaSP = MaSP;
            ViewBag.TenSP = TenSP;
            ViewBag.GBMin = GBMin;
            ViewBag.GBMax = GBMax;
            ViewBag.Ram = Ram ?? new string[] { };
            ViewBag.DungLuong = DungLuong ?? new string[] { };
            ViewBag.HDH = HDH;
            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", MaHSX);

            // Parse giá trị min/max
            int min = 0;
            int max = 70000000;
            if (!string.IsNullOrEmpty(GBMin)) int.TryParse(GBMin, out min);
            if (!string.IsNullOrEmpty(GBMax)) int.TryParse(GBMax, out max);

            // Tạo truy vấn LINQ
            var query = db.ChiTietSanPhams
                          .Include(ct => ct.SanPham)
                          .Include(ct => ct.SanPham.HangSanXuat)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(MaSP))
                query = query.Where(ct => ct.MaSP.Contains(MaSP));

            if (!string.IsNullOrEmpty(TenSP))
                query = query.Where(ct => ct.SanPham.TenSP.Contains(TenSP));

            query = query.Where(ct => ct.SanPham.GiaBan >= min && ct.SanPham.GiaBan <= max);

            if (Ram?.Any() == true)
            {
                var listRam = Ram
                    .SelectMany(r => r.Split(','))
                    .Select(r => r.Trim())
                    .Where(r => int.TryParse(r, out _))
                    .Select(r => int.Parse(r))
                    .ToList();
                query = query.Where(ct => listRam.Contains(ct.SanPham.Ram));
            }

            if (DungLuong?.Any() == true)
            {
                var listDL = DungLuong
                    .SelectMany(d => d.Split(','))
                    .Select(d => d.Trim())
                    .Where(d => int.TryParse(d, out _))
                    .Select(d => int.Parse(d))
                    .ToList();
                query = query.Where(ct => listDL.Contains(ct.SanPham.DungLuong));
            }

            if (!string.IsNullOrEmpty(HDH))
                query = query.Where(ct => ct.SanPham.HeDieuHanh.Contains(HDH));

            if (!string.IsNullOrEmpty(MaHSX))
                query = query.Where(ct => ct.SanPham.MaHSX == MaHSX);

            var result = query.ToList();

            // Phân trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            var danhMuc = result.GroupBy(ct => ct.SanPham)
                                .ToList()
                                .ToPagedList(pageNumber, pageSize);

            return View(danhMuc);
        }

        // Xem chi tiết sản phẩm
        [AuthenticationFilter("AD","NV")]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var chiTietSanPhams = db.ChiTietSanPhams
                                    .Include(ct => ct.SanPham)
                                    .Include(ct => ct.SanPham.HangSanXuat)
                                    .Where(ct => ct.MaSP == id)
                                    .ToList();
            if (chiTietSanPhams == null)
            {
                return HttpNotFound();
            }
            return View(chiTietSanPhams);
        }

        // Tạo mã sản phẩm tự động
        string LayMaSP()
        {
            var maMax = db.SanPhams.ToList().Select(n => n.MaSP).Max();
            int maSP = int.Parse(maMax.Substring(2)) + 1;
            return "SP" + maSP.ToString("D3");
        }

        // Thêm mới sản phẩm
        [AuthenticationFilter("AD")]
        public ActionResult Create_SP()
        {
            ViewBag.MaSP = LayMaSP();
            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX");
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Create_SP([Bind(Include = "MaSP,TenSP,GiaBan,Mota,Ram,DungLuong,HeDieuHanh,MaHSX")] SanPham sanPham, string MaMau)
        {
            // Lấy file ảnh upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
            var path = Server.MapPath("/Images/SanPham/" + postedFileName);
            imgNV.SaveAs(path);
            if (ModelState.IsValid)
            {
                // Thêm sản phẩm vào bảng SanPham
                sanPham.MaSP = LayMaSP(); // Tạo mã sản phẩm tự động
                db.SanPhams.Add(sanPham);
                db.SaveChanges();

                // Thêm chi tiết sản phẩm vào bảng ChiTietSanPham
                ChiTietSanPham chiTietSP = new ChiTietSanPham
                {
                    MaSP = sanPham.MaSP,
                    MaMau = MaMau,
                    AnhBia = postedFileName, // Lấy ảnh bìa là file vừa upload
                };
                db.ChiTietSanPhams.Add(chiTietSP);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", sanPham.MaHSX);
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", MaMau);
            return View(sanPham);
        }

        // Thêm màu cho sản phẩm hiện có
        [AuthenticationFilter("AD")]
        public ActionResult Create_Mau(string id)
        {
            var sp = db.SanPhams.Find(id);
            ViewData["MaSP"] = sp.MaSP;
            ViewBag.TenSP = sp.TenSP;
            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Create_Mau([Bind(Include = "MaSP, MaMau, AnhBia")] ChiTietSanPham chiTietSanPham)
        {
            // Lấy file ảnh upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
            var path = Server.MapPath("/Images/SanPham/" + postedFileName);
            imgNV.SaveAs(path);

            if (ModelState.IsValid)
            {
                // Kiểm tra sản phẩm đã có màu dự định thêm chưa
                var ctsp = db.ChiTietSanPhams.Any(ct => ct.MaSP == chiTietSanPham.MaSP && ct.MaMau == chiTietSanPham.MaMau); 
                if(ctsp)
                {
                    ViewBag.TB = "Đã tồn tại sản phẩm!";
                    ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", chiTietSanPham.MaMau);
                    return View();
                }
                chiTietSanPham.AnhBia = postedFileName; // Lấy ảnh bìa là file ảnh vừa upload
                db.ChiTietSanPhams.Add(chiTietSanPham);
                db.SaveChanges();

                return RedirectToAction("Details", "NV_SanPhams_23TH0014", new { id = chiTietSanPham.MaSP }); ;
            }

            ViewBag.MaMau = new SelectList(db.MauSacs, "MaMau", "TenMau", chiTietSanPham.MaMau);
            return View();
        }

        // Chỉnh sửa thông tin sản phẩm
        [AuthenticationFilter("AD")]
        public ActionResult Edit_SP(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", sanPham.MaHSX);
            return View(sanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Edit_SP([Bind(Include = "MaSP,TenSP,GiaBan,Mota,Ram,DungLuong,HeDieuHanh,MaHSX")] SanPham sanPham)
        {
            if (ModelState.IsValid)
            {
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "NV_SanPhams_23TH0014", new { id = sanPham.MaSP });
            }
            ViewBag.MaHSX = new SelectList(db.HangSanXuats, "MaHSX", "TenHSX", sanPham.MaHSX);
            return View(sanPham);
        }

        // Chỉnh sửa ảnh sản phẩm
        [AuthenticationFilter("AD")]
        public ActionResult Edit_Anh(string id, string maMau)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChiTietSanPham ct = db.ChiTietSanPhams.Where(s => s.MaSP == id && s.MaMau == maMau).First();
            if (ct == null)
            {
                return HttpNotFound();
            }
            return View(ct);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Edit_Anh([Bind(Include = "MaSP, MaMau, AnhBia")] ChiTietSanPham ct)
        {
            // Lấy file ảnh upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            try
            {
                string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
                var path = Server.MapPath("/Images/SanPham/" + postedFileName);
                imgNV.SaveAs(path);
            }
            catch
            { }
            if (ModelState.IsValid)
            {
                db.Entry(ct).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "NV_SanPhams_23TH0014", new { id = ct.MaSP });
            }
            return View(ct);
        }

        // Xóa sản phẩm
        [AuthenticationFilter("AD")]
        public ActionResult Delete_SP(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var chiTietSanPhams = db.ChiTietSanPhams
                                    .Include(ct => ct.SanPham)
                                    .Include(ct => ct.SanPham.HangSanXuat)
                                    .Where(ct => ct.MaSP == id)
                                    .ToList();
            if (chiTietSanPhams== null)
            {
                return HttpNotFound();
            }
            return View(chiTietSanPhams);
        }
        [HttpPost, ActionName("Delete_SP")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Kiểm tra xem sản phẩm có trong đơn đặt hàng không
            var existsInOrder = db.ChiTietDDHs.Any(ct => ct.MaSP == id);
            if (existsInOrder)
            {
               ViewBag.TB = "Không thể xóa sản phẩm này vì đang có trong đơn hàng!";
               var ctsp = db.ChiTietSanPhams
                                    .Include(ct => ct.SanPham)
                                    .Include(ct => ct.SanPham.HangSanXuat)
                                    .Where(ct => ct.MaSP == id)
                                    .ToList();
                if (ctsp == null)
                {
                    return HttpNotFound();
                }
                return View(ctsp);
            }

            // Xóa tất cả ChiTietSanPham trước khi xóa SanPham
            var chiTietSanPhams = db.ChiTietSanPhams.Where(ct => ct.MaSP == id).ToList();
            foreach (var ct in chiTietSanPhams)
            {
                db.ChiTietSanPhams.Remove(ct);
            }

            // Xóa SanPham
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            db.SanPhams.Remove(sanPham);
            db.SaveChanges();
            return RedirectToAction("Index", "NV_SanPhams_23TH0014");
        }

        // Xóa màu sản phẩm
        [AuthenticationFilter("AD")]
        public ActionResult Delete_Mau(string id, string maMau)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var chiTietSanPhams = db.ChiTietSanPhams
                                    .Include(ct => ct.SanPham)
                                    .Include(ct => ct.SanPham.HangSanXuat)
                                    .Where(ct => ct.MaSP == id && ct.MaMau == maMau)
                                    .ToList();
            if (chiTietSanPhams == null)
            {
                return HttpNotFound();
            }
            return View(chiTietSanPhams);
        }
        [HttpPost, ActionName("Delete_Mau")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteMauConfirmed(string id, string maMau)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Kiểm tra xem sản phẩm có trong đơn đặt hàng không
            var existsInOrder = db.ChiTietDDHs.Any(ct => ct.MaSP == id && ct.MaMau == maMau);
            if (existsInOrder)
            {
                ViewBag.TB = "Không thể xóa sản phẩm này vì đang có trong đơn hàng!";
                var ctsp = db.ChiTietSanPhams
                                     .Include(ct => ct.SanPham)
                                     .Include(ct => ct.SanPham.HangSanXuat)
                                     .Where(ct => ct.SanPham.MaSP == id && ct.MaMau == maMau)
                                     .ToList();
                if (ctsp == null)
                {
                    return HttpNotFound();
                }
                return View(ctsp);
            }

            // Xóa màu sản phẩm trong ChiTietSanPham
            var chiTietSanPhams = db.ChiTietSanPhams.Where(ct => ct.MaSP == id && ct.MaMau == maMau).FirstOrDefault();
            if (chiTietSanPhams == null)
            {
                return HttpNotFound();
            }
            db.ChiTietSanPhams.Remove(chiTietSanPhams);
            db.SaveChanges();

            // Kiểm tra xem sản phẩm còn bản ghi trong ChiTietSanPham không
            var sp = db.ChiTietSanPhams.Where(ct => ct.MaSP == id).ToList();
            if (sp.Count == 0) 
            {
                var sanPham = db.SanPhams.Find(id); // Nếu không còn thì xóa luôn sản phẩm
                if (sanPham != null)
                {
                    db.SanPhams.Remove(sanPham);
                    db.SaveChanges();
                    return RedirectToAction("Index", "NV_SanPhams_23TH0014");
                }
            }
            return RedirectToAction("Details", "NV_SanPhams_23TH0014", new { id = chiTietSanPhams.MaSP });
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
