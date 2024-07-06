using System;
using System.Collections.Generic;

namespace CRUDAPI.Models;

public partial class UsuarioAU
{
    public int Id { get; set; }

    public string? Nombre { get; set; }
    public string? Constrasena { get; set; }
    public string? Email { get; set; }
    public string? Rol { get; set; } = "Cliente";

}
