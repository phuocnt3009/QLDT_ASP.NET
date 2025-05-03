using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using QLDT_23TH0014.Filters;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Controllers.KhachHang
{
    public class KH_TaiKhoans_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Kiểm tra người dùng
        public bool CheckUser(string username, string password)
        {
            var kq = db.NguoiDungs.Where(x => x.Email == username && x.MatKhau == password).ToList();
            if (kq.Count() > 0)
            {
                Session["use"] = kq.First();
                return true;
            }
            else
            {
                Session["use"] = null;
                return false;
            }
        }

        // Đăng nhập
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(NguoiDung nguoiDung)
        {
            if (ModelState.IsValid)
            {
                if (CheckUser(nguoiDung.Email, nguoiDung.MatKhau) == true)
                {
                    FormsAuthentication.SetAuthCookie(nguoiDung.Email, true);
                    var kq = db.NguoiDungs.Where(x => x.Email == nguoiDung.Email && x.MatKhau == nguoiDung.MatKhau).First();
                    if (kq.VaiTro == "KH")
                    {
                        return RedirectToAction("Index", "KH_Sanphams_23TH0014"); //Chuyển hướng đến trang cho khánh hàng
                    }
                    else
                    {
                        return RedirectToAction("Index", "NV_ThongKes_23TH0014"); //Chuyển hướng đến trang cho nhân viên, admin
                    }
                }
                else
                {
                    ViewBag.TB = "Email hoặc mật khẩu không đúng!";
                    return View(nguoiDung);
                }
            }
            return View(nguoiDung);
        }

        // Đăng xuất
        public ActionResult DangXuat()
        {
            Session["use"] = null;
            Session["SLMatHang"] = null;
            Session["GioHang"] = null;
            return RedirectToAction("Index", "KH_SanPhams_23TH0014");

        }

        // Quên mật khẩu
        public ActionResult QuenMatKhau()
        {
            return View();
        }     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult QuenMatKhau(string email)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                ViewBag.TB = "Email không tồn tại trong hệ thống!";
                return View();
            }

            // Tạo mật khẩu mới
            string matKhauMoi = Guid.NewGuid().ToString().Substring(0, 8);
            user.MatKhau = matKhauMoi;
            db.SaveChanges();

            // Gửi email
            try
            {
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress("your-email.com", "No-reply"), // Sửa lại email của bạn
                    Subject = "Ego Mobile - Khôi phục mật khẩu",
                    Body = $"Mật khẩu mới của bạn là: {matKhauMoi}.\nVui lòng đổi mật khẩu sau khi đăng nhập lại!",
                    IsBodyHtml = false
                };
                mail.To.Add(email);
                SmtpClient smtp = new SmtpClient
                {
                    EnableSsl = true
                };
                smtp.Send(mail);
                ViewBag.TB = "Mật khẩu mới đã được gửi đến email của bạn!";
            }
            catch
            {
                ViewBag.TB = "Gửi email thất bại. Vui lòng thử lại sau!";
            }

            return View();
        }

        // Tạo mã người dùng tự động
        string LayMaUser()
        {
            var maMax = db.NguoiDungs.ToList().Select(n => n.MaNguoiDung).Max();
            int maUser = int.Parse(maMax.Substring(4)) + 1;
            return "USER" + maUser.ToString("D3");
        }

        // Xem chi tiết người dùng
        public ActionResult Details(string id)
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

        // Đăng ký
        public ActionResult Create()
        {
            ViewBag.MaNguoiDung = LayMaUser();
            ViewBag.VaiTro = "KH";
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNguoiDung,HoTen,GioiTinh,SDT,Email,MatKhau,DiaChi,AnhDaiDien,VaiTro")] NguoiDung nguoiDung)
        {
            //Lấy file ảnh được upload và lưu vào đường dẫn
            var imgNV = Request.Files["Avatar"];
            string postedFileName = System.IO.Path.GetFileName(imgNV.FileName);
            var path = Server.MapPath("/Images/User/" + postedFileName);
            imgNV.SaveAs(path);

            if (ModelState.IsValid)
            {
                var existingUser = db.NguoiDungs.FirstOrDefault(x => x.Email == nguoiDung.Email);
                if (existingUser != null) // Kiểm tra email đã tồn tại chưa
                {
                    ViewBag.TB = "Email đã tồn tại! Vui lòng chọn email khác.";
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
                nguoiDung.MaNguoiDung = LayMaUser(); // Tạo mã người dùng tự động
                nguoiDung.VaiTro = "KH";
                nguoiDung.AnhDaiDien = postedFileName; //Lưu ảnh đại diện là file ảnh vừa upload
                db.NguoiDungs.Add(nguoiDung);
                db.SaveChanges();
                Session["use"] = db.NguoiDungs.Where(x => x.MaNguoiDung == nguoiDung.MaNguoiDung).First();
                return RedirectToAction("Details", "KH_TaiKhoans_23TH0014", new { id = nguoiDung.MaNguoiDung });
            }
            return View(nguoiDung);
        }

        // Chỉnh sửa thông tin
        [AuthenticationFilter("KH")]
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
        [AuthenticationFilter("KH")]
        public ActionResult Edit([Bind(Include = "MaNguoiDung,HoTen,GioiTinh,SDT,Email,MatKhau,DiaChi,AnhDaiDien,VaiTro")] NguoiDung nguoiDung)
        {
            //Lấy file ảnh được upload và lưu vào đường dẫn
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
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "KH_TaiKhoans_23TH0014", new { id = nguoiDung.MaNguoiDung });
            }
            return View(nguoiDung);
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
