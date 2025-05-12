using System.ComponentModel.DataAnnotations;

namespace Mock.Models.DTO;

public class AppointmentDTO
{
    public DateTime Date { get; set; }
    public PatientDTO Patient { get; set; }
    public DoctorDTO Doctor { get; set; }
    public List<ServiceDTO> Services { get; set; } = new List<ServiceDTO>();
}

public class PatientDTO
{
    [MaxLength(100)]
    public string FirstName { get; set; }
    [MaxLength(100)]
    public string LastName { get; set; }
    public DateTime dateOfBirth { get; set; }
}

public class DoctorDTO
{
    public int DoctorId { get; set; }
    [MaxLength(7)]
    public string PWZ { get; set; }
}

public class ServiceDTO
{
    [MaxLength(100)]
    public string Name { get; set; }
    public decimal ServiceFee { get; set; }
}