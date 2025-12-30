using Calendar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Calendar.Data
{
    public class DB_Service(DB_Configuration db)
    {
        private readonly DB_Configuration _db = db;

        #region USER Methods
        public async Task<User> AddUser(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }
        public async Task<User?> GetUserById(Guid id)
        {
            return await _db.Users
                .Include(w => w.Schedules)
                .Include(w => w.ContainerTasks)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
        public async Task<User?> GetUserByName(string name)
        {
            return await _db.Users.FirstOrDefaultAsync(w => w.Name == name);
        }
        public async Task<User?> AuthenticateUser(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(w => w.Name == username);
            if(user == null)
                return null;
            
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            if(!isValid)
                return null;

            return user;
        }
        public async Task UpdateUser(User user)
        {
            User existingUser = await GetUserById(user.Id) ?? throw new KeyNotFoundException("Usuario no encontrado.");

            if(!string.IsNullOrEmpty(user.Name))
                existingUser.Name = user.Name;
            if(!string.IsNullOrEmpty(user.Password))
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _db.SaveChangesAsync();
        }

        #endregion
        #region TASK Methods

        public async Task UpdateContainerTasks(Guid id, CalendarEvent task)
        {
            try
            {
                User user = await GetUserById(id) ?? throw new KeyNotFoundException("Usuario no encontrado.");
                user.ContainerTasks.Add(task);
                await _db.SaveChangesAsync();
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task DeleteTask(Guid UserId, Guid taskId)
        {
            try
            {
                User user = await GetUserById(UserId) ?? throw new KeyNotFoundException("Usuario no encontrado.");
                var taskToRemove = user.ContainerTasks.FirstOrDefault(t => t.Id == taskId) ?? throw new KeyNotFoundException("Tarea no encontrada.");
                
                user.ContainerTasks.Remove(taskToRemove);
                _db.CalendarEvent.Remove(taskToRemove);
                await _db.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}