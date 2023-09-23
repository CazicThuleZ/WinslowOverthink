using System.ComponentModel.DataAnnotations.Schema;

namespace MediaService.Entities;

[Table("DiskVolumes")]
public class DiskVolume
{
    public Guid Id { get; set; } 
    public string Name { get; set; }
    public int AvailableSpace { get; set; }
}
