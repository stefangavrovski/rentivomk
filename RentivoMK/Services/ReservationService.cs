using RentivoMK.DTOs;
using RentivoMK.Enums;
using RentivoMK.Interfaces;
using RentivoMK.Models;

namespace RentivoMK.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public ReservationService(IReservationRepository reservationRepository, IVehicleRepository vehicleRepository)
    {
        _reservationRepository = reservationRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<IEnumerable<ReservationDto>> GetAllReservationsAsync()
    {
        var reservations = await _reservationRepository.GetAllWithDetailsAsync();
        return reservations.Select(MapToDto);
    }

    public async Task<ReservationDto?> GetReservationByIdAsync(int id)
    {
        var reservations = await _reservationRepository.GetAllWithDetailsAsync();
        var reservation = reservations.FirstOrDefault(r => r.Id == id);
        return reservation is null ? null : MapToDto(reservation);
    }

    public async Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int customerId)
    {
        var reservations = await _reservationRepository.GetByCustomerIdAsync(customerId);
        return reservations.Select(MapToDto);
    }

    public async Task<ReservationDto> CreateReservationAsync(int customerId, CreateReservationDto dto)
    {
        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        if (dto.StartDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Start date cannot be in the past.");

        var vehicle = await _vehicleRepository.GetByIdAsync(dto.VehicleId)
            ?? throw new KeyNotFoundException($"Vehicle with ID {dto.VehicleId} not found.");

        if (vehicle.Status != VehicleStatus.Available)
            throw new InvalidOperationException("The selected vehicle is not available.");

        // Check for date conflicts with existing approved/pending reservations
        var allReservations = await _reservationRepository.GetAllWithDetailsAsync();
        var conflicting = allReservations.Any(r =>
            r.VehicleId == dto.VehicleId &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved) &&
            r.StartDate < dto.EndDate &&
            r.EndDate > dto.StartDate);

        if (conflicting)
            throw new InvalidOperationException("The vehicle is already reserved for the selected date range.");

        var days = (dto.EndDate - dto.StartDate).Days;
        var totalPrice = vehicle.PricePerDay * days;

        var reservation = new Reservation
        {
            CustomerId = customerId,
            VehicleId = dto.VehicleId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            TotalPrice = totalPrice,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _reservationRepository.AddAsync(reservation);

        // Reload with details for response
        var created = (await _reservationRepository.GetAllWithDetailsAsync())
            .First(r => r.Id == reservation.Id);

        return MapToDto(created);
    }

    public async Task ApproveReservationAsync(int id)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reservation with ID {id} not found.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be approved.");

        var vehicle = await _vehicleRepository.GetByIdAsync(reservation.VehicleId)
            ?? throw new KeyNotFoundException("Associated vehicle not found.");

        reservation.Status = ReservationStatus.Approved;
        reservation.UpdatedAt = DateTime.UtcNow;
        vehicle.Status = VehicleStatus.Rented;

        await _reservationRepository.UpdateAsync(reservation);
        await _vehicleRepository.UpdateAsync(vehicle);
    }

    public async Task RejectReservationAsync(int id)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reservation with ID {id} not found.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be rejected.");

        var vehicle = await _vehicleRepository.GetByIdAsync(reservation.VehicleId)
            ?? throw new KeyNotFoundException("Associated vehicle not found.");

        reservation.Status = ReservationStatus.Rejected;
        reservation.UpdatedAt = DateTime.UtcNow;
        vehicle.Status = VehicleStatus.Available;

        await _reservationRepository.UpdateAsync(reservation);
        await _vehicleRepository.UpdateAsync(vehicle);
    }

    public async Task CancelReservationAsync(int id, int requestingUserId, bool isAdmin)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reservation with ID {id} not found.");

        if (!isAdmin && reservation.CustomerId != requestingUserId)
            throw new UnauthorizedAccessException("You are not authorized to cancel this reservation.");

        if (reservation.Status == ReservationStatus.Completed ||
            reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("This reservation cannot be cancelled.");

        bool wasApproved = reservation.Status == ReservationStatus.Approved;

        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedAt = DateTime.UtcNow;

        await _reservationRepository.UpdateAsync(reservation);

        if (wasApproved)
        {
            var vehicle = await _vehicleRepository.GetByIdAsync(reservation.VehicleId)
                ?? throw new KeyNotFoundException("Associated vehicle not found.");

            vehicle.Status = VehicleStatus.Available;
            await _vehicleRepository.UpdateAsync(vehicle);
        }
    }

    public async Task CompleteReservationAsync(int id)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Reservation with ID {id} not found.");

        if (reservation.Status != ReservationStatus.Approved)
            throw new InvalidOperationException("Only approved reservations can be completed.");

        var vehicle = await _vehicleRepository.GetByIdAsync(reservation.VehicleId)
            ?? throw new KeyNotFoundException("Associated vehicle not found.");

        reservation.Status = ReservationStatus.Completed;
        reservation.UpdatedAt = DateTime.UtcNow;
        vehicle.Status = VehicleStatus.Available;

        await _reservationRepository.UpdateAsync(reservation);
        await _vehicleRepository.UpdateAsync(vehicle);
    }

    private static ReservationDto MapToDto(Reservation r) => new ReservationDto
    {
        Id = r.Id,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer is not null ? $"{r.Customer.FirstName} {r.Customer.LastName}" : string.Empty,
        VehicleId = r.VehicleId,
        VehicleName = r.Vehicle is not null ? $"{r.Vehicle.Make} {r.Vehicle.Model} ({r.Vehicle.Year})" : string.Empty,
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        TotalPrice = r.TotalPrice,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}