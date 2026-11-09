// Variables globales para el filtro cruzado
let categoriaActual = 'todas';
let textoBusqueda = '';

// Función auxiliar para redondeo a 2 decimales estoy usando 3
function redondearSoles(monto) {
    return Math.round((monto + Number.EPSILON) * 100) / 100;
}

// Filtrar por Botón de Categoría
function filtrarPorCategoria(idCat, botonSeleccionado) {
    categoriaActual = idCat;

    // Cambiar el diseño de los botones para ver cuál está activo
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.remove('btn-success');
        btn.classList.add('btn-outline-success');
    });
    botonSeleccionado.classList.remove('btn-outline-success');
    botonSeleccionado.classList.add('btn-success');

    ejecutarFiltro();
}

// Filtrar al escribir en el buscador
document.getElementById('buscadorProductos').addEventListener('input', function (e) {
    textoBusqueda = e.target.value.toLowerCase().trim();
    ejecutarFiltro();
});

// El motor visual oculta o muestra las productos según las reglas de categoría y texto
function ejecutarFiltro() {
    const items = document.querySelectorAll('.item-producto');

    items.forEach(item => {
        const desc = item.getAttribute('data-descripcion');
        const cod = item.getAttribute('data-codigo');
        const cat = item.getAttribute('data-categoria');
        const esFiesta = item.getAttribute('data-fiesta');

        // ¿Cumple la categoría? (considerando fiesta)
        let pasaCategoria = false;
        if (categoriaActual === 'todas') {
            pasaCategoria = true;
        } else if (categoriaActual === 'fiesta') {
            pasaCategoria = (esFiesta === 'true');
        } else {
            pasaCategoria = (cat === categoriaActual);
        }

        // ¿Cumple el texto del buscador? 
        const pasaTexto = (desc.includes(textoBusqueda) || cod.includes(textoBusqueda));

        // Si cumple ambas reglas, lo mostramos, si no, lo ocultamos
        if (pasaCategoria && pasaTexto) {
            item.style.display = 'block';
        } else {
            item.style.display = 'none';
        }
    });
}


// LÓGICA DEL CARRITO DE COMPRAS


// Este es el arreglo maestro que vivirá en la memoria mientras la pantalla esté abierta
let carrito = [];

// agregar producto por click
function agregarAlCarrito(idProducto, descripcion, precio, stock) {
    const productoExistente = carrito.find(p => p.id === idProducto);

    if (productoExistente) {
        // No se puede vender más de lo que hay en inventario
        if (productoExistente.cantidad >= stock) {
            alert("No hay más stock disponible de este producto.");
            return;
        }
        productoExistente.cantidad++;
    } else {
        if (stock <= 0) {
            alert("Producto sin stock.");
            return;
        }
        carrito.push({
            id: idProducto,
            descripcion,
            precio: parseFloat(precio),
            cantidad: 1,
            stock
        });
    }

    actualizarBoletaHTML();
}
// Función para cambiar cantidades con los botones de + y - en la boleta
function cambiarCantidad(idProducto, cambio) {
    const index = carrito.findIndex(p => p.id === idProducto);

    if (index !== -1) {
        const nuevaCantidad = carrito[index].cantidad + cambio;

        // Cantidad 0 o menos: se quita de la boleta
        if (nuevaCantidad <= 0) {
            carrito.splice(index, 1);
        } else if (nuevaCantidad > carrito[index].stock) {
            alert("No hay más stock disponible de este producto.");
            return;
        } else {
            carrito[index].cantidad = nuevaCantidad;
        }

        actualizarBoletaHTML();
    }
}

//escribir cantidad manualmente en la boleta
function escribirCantidad(idProducto, nuevaCantidad) {
    const cantidad = parseInt(nuevaCantidad, 10);
    const index = carrito.findIndex(p => p.id === idProducto);

    if (index === -1) return;

    // Vacío, letras, 0 o negativo: se quita de la boleta
    if (isNaN(cantidad) || cantidad <= 0) {
        carrito.splice(index, 1);
    } else if (cantidad > carrito[index].stock) {
        alert(`Solo hay ${carrito[index].stock} unidades disponibles.`);
        carrito[index].cantidad = carrito[index].stock;
    } else {
        carrito[index].cantidad = cantidad;
    }

    actualizarBoletaHTML();
}

// Función para el botón del basurero (Elimina sin importar la cantidad)
function eliminarDelCarrito(idProducto) {
    const index = carrito.findIndex(p => p.id === idProducto);
    if (index !== -1) {
        carrito.splice(index, 1);
        actualizarBoletaHTML();
    }
}

// Función para vaciar todo (Botón Cancelar Venta)
function limpiarCarrito() {
    if (carrito.length > 0 && confirm('¿Estás seguro de cancelar toda la venta actual?')) {
        carrito = [];
        actualizarBoletaHTML();
    }
}

