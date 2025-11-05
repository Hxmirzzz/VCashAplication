namespace VCashApp.Models.DTOs
{
    public sealed record RouteListDto(
        string BranchRouteCode,
        string? RouteCode,
        string? RouteName,
        int? BranchId,
        string? BranchName,
        string? RouteType,
        string? ServiceType,
        string? VehicleType,
        decimal? Amount,
        bool IsActive
    );

    public sealed class RouteFilterDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public string? Search { get; set; }
        public int? BranchId { get; set; }
        public string? RouteType { get; set; }
        public string? ServiceType { get; set; }
        public string? VehicleType { get; set; }
        public int? IsActive { get; set; } // 1/0 desde UI; se traduce a bool?
    }

    public sealed class RouteUpsertDto
    {
        public string BranchRouteCode { get; set; } = default!;
        public string? RouteCode { get; set; }
        public string? RouteName { get; set; }
        public int? BranchId { get; set; }
        public string? RouteType { get; set; }
        public string? ServiceType { get; set; }
        public string? VehicleType { get; set; }
        public decimal? Amount { get; set; }
        public bool IsActive { get; set; }

        // Days flags and hours
        public bool Monday { get; set; }
        public TimeSpan? MondayStartTime { get; set; }
        public TimeSpan? MondayEndTime { get; set; }

        public bool Tuesday { get; set; }
        public TimeSpan? TuesdayStartTime { get; set; }
        public TimeSpan? TuesdayEndTime { get; set; }

        public bool Wednesday { get; set; }
        public TimeSpan? WednesdayStartTime { get; set; }
        public TimeSpan? WednesdayEndTime { get; set; }

        public bool Thursday { get; set; }
        public TimeSpan? ThursdayStartTime { get; set; }
        public TimeSpan? ThursdayEndTime { get; set; }

        public bool Friday { get; set; }
        public TimeSpan? FridayStartTime { get; set; }
        public TimeSpan? FridayEndTime { get; set; }

        public bool Saturday { get; set; }
        public TimeSpan? SaturdayStartTime { get; set; }
        public TimeSpan? SaturdayEndTime { get; set; }

        public bool Sunday { get; set; }
        public TimeSpan? SundayStartTime { get; set; }
        public TimeSpan? SundayEndTime { get; set; }

        public bool Holiday { get; set; }
        public TimeSpan? HolidayStartTime { get; set; }
        public TimeSpan? HolidayEndTime { get; set; }
    }

    public sealed record RouteExportDto(
        string RouteCode,
        string RouteName,
        string Branch,
        string RouteType,
        string ServiceType,
        string VehicleType,
        decimal? Amount,
        string Status // "ACTIVO/INACTIVO"
    );
}