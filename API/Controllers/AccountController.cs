using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext context;
        private readonly ITokenService tokenService;
        public AccountController(DataContext context, ITokenService tokenService)
        {
            this.tokenService = tokenService;
            this.context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegisterDto dto)
        {
            if (await UserExists(dto.Username))
                return BadRequest("Username is already in use.");

            using var hmac = new HMACSHA512();

            var user = new User
            {
                UserName = dto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password)),
                PasswordSalt = hmac.Key
            };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            var userDto = new UserDto 
            { 
                Username = user.UserName, 
                Token = tokenService.CreateToken(user) 
            };

            return userDto;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(UserLoginDto dto)
        {
            var user =  await 
                        context.Users
                        .Include(p => p.Photos)
                        .SingleOrDefaultAsync(u => u.UserName.ToLower() == dto.Username.ToLower());

            if (user == null)
                return Unauthorized("Invalid username and/or password.");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));

            if (!CompareHash(hash, user.PasswordHash))
                return Unauthorized("Invalid username and/or password.");

            var userDto = new UserDto 
            { 
                Username = user.UserName, 
                Token = tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain)?.Url
            };

            return userDto;
        }

        private async Task<bool> UserExists(string username)
        {
            return await context.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
        }

        private bool CompareHash(byte[] computedHash, byte[] hashFromDb)
        {
            bool isMatch = true;

            for (int i = 0; i < hashFromDb.Length; i++)
            {
                if (computedHash[i] != hashFromDb[i])
                {
                    isMatch = false;
                    break;
                }
            }

            return isMatch;
        }
    }
}
