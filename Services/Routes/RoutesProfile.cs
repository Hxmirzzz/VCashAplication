using AutoMapper;
using VCashApp.Models.DTOs;
using VCashApp.Models.Entities;

namespace VCashApp.Services.Routes
{
    public sealed class RoutesProfile : Profile
    {
        public RoutesProfile()
        {
            CreateMap<RouteUpsertDto, AdmRoute>()
                .ForMember(d => d.BranchRouteCode, m => m.MapFrom(s => s.BranchRouteCode))
                .ForMember(d => d.RouteCode, m => m.MapFrom(s => s.RouteCode))
                .ForMember(d => d.RouteName, m => m.MapFrom(s => s.RouteName))
                .ForMember(d => d.BranchId, m => m.MapFrom(s => s.BranchId))
                .ForMember(d => d.RouteType, m => m.MapFrom(s => s.RouteType))
                .ForMember(d => d.ServiceType, m => m.MapFrom(s => s.ServiceType))
                .ForMember(d => d.VehicleType, m => m.MapFrom(s => s.VehicleType))
                .ForMember(d => d.Amount, m => m.MapFrom(s => s.Amount))
                .ForMember(d => d.Status, m => m.MapFrom(s => s.IsActive))
                .ForMember(d => d.Monday, m => m.MapFrom(s => s.Monday))
                .ForMember(d => d.MondayStartTime, m => m.MapFrom(s => s.MondayStartTime))
                .ForMember(d => d.MondayEndTime, m => m.MapFrom(s => s.MondayEndTime))
                .ForMember(d => d.Tuesday, m => m.MapFrom(s => s.Tuesday))
                .ForMember(d => d.TuesdayStartTime, m => m.MapFrom(s => s.TuesdayStartTime))
                .ForMember(d => d.TuesdayEndTime, m => m.MapFrom(s => s.TuesdayEndTime))
                .ForMember(d => d.Wednesday, m => m.MapFrom(s => s.Wednesday))
                .ForMember(d => d.WednesdayStartTime, m => m.MapFrom(s => s.WednesdayStartTime))
                .ForMember(d => d.WednesdayEndTime, m => m.MapFrom(s => s.WednesdayEndTime))
                .ForMember(d => d.Thursday, m => m.MapFrom(s => s.Thursday))
                .ForMember(d => d.ThursdayStartTime, m => m.MapFrom(s => s.ThursdayStartTime))
                .ForMember(d => d.ThursdayEndTime, m => m.MapFrom(s => s.ThursdayEndTime))
                .ForMember(d => d.Friday, m => m.MapFrom(s => s.Friday))
                .ForMember(d => d.FridayStartTime, m => m.MapFrom(s => s.FridayStartTime))
                .ForMember(d => d.FridayEndTime, m => m.MapFrom(s => s.FridayEndTime))
                .ForMember(d => d.Saturday, m => m.MapFrom(s => s.Saturday))
                .ForMember(d => d.SaturdayStartTime, m => m.MapFrom(s => s.SaturdayStartTime))
                .ForMember(d => d.SaturdayEndTime, m => m.MapFrom(s => s.SaturdayEndTime))
                .ForMember(d => d.Sunday, m => m.MapFrom(s => s.Sunday))
                .ForMember(d => d.SundayStartTime, m => m.MapFrom(s => s.SundayStartTime))
                .ForMember(d => d.SundayEndTime, m => m.MapFrom(s => s.SundayEndTime))
                .ForMember(d => d.Holiday, m => m.MapFrom(s => s.Holiday))
                .ForMember(d => d.HolidayStartTime, m => m.MapFrom(s => s.HolidayStartTime))
                .ForMember(d => d.HolidayEndTime, m => m.MapFrom(s => s.HolidayEndTime));

            CreateMap<AdmRoute, RouteListDto>()
                .ConstructUsing(s => new RouteListDto(
                    s.BranchRouteCode,
                    s.RouteCode,
                    s.RouteName,
                    s.BranchId,
                    s.Branch != null ? s.Branch.NombreSucursal : null,
                    s.RouteType,
                    s.ServiceType,
                    s.VehicleType,
                    s.Amount,
                    s.Status
                ));
        }
    }
}