using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentManagement.Infrastructure.Data.Entities;

[Table("Locations")]
public class Location : AuditableEntity
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(50)]
    public string Code { get; set; } = "";
}
