document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById('points-form');

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = new FormData(form);

            Swal.fire({
                title: 'Cargando...',
                html: 'Por favor espera...',
                didOpen: () => Swal.showLoading()
            });

            const saveUrl = form.getAttribute('action');
            fetch(saveUrl, {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(response => response.ok ? response.json() : Promise.reject('Error en la respuesta del servidor'))
                .then(data => {
                    Swal.close();
                    if (data.success) {
                        Swal.fire({
                            title: '¡Éxito!',
                            text: data.message,
                            icon: 'success'
                        }).then(() => {
                            window.location.href = data.redirectUrl || '/Point/Index';
                        });
                    } else {
                        Swal.fire({
                            title: '¡Advertencia!',
                            text: data.message,
                            icon: 'error'
                        });
                    }
                })
                .catch((error) => {
                    Swal.close();
                    Swal.fire({
                        title: '¡Error!',
                        text: 'Hubo un problema al procesar la solicitud.',
                        icon: 'error'
                    });
                });
        });
    }
});

// ============================================================================
// 2. UTILIDADES DE NORMALIZACIÓN DE CÓDIGOS (PK)
// ============================================================================
function normalizePk(val) {
    return (val ?? '').trim().toUpperCase();
}

function hadOuterSpaces(val) {
    if (val == null) return false;
    return val !== val.trim();
}

function showWarn(msg) {
    if (window.Swal) {
        Swal.fire({ icon: 'warning', title: 'Atención', text: msg });
    } else {
        alert(msg);
    }
}

function attachPkSanitizer(selector, onNormalized) {
    const el = document.querySelector(selector);
    if (!el) return;

    el.addEventListener('blur', (e) => {
        const before = e.target.value;
        const after = normalizePk(before);
        const changed = before !== after;
        const outerSpaces = hadOuterSpaces(before);

        if (changed) {
            e.target.value = after;
            if (outerSpaces) showWarn('Se quitaron espacios al inicio/fin del código.');
        }
        if (typeof onNormalized === 'function') onNormalized(after, changed);
    });

    // Evita que el primer carácter sea espacio
    el.addEventListener('keydown', (e) => {
        if (e.key === ' ' && e.target.selectionStart === 0) {
            e.preventDefault();
        }
    });
}

// ============================================================================
// 3. GENERACIÓN DE CÓDIGOS (CodPunto y CodCas4u)
// ============================================================================
function updateCodPunto() {
    const codClienteEl = document.getElementById('CodCliente');
    const codPClienteEl = document.getElementById('CodPCliente');
    const codPuntoEl = document.getElementById('CodPunto');
    const codCash4uEl = document.getElementById('CodCas4u');

    if (!codClienteEl || !codPClienteEl || !codPuntoEl || !codCash4uEl) return;

    const codClienteRaw = codClienteEl.value;
    const codPClienteRaw = codPClienteEl.value;

    const codCliente = normalizePk(codClienteRaw);
    const codPCliente = normalizePk(codPClienteRaw);

    // Reflejar normalización en los inputs
    if (codClienteRaw !== codCliente) codClienteEl.value = codCliente;
    if (codPClienteRaw !== codPCliente) codPClienteEl.value = codPCliente;

    if (codCliente && codPCliente) {
        codPuntoEl.value = `${codCliente}-${codPCliente}`;
        codCash4uEl.value = `${codCliente}|${codPCliente}`;
    } else {
        codPuntoEl.value = '';
        codCash4uEl.value = '';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    attachPkSanitizer('#CodPCliente', () => updateCodPunto());
    attachPkSanitizer('#CodCliente', () => updateCodPunto());

    const srcIds = ['CodCliente', 'CodPCliente'];
    srcIds.forEach(id => {
        const el = document.getElementById(id);
        if (!el) return;
        ['input', 'change'].forEach(evt => el.addEventListener(evt, updateCodPunto));
    });

    updateCodPunto();
});

