// ============================================================================
// MÓDULO DE MAPAS PARA PUNTOS
// ============================================================================

const PointMapModule = (function () {
    // Utilidades privadas
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

    // Variables del mapa
    let map = null;
    let marker = null;
    let circle = null;
    let inited = false;
    let bsModal = null;

    // Inicialización del modal
    function initModal() {
        const modalEl = document.getElementById("pointMapModal");
        if (!modalEl) {
            console.error("Modal 'pointMapModal' no encontrado");
            return null;
        }
        bsModal = new bootstrap.Modal(modalEl);
        return modalEl;
    }

    // Inicializar o actualizar el mapa
    function initMap(lat, lng, radius) {
        const zoom = zoomByRadius(radius);

        if (!inited) {
            map = L.map("pointMapCanvas").setView([lat, lng], zoom);

            // Mapa modo oscuro
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
                if (!circle) {
                    circle = L.circle([lat, lng], {
                        radius,
                        color: "#3b82f6",
                        fillColor: "#3b82f6",
                        fillOpacity: 0.15
                    }).addTo(map);
                } else {
                    circle.setLatLng([lat, lng]).setRadius(radius);
                }
            } else if (circle) {
                map.removeLayer(circle);
                circle = null;
            }
        }
    }

    // Actualizar información del modal
    function updateModalInfo(lat, lng, radius, title) {
        document.getElementById("pointMapTitle").textContent = title;
        document.getElementById("pointGmapsLink").href = gmapsUrl(lat, lng);
        document.getElementById("pointStreetViewLink").href = streetViewUrl(lat, lng);
        document.getElementById("infoLat").textContent = lat.toFixed(6);
        document.getElementById("infoLng").textContent = lng.toFixed(6);
        document.getElementById("infoRad").textContent = radius ? radius + " m" : "-";

        // Configurar botones de copiar
        document.getElementById("btnCopyCoords").onclick = () => {
            navigator.clipboard.writeText(`${lat}, ${lng}`);
            Swal.fire("Copiado", "Coordenadas copiadas al portapapeles", "success");
        };

        document.getElementById("btnCopyUrl").onclick = () => {
            navigator.clipboard.writeText(gmapsUrl(lat, lng));
            Swal.fire("Copiado", "URL de Google Maps copiada", "success");
        };
    }

    // Mostrar el mapa con las coordenadas proporcionadas
    function showMap(lat, lng, radius, title) {
        if (!coordsValid(lat, lng)) {
            Swal.fire({
                icon: "warning",
                title: "Coordenadas inválidas",
                text: "Este punto no tiene latitud o longitud válida."
            });
            return;
        }

        const modalEl = document.getElementById("pointMapModal");
        if (!modalEl) return;

        updateModalInfo(lat, lng, radius, title);

        modalEl.addEventListener("shown.bs.modal", () => {
            initMap(lat, lng, radius);
            setTimeout(() => map.invalidateSize(), 200);
        }, { once: true });

        if (bsModal) {
            bsModal.show();
        }
    }

    // API Pública
    return {
        init: initModal,
        show: showMap,
        parseNum: parseNum,
        coordsValid: coordsValid
    };
})();

// Auto-inicializar cuando el DOM esté listo
document.addEventListener("DOMContentLoaded", () => {
    PointMapModule.init();
});