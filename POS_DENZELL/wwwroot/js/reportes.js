function iniciarGraficosBi(fechasLabels, fechasValores, catLabels, catValores) {
    // Gráfico de Evolución de Ventas
    const canvasEvolucion = document.getElementById('chartEvolucion');
    if (canvasEvolucion) {
        new Chart(canvasEvolucion, {
            type: 'bar',
            data: {
                labels: fechasLabels,
                datasets: [{
                    label: 'Ventas (S/)',
                    data: fechasValores,
                    backgroundColor: 'rgba(25, 135, 84, 0.8)',
                    borderColor: '#198754',
                    borderWidth: 1,
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } }
            }
        });
    }

    // Gráfico de Ventas por Categoría
    const canvasCategorias = document.getElementById('chartCategorias');
    if (canvasCategorias) {
        new Chart(canvasCategorias, {
            type: 'doughnut',
            data: {
                labels: catLabels,
                datasets: [{
                    data: catValores,
                    backgroundColor: ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#6f42c1', '#0dcaf0', '#fd7e14', '#20c997']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' }
                }
            }
        });
    }
}