using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLDT_23TH0014.Filters;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Controllers.NhanVien
{
    public class NV_MauSacs_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách màu sắc
        [AuthenticationFilter("AD","NV")]
        public ActionResult Index()
        {
            return View(db.MauSacs.ToList());
        }

        // Tạo mã màu tự động
        string LayMaMau()
        {
            var maMax = db.MauSacs.ToList().Select(n => n.MaMau).Max();
            int maMau = int.Parse(maMax.Substring(1)) + 1;
            return "M" + maMau.ToString("D3");
        }

        // Thêm mới màu
        [AuthenticationFilter("AD")]
        public ActionResult Create()
        {
            ViewBag.MaMau = LayMaMau();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Create([Bind(Include = "MaMau,TenMau")] MauSac mauSac)
        {
            if (ModelState.IsValid)
            {
                mauSac.MaMau = LayMaMau(); // Tạo mã màu tự động
                db.MauSacs.Add(mauSac);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(mauSac);
        }

        // Chỉnh sửa màu
        [AuthenticationFilter("AD")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MauSac mauSac = db.MauSacs.Find(id);
            if (mauSac == null)
            {
                return HttpNotFound();
            }
            return View(mauSac);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Edit([Bind(Include = "MaMau,TenMau")] MauSac mauSac)
        {
            if (ModelState.IsValid)
            {
                db.Entry(mauSac).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(mauSac);
        }

        // Xóa màu
        [AuthenticationFilter("AD")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MauSac mauSac = db.MauSacs.Find(id);
            if (mauSac == null)
            {
                return HttpNotFound();
            }
            return View(mauSac);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteConfirmed(string id)
        {
            MauSac mauSac = db.MauSacs.Find(id);
            if (mauSac == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem các sản phẩm có màu sắc dự định xóa không
            var sp = db.ChiTietSanPhams.Any(s => s.MaMau == id);
            if (sp)
            {
                ViewBag.TB = "Không thể xóa màu vì vẫn còn sản phẩm có màu này!";
                return View(mauSac);
            }
            db.MauSacs.Remove(mauSac);
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
