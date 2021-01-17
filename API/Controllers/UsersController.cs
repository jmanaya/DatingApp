using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository repo;
        private readonly IMapper mapper;
        private readonly IPhotoService photoService;
        public UsersController(DataContext context, IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            this.photoService = photoService;
            this.mapper = mapper;
            repo = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDetailsDto>>> GetUsers([FromQuery] UserParams userParams)
        {
            var user = await repo.GetUserByUsernameAsync(User.GetUsername());

            userParams.CurrentUsername = user.UserName;

            if (string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = (user.Gender == "male") ? "female" : "male";

            var users = await repo.GetUserDetailsAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        [HttpGet]
        [Route("{username}", Name = "GetUser")]
        public async Task<ActionResult<UserDetailsDto>> GetUser(string username)
        {
            return await repo.GetUserDetailsDtoAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(UserUpdateDto userUpdateDto)
        {
            var username = User.GetUsername();
            var user = await repo.GetUserByUsernameAsync(username);

            mapper.Map(userUpdateDto, user);

            repo.Update(user);

            if (await repo.SaveAllAsync())
                return NoContent();
            else
                return BadRequest("Unable to update user.");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var username = User.GetUsername();
            var user = await repo.GetUserByUsernameAsync(username);

            var result = await photoService.AddPhotoAsync(file);

            if (result.Error != null)
                return BadRequest(result.Error.Message);
            else 
            {
                var photo = new Photo 
                {
                    Url = result.SecureUrl.AbsoluteUri,
                    PublicId = result.PublicId
                };

                if (user.Photos.Count == 0)
                {
                    photo.IsMain = true;
                }

                user.Photos.Add(photo);

                if (await repo.SaveAllAsync())
                    return CreatedAtRoute("GetUser", new { username = user.UserName}, mapper.Map<PhotoDto>(photo));
                else
                    return BadRequest("Unexpected error while adding photo.");
            }
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await repo.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

            if (photo.IsMain)
                return BadRequest("This is already the main photo.");

            var currentMain = user.Photos.FirstOrDefault(p => p.IsMain);

            if (currentMain != null)
                currentMain.IsMain = false;
            
            photo.IsMain = true;

            if (await repo.SaveAllAsync())
                return NoContent();

            return BadRequest("Unable to set main photo.");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await repo.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

            if (photo == null)
                return NotFound();
            
            if (photo.IsMain)
                return BadRequest("Cannot delete main profile photo.");

            if (photo.PublicId != null)
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);

                if (result.Error != null)
                    return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await repo.SaveAllAsync())
                return Ok();
            
            return BadRequest("Unexpected error encountered while deleting photo.");
        }
    }
}
