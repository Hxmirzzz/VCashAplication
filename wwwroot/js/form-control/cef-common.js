window.CEF = window.CEF || (function () {
    const NF_INT = new Intl.NumberFormat('es-CO');

    function toNumber(v) {
        if (v == null) return 0;
        v = String(v).trim();
        if (v === '') return 0;
        // quita separador miles y normaliza decimales
        v = v.replace(/\./g, '').replace(',', '.');
        const n = Number(v);
        return isNaN(n) ? 0 : n;
    }

    function fmtInt(n) {
        return NF_INT.format(Number(n) || 0);
    }

    function fmtPeso(n) {
        const x = Number(n) || 0;
        if (x < 0) return '-$ ' + NF_INT.format(Math.abs(x));
        return '$ ' + NF_INT.format(x);
    }

    function fmtDec(n, d) {
        return new Intl.NumberFormat('es-CO', {
            minimumFractionDigits: d,
            maximumFractionDigits: d
        }).format(Number(n) || 0);
    }

    // Marcar inputs que sean numéricos (enteros por defecto)
    function markNumericFields($root) {
        const $scope = $root ? $($root) : $(document);
        $scope.find('input.js-num').each(function () {
            // Todos los .js-num los tratamos como texto con inputmode=numeric para controlar formateo
            $(this).attr('type', 'text').attr('inputmode', 'numeric');
        });
    }

    function unformatInput($i) {
        const raw = toNumber($i.val());
        if ($i.hasClass('js-num-int')) {
            $i.val(Number.isFinite(raw) ? String(Math.round(raw)) : '');
        } else {
            const d = Number($i.data('decimals') || 2);
            // CORRECCIÓN: Era Number.IsFinite (con I mayúscula), debe ser Number.isFinite
            $i.val(Number.isFinite(raw) ? raw.toFixed(d) : '');
        }
    }

    function formatInput($i) {
        const val = $i.val();
        if (val === '') return;
        const n = toNumber(val);
        if ($i.hasClass('js-num-int')) {
            $i.val(fmtInt(Math.round(n)));
        } else {
            const d = Number($i.data('decimals') || 2);
            $i.val(fmtDec(n, d));
        }
    }

    function formatAllNumbers($root) {
        const $scope = $root ? $($root) : $(document);
        $scope.find('.js-num').each(function () {
            formatInput($(this));
        });
    }

    function unformatAllNumbers($root) {
        const $scope = $root ? $($root) : $(document);
        $scope.find('.js-num').each(function () {
            unformatInput($(this));
        });
    }

    // Delegados de UX numérica
    function bindNumericUXDelegates() {
        // Evita doble binding si ya están
        if (bindNumericUXDelegates._bound) return;
        bindNumericUXDelegates._bound = true;

        $(document).on('focus', '.js-num', function () {
            unformatInput($(this));
            this.select();
        });

        $(document).on('blur', '.js-num', function () {
            formatInput($(this));
        });

        $(document).on('keydown', '.js-num', function (e) {
            const k = e.key;
            if (['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Home', 'End'].includes(k)) return;
            // Permite separador decimal solo si NO es entero
            if ((k === ',' || k === '.') && !$(this).hasClass('js-num-int')) return;
            if (!/^\d$/.test(k)) e.preventDefault();
        });
    }

    // Helper para "valor en palabras"
    async function amountInWords(url, value, currency, includeCents) {
        try {
            const res = await $.get(url, { value, currency, includeCents: !!includeCents });
            return (res && res.words) ? res.words : '';
        } catch {
            return '';
        }
    }

    // NUEVA FUNCIÓN: Inicialización automática
    function init($root) {
        markNumericFields($root);
        bindNumericUXDelegates();
        formatAllNumbers($root);
    }

    // Auto-inicialización cuando el DOM esté listo
    $(document).ready(function () {
        init();
    });

    // Expuesto
    return {
        toNumber, fmtInt, fmtPeso, fmtDec,
        markNumericFields, unformatInput, formatInput,
        formatAllNumbers, unformatAllNumbers,
        bindNumericUXDelegates,
        amountInWords,
        init
    };
})();