using RentivoMK.Models;

namespace RentivoMK.Interfaces;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Reservation>> GetAllWithDetailsAsync();
}