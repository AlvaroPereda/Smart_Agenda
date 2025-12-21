document.addEventListener('DOMContentLoaded', async function() {
    const task = await loadTasks();
    const CalendarObj = window.tui.Calendar;

    const calendar = new CalendarObj('#calendar', {
        defaultView: 'month',
        isReadOnly: false,
        month: {
            workweek: false,
            startDayOfWeek: 1,
            visibleWeeksCount: 1
        },
        template: {
            time: function(event) {
                const hora = event.start.getHours().toString().padStart(2, '0');
                const min = event.start.getMinutes().toString().padStart(2, '0');
                return `<strong>${hora}:${min}</strong> ${event.title}`;
            }
        }
    });

    document.getElementById('btnPrev').onclick = () => calendar.prev();
    document.getElementById('btnNext').onclick = () => calendar.next();
    document.getElementById('btnToday').onclick = () => calendar.today();

    console.log(task);
    // Pendiente de mejorar
    if (task.length) {
        task.forEach(e => {
            e.start = new Date(e.start);
            e.end = new Date(e.end);
        });
        calendar.createEvents(task);
    }
});

async function loadTasks() {
    try {
        const response = await fetch('/Task/GetTasksCalendar');
        const data = await response.json();
        if(response.ok)
            return data
        else 
            console.error(`Error ${response.status}: ${data.message}`);
        
    } catch (error) {
        console.error('Error cargando tareas:', error);
    }
}