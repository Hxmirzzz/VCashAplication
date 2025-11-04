using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Services.GestionServicio.Domain;

namespace VCashApp.Services.GestionServicio.Infrastructure
{
    public class EfServiceDropdownsProvider : IServiceDropdownsProvider
    {
        private readonly AppDbContext _db;
        public EfServiceDropdownsProvider(AppDbContext db) => _db = db;

        public async Task<List<SelectListItem>> ClientsAsync()
            => await _db.AdmClientes.Where(c => c.Status)
            .Select(c => new SelectListItem { Value = c.ClientCode.ToString(), Text = c.ClientName })
            .OrderBy(c => c.Text).ToListAsync();

        public async Task<List<SelectListItem>> BranchesAsync()
             => await _db.AdmSucursales.Where(s => s.Estado && s.CodSucursal != 32)
            .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
            .OrderBy(s => s.Text).ToListAsync();

        public async Task<List<SelectListItem>> ConceptsAsync()
            => await _db.AdmConceptos
                .Select(c => new SelectListItem { Value = c.CodConcepto.ToString(), Text = c.NombreConcepto })
                .ToListAsync();

        public async Task<List<SelectListItem>> StatusesAsync()
            => await _db.AdmEstados
                .Select(e => new SelectListItem { Value = e.StateCode.ToString(), Text = e.StateName })
                .OrderBy(e => e.Text).ToListAsync();

        public async Task<List<SelectListItem>> ServiceModalitiesAsync()
            => await Task.FromResult(new List<SelectListItem>{
                new("Programado","1"), new("Pedido","2"), new("Frecuente","3")
            });

        public List<SelectListItem> Currencies()
            => Enum.GetNames(typeof(CurrencyEnum))
                   .Select(code => new SelectListItem { Value = code, Text = code }).ToList();

        public List<SelectListItem> LocationTypes()
            => new() { new("Punto", "P"), new("ATM", "A"), new("Fondo", "F") };

        public async Task<List<SelectListItem>> FailedResponsiblesAsync()
            => await Task.FromResult(new List<SelectListItem>{
                new("Cliente", "Cliente"), new("Vatco", "Vatco")
            });
    }
}
