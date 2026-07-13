using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaERP.Models.Entities
{
    public class UsuarioWeb
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Login { get; set; }

        // Additional fields can be added as needed, e.g., Name, Email, etc.
        public string? Nombre { get; set; }
        public string? Email { get; set; }
    }
}
