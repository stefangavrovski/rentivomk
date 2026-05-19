using RentivoMK.Models;

namespace RentivoMK.Interfaces;

public interface IUserRepository : IRepository<User>
{
	Task<User?> GetByEmailAsync(string email);
}
