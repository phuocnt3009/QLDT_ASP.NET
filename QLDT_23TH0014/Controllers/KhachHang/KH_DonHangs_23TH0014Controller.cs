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
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.Web.Services.Description;
using System.Threading.Tasks;
using ZaloPay.Helper.Crypto;
using ZaloPay.Helper;
using System.IO;

namespace QLDT_23TH0014.Controllers.KhachHang
{
    public class KH_DonHangs_23TH0014Controller : Controller
    {
        private readonly QLDT_23TH0014Entities db = new QLDT_23TH0014Entities();

        // Danh sách đơn hàng
        [AuthenticationFilter("KH")]
        [HttpGet]
        public ActionResult Index(string MaDDH = "", DateTime? TuNgay = null, DateTime? DenNgay = null, int? TinhTrang = null, int? ThanhToan = null,
                                  string DiaChi = "", string TongTienMin = "0", string TongTienMax = "500000000", int? page = 1)
        {
            var use = Session["use"] as NguoiDung;

            // ViewBag để giữ giá trị tìm kiếm
            ViewBag.MaDDH = MaDDH;
            ViewBag.TuNgay = TuNgay;
            ViewBag.DenNgay = DenNgay;
            ViewBag.TinhTrang = TinhTrang;
            ViewBag.ThanhToan = ThanhToan;
            ViewBag.DiaChi = DiaChi;
            ViewBag.TongTienMin = TongTienMin;
            ViewBag.TongTienMax = TongTienMax;

            // Lấy dữ liệu từ cơ sở dữ liệu dưới dạng IQueryable
            var query = db.ChiTietDDHs
                                .Include(ddh => ddh.DonDatHang)
                                .Include(ddh => ddh.SanPham)
                                .Where(ddh => ddh.DonDatHang.MaKhachHang == use.MaNguoiDung)
                                .AsQueryable();

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

            // Sắp xếp
            danhSach = danhSach.OrderByDescending(d => d.DonHang.MaDDH);

            // Phân trang
            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Trả về kết quả phân trang
            return View(danhSach.ToPagedList(pageNumber, pageSize));
        }


