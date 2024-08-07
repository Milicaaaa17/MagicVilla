using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MagicVilla_VillaAPI.Helper;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public UserController(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserDTO userDto)
        {
            if (_db.Users.Any(u => u.Username == userDto.Username))
            {
                return BadRequest("Username already exists.");
            }
            if (_db.Users.Any(u => u.Email == userDto.Email))
            {
                return BadRequest("Email already exists.");
            }

            // Hash the password
            var passwordHash = HashPassword(userDto.Password);

            string role = _db.Users.Any(u => u.Role == "Admin") ? "User" : "Admin";

            // Create a new User entity
            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = passwordHash,
                Role = role
            };

            // Add the user to the database
            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok("User registered successfully.");
        }


        private string HashPassword(string password)
        {
            // Generate a salt
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            // Hash the password
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            // Combine salt and hash
            var hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            // Convert to base64 string
            return Convert.ToBase64String(hashBytes);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO loginDTO)
        {

            var user = _db.Users.FirstOrDefault(u => u.Email == loginDTO.Email);
            //provjerim da li je korisnik pronadjen u bazi i da li je dobra lozinka
            if (user != null && VerifyPassword(loginDTO.Password, user.PasswordHash))
            {
                var token = TokenHelper.GenerateToken(user, _configuration);

                return Ok(new { Token = token, User = user });
            }
            return NoContent();
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            // Extract the salt from the stored hash
            var hashBytes = Convert.FromBase64String(storedHash);
            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            // Compute the hash for the input password
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32);

            // Compare the computed hash with the stored hash
            var hashBytesInput = new byte[48];
            Array.Copy(salt, 0, hashBytesInput, 0, 16);
            Array.Copy(hash, 0, hashBytesInput, 16, 32);

            return Convert.ToBase64String(hashBytesInput) == storedHash;
        }


       [HttpGet("signin-google")]
        public IActionResult GoogleLogin()
        {

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", "User")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("signin-google/response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest($"Authentication failed. Error: {result.Failure?.Message}");

            var userInfo = result.Principal;
            var email = userInfo.FindFirst(ClaimTypes.Email)?.Value;

            var user = _db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Username = userInfo.FindFirst(ClaimTypes.Name)?.Value ?? email,
                    Email = email,
                    Role = "User"
                };
                _db.Users.Add(user);
                _db.SaveChanges();
            }

            var token = TokenHelper.GenerateToken(user, _configuration);

            return Ok(new
            {
                Token = token,
                Name = userInfo.FindFirst(ClaimTypes.Name)?.Value,
                Email = email
            });


        }
        }
}

