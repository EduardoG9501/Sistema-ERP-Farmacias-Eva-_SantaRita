using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaERP.Models.Entities
{
    public class CierreCaja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AperturaCajaId { get; set; }

        [ForeignKey("AperturaCajaId")]
        public AperturaCaja? Apertura { get; set; }

        [Required]
        public int UsuarioCierraId { get; set; }

        [ForeignKey("UsuarioCierraId")]
        public Usuario? UsuarioCierra { get; set; }

        [Required]
        public DateTime FechaCierre { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoFinal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Diferencia { get; set; }

        // JSON containing denominations breakdown (e.g., [{"DenominacionId":1,"Cantidad":5}])
        public string? DetalleDenominaciones { get; set; }

        // JSON containing payment methods breakdown (e.g., [{"Metodo":"Efectivo","Monto":100.00}])
        public string? MetodosPago { get; set; }

        // Compatibility properties for legacy code
        [NotMapped]
        public int Id_Usuario { get => UsuarioCierraId; set => UsuarioCierraId = value; }

        [NotMapped]
        public int UsuarioId { get => UsuarioCierraId; set => UsuarioCierraId = value; }
        
        [NotMapped]
        public DateTime Fecha_Cierre { get => FechaCierre; set => FechaCierre = value; }
        
        [NotMapped]
        public decimal Monto_Cierre { get => MontoFinal; set => MontoFinal = value; }
        
        // Alias for navigation property
        [NotMapped]
        public Usuario Usuario { get => UsuarioCierra; set => UsuarioCierra = value; }

    }
}
