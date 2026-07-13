using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaERP.Models.Entities
{
    public class AperturaCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        // Compatibility property for legacy code
        [NotMapped]
        public int UsuarioId_Legacy { get => UsuarioId; set => UsuarioId = value; }

        [Required]
        public DateTime FechaApertura { get; set; } = DateTime.Now;

        // Compatibility property for legacy code
        [NotMapped]
        public DateTime FechaHora_Registro { get => FechaApertura; set => FechaApertura = value; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        // Compatibility properties for legacy code
        [NotMapped]
        public int Id_Usuario { get => UsuarioId; set => UsuarioId = value; }
        [NotMapped]
        public DateTime Fecha_Apertura { get => FechaApertura; set => FechaApertura = value; }
        [NotMapped]
        public decimal Monto_Apertura { get => MontoInicial; set => MontoInicial = value; }


        public string? Observaciones { get; set; }
    }
}
