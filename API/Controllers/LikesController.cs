using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly ILikesRepository likesRepo;
        private readonly IUserRepository userRepo;
        public LikesController(IUserRepository userRepo, ILikesRepository likesRepo)
        {
            this.userRepo = userRepo;
            this.likesRepo = likesRepo;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await userRepo.GetUserByUsernameAsync(username);
            var sourceUser = await likesRepo.GetUserWithLikes(sourceUserId);

            if (likedUser == null)
                return NotFound();

            if (sourceUser.UserName == username)
                return BadRequest("You cannot like your own profile.");

            var userLike = await likesRepo.GetUserLike(sourceUserId, likedUser.Id);

            if (userLike != null)
                return BadRequest("You have already liked this user.");

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);

            if (await userRepo.SaveAllAsync())
                return Ok();

            return BadRequest("Unable to like user.");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLikes([FromQuery] LikeParams likeParams)
        {
            likeParams.UserId = User.GetUserId();

            var users = await likesRepo.GetUserLikes(likeParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }
    }
}
