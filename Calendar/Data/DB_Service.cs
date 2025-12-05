using Calendar.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<User> GetUserById(Guid id)
        {
            var result = await _db.Users
                .Include(w => w.Schedules)
                .Include(w => w.ContainerTasks)
                .FirstOrDefaultAsync(w => w.Id == id) ?? throw new Exception("Usuario no encontrado con ese id.");
            return result;
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

        #endregion
        #region TASK Methods
        public async Task UpdateContainerTasks(Guid id, TaskItem task)
        {
            try
            {
                User user = await GetUserById(id);
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
                User user = await GetUserById(UserId);
                TaskItem? taskToRemove = user.ContainerTasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToRemove != null)
                {
                    user.ContainerTasks.Remove(taskToRemove);
                    _db.Tasks.Remove(taskToRemove);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}