document.addEventListener('DOMContentLoaded', function() {
    loadTasks();
});

async function loadTasks() {
    try {
        const response = await fetch('/Task/GetTasks');
        const data = await response.json();
        
        const container = document.getElementById('task_container');
        container.innerHTML = '';
        
        data.forEach(task => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${task.title}</td>
                <td>${formatDate(task.deadline)}</td>
                <td>${task.hours}</td>
            `;
            container.appendChild(row);
        });
    } catch (error) {
        console.error('Error cargando tareas:', error);
    }
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString();
}