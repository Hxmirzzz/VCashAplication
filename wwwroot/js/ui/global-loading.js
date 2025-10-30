window.AppLoading = (function () {
    function show(title = 'Cargando...', text = 'Por favor, espere.') {
        if (Swal.isVisible()) return;
        Swal.fire({
            title, text,
            allowOutsideClick: false,
            allowEscapeKey: false,
            didOpen: () => Swal.showLoading()
        });
    }

    function hide() {
        if (Swal.isVisible()) Swal.close();
    }

    // Envuelve una promesa mostrando loader
    async function withSwal(promise, title, text) {
        try {
            show(title, text);
            const r = await promise;
            return r;
        } finally {
            hide();
        }
    }

    // Link loading: usa data-loading en <a> para mostrar loader al navegar
    function bindLinkLoading(selector = 'a[data-loading]') {
        document.addEventListener('click', (e) => {
            const a = e.target.closest(selector);
            if (!a) return;

            const sameTab = !a.target || a.target === '_self';
            const sameOrigin = a.origin === location.origin;
            if (sameTab && sameOrigin) {
                show('Cargando página...', 'Por favor, espere.');
            }
        });
    }

    // Form loading: para formularios no-AJAX (GET o POST naturales)
    function bindFormLoading(selector = 'form[data-loading]') {
        document.addEventListener('submit', (e) => {
            const form = e.target.closest(selector);
            if (!form) return;
            // Validación lado cliente (opcional)
            if (window.$ && $.validator && !$(form).valid()) return;
            show('Procesando...', 'Por favor, espere.');
        });
    }

    // Botón de descarga con protección anti-doble clic
    function bindDownload(selector = '[data-download]') {
        document.addEventListener('click', (e) => {
            const btn = e.target.closest(selector);
            if (!btn) return;

            if (btn.dataset.busy === '1') { e.preventDefault(); return; }
            btn.dataset.busy = '1';

            show('Preparando archivo...', 'Esto puede tardar unos segundos.');
            const release = () => { btn.dataset.busy = '0'; hide(); };
            setTimeout(release, 8000);
        });
    }

    async function fetchWithSwal(url, options = {}, { title, text } = {}) {
        return await withSwal(fetch(url, options), title || 'Cargando...', text || 'Por favor, espere.');
    }

    return { show, hide, withSwal, bindLinkLoading, bindFormLoading, bindDownload, fetchWithSwal };
})();