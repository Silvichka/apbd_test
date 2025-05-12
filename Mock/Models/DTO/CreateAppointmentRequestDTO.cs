using System.ComponentModel.DataAnnotations;

namespace Mock.Models.DTO;

public class CreateAppointmentRequestDTO
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    [MaxLength(7)]
    public string PWZ { get; set; }
    public List<ServiceDTO> Services { get; set; }
}