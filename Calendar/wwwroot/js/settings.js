document.addEventListener('DOMContentLoaded', async function() {
    const UserSettings = await getUserSettings();
    $('#name').val(UserSettings.name);
    $('#main-start').val(UserSettings.schedules[0].startTime);
    $('#main-end').val(UserSettings.schedules[0].endTime);
});

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

$(document).on('click', '#log-out', async function () {
    const response = await fetch("/Home/Logout", { method: 'POST' });
    if(response.ok) return window.location.href = "/Home/login";
});

$(document).on('click', '#save-user', async function () {
    const $name = $('#name');
    const $password = $('#password');
    const $errorLabel = $('#user-error');

    clearError($errorLabel, [$name, $password]);

    const name = $name.val();
    const password = $password.val();

    if(!name) return showError($errorLabel, $name, "El nombre de usuario es obligatorio.")

    if(!password) return showError($errorLabel, $password, "La contraseña no puede estar vacía.");

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
            showError($errorLabel, $name, data.message);
        }
    } catch (error) {
        console.error("Error al actualizar el usuario:", error);
        return null;
    }
});


$(document).on('click', '#add-schedule', function () {
    const newRowHtml = `
        <div class="schedule-row extra-row" style="margin-top: 10px;">
            <span>Descanso / Extra</span>
            <div class="time-range-container">
                <div class="time-range">
                    <input type="time" class="sub-start">
                    <span style="text-align: center">—</span>
                    <input type="time" class="sub-end">
                    <button type="button" class="remove-btn" style="border:none; background:none; cursor:pointer; margin-left:5px;">❌</button>
                </div>
                <span class="row-error" style="color: #e74c3c; font-size: 0.8em; display: block; margin-top: 4px;"></span>
            </div>
        </div>
    `;

    $('#extra-schedules-container').append(newRowHtml);
});

$(document).on('click', '.remove-btn', function() {
    $(this).closest('.schedule-row').remove();
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
    const $errorSpan = $row.find('.row-error');

    const subStart = $inputStart.val();
    const subEnd = $inputEnd.val();

    const mainStart = $('#main-start').val();
    const mainEnd = $('#main-end').val();

    $errorSpan.text('');
    $inputStart.css('border', '1px solid #ccc');
    $inputEnd.css('border', '1px solid #ccc');

    if (!subStart || !subEnd || !mainStart || !mainEnd) return;

    if (subStart >= subEnd) {
        showError($errorSpan, $inputStart, "El inicio debe ser antes del fin.");
        return;
    }

    if (subStart < mainStart) {
        showError($errorSpan, $inputStart, `No puede ser antes de la entrada (${mainStart})`);
        return;
    }

    if (subEnd > mainEnd) {
        showError($errorSpan, $inputEnd, `No puede ser después de la salida (${mainEnd})`);
        return;
    }
}

$(document).on('click', '#save-schedules', async function () {
    const $btn = $(this);
    
    const mainStart = $('#main-start').val();
    const mainEnd = $('#main-end').val();
    
    const breaks = [];
    let hasErrors = false;

    $('.extra-row').each(function() {
        const start = $(this).find('.sub-start').val();
        const end = $(this).find('.sub-end').val();

        if (!start || !end) {
            hasErrors = true;
            $(this).find('.sub-start').css('border', '1px solid red');
        } else {
            breaks.push({
                startTime: start,
                endTime: end
            });
        }
    });

    const scheduleData = {
        mainStartTime: mainStart,
        mainEndTime: mainEnd,
        breaks: breaks
    };

    console.log("Enviando datos:", scheduleData);

    const originalText = $btn.text();
    $btn.text('Guardando...').prop('disabled', true);

    try {
        const response = await fetch("/Home/SaveSchedule", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(scheduleData)
        });

        if (response.ok) {
            const result = await response.json();
            alert("✅ Horarios guardados correctamente");
            // Opcional: Recargar o actualizar UI
        } else {
            const error = await response.json();
            alert("❌ Error al guardar: " + (error.message || "Error desconocido"));
        }
    } catch (err) {
        console.error(err);
        alert("❌ Error de conexión");
    } finally {
        // Restaurar botón
        $btn.text(originalText).prop('disabled', false);
    }
});