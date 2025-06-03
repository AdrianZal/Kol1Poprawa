namespace WebAPI.Models.DTOs;

public class BookingDetailsDto
{
    public DateTime BookingDate { get; set; }
    public Guest Guest { get; set; }
    public Employee Employee { get; set; }
    public List<Attraction> Attractions { get; set; }
}

public class Guest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
}

public class Employee
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmployeeNumber { get; set; }
}

public class Attraction
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Amount { get; set; }
}