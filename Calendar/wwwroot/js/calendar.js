document.addEventListener('DOMContentLoaded', async function() {
    const task = await loadTasks();
    const CalendarObj = window.tui.Calendar;
    const calendar = new CalendarObj('#calendar', {
        defaultView: 'week',
        isReadOnly: false,
        week: {
            startDayOfWeek: 1,
            hourStart: 8,
            hourEnd: 22,
        },
    });


    document.getElementById('btnPrev').addEventListener('click', () => calendar.prev());
    document.getElementById('btnNext').addEventListener('click', () => calendar.next());
    document.getElementById('btnToday').addEventListener('click', () => calendar.today());
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