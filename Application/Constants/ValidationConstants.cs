namespace CRUDAPI.Application.Constants
{
    public static class ValidationConstants
    {
        #region Password Validation
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;
        public const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,100}$";
        public const string PasswordErrorMessage = "La contraseña debe contener al menos: 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial, y tener entre {0} y {1} caracteres";
        public const string PasswordLengthMessage = "La contraseña debe tener entre {2} y {1} caracteres";
        public const string PasswordRequiredMessage = "La contraseña es requerida";
        #endregion
        #region Email Validation
        public const string EmailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const int EmailMaxLength = 300;
        // Mensajes de error
        public const string EmailRequiredMessage = "El email es requerido";
        public const string EmailFormatMessage = "El formato del email no es válido";
        public const string EmailMaxLengthMessage = "El email no puede exceder {1} caracteres";
        #endregion
        #region Name Validation
        public const int NombreMinLength = 2;
        public const int NombreMaxLength = 255;
        // Mensajes de error
        public const string NombreRequiredMessage = "El nombre es requerido";
        public const string NombreLengthMessage = "El nombre debe tener entre {2} y {1} caracteres";

        #endregion
    }
}
