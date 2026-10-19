
document.addEventListener("DOMContentLoaded", function () {
    const inputBusqueda = document.getElementById("inputBusqueda");
    const selectCategoria = document.getElementById("selectCategoria");
    const filas = document.querySelectorAll(".fila-producto");
    const mensajeVacio = document.getElementById("mensajeVacio");

    // Medida de seguridad: Si no estamos en la vista de productos detenemos el script
    if (!inputBusqueda || !selectCategoria) return;

    function filtrarTabla() {
        const textoBuscado = inputBusqueda.value.toLowerCase().trim();
        const categoriaSeleccionada = selectCategoria.value;
        let productosVisibles = 0;

        filas.forEach(fila => {
            const nombre = fila.getAttribute("data-nombre");
            const codigo = fila.getAttribute("data-codigo");
            const categoriaFila = fila.getAttribute("data-categoria");
            const esFiesta = fila.getAttribute("data-fiesta");

            const cumpleTexto = nombre.includes(textoBuscado) || codigo.includes(textoBuscado);

            
            let cumpleCategoria = false;
            if (categoriaSeleccionada === "") {
                cumpleCategoria = true;
            } else if (categoriaSeleccionada === "fiesta") {
                cumpleCategoria = (esFiesta === "true");
            } else {
                cumpleCategoria = (categoriaFila === categoriaSeleccionada);
            }

            if (cumpleTexto && cumpleCategoria) {
                fila.style.display = "";
                productosVisibles++;
            } else {
                fila.style.display = "none";
            }
        });

        if (mensajeVacio) {
            mensajeVacio.style.display = productosVisibles === 0 ? "" : "none";
        }
    }

    // filtro texto
    inputBusqueda.addEventListener("input", filtrarTabla);
    
    // filtro categoria
    selectCategoria.addEventListener("change", filtrarTabla);

    filtrarTabla();

    // por si acaso
    window.addEventListener("pageshow", function () {
        setTimeout(filtrarTabla, 50);
    });
});