using VCashApp.Enums;
using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Models.DTOs;
using AutoMapper;
using VCashApp.Services.Routes.Application;
using VCashApp.Infrastructure.Branches;

namespace VCashApp.Services.Routes.Infrastucture
{
    public sealed class RouteService : IRouteService
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IBranchContext _branchCtx;

        public RouteService(AppDbContext db, IMapper mapper, IBranchContext branchCtx)
        { 
            _db = db;
            _mapper = mapper;
            _branchCtx = branchCtx;
        }

        public async Task<(bool ok, string message)> CreateAsync(RouteUpsertDto dto, string userId)
        {
            if (string.IsNullOrWhiteSpace(dto.BranchRouteCode))
                return (false, "El código de ruta por sucursal (CodRutaSuc) es obligatorio.");

            if (!await CanActOnBranchAsync(dto.BranchId))
                return (false, "No tiene permisos para crear rutas en la sucursal seleccionada.");

            var exists = await _db.Set<AdmRoute>()
                .AnyAsync(r => r.BranchRouteCode == dto.BranchRouteCode);
            if (exists) return (false, "Ya existe una ruta con ese codigo.");

            if (dto.RouteType is not null && !RouteCatalogs.RouteTypeCode.IsValid(dto.RouteType))
                return (false, "Tipo de ruta inválido.");

            if (dto.ServiceType is not null && !RouteCatalogs.ServiceTypeCode.IsValid(dto.ServiceType))
                return (false, "Tipo de atención inválido.");

            if (dto.VehicleType is not null && !RouteCatalogs.VehicleTypeCode.IsValid(dto.VehicleType))
                return (false, "Tipo de vehículo inválido.");

            var entity = _mapper.Map<AdmRoute>(dto);

            _db.Add(entity);
            await _db.SaveChangesAsync();

            return (true, "Ruta registrada correctamente.");
        }

        public async Task<(bool ok, string message)> UpdateAsync(RouteUpsertDto dto, string userId)
        {
            if (string.IsNullOrWhiteSpace(dto.BranchRouteCode))
                return (false, "El código de ruta por sucursal (CodRutaSuc) es obligatorio.");

            var entity = await _db.Set<AdmRoute>()
                .FirstOrDefaultAsync(r => r.BranchRouteCode == dto.BranchRouteCode);

            if (entity is null)
                return (false, "Ruta no encontrada.");

            if (!await CanActOnBranchAsync(entity.BranchId))
                return (false, "No tiene permisos para editar rutas en esta sucursal.");

            if (dto.RouteType is not null && !RouteCatalogs.RouteTypeCode.IsValid(dto.RouteType))
                return (false, "Tipo de ruta inválido.");

            if (dto.ServiceType is not null && !RouteCatalogs.ServiceTypeCode.IsValid(dto.ServiceType))
                return (false, "Tipo de atención inválido.");

            if (dto.VehicleType is not null && !RouteCatalogs.VehicleTypeCode.IsValid(dto.VehicleType))
                return (false, "Tipo de vehículo inválido.");

            _mapper.Map(dto, entity);
            await _db.SaveChangesAsync();

            return (true, "Ruta actualizada correctamente.");
        }

        public async Task<(bool ok, string message)> SetStatusAsync(string branchRouteCode, bool newStatus, string userId)
        {
            var entity = await _db.Set<AdmRoute>()
                .FirstOrDefaultAsync(r => r.BranchRouteCode == branchRouteCode);

            if (entity is null)
                return (false, "Ruta no encontrada.");

            if (!await CanActOnBranchAsync(entity.BranchId))
                return (false, "No tiene permisos para actualizar el estado de esta ruta.");

            entity.Status = newStatus;
            await _db.SaveChangesAsync();

            return (true, "Estado actualizado.");
        }

        private Task<bool> CanActOnBranchAsync(int? branchId)
        {
            if (branchId is null) return Task.FromResult(false);

            if (_branchCtx.AllBranches)
            {
                // Si hay lista de permitidas, respétala
                if (_branchCtx.PermittedBranchIds?.Any() == true)
                    return Task.FromResult(_branchCtx.PermittedBranchIds.Contains(branchId.Value));
                return Task.FromResult(true);
            }

            if (_branchCtx.CurrentBranchId.HasValue)
                return Task.FromResult(_branchCtx.CurrentBranchId.Value == branchId.Value);

            if (_branchCtx.PermittedBranchIds?.Any() == true)
                return Task.FromResult(_branchCtx.PermittedBranchIds.Contains(branchId.Value));

            return Task.FromResult(false);
        }
    }
}
