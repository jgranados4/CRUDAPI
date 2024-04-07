using System;
using System.Collections.Generic;

namespace CRUDAPI.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Nombre { get; set; }
    public string? Apellido { get; set; }

    public string? Estado { get; set; }
}