// La función más importante: Redibuja todo el panel derecho
function actualizarBoletaHTML() {
    const contenedor = document.getElementById('carritoItems');
    const lblTotal = document.getElementById('lblTotal');
    const lblSubtotal = document.getElementById('lblSubtotal');
    const btnCobrar = document.getElementById('btnCobrar');

    contenedor.innerHTML = '';

    if (carrito.length === 0) {
        contenedor.innerHTML = `
            <div class="text-center text-muted mt-5" id="carritoVacio">
                <i class="bi bi-cart-x fs-1"></i>
                <p>No hay productos en la boleta</p>
            </div>`;
        lblSubtotal.innerText = 'S/ 0.00';
        lblTotal.innerText = 'S/ 0.00';
        btnCobrar.disabled = true;
        return;
    }

    let totalVenta = 0;

    carrito.forEach(item => {
        // Redondeamos el subtotal de la fila a 2 decimales ANTES de sumarlo al total
        const subtotalItem = redondearSoles(item.precio * item.cantidad);
        totalVenta = redondearSoles(totalVenta + subtotalItem);

        // Mostramos hasta 3 decimales en el precio unitario solo si los tiene 
        const precioUnitarioTexto = Number(item.precio.toFixed(3));

        const fila = `
            <div class="border-bottom pb-2 mb-2 mx-0 px-1">
                <div class="d-flex justify-content-between align-items-start mb-1">
                    <div class="text-truncate pe-2">
                        <h6 class="mb-0 text-dark fw-bold" style="font-size: 0.85rem;" title="${item.descripcion}">${item.descripcion}</h6>
                        <small class="text-muted" style="font-size: 0.75rem;">S/ ${precioUnitarioTexto} c/u</small>
                    </div>
                    <button class="btn btn-sm btn-outline-danger p-0 d-flex justify-content-center align-items-center flex-shrink-0" 
                            style="width: 26px; height: 26px;" 
                            onclick="eliminarDelCarrito(${item.id})" title="Quitar producto">
                        <i class="bi bi-trash" style="font-size: 0.85rem;"></i>
                    </button>
                </div>
                
                <div class="d-flex justify-content-between align-items-center mt-2">
                    <div class="d-flex align-items-center">
                        <button class="btn btn-sm btn-outline-secondary p-0 d-flex justify-content-center align-items-center" 
                                style="width: 28px; height: 28px;" 
                                onclick="cambiarCantidad(${item.id}, -1)">
                            <i class="bi bi-dash fs-6"></i>
                        </button>
                        
                        <input type="number" class="form-control form-control-sm text-center mx-1 p-0 shadow-sm border-secondary" 
                               style="width: 45px; height: 28px; font-weight: bold; font-size: 0.9rem;" 
                               value="${item.cantidad}" 
                               onchange="escribirCantidad(${item.id}, this.value)" 
                               onclick="this.select()">
                        
                        <button class="btn btn-sm btn-outline-success p-0 d-flex justify-content-center align-items-center" 
                                style="width: 28px; height: 28px;" 
                                onclick="cambiarCantidad(${item.id}, 1)">
                            <i class="bi bi-plus fs-6"></i>
                        </button>
                    </div>
                    
                    <strong class="text-success m-0" style="font-size: 1rem;">${subtotalItem.toFixed(2)}</strong>
                </div>
            </div>
        `;

        contenedor.innerHTML += fila;
    });

    // Calculamos el subtotal sin IGV sobre el total ya redondeado
    const subtotalSinIgv = redondearSoles(totalVenta / 1.18);
    lblSubtotal.innerText = `S/ ${subtotalSinIgv.toFixed(2)}`;
    lblTotal.innerText = `S/ ${totalVenta.toFixed(2)}`;

    btnCobrar.disabled = false;
}

// Atajo de teclado: Cobrar al presionar F2
document.addEventListener('keydown', function (e) {
    if (e.key === 'F2' && carrito.length > 0) {
        abrirModalCobro(); // Esta función la programaremos en el siguiente paso
    }
});

// LÓGICA DEL MODAL DE COBRO (PAGO MIXTO + CLIENTE)

let modalBootstrapCobro = null;
let totalActualVenta = 0;

function abrirModalCobro() {
    totalActualVenta = carrito.reduce((sum, item) => {
        const subtotalFila = redondearSoles(item.precio * item.cantidad);
        return redondearSoles(sum + subtotalFila);
    }, 0);

    document.getElementById('modalTotalVenta').innerText = `S/ ${totalActualVenta.toFixed(2)}`;

    document.getElementById('inputEfectivo').value = '';
    document.getElementById('inputYape').value = '';
    document.getElementById('inputNumeroOperacion').value = '';
    document.getElementById('inputFotoYape').value = '';

    // Reiniciamos el cliente a "Público General" al abrir el modal
    limpiarClienteSeleccionado();

    calcularVuelto();

    if (!modalBootstrapCobro) {
        modalBootstrapCobro = new bootstrap.Modal(document.getElementById('modalCobro'));
    }
    modalBootstrapCobro.show();

    setTimeout(() => { document.getElementById('inputEfectivo').focus(); }, 500);
}

