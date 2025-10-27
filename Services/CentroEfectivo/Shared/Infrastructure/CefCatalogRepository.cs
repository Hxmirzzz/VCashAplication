using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Services.CentroEfectivo.Shared.Domain;

namespace VCashApp.Services.CentroEfectivo.Shared.Infrastructure
{
    public sealed class CefCatalogRepository :ICefCatalogRepository
    {
        private readonly AppDbContext _db;
        public CefCatalogRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> BuildDenomsJsonForTransactionAsync(int txId)
        {
            var tx = await _db.CefTransactions.AsNoTracking()
                .Where(t => t.Id == txId).Select(t => new { t.Currency })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"Transacción {txId} no existe.");

            var currency = tx.Currency ?? "COP";

            var denoms = await _db.AdmDenominaciones
                .AsNoTracking()
                .Where(d => d.DivisaDenominacion == currency)
                .Select(d => new
                {
                    id = d.CodDenominacion,
                    value = d.ValorDenominacion,
                    money = d.TipoDinero,
                    family = d.FamiliaDenominacion,
                    label = d.Denominacion,
                    bundleSize = d.CantidadUnidadAgrupamiento,
                    isHigh = d.AltaDenominacion
                })
                .OrderBy(d => d.money).ThenByDescending(d => d.value)
                .ToListAsync();

            var esCO = CultureInfo.GetCultureInfo("es-CO");

            string FormatLabel(string lbl, decimal? val) =>
                !string.IsNullOrWhiteSpace(lbl) ? lbl : (val.HasValue ? val.Value.ToString("C0", esCO) : "");

            string NormFam(string fam) => string.IsNullOrWhiteSpace(fam) ? "T" : fam.Trim().ToUpperInvariant();
            int DefaultBundle(string money) => money == "B" ? 100 : (money == "M" ? 1000 : 1);

            var payload = new
            {
                Billete = denoms.Where(d => d.money == "B").Select(d => new
                {
                    id = d.id,
                    value = d.value,
                    label = FormatLabel(d.label, d.value),
                    bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                    family = NormFam(d.family),
                    isHigh = d.isHigh
                }),
                Moneda = denoms.Where(d => d.money == "M").Select(d => new
                {
                    id = d.id,
                    value = d.value,
                    label = FormatLabel(d.label, d.value),
                    bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                    family = NormFam(d.family),
                    isHigh = false
                }),
                Documento = denoms.Where(d => d.money == "D").Select(d => new
                {
                    id = d.id,
                    value = d.value,
                    label = FormatLabel(d.label, d.value),
                    bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                    family = "T"
                }),
                Cheque = denoms.Where(d => d.money == "C").Select(d => new
                {
                    id = d.id,
                    value = d.value,
                    label = FormatLabel(d.label, d.value),
                    bundleSize = d.bundleSize ?? DefaultBundle(d.money),
                    family = "T"
                })
            };

            return JsonSerializer.Serialize(payload);
        }

        public async Task<string> BuildQualitiesJsonAsync()
        {
            var q = await _db.Set<AdmQuality>()
                .Where(c => c.Status)
                .OrderBy(c => c.TypeOfMoney).ThenBy(c => c.QualityName)
                .Select(c => new { id = c.Id, name = c.QualityName, money = c.TypeOfMoney, family = c.DenominationFamily })
                .ToListAsync();

            var obj = new
            {
                B = q.Where(x => x.money == "B").ToList(),
                M = q.Where(x => x.money == "M").ToList()
            };
            return JsonSerializer.Serialize(obj);
        }

        public async Task<string> BuildBankEntitiesJsonAsync()
        {
            var data = await _db.AdmBankEntities.AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new { value = b.Id, text = b.Name })
                .ToListAsync();
            return JsonSerializer.Serialize(data);
        }

        public Task<List<SelectListItem>> GetCurrenciesForDropdownAsync()
        {
            return Task.FromResult(new List<SelectListItem>
            {
                new ("COP", "COP"),
                new ("USD", "USD"),
                new ("EUR", "EUR")
            });
        }
    }
}
