using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherApiBackend.Context;
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

                x.Email  == userObj.Email && x.Password == userObj.Password
            );
            if (user == null)
                return NotFound(new { Message = "User not found! " });
            return Ok(new
            {
                Message = "Login Success! "
            });
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            await _appDbContext.Users.AddAsync(userObj);
            await _appDbContext.SaveChangesAsync();
            return Ok(new
            {
                Message = "User Registered Successfully! "
            });
        }
    }
}
