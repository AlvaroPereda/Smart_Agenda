let login = true;

const toggleBtn = document.getElementById("toggle");
const submitBtn = document.getElementById("submit-button");
const actionInput = document.getElementById("action-input");
const extraFields = document.getElementById("extra-fields");
const formTitle = document.getElementById("form-title");
const toggleText = document.getElementById("toggle-text");

toggleBtn.onclick = () => {
    login = !login;

    submitBtn.innerText = login ? "Entrar" : "Crear cuenta";
    actionInput.value = login ? "login" : "register";
    toggleBtn.innerText = login ? "Registrarse" : "Iniciar sesión";
    toggleText.innerText = login ? "¿No tienes cuenta?" : "¿Ya tienes cuenta?";
    formTitle.innerText = login ? "Iniciar sesión" : "Registro";

    extraFields.style.display = login ? "none" : "block";
};

// Rellenar selects de horas
function fillTimeSelect(select) {
    for (let h = 0; h < 24; h++) {
        const hh = String(h).padStart(2, '0');
        ["00", "30"].forEach(mm => {
            const opt = document.createElement("option");
            opt.value = `${hh}:${mm}`;
            opt.textContent = `${hh}:${mm}`;
            select.appendChild(opt);
        });
    }
}

fillTimeSelect(document.getElementById("start"));
fillTimeSelect(document.getElementById("end"));