function calcularVuelto() {
    let efectivo = parseFloat(document.getElementById('inputEfectivo').value) || 0;
    let yape = parseFloat(document.getElementById('inputYape').value) || 0;

    let pagoTotal = efectivo + yape;
    let diferencia = Math.round((pagoTotal - totalActualVenta) * 100) / 100;

    const cajaVuelto = document.getElementById('cajaVuelto');
    const lblTexto = document.getElementById('lblTextoVuelto');
    const modalVuelto = document.getElementById('modalVuelto');
    const btnConfirmar = document.getElementById('btnConfirmarVenta');

    if (diferencia < 0) {
        cajaVuelto.className = 'alert alert-secondary d-flex justify-content-between align-items-center mb-0 border-0 shadow-sm';
        lblTexto.innerText = 'Falta:';
        modalVuelto.className = 'fs-3 fw-bold text-danger';
        modalVuelto.innerText = `S/ ${Math.abs(diferencia).toFixed(2)}`;
        btnConfirmar.disabled = true;
    } else {
        // Pago completo
        cajaVuelto.className = 'alert alert-success d-flex justify-content-between align-items-center mb-0 border-0 shadow-sm';
        lblTexto.innerText = 'Vuelto (Efectivo):';
        modalVuelto.className = 'fs-3 fw-bold text-success';
        modalVuelto.innerText = `S/ ${diferencia.toFixed(2)}`;
        btnConfirmar.disabled = false;
    }
}

// Envía la venta al servidor para que la registre en la base de datos
function procesarVentaEnServidor() {
    // Apagamos el botón para evitar cobros dobles
    const btnConfirmar = document.getElementById('btnConfirmarVenta');
    btnConfirmar.disabled = true;
    btnConfirmar.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Procesando...';

    const formData = new FormData();
    formData.append('IdTurno', document.getElementById('IdTurnoActual').value);
    formData.append('Efectivo', document.getElementById('inputEfectivo').value || 0);
    formData.append('Yape', document.getElementById('inputYape').value || 0);
    formData.append('CarritoJson', JSON.stringify(carrito));
    formData.append('NumeroOperacion', document.getElementById('inputNumeroOperacion').value);

    // Si hay un cliente seleccionado, enviamos su IdCliente
    const idCliente = document.getElementById('idClienteSeleccionado').value;
    if (idCliente) {
        formData.append('IdCliente', idCliente);
    }

    const fotoInput = document.getElementById('inputFotoYape');
    if (fotoInput.files.length > 0) {
        formData.append('FotoYape', fotoInput.files[0]);
    }

    // Enviamos el paquete al servidor de Golosinas Denzell usando Fetch
    fetch('/Caja/ProcesarVenta', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                carrito = [];
                actualizarBoletaHTML();
                modalBootstrapCobro.hide();

                alert("¡Venta #" + data.idVenta + " registrada con éxito!");

                btnConfirmar.disabled = false;
                btnConfirmar.innerHTML = 'Confirmar Cobro';

                
                // se abre el ticket en ventana emergente
                const urlTicket = `/Caja/Ticket/${data.idVenta}`;
                window.open(urlTicket, 'TicketVenta', 'width=400,height=600,scrollbars=yes');
            } else {
                alert("Error al guardar: " + data.mensaje);
                btnConfirmar.disabled = false;
                btnConfirmar.innerHTML = 'Confirmar Cobro';
            }
        })
        .catch(error => {
            console.error("Error en Fetch:", error);
            alert("Ocurrió un error de conexión.");
            btnConfirmar.disabled = false;
            btnConfirmar.innerHTML = 'Confirmar Cobro';
        });
}
// Llena automáticamente el input con el total exacto de la venta
function llenarMontoExacto(metodo) {
    if (metodo === 'efectivo') {
        document.getElementById('inputEfectivo').value = totalActualVenta.toFixed(2);
        document.getElementById('inputYape').value = '';
    } else if (metodo === 'yape') {
        document.getElementById('inputYape').value = totalActualVenta.toFixed(2);
        document.getElementById('inputEfectivo').value = '';
    }
    calcularVuelto();
}

// ==========================================================
// BUSCADOR Y REGISTRO RÁPIDO DE CLIENTE EN MODAL DE COBRO
// ==========================================================

