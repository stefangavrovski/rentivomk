using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReservationRepository _reservationRepository;

    public VehicleService(IVehicleRepository vehicleRepository, IReservationRepository reservationRepository)
    {
        _vehicleRepository = vehicleRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync()
    {
        var vehicles = await _vehicleRepository.GetAllAsync();
        return vehicles.Select(MapToDto);
    }

    public async Task<IEnumerable<VehicleDto>> GetAvailableVehiclesAsync()
    {
        var vehicles = await _vehicleRepository.GetAvailableVehiclesAsync();
        return vehicles.Select(MapToDto);
    }

    public async Task<VehicleDto?> GetVehicleByIdAsync(int id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        return vehicle is null ? null : MapToDto(vehicle);
    }

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        var vehicle = new Vehicle
        {
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            PricePerDay = dto.PricePerDay,
            Category = dto.Category,
            Description = dto.Description,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _vehicleRepository.AddAsync(vehicle);
        return MapToDto(vehicle);
    }

    public async Task UpdateVehicleAsync(int id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

        vehicle.Make = dto.Make;
        vehicle.Model = dto.Model;
        vehicle.Year = dto.Year;
        vehicle.PricePerDay = dto.PricePerDay;
        vehicle.Category = dto.Category;
        vehicle.Description = dto.Description;

        if (dto.Status.HasValue)
            vehicle.Status = dto.Status.Value;

        await _vehicleRepository.UpdateAsync(vehicle);
    }

    public async Task DeleteVehicleAsync(int id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle with ID {id} not found.");

        var reservations = await _reservationRepository.GetAllWithDetailsAsync();
        var hasActive = reservations.Any(r =>
            r.VehicleId == id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved));

        if (hasActive)
            throw new InvalidOperationException("Cannot delete a vehicle with active (Pending or Approved) reservations.");

        await _vehicleRepository.DeleteAsync(vehicle);
    }

    private static VehicleDto MapToDto(Vehicle vehicle) => new VehicleDto
    {
        Id = vehicle.Id,
        Make = vehicle.Make,
        Model = vehicle.Model,
        Year = vehicle.Year,
        PricePerDay = vehicle.PricePerDay,
        Category = vehicle.Category,
        Status = vehicle.Status,
        Description = vehicle.Description,
        CreatedAt = vehicle.CreatedAt
    };
}