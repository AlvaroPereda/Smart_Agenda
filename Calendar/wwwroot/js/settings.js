const $user_error = $('#user-error');
const $schedule_error = $('#schedule-error');

document.addEventListener('DOMContentLoaded', loadSettings);

async function loadSettings() {
    $('#schedules-table tbody .extra-row').remove();
    const UserSettings = await getUserSettings();
    $('#name').val(UserSettings.name);
    $('#main-start').val(UserSettings.schedule.startTime);
    $('#main-end').val(UserSettings.schedule.endTime);

    UserSettings.containerTasks.forEach(task => generateTD(task.id, task.title, task.start, task.end));
}

async function getUserSettings() {
    try {
        const response = await fetch("/Home/GetUser");
        if (response.ok) {
            return await response.json();
        } else {
            const errorData = await response.json();
            console.error(`Error ${response.status}: ${errorData.message}`);
        }
    }
    catch (error) {
        console.error("Error obteniendo al usuario:", error);
        return null;
    }
}

function showError($label, $input, message) {
    $label.show();
    $label.text(message);
    $input.css('border', '1px solid #e74c3c');
}

function clearError($label, $inputs) {
    $label.text('');
    $label.hide();
    $inputs.forEach($input => $input.css('border', '1px solid #ccc'));
}

$(document).on('click', '#log-out', async function () {
    const response = await fetch("/Home/Logout", { method: 'POST' });
    if(response.ok) return window.location.href = "/Home/login";
});

$(document).on('click', '#save-user', async function () {
    const $name = $('#name');
    const $password = $('#password');

    clearError($user_error, [$name, $password]);

    const name = $name.val();
    const password = $password.val();

    if(!name) return showError($user_error, $name, "El nombre de usuario es obligatorio.")

    if(!password) return showError($user_error, $password, "La contraseña no puede estar vacía.");

    const updateUser = {name, password};

    try {
        const response = await fetch(`/Home/UpdateUser`, { 
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json' // Es necesario para enviar JSON
                },
                body: JSON.stringify(updateUser)
        });

        if(response.ok) window.location.reload();
        else if (response.status == 401)
            return window.location.href = "/Home/login";
        else {
            const data = await response.json();
            console.error(`Error ${response.status}: ${data.message}`);
            showError($user_error, $name, data.message);
        }
    } catch (error) {
        console.error("Error al actualizar el usuario:", error);
        return null;
    }
});


function generateTD(id, title, start, end) {
    const $tbody = $('#schedules-table tbody');
    id_aux = id == "" ? "" : "id = '" + id + "'";
    $tbody.append(`
        <tr ${id_aux} class="schedule-row extra-row" >
            <td><input type="text" value="${title}"></td>
            <td><input type="time" class="sub-start" value="${start}"></td>
            <td>
                <input type="time" class="sub-end" value="${end}">
                <button type="button" class="remove-btn secondary-btn" style="margin-left:8px;">Eliminar</button>
            </td>
        </tr>
    `);
}

$(document).on('click', '#add-schedule', function () {
    generateTD("", "Descanso", "", "");
});

$(document).on('click', '.remove-btn', async function() {
    const id = $(this).closest('.schedule-row').attr('id');
    clearError($schedule_error, []);
    if(id == undefined) {
        $(this).closest('.schedule-row').remove();
    } 
    else {
        try {
            const response = await fetch( `/Task/DeleteTask/${id}`, { method: 'DELETE'}) 
            if (response.ok) await loadSettings();
            else if (response.status == 401) return window.location.href = "/Home/login";
            else {
                const data = await response.json();
                console.error(`Error ${response.status}: ${data.message}`);
                showError($user_error, $name, data.message);
            }
        } catch (error) {
            console.error("Error al borrar al usuario:", error);
            return null;
        }
        
    }
});

$(document).on('change', '.sub-start, .sub-end', function() {
    validateScheduleRow($(this).closest('.schedule-row'));
});

$('#main-start, #main-end').on('change', function() {
    $('.extra-row').each(function() {
        validateScheduleRow($(this));
    });
});

function validateScheduleRow($row) {
    const $inputStart = $row.find('.sub-start');
    const $inputEnd = $row.find('.sub-end');

    const subStart = $inputStart.val();
    const subEnd = $inputEnd.val();

    const mainStart = $('#main-start').val();
    const mainEnd = $('#main-end').val();

    $inputStart.css('border', '1px solid #ccc');
    $inputEnd.css('border', '1px solid #ccc');

    clearError($schedule_error, [$inputStart, $inputEnd]);

    if (!subStart || !subEnd || !mainStart || !mainEnd) return;

    if (subStart >= subEnd) {
        showError($schedule_error, $inputStart, "El inicio debe ser antes del fin.");
        return;
    }

    if (subStart < mainStart) {
        showError($schedule_error, $inputStart, `No puede ser antes de la entrada (${mainStart})`);
        return;
    }

    if (subEnd > mainEnd) {
        showError($schedule_error, $inputEnd, `No puede ser después de la salida (${mainEnd})`);
        return;
    }
}

$(document).on('change keyup', '.extra-row input, .main-row input', function() {
    $(this).closest('tr').addClass('changed');
});

$(document).on('click', '#save-schedules', async function () {
    const $btn = $(this);
    const originalText = $btn.text();
    
    const promises = [];
    let hasErrors = false;

    $btn.text('Procesando...').prop('disabled', true);

    const $mainRow = $('.main-row');

    // Logica para horario principal
    if ($mainRow.hasClass('changed')) {
        const mainStart = $('#main-start').val();
        const mainEnd = $('#main-end').val();

        // Validación simple
        if (!mainStart || !mainEnd) {
            hasErrors = true;
            $('#main-start').css('border', '1px solid red');
        } else {
            const mainScheduleData = {
                StartTime: mainStart,
                EndTime: mainEnd
            };

            const requestMain = fetch("/Home/UpdateSchedule", { 
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(mainScheduleData)
            }).then(async res => {
                if(!res.ok) throw new Error("Error actualizando horario principal");
                $mainRow.removeClass('changed'); 
            });

            promises.push(requestMain);
        }
    }

    // Logica para los tramos
    $('.extra-row').each(function() {
        const $row = $(this);
        const id = $row.attr('id'); 
        
        const $titleInput = $row.find('input[type="text"]');
        const $startInput = $row.find('.sub-start');
        const $endInput = $row.find('.sub-end');

        const title = $titleInput.val();
        const start = $startInput.val();
        const end = $endInput.val();


        if (!start || !end) {
            hasErrors = true;
            $startInput.css('border', '1px solid red');
            return; 
        }

        const taskData = {
            Title: title || "Descanso",
            Category: "Break",
            Start: start,
            End: end,
        };

        if (!id) {
            const request = fetch("/Task/CreateBreakTask", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(taskData)
            }).then(async res => {
                if(!res.ok) throw new Error("Error creando");
            });
            promises.push(request);
        }
        else if (id && $row.hasClass('changed')) {
            const request = fetch(`/Task/UpdateBreakTask/${id}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(taskData)
            }).then(res => {
                if(!res.ok) throw new Error(`Error actualizando ${id}`);
                $row.removeClass('changed');
            });
            promises.push(request);
        }
    });
    clearError($schedule_error, []);
    if (hasErrors) {
        $btn.text(originalText).prop('disabled', false);
        schedule_error($schedule_error,"", "Por favor, corrige los errores antes de guardar.");
        return;
    }

    try {
        await Promise.all(promises);
        
        await loadSettings(); 

    } catch (err) {
        console.error(err);
    } finally {
        $btn.text(originalText).prop('disabled', false);
    }
});