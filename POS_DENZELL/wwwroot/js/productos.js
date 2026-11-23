
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

// ==========================================================
// MINICALCULADORA DE PRECIO UNITARIO (TIRAS / CAJAS A 6 DEC.)
// ==========================================================
function calcularPrecioDesdePaquete() {
    const precioPaquete = parseFloat(document.getElementById('calcPrecioPaquete')?.value);
    const unidades = parseInt(document.getElementById('calcUnidadesPaquete')?.value, 10);
    const inputPrecio = document.getElementById('inputPrecio');
    const lblResultado = document.getElementById('lblResultadoPaquete');

    if (!inputPrecio || !lblResultado) return;

    if (!precioPaquete || precioPaquete <= 0 || !unidades || unidades <= 0) {
        lblResultado.innerText = '';
        return;
    }

    const unitario = precioPaquete / unidades;

    // Deja hasta 6 decimales y limpia ceros sobrantes manteniendo al menos 2 (ej: 0.833333 o 1.50)
    let textoPrecio = unitario.toFixed(6)
        .replace(/(\.\d*?[1-9])0+$/, '$1')
        .replace(/\.0+$/, '.00');

    if (textoPrecio.includes('.') && textoPrecio.split('.')[1].length === 1) {
        textoPrecio += '0';
    }

    inputPrecio.value = textoPrecio;
    inputPrecio.classList.add('bg-success-subtle', 'fw-bold');
    setTimeout(() => inputPrecio.classList.remove('bg-success-subtle', 'fw-bold'), 800);

    lblResultado.innerText = `✓ S/ ${textoPrecio} c/u (${unidades} unid = S/ ${precioPaquete.toFixed(2)})`;
}