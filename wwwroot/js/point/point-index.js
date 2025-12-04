(function () {

    const parseNum = (v) => {
        if (v == null) return null;
        const n = Number(String(v).trim().replace(',', '.'));
        return Number.isFinite(n) ? n : null;
    };

    const coordsValid = (lat, lng) =>
        lat !== null && lng !== null &&
        lat >= -90 && lat <= 90 &&
        lng >= -180 && lng <= 180;

    function radiusToZoom(r) {
        if (!Number.isFinite(r) || r <= 0) return 16;
        if (r <= 50) return 18;
        if (r <= 100) return 17;
        if (r <= 250) return 16;
        if (r <= 500) return 15;
        if (r <= 1000) return 14;
        if (r <= 2000) return 13;
        if (r <= 5000) return 12;
        return 11;
    }

    function makeGMapsUrl(lat, lng) {
        return `https://www.google.com/maps/search/?api=1&query=${lat.toFixed(6)},${lng.toFixed(6)}`;
    }

    let modalEl, modalTitleEl, mapLinkEl;
    let map = null, marker = null, circle = null;
    let inited = false;
    let bsModal = null;
    let lastTrigger = null;

    const mapContainerId = 'pointMap';

    document.addEventListener('DOMContentLoaded', () => {

        modalEl = document.getElementById('PointMapModal');   // ✔ CORREGIDO
        modalTitleEl = document.getElementById('PointMapModalLabel');
        mapLinkEl = document.getElementById('pointGmapsLink'); // si lo usas

        if (!modalEl) return;

        bsModal = new bootstrap.Modal(modalEl);

        document.addEventListener('click', onEarthClick);

        modalEl.addEventListener('hidden.bs.modal', () => {
            if (lastTrigger && document.body.contains(lastTrigger)) lastTrigger.focus();
        });
    });

    function onEarthClick(e) {
        const btn = e.target.closest('.btn-earth');
        if (!btn) return;

        e.preventDefault();
        lastTrigger = btn;

        const lat = parseNum(btn.dataset.lat);
        const lng = parseNum(btn.dataset.lng);
        const radius = parseNum(btn.dataset.radius) ?? 0;

        const title = btn.dataset.title || btn.dataset.cod || "Ubicación";

        if (!coordsValid(lat, lng)) {
            Swal.fire({
                icon: "warning",
                title: "Coordenadas inválidas",
                text: "Este punto no tiene latitud/longitud válidas."
            });
            return;
        }

        modalTitleEl.textContent = `${title} — ${lat.toFixed(6)}, ${lng.toFixed(6)}`;

        modalEl.addEventListener('shown.bs.modal', () => {
            initOrUpdateMap(lat, lng, radius);
        }, { once: true });

        bsModal.show();
    }

    function initOrUpdateMap(lat, lng, radius) {
        const zoom = radiusToZoom(radius);

        if (!inited) {
            map = L.map(mapContainerId).setView([lat, lng], zoom);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19
            }).addTo(map);

            marker = L.marker([lat, lng]).addTo(map);

            if (radius > 0) {
                circle = L.circle([lat, lng], {
                    radius,
                    color: '#235286',
                    fillColor: '#235286',
                    fillOpacity: 0.15
                }).addTo(map);
            }

            inited = true;
            setTimeout(() => map.invalidateSize(), 200);
        } else {
            map.setView([lat, lng], zoom);
            marker.setLatLng([lat, lng]);

            if (radius > 0) {
                if (!circle) {
                    circle = L.circle([lat, lng], {
                        radius,
                        color: '#235286',
                        fillColor: '#235286',
                        fillOpacity: 0.15
                    }).addTo(map);
                } else {
                    circle.setLatLng([lat, lng]).setRadius(radius);
                }
            } else if (circle) {
                map.removeLayer(circle);
                circle = null;
            }

            setTimeout(() => map.invalidateSize(), 150);
        }
    }

})();
