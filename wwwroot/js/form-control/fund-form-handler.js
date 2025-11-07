document.addEventListener('DOMContentLoaded', () => {
    const form = document.getElementById('funds-form');
    if (!form) return;

    // ===== util anti-forgery =====
    const antiForgery = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ===== inputs =====
    const inputFundCode = document.getElementById('FundCode');
    const selClientCode = document.getElementById('ClientCode');
    const inputVatco = document.getElementById('VatcoFundCode');
    const inputCas4u = document.getElementById('Cas4uCode');
    const selStatus = document.getElementById('FundStatusSelect');
    const hiddenStatus = document.getElementById('FundStatus');

    // ===== sync estado (combo -> hidden) =====
    const syncStatus = () => { if (hiddenStatus && selStatus) hiddenStatus.value = (selStatus.value === 'true'); };
    syncStatus();
    selStatus?.addEventListener('change', syncStatus);

    // ===== auto-build FundCode y Cas4uCode =====
    function buildFundCode() {
        const client = (selClientCode?.value || '').trim();
        const vatco = (inputVatco?.value || '').trim();
        if (!client || !vatco) return '';
        return `${client}-${vatco}`;
    }
    function buildCas4u() {
        const client = (selClientCode?.value || '').trim();
        const vatco = (inputVatco?.value || '').trim();
        if (!client || !vatco) return '';
        return `${client}|${vatco}`;
    }
    function refreshAutoCodes() {
        const code = buildFundCode();
        const cash = buildCas4u();
        // si es Create (FundCode editable), autollenamos
        if (inputFundCode && !inputFundCode.readOnly) inputFundCode.value = code;
        if (inputCas4u) inputCas4u.value = cash;
    }
    selClientCode?.addEventListener('change', refreshAutoCodes);
    inputVatco?.addEventListener('input', refreshAutoCodes);
    if (inputFundCode && !inputFundCode.readOnly) refreshAutoCodes();

    // ===== envío AJAX =====
    async function postForm(formEl) {
        const fd = new FormData(formEl);
        // Si alguien borra el hidden, recomponemos IsEdit según readonly del código
        if (!fd.has('IsEdit')) {
            const isEdit = !!(inputFundCode?.readOnly);
            fd.append('IsEdit', isEdit ? 'true' : 'false');
        }
        const resp = await fetch(formEl.action, {
            method: 'POST',
            body: fd,
            headers: {
                'RequestVerificationToken': antiForgery(),
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        if (!resp.ok) throw new Error(`HTTP ${resp.status} ${resp.statusText}`);
        const ct = resp.headers.get('content-type') || '';
        if (!ct.includes('application/json')) throw new Error('La respuesta no es JSON válido');
        return await resp.json();
    }

    let submitting = false;
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (submitting) return;
        submitting = true;

        try {
            // jQuery unobtrusive validation (si está activo)
            if (window.$ && $.validator && !$(form).valid()) { submitting = false; return; }

            // validación rápida de cliente/vatco
            const client = (selClientCode?.value || '').trim();
            const vatco = (inputVatco?.value || '').trim();
            if (!client) { await Swal.fire({ icon: 'warning', title: 'Cliente requerido', text: 'Seleccione un cliente.' }); submitting = false; return; }
            if (!vatco) { await Swal.fire({ icon: 'warning', title: 'Fondo Vatco requerido', text: 'Ingrese el código Vatco.' }); submitting = false; return; }

            // si es Create, fuerza recomputar códigos
            if (inputFundCode && !inputFundCode.readOnly) {
                inputFundCode.value = buildFundCode();
                inputCas4u && (inputCas4u.value = buildCas4u());
            }

            const isEdit = !!(inputFundCode?.readOnly);
            const confirm = await Swal.fire({
                title: isEdit ? '¿Guardar cambios?' : '¿Crear fondo?',
                text: 'Se guardará la información del fondo.',
                icon: 'question', showCancelButton: true,
                confirmButtonText: 'Sí, guardar', cancelButtonText: 'Cancelar'
            });
            if (!confirm.isConfirmed) { submitting = false; return; }

            Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

            const result = await postForm(form);
            Swal.close();

            if (result && result.success) {
                await Swal.fire({ icon: 'success', title: '¡Éxito!', text: result.message || (isEdit ? 'Fondo actualizado.' : 'Fondo creado.') });
                window.location.href = '/Funds/Index';
            } else {
                let html = (result && result.message) ? result.message : 'No se pudo guardar.';
                if (result && result.errors) {
                    const flat = Object.values(result.errors).flat();
                    if (flat.length) html += '<br>' + flat.map(e => `• ${e}`).join('<br>');
                }
                await Swal.fire({ icon: 'error', title: 'Error', html });
            }
        } catch (err) {
            console.error(err);
            await Swal.fire({ icon: 'error', title: 'Error de red', text: 'No se pudo guardar. Verifique su conexión.' });
        } finally {
            submitting = false;
        }
    });
});