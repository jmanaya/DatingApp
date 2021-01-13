using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository repo;
        private readonly IMapper mapper;
        public UsersController(DataContext context, IUserRepository userRepository, IMapper mapper)
        {
            this.mapper = mapper;
            repo = userRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UserDetailsDto>>> GetUsers()
        {
            var users = await repo.GetUserDetailsAsync();

            return Ok(users);
        }

        [HttpGet]
        [Route("{username}")]
        public async Task<ActionResult<UserDetailsDto>> GetUser(string username)
        {
            return await repo.GetUserDetailsDtoAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(UserUpdateDto userUpdateDto) 
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await repo.GetUserByUsernameAsync(username);

            mapper.Map(userUpdateDto, user);

            repo.Update(user);

            if (await repo.SaveAllAsync())
                return NoContent();
            else
                return BadRequest("Unable to update user.");
        }
    }
}