const inputBuscarClientePos = document.getElementById('inputBuscarClientePos');
const listaResultadosCliente = document.getElementById('listaResultadosCliente');

if (inputBuscarClientePos) {
    inputBuscarClientePos.addEventListener('input', function () {
        const termino = this.value.trim();
        if (termino.length < 2) {
            listaResultadosCliente.classList.add('d-none');
            listaResultadosCliente.innerHTML = '';
            return;
        }

        fetch(`/Cliente/BuscarPos?termino=${encodeURIComponent(termino)}`)
            .then(r => r.json())
            .then(clientes => {
                listaResultadosCliente.innerHTML = '';
                if (clientes.length === 0) {
                    listaResultadosCliente.innerHTML = `
                        <div class="list-group-item small text-muted text-center py-2">
                            No encontrado. Usa el botón <b>+ Nuevo</b> para registrarlo.
                        </div>`;
                } else {
                    clientes.forEach(c => {
                        const btn = document.createElement('button');
                        btn.type = 'button';
                        btn.className = 'list-group-item list-group-item-action py-1 px-2 small d-flex justify-content-between align-items-center';
                        btn.innerHTML = `
                            <span class="fw-bold text-dark text-truncate">${c.nombreCompleto}</span>
                            <span class="badge bg-secondary ms-2">${c.tipoDocumento}: ${c.numeroDocumento}</span>
                        `;
                        btn.onclick = () => seleccionarClientePos(c.idCliente, `${c.tipoDocumento}: ${c.numeroDocumento}`, c.nombreCompleto);
                        listaResultadosCliente.appendChild(btn);
                    });
                }
                listaResultadosCliente.classList.remove('d-none');
            });
    });
}

function seleccionarClientePos(idCliente, documento, nombre) {
    document.getElementById('idClienteSeleccionado').value = idCliente;

    const badge = document.getElementById('badgeClienteActual');
    badge.className = 'badge bg-success';
    badge.innerText = `✓ ${nombre} (${documento})`;

    document.getElementById('inputBuscarClientePos').value = '';
    document.getElementById('listaResultadosCliente').classList.add('d-none');
    document.getElementById('panelNuevoClienteRapido').classList.add('d-none');
    document.getElementById('btnQuitarCliente').classList.remove('d-none');
}

function limpiarClienteSeleccionado() {
    document.getElementById('idClienteSeleccionado').value = '';

    const badge = document.getElementById('badgeClienteActual');
    badge.className = 'badge bg-secondary';
    badge.innerText = '👤 Público General';

    document.getElementById('inputBuscarClientePos').value = '';
    document.getElementById('listaResultadosCliente').classList.add('d-none');
    document.getElementById('panelNuevoClienteRapido').classList.add('d-none');
    document.getElementById('btnQuitarCliente').classList.add('d-none');
}

function toggleNuevoClienteRapido() {
    const panel = document.getElementById('panelNuevoClienteRapido');
    panel.classList.toggle('d-none');
    document.getElementById('listaResultadosCliente').classList.add('d-none');

    if (!panel.classList.contains('d-none')) {
        // Si ya había escrito un DNI en el buscador, lo copiamos directo al formulario rápido
        const textoBuscado = document.getElementById('inputBuscarClientePos').value.trim();
        if (/^\d+$/.test(textoBuscado)) {
            document.getElementById('rapidoNumeroDoc').value = textoBuscado;
            document.getElementById('rapidoTipoDoc').value = textoBuscado.length === 11 ? 'RUC' : 'DNI';
        }
        document.getElementById('rapidoNumeroDoc').focus();
    }
}

function guardarClienteRapidoPos() {
    const tipoDoc = document.getElementById('rapidoTipoDoc').value;
    const numeroDoc = document.getElementById('rapidoNumeroDoc').value.trim();
    const nombre = document.getElementById('rapidoNombre').value.trim();
    const telefono = document.getElementById('rapidoTelefono').value.trim();
    const direccion = document.getElementById('rapidoDireccion').value.trim();

    if (!numeroDoc || !nombre) {
        alert("Ingresa el número de documento y el nombre del cliente.");
        return;
    }

    const formData = new FormData();
    formData.append('tipoDoc', tipoDoc);
    formData.append('numeroDoc', numeroDoc);
    formData.append('nombre', nombre);
    formData.append('telefono', telefono);
    formData.append('direccion', direccion);

    fetch('/Cliente/CrearRapidoPos', {
        method: 'POST',
        body: formData
    })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                document.getElementById('rapidoNumeroDoc').value = '';
                document.getElementById('rapidoNombre').value = '';
                document.getElementById('rapidoTelefono').value = '';
                document.getElementById('rapidoDireccion').value = '';
                seleccionarClientePos(res.idCliente, res.documento, res.nombre);
            } else {
                alert(res.mensaje);
            }
        });
}