function abrirModalEditar(id, nombre, username, rol) {
    document.getElementById('editIdUsuario').value = id;
    document.getElementById('editNombre').value = nombre;
    document.getElementById('editUsername').value = username;
    document.getElementById('editRol').value = rol;

    const modal = new bootstrap.Modal(document.getElementById('modalEditarUsuario'));
    modal.show();
}