document.addEventListener('DOMContentLoaded', function() {
    loadTasks();
});

async function loadTasks() {
    try {
        const response = await fetch('/Task/GetTaskId');
        const data = await response.json();
        
        const container = document.getElementById('task_container');
        container.innerHTML = '';
        
        data.forEach(task => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="edit-title">${task.title}</td>
                <td class="edit-deadline" data-iso="${task.deadline}">${formatDate(task.deadline)}</td>
                <td class="edit-hours">${task.hours}</td>
                <td>
                    <div class="btn-group">
                        <button class="btnEdit" data-task-id="${task.id}">Editar</button>
                        <button class="btnDelete" data-task-id="${task.id}">Borrar</button>
                        <button class="btnConfirm" data-task-id="${task.id}" title="Confirmar" hidden>✓</button>
                        <button class="btnCancel" title="Cancelar" hidden>↩</button>
                    </div>
                </td>
            `;
            container.appendChild(row);
        });
    } catch (error) {
        console.error('Error cargando tareas:', error);
    }
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString(); 
}

$(document).on('click', '.btnDelete', function () {
    const taskId = $(this).data('task-id');
    const buttonGroup = $(this).parent();
    buttonGroup.find('.btnEdit, .btnDelete').attr('hidden', true);
    buttonGroup.find('.btnConfirm, .btnCancel').attr('hidden', false);
    buttonGroup.find('.btnConfirm').data('action', 'delete');
});

let originalData = {};

$(document).on('click', '.btnEdit', function () {
    const row = $(this).closest('tr');
    const taskId = $(this).data('task-id');
    const buttonGroup = $(this).parent();

    buttonGroup.find('.btnEdit, .btnDelete').attr('hidden', true);
    buttonGroup.find('.btnConfirm, .btnCancel').attr('hidden', false);
    buttonGroup.find('.btnConfirm').data('action', 'edit');

    const titleTd = row.find('.edit-title');
    const deadlineTd = row.find('.edit-deadline');
    const hoursTd = row.find('.edit-hours');

    originalData[taskId] = {
        title: titleTd.text().trim(),
        deadlineIso: deadlineTd.data('iso'), 
        deadlineText: deadlineTd.text().trim(),
        hours: hoursTd.text().trim()
    };

    titleTd.html(`<input type="text" class="form-control input-title" value="${originalData[taskId].title}" style="width:100%">`);

    let isoDate = originalData[taskId].deadlineIso ? originalData[taskId].deadlineIso.split('T')[0] : '';
    deadlineTd.html(`<input type="date" class="form-control input-deadline" value="${isoDate}">`);
    hoursTd.html(`<input type="number" class="form-control input-hours" value="${originalData[taskId].hours}" min="0" step="0.5">`);
});

$(document).on('click', '.btnCancel', function () {
    const row = $(this).closest('tr');
    const buttonGroup = $(this).parent();
    const taskId = buttonGroup.find('.btnConfirm').data('task-id');

    buttonGroup.find('.btnEdit, .btnDelete').attr('hidden', false);
    buttonGroup.find('.btnConfirm, .btnCancel').attr('hidden', true);

    if (originalData[taskId]) {
        row.find('.edit-title').text(originalData[taskId].title);
        const dateTd = row.find('.edit-deadline');
        dateTd.text(originalData[taskId].deadlineText);
        dateTd.data('iso', originalData[taskId].deadlineIso);
        row.find('.edit-hours').text(originalData[taskId].hours);
        delete originalData[taskId]; 
    }
});

$(document).on('click', '.btnConfirm', async function () {
    const taskId = parseInt($(this).data('task-id'));
    const action = $(this).data('action');
    const row = $(this).closest('tr');

    if (action === 'delete') {
        await fetch(`/Task/DeleteTask/${taskId}`, { method: 'DELETE'}) 
            .then(response => {
                if (!response.ok) 
                    throw new Error(`Error HTTP: ${response.status} - No se pudo eliminar la tarea.`);
                return response;
            })
            .then(() => loadTasks())
            .catch(error => console.error('Error:', error));
    } else if (action === 'edit') {
        const newTitle = row.find('.input-title').val();
        const newDeadline = row.find('.input-deadline').val();
        const newHours = row.find('.input-hours').val();
        const updatedTask = {
            title: newTitle,
            deadline: newDeadline,
            hours: newHours
        };

        try {
            await fetch(`/Task/UpdateTask/${taskId}`, { 
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json' // Es necesario para enviar JSON
                },
                body: JSON.stringify(updatedTask)
            }).then(response => {
                if (!response.ok) 
                    throw new Error(`Error HTTP: ${response.status} - No se pudo eliminar la tarea.`);
                return response;
            }).then(() => {
                delete originalData[taskId];
                loadTasks()
            });
        } catch (error) {
            console.error('Error al actualizar:', error);
            alert("Hubo un error al guardar los cambios.");
        }
    }
});

