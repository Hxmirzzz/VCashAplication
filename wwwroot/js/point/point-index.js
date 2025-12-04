(function () {
    const parseNum = v => {
        if (!v) return null;
        const n = Number(String(v).replace(",", "."));
        return Number.isFinite(n) ? n : null;
    };

    const coordsValid = (lat, lng) =>
        lat !== null && lng !== null &&
        lat >= -90 && lat <= 90 &&
        lng >= -180 && lng <= 180;

    function zoomByRadius(r) {
        if (!r) return 16;
        if (r <= 50) return 18;
        if (r <= 100) return 17;
        if (r <= 250) return 16;
        if (r <= 500) return 15;
        if (r <= 1000) return 14;
        if (r <= 2000) return 13;
        if (r <= 5000) return 12;
        return 11;
    }

    const gmapsUrl = (lat, lng) =>
        `https://www.google.com/maps/search/?api=1&query=${lat},${lng}`;

    const streetViewUrl = (lat, lng) =>
        `https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=${lat},${lng}`;

    let map = null, marker = null, circle = null, inited = false;
    let bsModal;

    document.addEventListener("DOMContentLoaded", () => {
        const modalEl = document.getElementById("pointMapModal");
        bsModal = new bootstrap.Modal(modalEl);

        document.addEventListener("click", e => {
            const btn = e.target.closest(".btn-earth");
            if (!btn) return;

            e.preventDefault();

            const lat = parseNum(btn.dataset.lat);
            const lng = parseNum(btn.dataset.lng);
            const radius = parseNum(btn.dataset.radius);
            const title = btn.dataset.title || btn.dataset.cod;

            if (!coordsValid(lat, lng)) {
                Swal.fire({
                    icon: "warning",
                    title: "Coordenadas inválidas",
                    text: "Este punto no tiene latitud o longitud válida."
                });
                return;
            }

            document.getElementById("pointMapTitle").textContent = `${title}`;
            document.getElementById("pointGmapsLink").href = gmapsUrl(lat, lng);
            document.getElementById("pointStreetViewLink").href = streetViewUrl(lat, lng);
            document.getElementById("infoLat").textContent = lat.toFixed(6);
            document.getElementById("infoLng").textContent = lng.toFixed(6);
            document.getElementById("infoRad").textContent = radius ? radius + " m" : "-";

            // Copiar coordenadas
            document.getElementById("btnCopyCoords").onclick = () => {
                navigator.clipboard.writeText(`${lat}, ${lng}`);
                Swal.fire("Copiado", "Coordenadas copiadas al portapapeles", "success");
            };

            // Copiar URL
            document.getElementById("btnCopyUrl").onclick = () => {
                navigator.clipboard.writeText(gmapsUrl(lat, lng));
                Swal.fire("Copiado", "URL de Google Maps copiada", "success");
            };

            modalEl.addEventListener("shown.bs.modal", () => {
                initMap(lat, lng, radius);
                setTimeout(() => map.invalidateSize(), 200);
            }, { once: true });

            bsModal.show();
        });
    });

    function initMap(lat, lng, radius) {
        const zoom = zoomByRadius(radius);

        if (!inited) {
            map = L.map("pointMapCanvas").setView([lat, lng], zoom);

            /* Mapa modo oscuro */
            L.tileLayer(
                "https://tiles.stadiamaps.com/tiles/alidade_smooth_dark/{z}/{x}/{y}{r}.png",
                { maxZoom: 20 }
            ).addTo(map);

            marker = L.marker([lat, lng]).addTo(map);

            if (radius > 0) {
                circle = L.circle([lat, lng], {
                    radius,
                    color: "#3b82f6",
                    fillColor: "#3b82f6",
                    fillOpacity: 0.15
                }).addTo(map);
            }

            inited = true;
        } else {
            map.setView([lat, lng], zoom);
            marker.setLatLng([lat, lng]);

            if (radius > 0) {
                if (!circle)
                    circle = L.circle([lat, lng], { radius }).addTo(map);
                else
                    circle.setLatLng([lat, lng]).setRadius(radius);
            }
        }
    }
})();