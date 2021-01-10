using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interface;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            this.mapper = mapper;
            this.context = context;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await context.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await context.Users.Include(p => p.Photos)
                .SingleOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<UserDetailsDto> GetUserDetailsDtoAsync(string username)
        {
            return await context.Users
            .Where(u => u.UserName == username)
            .ProjectTo<UserDetailsDto>(mapper.ConfigurationProvider)
            .SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<UserDetailsDto>> GetUserDetailsAsync()
        {
            return await context.Users.ProjectTo<UserDetailsDto>(mapper.ConfigurationProvider).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await context.Users.Include(p => p.Photos).ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return (await context.SaveChangesAsync() > 0);
        }

        public void Update(User user)
        {
            context.Entry(user).State = EntityState.Modified;
        }
    }
}