        // Xem chi tiết đơn hàng
        [AuthenticationFilter("KH")]
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
                    d.DonDatHang.MaKhachHang
                })
                .ToList();
            if (use.VaiTro == "KH")
                if (use.MaNguoiDung != chitiet.FirstOrDefault().MaKhachHang)
                    return HttpNotFound();
            return View(chitiet);
        }

        // Hủy đơn hàng
        [AuthenticationFilter("KH")]
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
                    d.DonDatHang.ThanhToan,
                    d.DonDatHang.TinhTrang
                })
                .ToList();
            return View(chitiet);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("KH")]
        public ActionResult DeleteConfirmed(string id)
        {
            DonDatHang donDatHang = db.DonDatHangs.Find(id);
            if (donDatHang.TinhTrang == 0)
            {
                donDatHang.TinhTrang = 3; //3: Đã hủy
                db.Entry(donDatHang).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "KH_DonHangs_23TH0014");
            }
            else
            {
                return RedirectToAction("Index", "KH_DonHangs_23TH0014");
            }
        }

        // Giỏ hàng
        [AuthenticationFilter("KH")]
        public ActionResult GioHang()
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014> ?? new List<GioHang_23TH0014>();
            return View(gioHang);
        }

        // Thêm sản phẩm vào giỏ hàng
        [AuthenticationFilter("KH")]
        public ActionResult ThemVaoGioHang(string maSP, string maMau, int soLuong = 1)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014> ?? new List<GioHang_23TH0014>();
            var sanPham = db.ChiTietSanPhams.Include(s => s.SanPham).Include(s => s.MauSac).FirstOrDefault(s => s.MaSP == maSP && s.MaMau == maMau);
            var spGioHang = gioHang.FirstOrDefault(sp => sp.MaSP == maSP && sp.MaMau == maMau);
            if (spGioHang == null)
            {
                gioHang.Add(new GioHang_23TH0014
                {
                    MaSP = maSP,
                    TenSP = sanPham.SanPham.TenSP,
                    MaMau = maMau,
                    TenMau = sanPham.MauSac.TenMau,
                    GiaBan = sanPham.SanPham.GiaBan,
                    SoLuong = soLuong,
                    AnhBia = sanPham.AnhBia
                });
            }

            // Cập nhật lại session
            Session["SLMatHang"] = gioHang.Count();
            Session["GioHang"] = gioHang;

            // Trả về số lượng mặt hàng trong giỏ hàng
            return Json(new { soLuongMatHang = gioHang.Count() });
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [AuthenticationFilter("KH")]
        public void CapNhatSoLuong(string maSP, string maMau, int soLuong)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014> ?? new List<GioHang_23TH0014>();
            var spGioHang = gioHang.FirstOrDefault(sp => sp.MaSP == maSP && sp.MaMau == maMau);
            spGioHang.SoLuong = soLuong;

            // Cập nhật lại session
            Session["GioHang"] = gioHang;
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [AuthenticationFilter("KH")]
        public void XoaKhoiGioHang(string maSP, string maMau)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014>;
            gioHang.RemoveAll(sp => sp.MaSP == maSP && sp.MaMau == maMau);

            // Cập nhật lại session
            Session["GioHang"] = gioHang;
            Session["SLMatHang"] = gioHang.Count();
        }

        // Gợi ý sản phẩm
        [AuthenticationFilter("KH")]
        public ActionResult SanPhamGoiY()
        {
            var use = Session["use"] as NguoiDung;

            // B1: Thống kê số lượng sản phẩm khách hàng đã đặt mua theo Hãng sản xuất
            var topHSX = db.ChiTietDDHs
                .Where(ct => ct.DonDatHang.MaKhachHang == use.MaNguoiDung)
                .Where(ct => ct.DonDatHang.ThanhToan < 3) // Không tính những đơn hàng đã hủy
                .Include(ct => ct.SanPham)
                .GroupBy(x => x.SanPham.MaHSX)
                .Select(g => new
                {
                    MaHSX = g.Key,
                    SoLuong = g.Count()
                })
                .OrderByDescending(g => g.SoLuong)
                .Select(h => h.MaHSX)
                .Take(2)
                .ToList();

            // B2: Gợi ý sản phẩm bán chạy nhất của các hãng đó
            var spGoiY = db.ChiTietDDHs
                .Include(ct => ct.DonDatHang)
                .Include(ct => ct.SanPham)
                .Where(x => !topHSX.Any() || topHSX.Contains(x.SanPham.MaHSX)) // Nếu topHSX rỗng, lấy tất cả sản phẩm
                .Where(ct => ct.DonDatHang.ThanhToan < 3) // Không tính những đơn hàng đã hủy
                .GroupBy(x => x.SanPham.MaSP)
                .Select(g => new
                {
                    MaSP = g.Key,
                    SoLuongBan = g.Count(),
                    TenSanPham = g.Select(x => x.SanPham.TenSP).FirstOrDefault(),
                    GiaBan = g.Select(x => x.SanPham.GiaBan).FirstOrDefault(),
                    AnhBia = db.ChiTietSanPhams
                                   .Where(ct => ct.MaSP == g.Key)
                                   .Select(ct => ct.AnhBia)
                                   .FirstOrDefault()
                })
                .OrderByDescending(g => g.SoLuongBan)
                .Take(6)
                .ToList();

            return PartialView(spGoiY);
        }

        // Tạo mã đơn hàng tự động
        string LayMaDH()
        {
            var maMax = db.DonDatHangs.ToList().Select(n => n.MaDDH).Max();
            int maDH = int.Parse(maMax.Substring(2)) + 1;
            return "DH" + maDH.ToString("D3");
        }

        // Xác nhận đặt hàng
        [AuthenticationFilter("KH")]
        public ActionResult DatHang()
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014>;
            var use = Session["use"] as NguoiDung;
            ViewBag.DiaChiNhanHang = use.DiaChi;
            return View(gioHang);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthenticationFilter("KH")]
        public ActionResult DatHang(string diaChi, int? thanhToan)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014>;
            var use = Session["use"] as NguoiDung;
            string maDon = LayMaDH(); // Tạo mã đơn hàng tự động

            // Tạo đối tượng DonHang và lưu vào cơ sở dữ liệu
            var donHang = new DonDatHang
            {
                MaDDH = maDon,
                Ngay = DateTime.Now,
                TinhTrang = 0, //0: Đang chờ xác nhận
                ThanhToan = 0, //0: Chưa thanh toán
                DiaChiNhanHang = diaChi,
                MaKhachHang = use.MaNguoiDung,
                MaNhanVien = null
            };
            db.DonDatHangs.Add(donHang);
            db.SaveChanges();

            // Lưu chi tiết đơn hàng
            foreach (var item in gioHang)
            {
                var chiTiet = new ChiTietDDH
                {
                    MaDDH = maDon,
                    MaSP = item.MaSP,
                    MaMau = item.MaMau,
                    SoLuong = item.SoLuong,
                };
                db.ChiTietDDHs.Add(chiTiet);
            }
            db.SaveChanges();

            if(thanhToan == 0) //0: COD
            {
                // Xóa giỏ hàng
                Session["GioHang"] = null;
                Session["SLMatHang"] = null;
                return RedirectToAction("Index", "KH_DonHangs_23TH0014");
            } 
            else if (thanhToan == 1) //1: MoMo
            {
                return RedirectToAction("MoMo", "KH_DonHangs_23TH0014", new { id = donHang.MaDDH });
            }
            else //2: ZaloPay
            {
                return RedirectToAction("ZaloPay", "KH_DonHangs_23TH0014", new { id = donHang.MaDDH });
            }                    
        }

        // Tạo chữ ký MoMo
        private string CreateSignatureMoMo(string rawData, string secretKey)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(rawData);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }

        // Thanh toán qua MoMo
        [AuthenticationFilter("KH")]
        public ActionResult MoMo(string id)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014>;
            var use = Session["use"] as NguoiDung;
            var tenKH = db.NguoiDungs.Where(d => d.MaNguoiDung == use.MaNguoiDung).Select(d => d.HoTen);

            string orderId = id;
            string requestId = id;
            string endpoint = ConfigurationManager.AppSettings["MoMoEndpoint"];
            string partnerCode = ConfigurationManager.AppSettings["MoMoPartnerCode"];
            string accessKey = ConfigurationManager.AppSettings["MoMoAccessKey"];
            string secretKey = ConfigurationManager.AppSettings["MoMoSecretKey"];
            string amount = gioHang.Sum(g => g.GiaBan * g.SoLuong).ToString();
            string orderInfo = $"{id} - {use.MaNguoiDung} - {tenKH}";
            string returnUrl = Url.Action("KetQuaMoMo", "KH_DonHangs_23TH0014", null, Request.Url.Scheme);
            string notifyUrl = returnUrl;
            string requestType = "payWithMethod";
            string extraData = "";

            // Tạo chữ ký
            string rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
            string signature = CreateSignatureMoMo(rawSignature, secretKey);

            // Tạo dữ liệu gửi lên MoMo
            var request = new
            {
                partnerCode,
                accessKey,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                extraData,
                requestType,
                signature,
                lang = "vi",
                partnerName = "MoMo Payment",
                storeId = "Test Store",
                autoCapture = true
            };

            // Gửi yêu cầu tạo đơn hàng
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = client.PostAsync(endpoint, content).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;
                dynamic json = JsonConvert.DeserializeObject(responseBody);
                string payUrl = json?.payUrl;
                if (string.IsNullOrEmpty(payUrl))
                {
                    string code = json?.code ?? "-1";
                    string message = json?.message ?? "Không thể kết nối với Momo.";
                    return RedirectToAction("KetQuaMoMo", new { resultCode = code, id = orderId, message });
                }
                return Redirect(payUrl);
            }
        }

        // Xử lý kết quả thanh toán MoMo
        [AuthenticationFilter("KH")]
        public ActionResult KetQuaMoMo(string resultCode, string orderId, string message)
        {
            // Xóa giỏ hàng
            Session["GioHang"] = null;
            Session["SLMatHang"] = null;

            // Kiểm tra nếu thanh toán thành công
            if (resultCode == "0")
            {
                var donHang = db.DonDatHangs.FirstOrDefault(d => d.MaDDH == orderId);
                donHang.ThanhToan = 1; //1: Đã thanh toán
                db.SaveChanges();
                TempData["TB"] = $"Thanh toán thành công đơn hàng {orderId}!";
            }
            else
            {
                TempData["TB"] = $"Thanh toán thất bại: {message}";
            }
            return View();
        }

        // Thanh toán qua ZaloPay
        [AuthenticationFilter("KH")]
        public async Task<ActionResult> ZaloPay(string id)
        {
            var gioHang = Session["GioHang"] as List<GioHang_23TH0014>;
            var use = Session["use"] as NguoiDung;
            var tenKH = db.NguoiDungs.Where(d => d.MaNguoiDung == use.MaNguoiDung).Select(d => d.HoTen).FirstOrDefault();

            string appId = ConfigurationManager.AppSettings["ZaloPayAppId"];
            string key1 = ConfigurationManager.AppSettings["ZaloPayKey1"];
            string createOrderUrl = ConfigurationManager.AppSettings["ZaloPayCreateOrderUrl"];
            string appTransId = DateTime.Now.ToString("yyMMdd") + "_" + id;
            string amount = gioHang.Sum(g => g.GiaBan * g.SoLuong).ToString();
            string appTime = Utils.GetTimeStamp().ToString();
            string appUser = use.MaNguoiDung;
            string description = $"{id} - {use.MaNguoiDung} - {tenKH}";
            var embedData = new
            {
                redirecturl = Url.Action("KetQuaZaloPay", "KH_DonHangs_23TH0014", new { appTransId }, Request.Url.Scheme),
                method = ""
            };
            var items = gioHang.Select(g => new
            {
                itemid = g.MaSP,
                itemname = g.TenSP,
                itemprice = (long)g.GiaBan,
                itemquantity = g.SoLuong
            }).ToArray();

            // Tạo dữ liệu gửi lên ZaloPay
            var param = new Dictionary<string, string>
            {
                { "appid", appId },
                { "appuser", appUser },
                { "apptime", appTime.ToString() },
                { "amount", amount },
                { "apptransid", appTransId },
                { "embeddata", JsonConvert.SerializeObject(embedData) },
                { "item", JsonConvert.SerializeObject(items) },
                { "description", description },
                { "bankcode", null }
            };

            // Tạo chữ ký (mac)
            string data = $"{appId}|{param["apptransid"]}|{param["appuser"]}|{param["amount"]}|{param["apptime"]}|{param["embeddata"]}|{param["item"]}";
            param.Add("mac", HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key1, data));

            // Gửi yêu cầu tạo đơn hàng
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(createOrderUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);
                string orderUrl = result?.orderurl;
                if (string.IsNullOrEmpty(orderUrl))
                {                 
                    return RedirectToAction("KetQuaZaloPay", "KH_DonHangs_23TH0014", new { appTransId });
                }
                return Redirect(orderUrl);
            }
        }

        // Xử lý kết quả thanh toán ZaloPay
        [AuthenticationFilter("KH")]
        public async Task<ActionResult> KetQuaZaloPay(string appTransId)
        {
            // Xóa giỏ hàng
            Session["GioHang"] = null;
            Session["SLMatHang"] = null;

            string appId = ConfigurationManager.AppSettings["ZaloPayAppId"];
            string key1 = ConfigurationManager.AppSettings["ZaloPayKey1"];
            string queryOrderUrl = ConfigurationManager.AppSettings["ZaloPayQueryOrderUrl"];

            // Chuẩn bị các tham số gửi lên ZaloPay
            var param = new Dictionary<string, string>
            {
                { "appid", appId },
                { "apptransid", appTransId }
            };

            // Tạo chữ ký (mac)
            string data = $"{appId}|{appTransId}|{key1}";
            param.Add("mac", HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, key1, data));

            // Gửi yêu cầu kiểm tra trạng thái thanh toán đến ZaloPay
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(queryOrderUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                // Kiểm tra nếu yêu cầu thành công
                if (result?.returncode == "1")
                {
                    var orderId = appTransId.Split('_').Last();
                    var donHang = db.DonDatHangs.FirstOrDefault(d => d.MaDDH == orderId);
                    donHang.ThanhToan = 1; //1: Đã thanh toán
                    db.SaveChanges();
                    TempData["TB"] = $"Thanh toán thành công đơn hàng {orderId}!";
                }
                else
                {
                    TempData["TB"] = $"Thanh toán thất bại: {result?.returnmessage ?? "Không thể kết nối với ZaloPay"}";
                }
            }
            return View();
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
