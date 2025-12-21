document.addEventListener('DOMContentLoaded', async function() {
    const task = await loadTasks();
    const CalendarObj = window.tui.Calendar;

    const calendar = new CalendarObj('#calendar', {
        defaultView: 'week',
        isReadOnly: false,
        usageStatistics: false, 
        
        week: {
            taskView: false,     
            eventView: ['time'],  
            hourStart: 7,     // Esto hay que cambiarlo en función del schedule del usuario    
            hourEnd: 19,          
            startDayOfWeek: 1,    
            workweek: true,  
            dayNames: ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb']
        },
        
        template: {
            weekDayName: function(model) {
                return `<span class="tui-full-calendar-dayname-date">${model.date}</span>&nbsp;&nbsp;<span class="tui-full-calendar-dayname-name">${model.dayName}</span>`;
            },
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

    if (task.length) {
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