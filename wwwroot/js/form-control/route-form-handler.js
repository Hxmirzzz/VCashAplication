document.addEventListener('DOMContentLoaded', () => {
    const form =
        document.getElementById('route-create-form') ||
        document.getElementById('route-edit-form');

    if (!form) return;

    const normalizeRouteCode = (v) => (v || '').toString().trim().toUpperCase().replace(/\s+/g, '');

    // ===== Construir BranchRouteCode: {BranchId}_{RouteCode} =====
    function buildBranchRouteCode() {
        const branchIdEl = form.querySelector('[name="BranchId"]');
        const routeCodeEl = form.querySelector('[name="RouteCode"]');
        const hiddenPk = form.querySelector('[name="BranchRouteCode"]');
        const previewPk = document.getElementById('BranchRouteCodePreview');

        const branchId = (branchIdEl?.value || '').trim();
        const routeRaw = routeCodeEl?.value || '';
        const routeCode = normalizeRouteCode(routeRaw);

        if (routeCodeEl && routeCodeEl.value !== routeCode) {
            routeCodeEl.value = routeCode;
        }

        let pk = '';
        if (branchId && routeCode) pk = `${branchId}_${routeCode}`;

        if (hiddenPk) hiddenPk.value = pk;
        if (previewPk) previewPk.value = pk;
    }

    const branchEl = form.querySelector('[name="BranchId"]');
    const routeEl = form.querySelector('[name="RouteCode"]');
    if (branchEl) branchEl.addEventListener('change', buildBranchRouteCode);
    if (routeEl) routeEl.addEventListener('input', buildBranchRouteCode);

    buildBranchRouteCode();

    // ===== Anti-forgery =====
    const antiForgery = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ===== Estado (select -> hidden) =====
    const statusSelect = document.getElementById('routeStatusSelect');
    const statusHidden = document.getElementById('IsActive');
    const syncStatus = () => {
        if (statusSelect && statusHidden) statusHidden.value = (statusSelect.value === 'true');
    };
    if (statusSelect && statusHidden) {
        statusSelect.addEventListener('change', syncStatus);
        syncStatus();
    }

    // ===== Habilitar/Deshabilitar filas por día =====
    function clearRow($tr) {
        $tr.querySelectorAll('.js-t').forEach(i => {
            i.value = '';
            i.classList.remove('is-invalid');
        });
    }
    function toggleRow($tr) {
        const checked = $tr.querySelector('.js-day-flag')?.checked;
        if (!checked) clearRow($tr);
        $tr.querySelectorAll('.js-t').forEach(i => i.disabled = !checked);
    }
    document.querySelectorAll('tr[data-day]').forEach(tr => toggleRow(tr));
    form.addEventListener('change', (e) => {
        const tr = e.target.closest('tr[data-day]');
        if (tr && e.target.classList.contains('js-day-flag')) toggleRow(tr);
    });

    // ===== Validación HH:mm =====
    const mapDay = { mon: 'Lunes', tue: 'Martes', wed: 'Miércoles', thu: 'Jueves', fri: 'Viernes', sat: 'Sábado', sun: 'Domingo', hol: 'Festivo' };
    const parseHM = (v) => {
        if (!v) return null;
        const [h, m] = v.split(':').map(Number);
        if (Number.isNaN(h) || Number.isNaN(m)) return null;
        return h * 60 + m;
    };
    function validateDayRow(tr) {
        const dayKey = tr.getAttribute('data-day') || '';
        const dayName = mapDay[dayKey] || 'Día';
        const enabled = tr.querySelector('.js-day-flag')?.checked;
        const errors = [];
        if (!enabled) return { ok: true, errors };

        const [hi, hf] = tr.querySelectorAll('.js-t');
        hi?.classList.remove('is-invalid'); hf?.classList.remove('is-invalid');

        const vHi = hi?.value?.trim() || '';
        const vHf = hf?.value?.trim() || '';
        const hasHi = !!vHi, hasHf = !!vHf;

        if (!hasHi && !hasHf) {
            errors.push(`${dayName}: debe diligenciar al menos un rango (hora inicial y final).`);
            hi?.classList.add('is-invalid');
            hf?.classList.add('is-invalid');
            return { ok: false, errors };
        }

        if (hasHi ^ hasHf) {
            errors.push(`${dayName}: si diligencia una hora debe diligenciar la otra.`);
            (hasHi ? hf : hi)?.classList.add('is-invalid');
            return { ok: false, errors };
        }

        const mHi = parseHM(vHi), mHf = parseHM(vHf);
        if (mHi == null || mHf == null) {
            errors.push(`${dayName}: formato de hora inválido (HH:mm).`);
            hi?.classList.add('is-invalid'); hf?.classList.add('is-invalid');
            return { ok: false, errors };
        }
        if (mHi >= mHf) {
            errors.push(`${dayName}: la hora inicial debe ser menor que la final.`);
            hi?.classList.add('is-invalid'); hf?.classList.add('is-invalid');
            return { ok: false, errors };
        }

        return { ok: errors.length === 0, errors };
    }
    function validateAllDays() {
        let ok = true, errs = [];
        const rows = document.querySelectorAll('tr[data-day]');
        let hasAnyChecked = false;

        rows.forEach(tr => {
            const checkbox = tr.querySelector('.js-day-flag');
            if (checkbox?.checked) hasAnyChecked = true;

            const r = validateDayRow(tr);
            if (!r.ok) {
                ok = false;
                errs = errs.concat(r.errors);
            }
        });

        if (!hasAnyChecked) {
            ok = false;
            errs.unshift('Debe seleccionar al menos un día habilitado.');
        }

        return { ok, errs };
    }

    // ===== Submit AJAX con SweetAlert -> Route/Save =====
    async function postForm(formEl) {
        const resp = await fetch(formEl.action, {
            method: 'POST',
            body: new FormData(formEl),
            headers: {
                'RequestVerificationToken': antiForgery(),
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const ct = resp.headers.get('content-type') || '';
        if (!ct.includes('application/json')) throw new Error('Respuesta no es JSON');
        return await resp.json();
    }

    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // Unobtrusive MVC
        if (window.$ && $.validator && !$(form).valid()) return;

        // Horarios
        const v = validateAllDays();
        if (!v.ok) {
            await Swal.fire({
                icon: 'warning',
                title: 'Revisa los horarios',
                html: v.errs.map(x => `• ${x}`).join('<br>')
            });
            return;
        }

        const isEdit = form.id === 'route-edit-form';
        const confirm = await Swal.fire({
            title: isEdit ? '¿Guardar cambios?' : '¿Crear ruta?',
            text: 'Se guardará la información de la ruta.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, guardar',
            cancelButtonText: 'Cancelar'
        });
        if (!confirm.isConfirmed) return;

        Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

        try {
            const result = await postForm(form);
            Swal.close();

            if (result && result.success) {
                await Swal.fire({ icon: 'success', title: '¡Éxito!', text: result.message || (isEdit ? 'Ruta actualizada.' : 'Ruta creada.') });
                if (result.data?.redirectUrl) window.location.href = result.data.redirectUrl;
                else window.location.href = '/Route/Index';
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
            Swal.close();
            await Swal.fire({ icon: 'error', title: 'Error de red', text: 'No se pudo guardar. Verifique su conexión.' });
        }
    });
});