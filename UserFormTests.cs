using AutoMapper;
using Indotalent.Applications.ApplicationUsers;
using Indotalent.Applications.Companies;
using Indotalent.AppSettings;
using Indotalent.Infrastructures.Countries;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class UserFormTest
    {
        private UserFormModel _userFormModel;
        private Mock<IMapper> _mapperMock;
        private Mock<ApplicationUserService> _applicationUserServiceMock;
        private Mock<CompanyService> _companyServiceMock;
        private Mock<ICountryService> _countryServiceMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<IOptions<ApplicationConfiguration>> _appConfigMock;

        [SetUp]
        public void Setup()
        {
            _mapperMock = new Mock<IMapper>();
            _applicationUserServiceMock = new Mock<ApplicationUserService>();
            _companyServiceMock = new Mock<CompanyService>();
            _countryServiceMock = new Mock<ICountryService>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>();
            _appConfigMock = new Mock<IOptions<ApplicationConfiguration>>();

            _userFormModel = new UserFormModel(
                _mapperMock.Object,
                _applicationUserServiceMock.Object,
                _companyServiceMock.Object,
                _countryServiceMock.Object,
                _userManagerMock.Object,
                _appConfigMock.Object
            );
        }

        // 1. Test: Carga de modelo nuevo: Verifica que OnGetAsync cree un nuevo modelo de usuario si id es nulo.
        [Test]
        public async Task OnGetAsync_CreatesNewUserModel_WhenIdIsNull()
        {
            // Act
            await _userFormModel.OnGetAsync(null);

            // Assert
            Assert.NotNull(_userFormModel.UserForm);
            Assert.AreEqual(Guid.NewGuid().ToString(), _userFormModel.UserForm.Id);
        }

        // 2. Test: Carga de datos existentes: Verifica que OnGetAsync cargue correctamente un usuario existente.
        [Test]
        public async Task OnGetAsync_LoadsExistingUser_WhenIdIsProvided()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", FullName = "Test User" };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);
            _mapperMock.Setup(m => m.Map<UserFormModel.UserModel>(existingUser)).Returns(new UserFormModel.UserModel { Id = "1", FullName = "Test User" });

            // Act
            await _userFormModel.OnGetAsync("1");

            // Assert
            Assert.AreEqual("Test User", _userFormModel.UserForm.FullName);
        }

        // 3. Test: Crear nuevo usuario: Verifica que OnPostAsync cree un nuevo usuario.
        [Test]
        public async Task OnPostAsync_CreatesNewUser_WhenActionIsCreate()
        {
            // Arrange
            var input = new UserFormModel.UserModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                UserType = UserType.Internal
            };
            _applicationUserServiceMock.Setup(s => s.IsEmailAlreadyExist(input.Email)).Returns(false);
            _mapperMock.Setup(m => m.Map<ApplicationUser>(input)).Returns(new ApplicationUser());

            // Act
            var result = await _userFormModel.OnPostAsync(input);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
        }

        // 4. Test: Excepción por correo existente: Verifica que OnPostAsync lance una excepción si el correo ya existe.
        [Test]
        public async Task OnPostAsync_ThrowsException_WhenEmailAlreadyExists()
        {
            // Arrange
            var input = new UserFormModel.UserModel
            {
                Email = "existing@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };
            _applicationUserServiceMock.Setup(s => s.IsEmailAlreadyExist(input.Email)).Returns(true);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _userFormModel.OnPostAsync(input));
        }

        // 5. Test: Actualiza usuario existente: Verifica que OnPostAsync actualice un usuario existente.
        [Test]
        public async Task OnPostAsync_UpdatesExistingUser_WhenActionIsEdit()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", Email = "old@example.com" };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);
            var input = new UserFormModel.UserModel
            {
                Id = "1",
                Email = "new@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var result = await _userFormModel.OnPostAsync(input);

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
        }

        // 6. Test: Excepción por contraseñas no coincidentes: Verifica que OnPostAsync lance una excepción si las contraseñas no coinciden.
        [Test]
        public async Task OnPostAsync_ThrowsException_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var input = new UserFormModel.UserModel
            {
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword123!"
            };

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _userFormModel.OnPostAsync(input));
        }

        // 7. Test: Elimina usuario: Verifica que OnPostAsync elimine un usuario.
        [Test]
        public async Task OnPostAsync_DeletesUser_WhenActionIsDelete()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", Email = "test@example.com" };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);

            // Act
            var result = await _userFormModel.OnPostAsync(new UserFormModel.UserModel { Id = "1" });

            // Assert
            Assert.IsInstanceOf<RedirectResult>(result);
        }

        // 8. Test: Excepción si el usuario no existe: Verifica que OnPostAsync lance una excepción si el usuario no existe al intentar eliminar.
        [Test]
        public async Task OnPostAsync_ThrowsException_WhenUserDoesNotExistForDelete()
        {
            // Arrange
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _userFormModel.OnPostAsync(new UserFormModel.UserModel { Id = "1" }));
        }

        // 9. Test: Actualiza correo electrónico: Verifica que OnPostAsync actualice el correo electrónico si cambia.
        [Test]
        public async Task OnPostAsync_UpdatesEmail_WhenEmailChanges()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", Email = "old@example.com" };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);
            var input = new UserFormModel.UserModel
            {
                Id = "1",
                Email = "new@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            await _userFormModel.OnPostAsync(input);

            // Assert
            _userManagerMock.Verify(um => um.ChangeEmailAsync(existingUser, input.Email, It.IsAny<string>()), Times.Once);
        }

        // 10. Test: Cambia el nombre de usuario: Verifica que OnPostAsync cambie el nombre de usuario si el correo cambia.
        [Test]
        public async Task OnPostAsync_ChangesUsername_WhenEmailChanges()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", Email = "old@example.com" };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);
            var input = new UserFormModel.UserModel
            {
                Id = "1",
                Email = "new@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            await _userFormModel.OnPostAsync(input);

            // Assert
            _userManagerMock.Verify(um => um.SetUserNameAsync(existingUser, input.Email), Times.Once);
        }

        // 11. Test: Actualiza roles: Verifica que OnPostAsync actualice los roles si cambia el tipo de usuario.
        [Test]
        public async Task OnPostAsync_UpdatesRoles_WhenUserTypeChanges()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "1", UserType = UserType.Internal };
            _applicationUserServiceMock.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(existingUser);
            var input = new UserFormModel.UserModel
            {
                Id = "1",
                UserType = UserType.Customer
            };

            // Act
            await _userFormModel.OnPostAsync(input);

            // Assert
            _applicationUserServiceMock.Verify(um => um.UpdateUserRoles(existingUser, input.UserType), Times.Once);
        }
        // 12. Test: Contraseña no válida: Verifica que OnPostAsync no permita la creación de un usuario si las contraseñas no coinciden.
        [Test]
        public async Task OnPostAsync_DoesNotCreateUser_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var input = new UserFormModel.UserModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword123!",
                UserType = UserType.Internal
            };
            
            _applicationUserServiceMock.Setup(s => s.IsEmailAlreadyExist(input.Email)).Returns(false);
            
            // Act
            var result = await _userFormModel.OnPostAsync(input);

            // Assert
            // Verifica que no se haya creado el usuario
            _applicationUserServiceMock.Verify(s => s.CreateUserAsync(It.IsAny<ApplicationUser>()), Times.Never);
            
            // Verifica que el resultado no sea un RedirectResult
            Assert.IsNotInstanceOf<RedirectResult>(result);
            
            // También puedes comprobar que se estableció un error de modelo
            Assert.IsFalse(_userFormModel.ModelState.IsValid);
            Assert.IsTrue(_userFormModel.ModelState.ContainsKey("ConfirmPassword"));
        }

    }
}