document.addEventListener('DOMContentLoaded', () => {
    const codClienteEl = document.getElementById('CodCliente');
    const tipoPuntoEl = document.getElementById('TipoPunto');

    if (!codClienteEl && !tipoPuntoEl) return;

    ['change', 'blur'].forEach(evt => {
        codClienteEl?.addEventListener(evt, updateCodVatco);
        tipoPuntoEl?.addEventListener(evt, updateCodVatco);
    });

    updateCodVatco();
});

// ============================================================================
// 4. OBTENER CÓDIGO VATCO
// ============================================================================
function updateCodVatco() {
    const codClienteEl = document.getElementById('CodCliente');
    const vatcoCodeEl = document.getElementById('VatcoPointCode');
    const tipoPuntoEl = document.getElementById('TipoPunto');

    if (!codClienteEl || !vatcoCodeEl || !tipoPuntoEl) return;

    const codCliente = parseInt(codClienteEl.value, 10);
    const tipoPunto = parseInt(tipoPuntoEl.value, 10);

    // Validaciones reales
    if (!Number.isInteger(codCliente) || !Number.isInteger(tipoPunto)) {
        vatcoCodeEl.value = '';
        return;
    }

    fetch('/Point/GetNewVatcoCode', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `codCliente=${codCliente}&tipoPunto=${tipoPunto}`
    })
        .then(r => {
            if (!r.ok) throw new Error('Error backend');
            return r.text();
        })
        .then(code => {
            vatcoCodeEl.value = code.trim();
        })
        .catch(err => {
            console.error('VATCO error:', err);
            vatcoCodeEl.value = '';
        });
}

document.addEventListener('DOMContentLoaded', () => {
    const clientSelect = document.getElementById('CodCliente');
    const mainClientSelect = document.getElementById('CodClientePpal');

    if (!clientSelect || !mainClientSelect) return;

    async function refreshMainClient() {
        const codCliente = clientSelect.value;

        if (!codCliente) {
            mainClientSelect.innerHTML = '<option value="0">NINGUNO</option>';
            mainClientSelect.disabled = true;
            return;
        }

        try {
            const data = await fetch(
                `/Point/GetMainClientOptions?codCliente=${codCliente}`
            ).then(r => r.json());

            mainClientSelect.innerHTML = '';

            data.forEach(o => {
                const opt = document.createElement('option');
                opt.value = o.value;
                opt.textContent = o.text;
                opt.selected = o.selected;
                mainClientSelect.appendChild(opt);
            });

            mainClientSelect.disabled = data[0]?.lockSelect === true;

        } catch {
            mainClientSelect.innerHTML = '<option value="0">NINGUNO</option>';
            mainClientSelect.disabled = true;
        }
    }

    clientSelect.addEventListener('change', refreshMainClient);

    // Edición
    refreshMainClient();
});

// ============================================================================
// 5. GESTIÓN DE FONDOS (basado en Sucursal y Cliente Principal)
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const sucSelect = document.getElementById('CodSuc');
    const fondosSelect = document.getElementById('CodFondo');
    const fondoPuntoSelect = document.getElementById('FondoPunto');

    if (!sucSelect || !fondosSelect || !fondoPuntoSelect) return;

    function toggleFondosEnabled() {
        const modo = String(fondoPuntoSelect.value);
        if (modo === '1') {
            fondosSelect.disabled = false;
            fondosSelect.multiple = false;
        } else {
            fondosSelect.disabled = true;
            fondosSelect.value = '';
        }
    }

    async function refreshFondos() {
        const codSuc = sucSelect.value;
        const codCliente = document.getElementById('CodCliente')?.value || 0;
        const codClientePpal = document.getElementById('CodClientePpal')?.value || 0;

        if (!codSuc) {
            fondosSelect.innerHTML = '<option value="">-- Seleccione fondo --</option>';
            return;
        }

        try {
            const url = `/Point/GetFunds?branchId=${encodeURIComponent(codSuc)}&clientId=${encodeURIComponent(codCliente)}&mainClientId=${encodeURIComponent(codClientePpal)}`;
            const html = await (await fetch(url)).text();
            fondosSelect.innerHTML = html;
            toggleFondosEnabled();
        } catch {
            fondosSelect.innerHTML = '<option value="">Error al cargar</option>';
        }
    }

    sucSelect.addEventListener('change', refreshFondos);
    fondoPuntoSelect.addEventListener('change', toggleFondosEnabled);

    toggleFondosEnabled();
    refreshFondos();
});

