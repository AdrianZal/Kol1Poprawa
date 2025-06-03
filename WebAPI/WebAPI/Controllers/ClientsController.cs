using WebAPI.Exceptions;
using WebAPI.Models.DTOs;
using WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IDbService _dbService;
        public ClientsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientDetailsById(int id)
        {
            try
            {
                var res = await _dbService.GetClientDetailsByIdAsync(id);
                return Ok(res);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddNewClientWithRental(CreateClientWithRentalRequestDto createClientWithRentalRequest)
        {
            try
            {
                await _dbService.AddNewClientWithRentalAsync(createClientWithRentalRequest);
            }
            catch (ConflictException e)
            {
                return Conflict(e.Message);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            
            return Ok();
        }    
    }
}
