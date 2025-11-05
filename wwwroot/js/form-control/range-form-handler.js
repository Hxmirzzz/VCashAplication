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
        statusSelect.addEventListener('change', syncStatus);
        syncStatus();
    }

    // ====== habilitar/deshabilitar inputs de cada día ======
    function toggleRow($tr) {
        const checked = $tr.querySelector('.js-day-flag')?.checked;
        $tr.querySelectorAll('.js-t').forEach(i => i.disabled = !checked);
    }
    document.querySelectorAll('tr[data-day]').forEach(tr => toggleRow(tr));
    form.addEventListener('change', (e) => {
        const tr = e.target.closest('tr[data-day]');
        if (tr && e.target.classList.contains('js-day-flag')) toggleRow(tr);
    });

    // ====== validación de horarios en cliente ======
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

        // detecta los names presentes en esa fila (Create y Edit comparten sufijos)
        const pairs = [
            ['1Hi', '1Hf', 1],
            ['2Hi', '2Hf', 2],
            ['3Hi', '3Hf', 3],
        ];

        pairs.forEach(([a, b, n]) => {
            const hi = tr.querySelector(`input[name$="${a}"]`);
            const hf = tr.querySelector(`input[name$="${b}"]`);
            if (!hi || !hf) return;

            // limpiar marcas previas
            hi.classList.remove('is-invalid'); hf.classList.remove('is-invalid');

            const vHi = hi.value?.trim();
            const vHf = hf.value?.trim();
            const hasHi = !!vHi, hasHf = !!vHf;

            // si hay uno, el otro es obligatorio
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
                }
            }
        });

        return { ok: errors.length === 0, errors };
    }

    function validateAllDays() {
        let allOk = true;
        let allErrors = [];
        document.querySelectorAll('tr[data-day]').forEach(tr => {
            const res = validateDayRow(tr);
            if (!res.ok) {
                allOk = false;
                allErrors = allErrors.concat(res.errors);
            }
        });
        return { allOk, allErrors };
    }

    // ====== FUNCIÓN CORREGIDA: arma payload JSON para ValidateUnique ======
    function buildVmForValidateUnique(formEl) {
        const fd = new FormData(formEl);
        const vm = {};

        // Obtener todos los campos del FormData
        for (const [key, value] of fd.entries()) {
            vm[key] = value;
        }

        // Conversiones específicas
        vm.ClientId = parseInt(vm.ClientId || '0', 10) || 0;

        // Convertir campos booleanos
        const booleanFields = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday', 'Holiday', 'RangeStatus'];
        booleanFields.forEach(field => {
            if (field in vm) {
                vm[field] = vm[field] === 'true' || vm[field] === 'on' || vm[field] === 'True' || vm[field] === true;
            } else {
                vm[field] = false; // Valor por defecto para checkboxes no marcados
            }
        });

        // Convertir campos TimeSpan (formato HH:mm a TimeSpan string o null)
        const timeFields = [
            'Lr1Hi', 'Lr1Hf', 'Lr2Hi', 'Lr2Hf', 'Lr3Hi', 'Lr3Hf', // Lunes
            'Mr1Hi', 'Mr1Hf', 'Mr2Hi', 'Mr2Hf', 'Mr3Hi', 'Mr3Hf', // Martes
            'Wr1Hi', 'Wr1Hf', 'Wr2Hi', 'Wr2Hf', 'Wr3Hi', 'Wr3Hf', // Miércoles
            'Jr1Hi', 'Jr1Hf', 'Jr2Hi', 'Jr2Hf', 'Jr3Hi', 'Jr3Hf', // Jueves
            'Vr1Hi', 'Vr1Hf', 'Vr2Hi', 'Vr2Hf', 'Vr3Hi', 'Vr3Hf', // Viernes
            'Sr1Hi', 'Sr1Hf', 'Sr2Hi', 'Sr2Hf', 'Sr3Hi', 'Sr3Hf', // Sábado
            'Dr1Hi', 'Dr1Hf', 'Dr2Hi', 'Dr2Hf', 'Dr3Hi', 'Dr3Hf', // Domingo
            'Fr1Hi', 'Fr1Hf', 'Fr2Hi', 'Fr2Hf', 'Fr3Hi', 'Fr3Hf'  // Festivo
        ];

        timeFields.forEach(field => {
            if (field in vm && vm[field]) {
                const timeValue = vm[field].toString().trim();
                // Validar formato HH:mm
                if (timeValue && /^\d{2}:\d{2}$/.test(timeValue)) {
                    // Mantener como string en formato HH:mm para que ASP.NET Core lo convierta automáticamente
                    vm[field] = timeValue;
                } else {
                    vm[field] = null;
                }
            } else {
                vm[field] = null;
            }
        });

        if ('Id' in vm && (vm.Id === '' || vm.Id === '0')) {
            delete vm.Id;
        } else if ('Id' in vm) {
            vm.Id = parseInt(vm.Id, 10) || undefined;
        }

        return vm;
    }

    // ====== Envío AJAX genérico del form (Create/Edit) ======
    async function postForm(formEl) {
        const isEdit = formEl.id === 'range-edit-form';

        let actionUrl;
        if (isEdit) {
            actionUrl = '/Range/Edit';
        } else {
            actionUrl = formEl.action;
        }

        const fd = new FormData(formEl);

        const resp = await fetch(actionUrl, {
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

    function clear($tr) {
        $tr.querySelectorAll('.js-t').forEach(i => {
            i.value = '';
            i.classList.remove('is-invalid');
        });
    }

    function toggleRow($tr) {
        const checked = $tr.querySelector('.js-day-flag')?.checked;

        if (!checked) {
            clear($tr);
        }

        $tr.querySelectorAll('.js-t').forEach(i => {
            i.disabled = !checked;
        });
    }

    document.querySelectorAll('tr[data-day]').forEach(tr => toggleRow(tr));

    form.addEventListener('change', (e) => {
        const tr = e.target.closest('tr[data-day]');
        if (tr && e.target.classList.contains('js-day-flag')) {
            toggleRow(tr);
        }
    });

    // ====== flujo unificado submit ======
    form.addEventListener('submit', async (e) => {
        e.preventDefault();

        // 1) validación MVC unobtrusive si existe
        if (window.$ && $.validator && !$(form).valid()) return;

        // 2) validación de horarios en cliente
        const v = validateAllDays();
        if (!v.allOk) {
            Swal.fire({
                icon: 'warning',
                title: 'Revisa los horarios',
                html: v.allErrors.map(x => `• ${x}`).join('<br>')
            });
            return;
        }

        // 3) confirmación
        const isEdit = form.id === 'range-edit-form';
        const confirm = await Swal.fire({
            title: isEdit ? '¿Guardar cambios?' : '¿Crear rango?',
            text: 'Se guardará la información del rango.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, guardar',
            cancelButtonText: 'Cancelar'
        });
        if (!confirm.isConfirmed) return;

        // 4) loading
        Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

        try {
            // 5) ValidateUnique (usa el mismo endpoint para Create/Edit)
            const vm = buildVmForValidateUnique(form);
            const validateUrl = form.getAttribute('data-validate-url') || '/Range/ValidateUnique';

            let canContinue = true;
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

                let vuResp = null;
                if ((resp.headers.get('content-type') || '').includes('application/json')) {
                    vuResp = await resp.json();
                }

                if (vuResp && vuResp.success === false) {
                    if (vuResp.code === 'duplicate') {
                        const msg = vuResp.message || 'La combinación cliente + horarios ya existe.';
                        await Swal.fire({ icon: 'info', title: 'Validación', text: msg });
                        canContinue = false;
                    } else {
                        console.warn('ValidateUnique warning:', vuResp.code, vuResp.message);
                    }
                }
            } catch (e) {
                console.warn('ValidateUnique falló; continuo con POST real.', e);
            }

            if (!canContinue) return;

            // 6) Enviar formulario al Action real (Create o Edit)
            const result = await postForm(form);

            if (result && result.success) {
                await Swal.fire({ icon: 'success', title: '¡Éxito!', text: result.message || (isEdit ? 'Rango actualizado.' : 'Rango creado.') });

                if (result.data && result.data.redirectUrl) {
                    window.location.href = result.data.redirectUrl;
                } else if (result.data && result.data.id) {
                    window.location.href = `/Range/Details/${result.data.id}`;
                } else {
                    window.location.href = '/Range/Index';
                }
            } else {
                let html = (result && result.message) ? result.message : 'No se pudo guardar.';
                if (result && result.errors) {
                    const flat = Object.values(result.errors).flat();
                    if (flat.length) html += '<br>' + flat.map(e => `• ${e}`).join('<br>');
                }
                await Swal.fire({ icon: 'error', title: 'Error', html });
            }
        } catch (err) {
            console.error('Error en submit:', err);
            await Swal.fire({ icon: 'error', title: 'Error de red', text: 'No se pudo guardar. Verifique su conexión.' });
        }
    });
});