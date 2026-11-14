
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
// ESCÁNER DE CÓDIGO DE BARRAS PARA INVENTARIO (CREAR/EDITAR)
// ==========================================================

let scannerInventario = null;
let scannerInventarioActivo = false;
let contenedorActivoId = null;
let botonActivoId = null;

function sonarBeepInventario() {
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(1200, ctx.currentTime);
        gain.gain.setValueAtTime(0.15, ctx.currentTime);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        osc.stop(ctx.currentTime + 0.12);
    } catch (e) { }
}

function toggleScannerInventario(inputId, contenedorId, readerId, btnId) {
    if (scannerInventarioActivo) {
        detenerScannerInventario();
        return;
    }

    const contenedor = document.getElementById(contenedorId);
    const btn = document.getElementById(btnId);
    contenedor.classList.remove('d-none');
    btn.classList.replace('btn-dark', 'btn-danger');
    btn.innerHTML = '<i class="bi bi-stop-circle me-1"></i> Cerrar';

    contenedorActivoId = contenedorId;
    botonActivoId = btnId;

    scannerInventario = new Html5Qrcode(readerId);

    scannerInventario.start(
        { facingMode: "environment" },
        { fps: 10, qrbox: { width: 230, height: 100 } },
        (codigoDetectado) => {
            sonarBeepInventario();

            const inputCodigo = document.getElementById(inputId);
            if (inputCodigo) {
                inputCodigo.value = codigoDetectado.trim();
                inputCodigo.classList.add('bg-success-subtle', 'fw-bold');
                setTimeout(() => inputCodigo.classList.remove('bg-success-subtle', 'fw-bold'), 1500);
            }

            // Cerramos la cámara automáticamente apenas captura el código
            detenerScannerInventario();
        },
        () => { }
    ).then(() => {
        scannerInventarioActivo = true;
    }).catch(err => {
        console.error("Error cámara inventario:", err);
        alert("No se pudo abrir la cámara. Verifica los permisos del navegador.");
        detenerScannerInventario();
    });
}

function detenerScannerInventario() {
    const limpiarUI = () => {
        if (contenedorActivoId) {
            document.getElementById(contenedorActivoId)?.classList.add('d-none');
        }
        if (botonActivoId) {
            const btn = document.getElementById(botonActivoId);
            if (btn) {
                btn.classList.replace('btn-danger', 'btn-dark');
                btn.innerHTML = '<i class="bi bi-camera-fill me-1"></i> Escanear';
            }
        }
        scannerInventarioActivo = false;
    };

    if (scannerInventario && scannerInventarioActivo) {
        scannerInventario.stop().then(() => {
            scannerInventario.clear();
            limpiarUI();
        }).catch(() => limpiarUI());
    } else {
        limpiarUI();
    }
}

// Si el usuario cierra el modal con la cámara encendida, la apagamos automáticamente
document.addEventListener('hidden.bs.modal', function () {
    if (scannerInventarioActivo) {
        detenerScannerInventario();
    }
});