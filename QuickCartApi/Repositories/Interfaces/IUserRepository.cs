using QuickCartApi.Entities;

namespace QuickCartApi.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User> AddUserAsync(User user);
        Task<User?> FindUserAsync(string username);
        Task<User?> FindUserByIdAsync(int id);
        Task<bool> FindUserNameExistAsync(string username);
        Task<User> UpdateUserAsync(User user);
        Task<bool> FindEmailExistAsync(string email);
        Task<bool> UsernameTakenByOtherAsync(string username, int excludeUserId);
        Task<bool> EmailTakenByOtherAsync(string email, int excludeUserId);
    }
}
