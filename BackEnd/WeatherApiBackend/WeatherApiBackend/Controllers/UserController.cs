using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WeatherApiBackend.Context;
using WeatherApiBackend.Helpers;
using WeatherApiBackend.Models;

namespace WeatherApiBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;
        public UserController(AppDbContext appDbContext)
        {
            _appDbContext= appDbContext;
        }
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _appDbContext.Users.FirstOrDefaultAsync( x =>

                x.Email  == userObj.Email );
            if (user == null)
                return NotFound(new { Message = "User not found! " });
            if(!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest( new { Message = "Password is Incorrect"});
            }

            user.Token = CreateJwt(user);

            return Ok(new
            {
                Token = user.Token,
                Message = "Login Success! "
            });
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            // check email
            if(await CheckEmailExistAsync(userObj.Email)) 
                return BadRequest(new {Message = "Username (Email) Already Exists! "});


            // chech pass
            var pass = CheckPasswordStrenght(userObj.Password);
            if(!string.IsNullOrEmpty(pass))
                return BadRequest(new {Message = pass.ToString() });

            userObj.Password = PasswordHasher.HashPassword(userObj.Password);
            userObj.Token = "";


            await _appDbContext.Users.AddAsync(userObj);
            await _appDbContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "User Registered Successfully! "
            });
        }

        private Task<bool> CheckEmailExistAsync(string email)
            => _appDbContext.Users.AnyAsync(x => x.Email == email);

        private string CheckPasswordStrenght(string password)
        {
            StringBuilder sb = new StringBuilder();
            if(password.Length < 8)
                sb.Append("Minimum password lenght should be atleast 8" +Environment.NewLine);
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should contain atleast one Capital Letter and a number" + Environment.NewLine);
            if (!Regex.IsMatch(password, "[<,>,@,!,#,$,%,^,&,*,(,),_,+,\\[,\\],{,},?,:,;,|,',\\,.,/,~,`,-,=]"))
                sb.Append("Password should contain special charcter" + Environment.NewLine);
            return sb.ToString();
        }

        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("verysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                /*new Claim(ClaimTypes.Role, user.Role),*/
                new Claim(ClaimTypes.Email,user.Email)
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddSeconds(60),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }
        [HttpGet]
        public async Task<ActionResult<User>> GetAllUsers()
        {
            return Ok(await _appDbContext.Users.ToListAsync());
        }
    }
}
