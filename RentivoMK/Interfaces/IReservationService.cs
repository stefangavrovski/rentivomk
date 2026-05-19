using RentivoMK.DTOs;

namespace RentivoMK.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<ReservationDto>> GetAllReservationsAsync();
    Task<ReservationDto?> GetReservationByIdAsync(int id);
    Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int customerId);
    Task<ReservationDto> CreateReservationAsync(int customerId, CreateReservationDto dto);
    Task ApproveReservationAsync(int id);
    Task RejectReservationAsync(int id);
    Task CancelReservationAsync(int id, int requestingUserId, bool isAdmin);
    Task CompleteReservationAsync(int id);
}