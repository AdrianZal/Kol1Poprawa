using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public interface IDbService
{
    Task<BookingDetailsDto> GetBookingByIdAsync(int bookingId);
    Task AddNewBookingAsync(CreateBookingRequestDto bookingRequest);
}