namespace VCashApp.Enums
{
    public enum LocationTypeEnum
    {
        Punto,
        ATM,
        Fondo
    }

    public enum CefTransactionTypeEnum
    {
        Collection,
        Provision,
        Audit
    }

    public enum CefTransactionStatusEnum
    {
        Checkin,
        EnqueuedForCounting,
        BillCounting,
        CoinCounting,
        CheckCounting,
        DocumentCounting,
        PendingReview,
        Approved,
        Rejected,
        Cancelled
    }

    public enum CefContainerTypeEnum
    {
        Bag,
        Envelope
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
        Bill,
        Coin,
        Check,
        Document
    }

    public enum CefIncidentTypeCategoryEnum // Renamed to avoid confusion with the entity
    {
        Overload, // Sobrante
        Shortage, // Faltante
        Fake, // Falso
        Damaged, // Deteriorado
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