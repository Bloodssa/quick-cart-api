using Microsoft.EntityFrameworkCore;
using QuickCartApi.Data;
using QuickCartApi.Entities;
using QuickCartApi.Repositories.Interfaces;

namespace QuickCartApi.Repositories
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        public async Task<User> AddUserAsync(User user)
        {
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> FindEmailExistAsync(string email) =>
            await context.Users.AnyAsync(u => u.Email == email);

        public async Task<User?> FindUserAsync(string username)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> FindUserByIdAsync(int id)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> FindUserNameExistAsync(string username)
        {
            return await context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> UsernameTakenByOtherAsync(string username, int excludeUserId) =>
            await context.Users.AnyAsync(u => u.Username == username && u.Id != excludeUserId);

        public async Task<bool> EmailTakenByOtherAsync(string email, int excludeUserId) =>
            await context.Users.AnyAsync(u => u.Email == email && u.Id != excludeUserId);
    }
}
