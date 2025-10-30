(function () {
    function initUploader(root) {
        if (root.dataset.bound === "1") return;
        root.dataset.bound = "1";

        const input = root.querySelector('.uploader__input');
        const preview = root.querySelector('.uploader__preview');
        const img = preview ? preview.querySelector('img') : null;
        const removeBtn = root.querySelector('[data-remove]');
        const removedFlag = root.querySelector('[data-removed-flag]');

        function setHasFile(on) {
            if (on) root.classList.add('has-file'); else root.classList.remove('has-file');
        }

        function showPreview(fileOrUrl) {
            if (!img) return;
            const url = (typeof fileOrUrl === 'string') ? fileOrUrl : URL.createObjectURL(fileOrUrl);
            img.src = url;
            preview.hidden = false;
            removedFlag && (removedFlag.value = "false");
            root.classList.add('has-file');
        }

        function clearPreview(e) {
            if (e) { e.preventDefault(); e.stopPropagation(); }
            if (img && img.src && img.src.startsWith('blob:')) URL.revokeObjectURL(img.src);
            if (input) input.value = "";
            if (img) img.removeAttribute('src');
            if (preview) preview.hidden = true;
            if (removedFlag) removedFlag.value = "true";
            setHasFile(false);
        }

        // Drag & Drop
        ['dragenter', 'dragover'].forEach(ev =>
            root.addEventListener(ev, e => { e.preventDefault(); e.stopPropagation(); root.classList.add('is-drag'); })
        );
        ['dragleave', 'drop'].forEach(ev =>
            root.addEventListener(ev, e => { e.preventDefault(); e.stopPropagation(); root.classList.remove('is-drag'); })
        );
        root.addEventListener('drop', e => {
            const dt = e.dataTransfer;
            if (!dt || !dt.files || !dt.files.length) return;
            const list = new DataTransfer();
            list.items.add(dt.files[0]);
            if (input) input.files = list.files;
            showPreview(dt.files[0]);
        });

        input && input.addEventListener('change', () => {
            const f = input.files && input.files[0];
            if (f) showPreview(f);
        });

        removeBtn && removeBtn.addEventListener('click', (e) => {
            e.preventDefault(); e.stopPropagation();
            if (img && img.src && img.src.startsWith('blob:')) URL.revokeObjectURL(img.src);
            img.removeAttribute('src');
            preview.hidden = true;
            removedFlag && (removedFlag.value = "true");
            root.classList.remove('has-file');
        });

        if (preview && !preview.hasAttribute('hidden')) setHasFile(true);
    }

    function initSignature(container) {
        initUploader(container);

        const pad = container.querySelector('[data-pad]');
        if (!pad) return;

        const canvas = pad.querySelector('canvas');
        const clearBtn = pad.querySelector('[data-clear]');
        const applyBtn = pad.querySelector('[data-apply]');
        const hidden = container.querySelector('input[type="hidden"][name="SignatureDataUrl"]');
        const ctx = canvas.getContext('2d');

        function fit() {
            const r = canvas.getBoundingClientRect();
            canvas.width = Math.max(600, Math.floor(r.width));
            canvas.height = 260;
            ctx.fillStyle = "#fff"; ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.lineWidth = 2; ctx.lineJoin = "round"; ctx.lineCap = "round"; ctx.strokeStyle = "#111";
        }
        fit();
        window.addEventListener('resize', fit);

        let drawing = false, px = 0, py = 0;
        function pos(e) {
            const r = canvas.getBoundingClientRect();
            if (e.touches && e.touches.length) {
                return { x: e.touches[0].clientX - r.left, y: e.touches[0].clientY - r.top };
            }
            return { x: e.clientX - r.left, y: e.clientY - r.top };
        }
        function start(e) { drawing = true; const a = pos(e); px = a.x; py = a.y; e.preventDefault(); }
        function move(e) {
            if (!drawing) return;
            const a = pos(e);
            ctx.beginPath(); ctx.moveTo(px, py); ctx.lineTo(a.x, a.y); ctx.stroke();
            px = a.x; py = a.y;
            e.preventDefault();
        }
        function end() { drawing = false; }

        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        window.addEventListener('mouseup', end);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        window.addEventListener('touchend', end);

        clearBtn && clearBtn.addEventListener('click', () => fit());
        applyBtn && applyBtn.addEventListener('click', () => { if (hidden) hidden.value = canvas.toDataURL('image/png'); });
    }

    function boot() {
        document.querySelectorAll('[data-uploader]').forEach(initUploader);
        document.querySelectorAll('[data-signature]').forEach(initSignature);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot); else boot();
})();