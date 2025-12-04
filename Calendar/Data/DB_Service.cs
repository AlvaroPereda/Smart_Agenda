using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Data
{
    public class DB_Service(DB_Configuration db)
    {
        private readonly DB_Configuration _db = db;

        public async Task AddTask(TaskItem task)
        {
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
        }

        public async Task<User> AddUser(User wser)
        {
            _db.Users.Add(wser);
            await _db.SaveChangesAsync();
            return wser;
        }

        public async Task<List<User>> GetUsers()
        {
            return await _db.Users.Include(w => w.Schedules).ToListAsync();
        }
        public async Task<User> GetUserById(int id)
        {
            var result = await _db.Users
                .Include(w => w.Schedules)
                .Include(w => w.ContainerTasks)
                .FirstOrDefaultAsync(w => w.Id == id) ?? throw new Exception("Usuario no encontrado con ese id.");
            return result;
        }

        public async Task UpdateContainerTasks(int id, TaskItem task)
        {
            try
            {
                User wser = await GetUserById(id);
                wser.ContainerTasks.Add(task);
                await _db.SaveChangesAsync();
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task<User> GetAllTasks(int id)
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
            return await _db.Users.FirstOrDefaultAsync(w => w.Name == username && w.Password == password);
        }
        public async Task DeleteTask(int UserId, int taskId)
        {
            try
            {
                User wser = await GetUserById(UserId);
                TaskItem? taskToRemove = wser.ContainerTasks.FirstOrDefault(t => t.Id == taskId);
                if (taskToRemove != null)
                {
                    wser.ContainerTasks.Remove(taskToRemove);
                    _db.Tasks.Remove(taskToRemove);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task UpdateTask(TaskItem task)
        {
            _db.Tasks.Update(task);
            await _db.SaveChangesAsync();
        }
    }
}