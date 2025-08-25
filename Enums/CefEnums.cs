namespace VCashApp.Enums
{
    public enum LocationTypeEnum
    {
        Punto = 0,
        ATM= 1,
        Fondo = 2
    }

    public enum CefEnvelopeSubTypeEnum
    {
        Efectivo = 0,
        Documento = 1,
        Cheque = 2
    }

    public enum CefTransactionTypeEnum // ELIMINAR
    {
        Collection,
        Provision,
        Audit
    }

    public enum CefTransactionStatusEnum
    {
        RegistroTesoreria,        // Checkin
        EncoladoParaConteo,       // EnqueuedForCounting
        ConteoBilletes,           // BillCounting
        ConteoMonedas,            // CoinCounting
        ConteoCheques,            // CheckCounting
        ConteoDocumentos,         // DocumentCounting
        PendienteRevision,        // PendingReview
        Aprobado,                 // Approved
        Rechazado,                // Rejected
        Cancelado                 // Cancelled
    }


    public enum CefContainerTypeEnum
    {
        Bolsa,
        Sobre
    }

    public enum CefContainerStatusEnum
    {
        Pending,
        InProcess,
        Counted,
        Verified,
        WithIncident
    }

    public enum CefValueTypeEnum
    {
        Billete,
        Moneda,
        Documento,
        Cheque
    }

    public enum CefIncidentTypeCategoryEnum
    {
        Overload,
        Shortage,
        Fake,
        Damaged,
        CountingError,
        ContainerInconsistency,
        Other
    }

    public enum AppliesToEnum
    {
        Service,
        Incident,
        Both
    }

    // Tu EstadoRuta existente si se sigue usando en el módulo de rutas
}