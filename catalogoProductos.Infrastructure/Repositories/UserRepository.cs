using Microsoft.EntityFrameworkCore;
using catalogoProductos.Domain.Entities;
using catalogoProductos.Domain.Interfaces;
using catalogoProductos.Infrastructure.Data;

namespace catalogoProductos.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        public async Task<User?> GetByIdAsync(int id)
            => await _db.Users.FindAsync(id);

        public async Task<User?> GetByUserNameAsync(string username)
            => await _db.Users.FirstOrDefaultAsync(u => u.UserName == username);

        public async Task<User?> GetByEmailAsync(string email)
            => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<IEnumerable<User>> GetAllAsync()
            => await _db.Users.ToListAsync();

        public async Task<User> AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
        
    }
}