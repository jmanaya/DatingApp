using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext context;
        private readonly ITokenService tokenService;
        private readonly IMapper mapper;
        public AccountController(DataContext context, ITokenService tokenService, IMapper mapper)
        {
            this.mapper = mapper;
            this.tokenService = tokenService;
            this.context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(UserRegisterDto dto)
        {
            if (await UserExists(dto.Username))
                return BadRequest("Username is already in use.");

            var user = mapper.Map<User>(dto);

            using var hmac = new HMACSHA512();

            user.UserName = dto.Username.ToLower();
            user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dto.Password));
            user.PasswordSalt = hmac.Key;

            context.Users.Add(user);

            await context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Username = user.UserName,
                Token = tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };

            return userDto;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(UserLoginDto dto)
        {
            var user = await
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
                KnownAs = user.KnownAs,
                Token = tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain)?.Url,
                Gender = user.Gender
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