// ============================================================================
// 6. COBERTURA Y ESCALA INTERURBANA
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const selectCobertura = document.getElementById('CoberturaPunto');
    const selectEscala = document.getElementById('EscalaInterurbanos');

    if (!selectCobertura || !selectEscala) return;

    function updateEscala() {
        const cobertura = selectCobertura.value;
        selectEscala.innerHTML = '';

        if (cobertura === 'I') {
            for (let i = 1; i <= 5; i++) {
                const option = document.createElement('option');
                option.value = i;
                option.text = i;
                selectEscala.appendChild(option);
            }
        } else if (cobertura === 'U' || cobertura === 'A') {
            const option = document.createElement('option');
            option.value = '0';
            option.text = '0';
            selectEscala.appendChild(option);
        }

        if (cobertura !== 'I') {
            selectEscala.value = '0';
        }
    }

    selectCobertura.addEventListener('change', updateEscala);
    updateEscala();
});

// ============================================================================
// 7. FILTRAR RUTAS POR SUCURSAL
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const sucSelect = document.getElementById('CodSuc');
    const rutaSelect = document.getElementById('CodRutaSuc');

    if (!sucSelect || !rutaSelect) return;

    function resetRutas() {
        rutaSelect.innerHTML = '<option value="">-- Seleccione ruta --</option>';
        rutaSelect.disabled = true;
    }

    async function cargarRutas(codSuc) {
        try {
            const html = await fetch(`/Point/GetRoutes?branchId=${codSuc}`)
                .then(r => r.text());

            rutaSelect.innerHTML = html;
            rutaSelect.disabled = false;
        } catch {
            rutaSelect.innerHTML = '<option value="">Error al cargar</option>';
            rutaSelect.disabled = true;
        }
    }

    resetRutas();

    sucSelect.addEventListener('change', function () {
        const codSuc = this.value;

        if (!codSuc || codSuc === '0') {
            resetRutas();
            return;
        }

        cargarRutas(codSuc);
    });
});

// ============================================================================
// 8. HABILITAR/DESHABILITAR CAMPO "OTROS"
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const selectOtrosValores = document.getElementById('OtrosValoresPunto');
    const inputOtros = document.getElementById('Otros');

    if (!selectOtrosValores || !inputOtros) return;

    function toggleOtros() {
        if (selectOtrosValores.value == '1') {
            inputOtros.disabled = false;
        } else {
            inputOtros.disabled = true;
            inputOtros.value = '';
        }
    }

    selectOtrosValores.addEventListener('change', toggleOtros);
    toggleOtros();
});

// ============================================================================
// 9. GESTIÓN DE RANGOS (filtrado por cliente)
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const codClienteEl = document.getElementById('CodCliente');
    const rangoSelect = document.getElementById('CodRango');
    const infoRangoInput = document.getElementById('info_rango_atencion');

    if (!codClienteEl || !rangoSelect) return;

    function cargarRangosPorCliente() {
        const codCliente = codClienteEl.value;

        if (codCliente) {
            fetch(`/Point/GetRangesByClient?clientId=${codCliente}`)
                .then(response => response.text())
                .then(data => {
                    rangoSelect.innerHTML = data;
                    if (infoRangoInput) infoRangoInput.value = '';
                })
                .catch(() => {
                    rangoSelect.innerHTML = '<option value="">Error al cargar</option>';
                });
        } else {
            rangoSelect.innerHTML = '<option value="">-- Seleccione rango --</option>';
            if (infoRangoInput) infoRangoInput.value = '';
        }
    }

    codClienteEl.addEventListener('change', cargarRangosPorCliente);

    cargarRangosPorCliente();
});

