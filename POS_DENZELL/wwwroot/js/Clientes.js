document.addEventListener("DOMContentLoaded", function () {
    const buscador = document.getElementById('buscadorClientes');
    if (buscador) {
        buscador.addEventListener('input', function () {
            const filtro = this.value.toLowerCase().trim();
            document.querySelectorAll('.fila-cliente').forEach(fila => {
                const texto = fila.getAttribute('data-busqueda') || '';
                fila.style.display = texto.includes(filtro) ? '' : 'none';
            });
        });
    }
});

function abrirModalEditarCliente(id, tipoDoc, numeroDoc, nombre, telefono, direccion) {
    document.getElementById('editIdCliente').value = id;
    document.getElementById('editTipoDocumento').value = tipoDoc;
    document.getElementById('editNumeroDocumento').value = numeroDoc;
    document.getElementById('editNombreCompleto').value = nombre;
    document.getElementById('editTelefono').value = telefono;
    document.getElementById('editDireccion').value = direccion;

    const modal = new bootstrap.Modal(document.getElementById('modalEditarCliente'));
    modal.show();
}