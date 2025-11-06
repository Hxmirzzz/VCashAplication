document.addEventListener('DOMContentLoaded', () => {
    const form =
        document.getElementById('range-create-form') ||
        document.getElementById('range-edit-form');
    if (!form) return;

    // ====== util anti-forgery ======
    const antiForgery = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ====== sincroniza combo estado -> hidden ======
    const statusSelect = document.getElementById('rangeStatusSelect');
    const statusHidden = document.getElementById('RangeStatus');
    const syncStatus = () => {
        if (statusSelect && statusHidden) statusHidden.value = (statusSelect.value === 'true');
    };
    if (statusSelect && statusHidden) {
        syncStatus();
        statusSelect.addEventListener('change', syncStatus);
    }

    // ===== Habilitar/Deshabilitar filas por día + limpiar horas si se desmarca =====
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

    // ===== Validación HH:mm por rangos =====
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

        const pairs = [
            ['1Hi', '1Hf', 1],
            ['2Hi', '2Hf', 2],
            ['3Hi', '3Hf', 3],
        ];

        let hasAnyCompletePair = false;

        pairs.forEach(([a, b, n]) => {
            const hi = tr.querySelector(`input[name$="${a}"]`);
            const hf = tr.querySelector(`input[name$="${b}"]`);
            if (!hi || !hf) return;

            hi.classList.remove('is-invalid'); hf.classList.remove('is-invalid');

            const vHi = hi.value?.trim();
            const vHf = hf.value?.trim();
            const hasHi = !!vHi, hasHf = !!vHf;

            if (hasHi ^ hasHf) {
                errors.push(`${dayName}, Rango ${n}: si diligencia una hora debe diligenciar la otra.`);
                (hasHi ? hf : hi).classList.add('is-invalid');
                return;
            }
            if (hasHi && hasHf) {
                const mHi = parseHM(vHi), mHf = parseHM(vHf);
                if (mHi == null || mHf == null) {
                    errors.push(`${dayName}, Rango ${n}: formato inválido (HH:mm).`);
                    hi.classList.add('is-invalid'); hf.classList.add('is-invalid');
                    return;
                }
                if (mHi >= mHf) {
                    errors.push(`${dayName}, Rango ${n}: la hora inicial debe ser menor que la final.`);
                    hi.classList.add('is-invalid'); hf.classList.add('is-invalid');
                    return;
                }
                hasAnyCompletePair = true;
            }
        });

        if (!hasAnyCompletePair) {
            errors.unshift(`${dayName}: debe diligenciar al menos un rango completo (inicio y fin).`);
        }

        return { ok: errors.length === 0, errors };
    }

    function validateAllDays() {
        let allOk = true;
        let allErrors = [];
        let hasAnyChecked = false;

        document.querySelectorAll('tr[data-day]').forEach(tr => {
            if (tr.querySelector('.js-day-flag')?.checked) hasAnyChecked = true;
            const res = validateDayRow(tr);
            if (!res.ok) {
                allOk = false;
                allErrors = allErrors.concat(res.errors);
            }
        });

        if (!hasAnyChecked) {
            allOk = false;
            allErrors.unshift('Debe seleccionar al menos un día habilitado.');
        }
        return { allOk, allErrors };
    }

    // ===== Payload DTO para ValidateUnique =====
    function buildVmForValidateUnique(formEl) {
        const fd = new FormData(formEl);
        const vm = {};
        for (const [k, v] of fd.entries()) vm[k] = v;

        vm.ClientId = parseInt(vm.ClientId || '0', 10) || 0;

        const bools = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday', 'Holiday', 'RangeStatus'];
        bools.forEach(f => vm[f] = (vm[f] === 'true' || vm[f] === 'on' || vm[f] === 'True' || vm[f] === true));

        const timeFields = [
            'Lr1Hi', 'Lr1Hf', 'Lr2Hi', 'Lr2Hf', 'Lr3Hi', 'Lr3Hf',
            'Mr1Hi', 'Mr1Hf', 'Mr2Hi', 'Mr2Hf', 'Mr3Hi', 'Mr3Hf',
            'Wr1Hi', 'Wr1Hf', 'Wr2Hi', 'Wr2Hf', 'Wr3Hi', 'Wr3Hf',
            'Jr1Hi', 'Jr1Hf', 'Jr2Hi', 'Jr2Hf', 'Jr3Hi', 'Jr3Hf',
            'Vr1Hi', 'Vr1Hf', 'Vr2Hi', 'Vr2Hf', 'Vr3Hi', 'Vr3Hf',
            'Sr1Hi', 'Sr1Hf', 'Sr2Hi', 'Sr2Hf', 'Sr3Hi', 'Sr3Hf',
            'Dr1Hi', 'Dr1Hf', 'Dr2Hi', 'Dr2Hf', 'Dr3Hi', 'Dr3Hf',
            'Fr1Hi', 'Fr1Hf', 'Fr2Hi', 'Fr2Hf', 'Fr3Hi', 'Fr3Hf'
        ];
        timeFields.forEach(f => {
            if (f in vm && vm[f]) {
                const t = vm[f].toString().trim();
                vm[f] = (/^\d{2}:\d{2}$/).test(t) ? t : null;
            } else vm[f] = null;
        });

        if ('Id' in vm) {
            const n = parseInt(vm.Id || '0', 10);
            if (!n) delete vm.Id; else vm.Id = n;
        }

        return vm;
    }

    // ====== Envío AJAX genérico del form (Create/Edit) ======
    async function postForm(formEl) {
        const fd = new FormData(formEl);

        const isEdit = (formEl.id === 'range-edit-form');
        if (!fd.has('IsEdit')) fd.append('IsEdit', isEdit ? 'true' : 'false');

        const resp = await fetch(formEl.action, {
            method: 'POST',
            body: fd,
            headers: {
                'RequestVerificationToken': antiForgery(),
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (!resp.ok) {
            throw new Error(`HTTP ${resp.status}: ${resp.statusText}`);
        }

        const contentType = resp.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            throw new Error('La respuesta no es JSON válido');
        }

        return await resp.json();
    }

    // ====== flujo unificado submit ======
    let submitting = false;
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (submitting) return;
        submitting = true;

        try {
            if (window.$ && $.validator && !$(form).valid()) {
                submitting = false; return;
            }

            const v = validateAllDays();
            if (!v.allOk) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Revisa los horarios',
                    html: v.allErrors.map(x => `• ${x}`).join('<br>')
                });
                return;
            }

            const vm = buildVmForValidateUnique(form);
            const validateUrl = form.getAttribute('data-validate-url') || '/Range/ValidateUnique';

            try {
                const resp = await fetch(validateUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': antiForgery(),
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(vm)
                });
                const isJson = (resp.headers.get('content-type') || '').includes('application/json');
                const vu = isJson ? await resp.json() : null;

                if (vu && vu.success === false && vu.code === 'duplicate') {
                    submitting = false;
                    await Swal.fire({ icon: 'info', title: 'Duplicado', text: vu.message || 'La combinación cliente + horarios ya existe.' });
                    return;
                }

                const isEdit = form.id === 'range-edit-form';
                const confirm = await Swal.fire({
                    title: isEdit ? '¿Guardar cambios?' : '¿Crear rango?',
                    text: 'Se guardará la información del rango.',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'Sí, guardar',
                    cancelButtonText: 'Cancelar'
                });
                if (!confirm.isConfirmed) { submitting = false; return; }

                Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

                const result = await postForm(form);
                Swal.close();

                if (result && result.success) {
                    await Swal.fire({ icon: 'success', title: '¡Éxito!', text: result.message || (isEdit ? 'Rango actualizado.' : 'Rango creado.') });
                    if (result.data?.redirectUrl) window.location.href = result.data.redirectUrl;
                    else if (result.data?.id) window.location.href = `/Range/Details/${result.data.id}`;
                    else window.location.href = '/Range/Index';
                } else {
                    let html = (result && result.message) ? result.message : 'No se pudo guardar.';
                    if (result && result.errors) {
                        const flat = Object.values(result.errors).flat();
                        if (flat.length) html += '<br>' + flat.map(e => `• ${e}`).join('<br>');
                    }
                    await Swal.fire({ icon: 'error', title: 'Error', html });
                }
            } catch (err) {
                console.warn('ValidateUnique falló; continuo con POST real.', err);
            }
        } catch (err) {
            console.error('Error en submit:', err);
            await Swal.fire({ icon: 'error', title: 'Error de red', text: 'No se pudo guardar. Verifique su conexión.' });
        } finally {
            submitting = false;
        }
    });
});