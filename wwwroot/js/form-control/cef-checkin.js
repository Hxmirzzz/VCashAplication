$(function () {
    const { toNumber, fmtPeso, markNumericFields, formatAllNumbers, unformatAllNumbers, bindNumericUXDelegates, amountInWords } = window.CEF;

    bindNumericUXDelegates();

    // Marca campos numéricos: usa .js-num-int para enteros
    $('#cefCheckinForm #TotalDeclaredValue, #cefCheckinForm #DeclaredBagCount')
        .addClass('js-num js-num-int');

    markNumericFields('#cefCheckinForm');
    formatAllNumbers('#cefCheckinForm');

    // Recalcular total (simple): usa el TotalDeclaredValue directo
    async function recalc() {
        const total = toNumber($('#TotalDeclaredValue').val());
        $('#sumTotal').text(fmtPeso(total));

        // opcional: forzar entero al input model
        $('#TotalDeclaredValue').val(total ? Math.round(total) : '');

        const currency = $('#Currency').val() || 'COP';
        const words = await amountInWords($('#cefCheckinForm').data('amount-words-url'), total, currency, false);
        $('#declaredWordsText').text(words);
    }
    $('.js-calc-total, #Currency').on('input change', recalc);
    recalc();

    // Ocultar stepper y dejar 1 solo paso (opcional)
    // Si quieres “modo 1-step”, escondemos el stepper y mostramos todos los bloques:
    $('#progressStepper').hide();
    // Si tu contenido está en pestañas por step, fuerza visibles:
    $('#myTabContent .tab-pane').addClass('show active');

    $('button[data-step-action="submit-final"]').off('click').on('click', function (e) {
        e.preventDefault();

        Swal.fire({
            title: '¿Registrar check-in?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sí, registrar',
            cancelButtonText: 'Cancelar'
        }).then((r) => {
            if (!r.isConfirmed) return;

            Swal.fire({ title: 'Guardando...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

            const $form = $('#cefCheckinForm');

            ['DeclaredEnvelopeCount', 'DeclaredCheckCount', 'DeclaredDocumentCount'].forEach(id => {
                const $i = $('#' + id);
                if ($i.length && ($i.val() === '' || $i.val() == null)) $i.val('0');
            }); 

            unformatAllNumbers('#cefCheckinForm');
            const payload = $form.serialize();
            formatAllNumbers('#cefCheckinForm');

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

            $.ajax({
                url: $form.attr('action'),
                type: 'POST',
                data: payload,
                headers: { 'RequestVerificationToken': token, 'X-Requested-With': 'XMLHttpRequest' }
            }).done(function (response) {
                Swal.close();
                if (response?.success) {
                    Swal.fire('¡Éxito!', response.message || 'Check-in registrado.', 'success')
                        .then(() => window.location.href = response?.data?.url || '@Url.Action("Reception","Cef")');
                } else {
                    Swal.fire('Error', (response?.message) || 'Corrija los errores del formulario.', 'error');
                }
            }).fail(function (xhr) {
                Swal.close();
                const msg = (xhr.responseJSON && xhr.responseJSON.message) || xhr.statusText || 'Error de red';
                Swal.fire('Error', msg, 'error');
            });
        });
    });
});