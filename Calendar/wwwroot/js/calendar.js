document.body.classList.add("no-scroll");

document.addEventListener('DOMContentLoaded', async function() {
    const result = await loadTasks();
    const CalendarObj = window.tui.Calendar;

    const start = parseInt(result.schedule.startTime.split(':'));
    const end = parseInt(result.schedule.endTime.split(':'));

    const calendar = new CalendarObj('#calendar', {
        defaultView: 'week',
        isReadOnly: false,
        usageStatistics: false, 
        calendars: [
            {
                id: 'Work', 
                color: '#000000', 
                backgroundColor: '#9BBC76',
                borderColor: '#86a860ff',    
                dragBackgroundColor: '#9BBC76',
            },
            {
                id: 'Break',
                color: '#000000',
                backgroundColor: '#e67e22',
                borderColor: '#d35400',
                dragBackgroundColor: '#e67e22',
            }
        ],
        week: {
            taskView: false,     
            eventView: ['time'],  
            hourStart: start, 
            hourEnd: end,          
            startDayOfWeek: 1,    
            workweek: false,  
            dayNames: ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', "Sáb"]
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

    if (result.calendarEvents.length > 0) {
        const tasks = result.calendarEvents.map(task => ({
            id: task.id.toString(),
            calendarId: task.category,
            title: task.title,
            category: 'time',
            start: new Date(task.start),
            end: new Date(task.end),
        }));

        calendar.createEvents(tasks);
    }
});

async function loadTasks() {
    try {
        const response = await fetch('/Task/GetTasksCalendar');
        if (response.ok) {
            const data = await response.json();
            return data;
        } 
        else if (response.status == 401) {
            window.location.href = "/Home/login";
        }
        else {
            const errorData = await response.json();
            console.error(`Error ${response.status}: ${errorData.message}`);
        }        
    } catch (error) {
        console.error('Error cargando tareas:', error);
    }
}
