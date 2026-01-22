using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Caro.Core.Entities;
using Caro.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Caro.Services;

/// <summary>
/// Service xử lý authentication và authorization cho ứng dụng Caro Online
/// Bao gồm đăng ký, đăng nhập, và tạo JWT token
/// </summary>
public class AuthService : IAuthService
{
    private readonly CaroDbContext _db; // Database context để truy cập dữ liệu
    private readonly IConfiguration _config; // Configuration để lấy JWT settings

    /// <summary>
    /// Khởi tạo AuthService với database context và configuration
    /// </summary>
    /// <param name="db">Database context</param>
    /// <param name="config">Configuration để lấy JWT settings</param>
    public AuthService(CaroDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới với username, email và password
    /// </summary>
    /// <param name="username">Tên đăng nhập (phải unique)</param>
    /// <param name="email">Email (phải unique và đúng format)</param>
    /// <param name="password">Mật khẩu dạng plain text (sẽ được hash bằng BCrypt)</param>
    /// <param name="displayName">Tên hiển thị (tùy chọn)</param>
    /// <param name="ct">Cancellation token để hủy operation nếu cần</param>
    /// <returns>User entity đã được tạo</returns>
    /// <exception cref="ArgumentException">Nếu input không hợp lệ (username/email/password rỗng, email sai format, password < 6 ký tự)</exception>
    /// <exception cref="InvalidOperationException">Nếu username hoặc email đã tồn tại</exception>
    public async Task<User> RegisterAsync(string username, string email, string password, string? displayName, CancellationToken ct = default)
    {
        // Kiểm tra input không được rỗng
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));
        
        // Kiểm tra format email bằng regex
        // Pattern: có ký tự @, có domain, không có khoảng trắng
        if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException("Invalid email format", nameof(email));
        
        // Kiểm tra độ dài password tối thiểu 6 ký tự
        if (password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long", nameof(password));

        // Kiểm tra username đã tồn tại chưa
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            throw new InvalidOperationException("Username already exists");

        // Kiểm tra email đã tồn tại chưa
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already exists");

        // Hash password bằng BCrypt với cost factor mặc định (10)
        // BCrypt tự động thêm salt và hash password một cách an toàn
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), // Hash password trước khi lưu
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow // Lưu thời gian tạo tài khoản
        };
        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Xử lý lỗi constraint violation từ database
            // Có thể xảy ra nếu có race condition (2 requests cùng lúc tạo user với cùng username/email)
            if (dbEx.InnerException != null)
            {
                var innerMsg = dbEx.InnerException.Message;
                // Kiểm tra lỗi unique constraint cho Email
                if (innerMsg.Contains("Email") || innerMsg.Contains("IX_Users_Email"))
                    throw new InvalidOperationException("Email already exists");
                // Kiểm tra lỗi unique constraint cho Username
                if (innerMsg.Contains("Username") || innerMsg.Contains("IX_Users_Username"))
                    throw new InvalidOperationException("Username already exists");
            }
            throw; // Nếu không xác định được lỗi cụ thể, throw lại exception gốc
        }
        return user;
    }

    /// <summary>
    /// Xác thực người dùng bằng username/email và password
    /// </summary>
    /// <param name="usernameOrEmail">Username hoặc email để đăng nhập</param>
    /// <param name="password">Mật khẩu dạng plain text</param>
    /// <param name="ct">Cancellation token để hủy operation nếu cần</param>
    /// <returns>Tuple chứa User entity và JWT token</returns>
    /// <exception cref="ArgumentException">Nếu usernameOrEmail hoặc password rỗng</exception>
    /// <exception cref="InvalidOperationException">Nếu thông tin đăng nhập không hợp lệ (user không tồn tại hoặc password sai)</exception>
    public async Task<(User user, string token)> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        // Kiểm tra input không được rỗng
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            throw new ArgumentException("Username or email is required", nameof(usernameOrEmail));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        // Tìm user trong database bằng username hoặc email
        // Sử dụng FirstOrDefaultAsync để tránh exception nếu không tìm thấy
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == usernameOrEmail || u.Email == usernameOrEmail, ct)
                   ?? throw new InvalidOperationException("Invalid credentials"); // Nếu không tìm thấy user
        
        // Xác thực password bằng BCrypt.Verify
        // BCrypt tự động extract salt từ hash và so sánh với password input
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials"); // Password không khớp
        
        // Tạo JWT token để client sử dụng cho các request sau
        var token = GenerateJwtToken(user);
        return (user, token);
    }

    /// <summary>
    /// Tạo JWT token cho user đã đăng nhập
    /// Token chứa thông tin user ID và username, có thời hạn 7 ngày
    /// </summary>
    /// <param name="user">User entity để lấy thông tin (Id, Username)</param>
    /// <returns>JWT token dạng string</returns>
    public string GenerateJwtToken(User user)
    {
        // Lấy JWT key từ configuration, nếu không có thì dùng default key (chỉ dùng cho dev)
        // Production phải set key trong appsettings.json hoặc environment variable
        var key = _config["Jwt:Key"] ?? "super_secret_dev_key_change_me_to_production_256bits_minimum_required_here";
        var issuer = _config["Jwt:Issuer"] ?? "Caro"; // Issuer của token
        var audience = _config["Jwt:Audience"] ?? "CaroClients"; // Audience của token

        // Tạo claims (thông tin trong token)
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject claim = User ID
            new("username", user.Username) // Custom claim chứa username
        };

        // Tạo signing credentials với symmetric key (HMAC SHA256)
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), 
            SecurityAlgorithms.HmacSha256);
        
        // Tạo JWT token với:
        // - Issuer: người phát hành token
        // - Audience: người nhận token
        // - Claims: thông tin user
        // - Expires: hết hạn sau 7 ngày
        // - SigningCredentials: chữ ký để verify token
        var token = new JwtSecurityToken(
            issuer, 
            audience, 
            claims, 
            expires: DateTime.UtcNow.AddDays(7), // Token hết hạn sau 7 ngày
            signingCredentials: creds);
        
        // Serialize token thành string để gửi về client
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