// ============================================================================
// 10. SINCRONIZAR ESTADO (select visible con hidden)
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const estadoSelect = document.getElementById('EstadoSelect');
    const estadoHidden = document.getElementById('EstadoPunto');

    if (!estadoSelect || !estadoHidden) return;

    estadoSelect.addEventListener('change', function () {
        estadoHidden.value = this.value;
    });

    // Inicializar valor
    const estadoActual = estadoHidden.value || 'true';
    estadoSelect.value = estadoActual;
});

// ============================================================================
// 11. DROPZONE - CARTA DE INCLUSIÓN
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const dropzone = document.getElementById('carta-dropzone-puntos');
    const fileInput = document.getElementById('carta_file');
    const clearBtn = document.getElementById('carta_clear_btn_puntos');
    const filePreview = document.getElementById('file-preview-puntos');
    const fileName = document.getElementById('file-name-puntos');
    const fileSize = document.getElementById('file-size-puntos');
    const cartaPreview = document.getElementById('carta_preview_puntos');
    const successMessage = document.getElementById('success-message-puntos');

    if (!dropzone || !fileInput) return;

    // Click en dropzone
    dropzone.addEventListener('click', () => fileInput.click());

    // Drag & Drop
    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
    });

    dropzone.addEventListener('dragleave', () => {
        dropzone.classList.remove('dragover');
    });

    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
        if (e.dataTransfer.files.length) {
            fileInput.files = e.dataTransfer.files;
            handleFileSelect(e.dataTransfer.files[0]);
        }
    });

    // Selección de archivo
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length) {
            handleFileSelect(e.target.files[0]);
        }
    });

    // Limpiar archivo
    if (clearBtn) {
        clearBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            fileInput.value = '';
            dropzone.classList.remove('has-file');
            if (filePreview) filePreview.classList.remove('show');
            if (successMessage) successMessage.classList.remove('show');
        });
    }

    function handleFileSelect(file) {
        if (fileName) fileName.textContent = file.name;
        if (fileSize) fileSize.textContent = formatFileSize(file.size);
        if (cartaPreview) cartaPreview.textContent = file.name;

        dropzone.classList.add('has-file');
        if (filePreview) filePreview.classList.add('show');
        if (successMessage) successMessage.classList.add('show');
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }
});

// ============================================================================
// 12. INFO DE RANGO (MODAL)
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    const rangoSelect = document.getElementById('CodRango');
    const btnInfo = document.getElementById('btnInfoRango');
    const modalBody = document.getElementById('modalRangoInfoBody');
    const modalElement = document.getElementById('modalRangoInfo');
    const modal = new bootstrap.Modal(modalElement);

    if (!rangoSelect || !btnInfo) return;

    function toggleBtn() {
        btnInfo.disabled = !rangoSelect.value;
    }

    btnInfo.addEventListener('click', async () => {
        const rangeId = rangoSelect.value;
        if (!rangeId) return;

        modalBody.innerHTML = '<div class="text-center">Cargando...</div>';
        modal.show();

        try {
            const html = await fetch(`/Point/GetRangeInfo?rangeId=${rangeId}`)
                .then(r => r.text());
            modalBody.innerHTML = html;
        } catch {
            modalBody.innerHTML = '<div class="text-danger">Error al cargar información</div>';
        }
    });

    modalElement.addEventListener('hide.bs.modal', function (e) {
        if (document.activeElement && modalElement.contains(document.activeElement)) {
            document.activeElement.blur();
        }
    });

    modalElement.addEventListener('hidden.bs.modal', function (e) {
        modalBody.innerHTML = '';
    });

    rangoSelect.addEventListener('change', toggleBtn);
    toggleBtn();
});