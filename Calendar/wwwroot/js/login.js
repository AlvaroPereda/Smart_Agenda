let login = true;

const toggleBtn = document.getElementById("toggle");
const submitBtn = document.getElementById("submit-button");
const actionInput = document.getElementById("action-input");
const extraFields = document.getElementById("extra-fields");
const formTitle = document.getElementById("form-title");
const toggleText = document.getElementById("toggle-text");
const start = document.getElementById("start");
const end = document.getElementById("end");

toggleBtn.onclick = () => {
    login = !login;

    submitBtn.innerText = login ? "Entrar" : "Crear cuenta";
    actionInput.value = login ? "login" : "register";
    toggleBtn.innerText = login ? "Registrarse" : "Iniciar sesión";
    toggleText.innerText = login ? "¿No tienes cuenta?" : "¿Ya tienes cuenta?";
    formTitle.innerText = login ? "Iniciar sesión" : "Registro";

    extraFields.style.display = login ? "none" : "block";
    start.required = !login;
    end.required = !login;
    start.disabled = login; 
    end.disabled = login;
};