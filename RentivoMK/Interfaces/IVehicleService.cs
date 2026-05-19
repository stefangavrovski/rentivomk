using RentivoMK.DTOs;

namespace RentivoMK.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync();
    Task<IEnumerable<VehicleDto>> GetAvailableVehiclesAsync();
    Task<VehicleDto?> GetVehicleByIdAsync(int id);
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto);
    Task UpdateVehicleAsync(int id, UpdateVehicleDto dto);
    Task DeleteVehicleAsync(int id);
}