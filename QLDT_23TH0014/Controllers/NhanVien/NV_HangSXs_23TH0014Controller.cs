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
    public class NV_HangSXs_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách hãng sản xuất
        [AuthenticationFilter("AD","NV")]
        public ActionResult Index()
        {
            return View(db.HangSanXuats.ToList());
        }

        // Tạo mã HSX tự động
        string LayMaHSX()
        {
            var maMax = db.HangSanXuats.ToList().Select(n => n.MaHSX).Max();
            int maHSX = int.Parse(maMax.Substring(3)) + 1;
            return "HSX" + maHSX.ToString("D3");
        }

        // Thêm mới HSX
        [AuthenticationFilter("AD")]
        public ActionResult Create()
        {
            ViewBag.MaHSX = LayMaHSX();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Create([Bind(Include = "MaHSX,TenHSX")] HangSanXuat hangSanXuat)
        {
            if (ModelState.IsValid)
            {
                hangSanXuat.MaHSX = LayMaHSX(); // Tạo mã HSX tự động
                db.HangSanXuats.Add(hangSanXuat);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(hangSanXuat);
        }

        // Chỉnh sửa HSX
        [AuthenticationFilter("AD")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HangSanXuat hangSanXuat = db.HangSanXuats.Find(id);
            if (hangSanXuat == null)
            {
                return HttpNotFound();
            }
            return View(hangSanXuat);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult Edit([Bind(Include = "MaHSX,TenHSX")] HangSanXuat hangSanXuat)
        {
            if (ModelState.IsValid)
            {
                db.Entry(hangSanXuat).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(hangSanXuat);
        }

        // Xóa HSX
        [AuthenticationFilter("AD")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            HangSanXuat hangSanXuat = db.HangSanXuats.Find(id);
            if (hangSanXuat == null)
            {
                return HttpNotFound();
            }
            return View(hangSanXuat);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("AD")]
        public ActionResult DeleteConfirmed(string id)
        {
            HangSanXuat hangSanXuat = db.HangSanXuats.Find(id);
            if (hangSanXuat == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem có sản phẩm nào thuộc hãng không
            var sp = db.SanPhams.Any(s => s.MaHSX == id);
            if (sp)
            {
                ViewBag.TB = "Không thể xóa hãng sản xuất vì còn sản phẩm thuộc hãng này!";
                return View(hangSanXuat);
            }
            db.HangSanXuats.Remove(hangSanXuat);
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
