using System;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLDT_23TH0014.Models;

namespace QLDT_23TH0014.Filters
{
    // Custom Authentication Filter
    public class AuthenticationFilterAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        // Constructor nhận vào các vai trò được phép
        public AuthenticationFilterAttribute(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Lấy thông tin người dùng từ Session
            if (HttpContext.Current.Session["use"] is NguoiDung use)
            {
                // Kiểm tra nếu vai trò người dùng không thuộc các vai trò cho phép
                if (!_allowedRoles.Contains(use.VaiTro))
                {
                    filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                    return;
                }
            }
            else
            {
                // Nếu chưa đăng nhập, chuyển hướng về trang đăng nhập
                filterContext.Result = new RedirectResult("/KH_TaiKhoans_23TH0014/DangNhap");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
