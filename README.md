# QLDT_ASP.NET
# Dự án cá nhân xây dựng website bán điện thoại bằng ASP.NET MVC
# Hướng dẫn chạy chương trình:
Bước 1: Tải và giải nén file chương trình.
Bước 2: Chạy file sql.
Bước 3: Sửa lại thông tin database (data source=your-data-source) trong WebConfig ở dòng
<connectionStrings><add name="QLDT_23TH0014Entities" connectionString="metadata=res://*/Models.Model_23TH0014.csdl|res://*/Models.Model_23TH0014.ssdl|res://*/Models.Model_23TH0014.msl;provider=System.Data.SqlClient;provider connection string=&quot;data source=your-data-source;initial catalog=QLDT_23TH0014;integrated security=True;trustservercertificate=True;MultipleActiveResultSets=True;App=EntityFramework&quot;" providerName="System.Data.EntityClient" /></connectionStrings>
Bước 4: Nếu bạn muốn sử dụng chức năng Quên mật khẩu
Sửa lại thông tin email và mật khẩu ứng dụng của bạn trong WebConfig ở dòng
<smtp from="your-email.com">
<network host="smtp.gmail.com" port="587" userName="your-email.com" password="your-app-password" enableSsl="true" />
</smtp>
Sửa lại thông tin email trong KH_TaiKhoans_23TH0014Controller ở dòng
MailMessage mail = new MailMessage
{
    From = new MailAddress("your-email.com", "No-reply"), // Sửa lại email của bạn
    ...
};
