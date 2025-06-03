namespace WebAPI.Models.DTOs;

public class CreateBookingRequestDto
{
    public int BookingId { get; set; }
    public int GuestId { get; set; }
    public string EmployeeNumber { get; set; }
    public List<AddedAttraction> Attractions { get; set; }
}

public class AddedAttraction
{
    public string Name { get; set; }
    public int Amount { get; set; }
}