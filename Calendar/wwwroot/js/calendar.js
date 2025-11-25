document.addEventListener('DOMContentLoaded', async function() {
    const task = await loadTasks()
    const CalendarObj = window.tui.Calendar
    const calendar = new CalendarObj('#calendar', {
        defaultView: 'month',
        taskView: false,
        scheduleView: false,
        isReadOnly: false,
        month: {
            workweek: true,
            startDayOfWeek: 1,
            hourStart: 8,
            hourEnd: 22,
            taskView: false,
            visibleWeeksCount: 2
        },
        template: {
            time: function(event) {
                const hora = event.start.getHours().toString().padStart(2, '0')
                const min = event.start.getMinutes().toString().padStart(2, '0')
                return `<strong>${hora}:${min}</strong> ${event.title}`
            }
        }
    });


    document.getElementById('btnPrev').addEventListener('click', () => calendar.prev());
    document.getElementById('btnNext').addEventListener('click', () => calendar.next());
    document.getElementById('btnToday').addEventListener('click', () => calendar.today());

    // Esta parte se tiene que mejorar, esto es no es correcto
    if (task.length > 0) {
        task.forEach(e => {
            e.start = new Date(e.start);
            e.end = new Date(e.end);
            e.start.setHours(e.start.getHours() - 1);
            e.end.setHours(e.end.getHours() - 1);
        });
    }

    calendar.createEvents(task);
});

async function loadTasks() {
    try {
        const response = await fetch('/Task/GetTasksCalendar');
        const data = await response.json();
        return data
        
    } catch (error) {
        console.error('Error cargando tareas:', error);
    }
}