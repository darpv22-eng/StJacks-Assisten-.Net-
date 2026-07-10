// wwwroot/js/dashboard.js
document.addEventListener("DOMContentLoaded", function () {
    // 1. Reloj
    function updateClock() {
        const now = new Date();
        const el = document.getElementById('current-time');
        if (el) {
            el.innerText = now.toLocaleTimeString('es-SV', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        }
    }
    setInterval(updateClock, 1000);
    updateClock();

    // 2. Gráfico de Línea
    const ctx = document.getElementById('mainDashboardChart')?.getContext('2d');
    if (ctx) {
        const primaryColor = '#4361ee';
        const successColor = '#2ec4b6';
        const fillGradient = ctx.createLinearGradient(0, 0, 0, 300);
        fillGradient.addColorStop(0, 'rgba(67, 97, 238, 0.15)');
        fillGradient.addColorStop(1, 'rgba(67, 97, 238, 0)');

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['01 Abr', '02 Abr', '03 Abr', '04 Abr', '05 Abr', '08 Abr', '09 Abr', '10 Abr'],
                datasets: [{
                    label: 'Tasa de Asistencia (%)',
                    data: [95, 93, 98, 92, 95, 96, 94, 97],
                    borderColor: primaryColor,
                    backgroundColor: fillGradient,
                    fill: true,
                    tension: 0.4
                }, {
                    label: 'Eficiencia Operativa (%)',
                    data: [84, 86, 89, 82, 85, 88, 87, 91],
                    borderColor: successColor,
                    fill: false,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: { y: { min: 75, ticks: { callback: v => v + '%' } } }
            }
        });
    }

    // 3. Gráfico Circular
    const ctxPie = document.getElementById('distributionPieChart')?.getContext('2d');
    if (ctxPie) {
        new Chart(ctxPie, {
            type: 'doughnut',
            data: {
                labels: ['Costura', 'Corte', 'Empaque', 'Calidad'],
                datasets: [{
                    data: [65, 20, 25, 11],
                    backgroundColor: ['#4361ee', '#4cc9f0', '#ff9f1c', '#7209b7']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, cutout: '70%' }
        });
    }
});