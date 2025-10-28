using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Services.EmployeeLog.Queries
{
    public sealed class EmployeeLogDropdownsDto
    {
        public SelectList Branches { get; }
        public SelectList Cargos { get; }
        public SelectList Unidades { get; }
        public SelectList LogStatuses { get; }

        public EmployeeLogDropdownsDto(
            SelectList branches,
            SelectList cargos,
            SelectList unidades,
            SelectList logStatuses)
        {
            Branches = branches;
            Cargos = cargos;
            Unidades = unidades;
            LogStatuses = logStatuses;
        }
    }
}
