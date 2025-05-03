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
    public class NV_TaiKhoans_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách người dùng (không bao gồm admin)
        [AuthenticationFilter("AD","NV")]
        [HttpGet]
        public ActionResult Index(string MaNguoiDung = "", string HoTen = "", string SDT = "", string Email = "", bool? GioiTinh = null, string VaiTro = "", int? page = 1)
        {
            // Lưu lại giá trị filter vào ViewBag
            ViewBag.MaNguoiDung = MaNguoiDung;
            ViewBag.HoTen = HoTen;
            ViewBag.SDT = SDT;
            ViewBag.Email = Email;
            ViewBag.GioiTinh = GioiTinh;
            ViewBag.VaiTro = VaiTro;

            // Tạo query cơ bản
            var query = db.NguoiDungs.AsQueryable();

            if (!string.IsNullOrEmpty(MaNguoiDung))
                query = query.Where(nd => nd.MaNguoiDung.Contains(MaNguoiDung));

            if (!string.IsNullOrEmpty(HoTen))
                query = query.Where(nd => nd.HoTen.Contains(HoTen));

            if (!string.IsNullOrEmpty(SDT))
                query = query.Where(nd => nd.SDT.Contains(SDT));

            if (!string.IsNullOrEmpty(Email))
                query = query.Where(nd => nd.Email.Contains(Email));

            if (GioiTinh != null)
            {
                query = query.Where(nd => nd.GioiTinh == GioiTinh);
            }

            if (!string.IsNullOrEmpty(VaiTro))
                query = query.Where(nd => nd.VaiTro == VaiTro);

            var result = query.Where(nd => nd.VaiTro != "AD").OrderBy(nd => nd.MaNguoiDung).ToList();

            if (!result.Any())
                ViewBag.TB = "Không tìm thấy người dùng phù hợp.";

            // Phân trang
            int pageSize = 10;
            int pageNumber = page ?? 1;
            var pagedList = result.ToPagedList(pageNumber, pageSize);

            return View(pagedList);
        }


        // Xem chi tiết người dùng
        [AuthenticationFilter("AD","NV")]
        public ActionResult Details(string id)
        {
            var use = Session["use"] as NguoiDung;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null || (use.VaiTro == "NV" && nguoiDung.VaiTro == "AD"))
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        // Tạo mã người dùng tự động
        string LayMaUser()
        {
            var maMax = db.NguoiDungs.ToList().Select(n => n.MaNguoiDung).Max();
            int maUser = int.Parse(maMax.Substring(4)) + 1;
            return "USER" + maUser.ToString("D3");
        }

        // Thêm người dùng
        [AuthenticationFilter("AD")]
        public ActionResult Create()
        {
            ViewBag.MaNguoiDung = LayMaUser();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Create([Bind(Include = "MaNguoiDung,HoTen,GioiTinh,SDT,Email,MatKhau,DiaChi,AnhDaiDien,VaiTro")] NguoiDung nguoiDung)
        {
            // Lấy file ảnh upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
            var path = Server.MapPath("/Images/User/" + postedFileName);
            imgNV.SaveAs(path);

            if (ModelState.IsValid)
            {
                var existingUser = db.NguoiDungs.FirstOrDefault(x => x.Email == nguoiDung.Email); 
                if (existingUser != null) // Kiểm tra email đã tồn tại chưa
                {
                    ViewBag.TB = "Email đã tồn tại! Vui lòng chọn email khác";
                    return View(nguoiDung);
                }
                if (!nguoiDung.Email.EndsWith("@gmail.com")) // Kiểm tra định dạng email
                {
                    ViewBag.TB = "Vui lòng sử dụng email có đuôi @gmail.com!";
                    return View(nguoiDung);
                }
                nguoiDung.MaNguoiDung = LayMaUser(); // Tạo mã người dùng tự động
                nguoiDung.AnhDaiDien = postedFileName; // Lấy ảnh đại diện là file ảnh upload
                db.NguoiDungs.Add(nguoiDung);
                db.SaveChanges();
                return RedirectToAction("Index", "NV_TaiKhoans_23TH0014");
            }
            return View(nguoiDung);
        }

        // Chỉnh sửa thông tin người dùng
        [AuthenticationFilter("AD", "NV")]
        public ActionResult Edit(string id)
        {
            var use = Session["use"] as NguoiDung;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null || id != use.MaNguoiDung)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD", "NV")]
        public ActionResult Edit([Bind(Include = "MaNguoiDung,HoTen,GioiTinh,SDT,Email,MatKhau,DiaChi,AnhDaiDien,VaiTro")] NguoiDung nguoiDung)
        {
            // Lấy file ảnh upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            try
            {
                string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
                var path = Server.MapPath("/Images/User/" + postedFileName);
                imgNV.SaveAs(path);
            }
            catch
            { }

            if (ModelState.IsValid)
            {
                var existingUser = db.NguoiDungs.FirstOrDefault(u => u.Email == nguoiDung.Email && u.MaNguoiDung != nguoiDung.MaNguoiDung);
                if (existingUser != null) // Kiểm tra email đã tồn tại chưa
                {
                    ViewBag.TB = "Email đã tồn tại! Vui lòng chọn email khác";
                    return View(nguoiDung);
                }
                if (!nguoiDung.Email.EndsWith("@gmail.com")) // Kiểm tra định dạng email
                {
                    ViewBag.TB = "Vui lòng sử dụng email có đuôi @gmail.com!";
                    return View(nguoiDung);
                }
                if (existingUser.SDT == nguoiDung.SDT) // Kiểm tra SĐT đã tồn tại chưa
                {
                    ViewBag.TB = "Số điện thoại đã tồn tại! Vui lòng chọn số khác.";
                    return View(nguoiDung);
                }
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "KH_TaiKhoans_23TH0014", new { id = nguoiDung.MaNguoiDung });
            }
            return View(nguoiDung);
        }

        // Xóa người dùng
        [AuthenticationFilter("AD")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }
            return View(nguoiDung);
        }

        // POST: NV_TaiKhoans_23TH0014/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteConfirmed(string id)
        {
            NguoiDung nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            // Không được xóa tài khoản có quyền admin
            if (nguoiDung.VaiTro == "AD")
            {
                ViewBag.TB = "Không thể xóa tài khoản của admin!";
                return View(nguoiDung);
            }

            // Kiểm tra xem có đơn đặt hàng liên quan đến người dùng không
            var kh = db.DonDatHangs.Any(d => d.MaKhachHang == id);
            var nv = db.DonDatHangs.Any(d => d.MaNhanVien == id);

            if (kh)
            {
                ViewBag.TB = "Không thể xóa khách hàng vì đang có đơn hàng liên quan!";
                return View(nguoiDung);
            }
            if (nv)
            {
                ViewBag.TB = "Không thể xóa nhân viên vì đang có đơn hàng liên quan!";
                return View(nguoiDung);
            }
            db.NguoiDungs.Remove(nguoiDung);
            db.SaveChanges();
            return RedirectToAction("Index");
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
