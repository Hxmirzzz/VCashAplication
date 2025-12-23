document.addEventListener("DOMContentLoaded", () => {
    document.addEventListener("click", e => {
        const btn = e.target.closest(".btn-earth");
        if (!btn) return;

        e.preventDefault();

        const lat = PointMapModule.parseNum(btn.dataset.lat);
        const lng = PointMapModule.parseNum(btn.dataset.lng);
        const radius = PointMapModule.parseNum(btn.dataset.radius);
        const title = btn.dataset.title || btn.dataset.cod || "Punto";

        PointMapModule.show(lat, lng, radius, title);
    });
});