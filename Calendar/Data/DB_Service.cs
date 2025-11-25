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

        public async Task AddWorker(Worker worker)
        {
            _db.Workers.Add(worker);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Worker>> GetWorkers()
        {
            return await _db.Workers.Include(w => w.Schedules).ToListAsync();
        }
        public async Task<Worker?> GetWorkerById(int id)
        {
            return await _db.Workers
                .Include(w => w.Schedules)
                .Include(w => w.ContainerTasks)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task UpdateContainerTasks(int id, TaskItem task)
        {
            Worker? worker = await GetWorkerById(id);
            if (worker != null)
            {
                worker.ContainerTasks.Add(task);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<Worker?> GetAllTasks(int id)
        {
            return await _db.Workers
                .Include(w => w.Schedules)
                .Include(w => w.ContainerTasks)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<Worker?> AuthenticateUser(string username, string password)
        {
            return await _db.Workers.FirstOrDefaultAsync(w => w.Name == username && w.Password == password);
        }
    }
}