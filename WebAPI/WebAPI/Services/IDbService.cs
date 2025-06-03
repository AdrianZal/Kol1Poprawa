using WebAPI.Models.DTOs;

namespace WebAPI.Services;

public interface IDbService
{
    Task<ClientDetailsDto> GetClientDetailsByIdAsync(int id);
    Task AddNewClientWithRentalAsync(CreateClientWithRentalRequestDto clientWithRentalRequest);
}