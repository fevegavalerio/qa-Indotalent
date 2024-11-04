using AutoMapper;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.VendorContacts;
using Indotalent.Applications.Vendors;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.VendorContacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject2
{
    [TestFixture]
    public class VendorContactFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private VendorContactService _vendorContactService;
        private NumberSequenceService _numberSequenceService;
        private VendorService _vendorService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private VendorContactFormModel _vendorContactFormModel;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear y asignar la instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando _dbContext real y los mocks de otras dependencias
            _vendorContactService = new VendorContactService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _vendorService = new VendorService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _vendorContactFormModel = new VendorContactFormModel(_mapperMock.Object, _vendorContactService, _numberSequenceService, _vendorService);

            // Configurar TempData para evitar errores de referencia nula
            _vendorContactFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _vendorContactFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        public async Task OnPostAsync_AgregarNuevoVendorContact()
        {
            // Arrange
            var newVendorContactModel = new VendorContactFormModel.VendorContactModel
            {
                Name = "Prueba VendorContact",
                EmailAddress = "prueba@gmail.com",
                PhoneNumber = "84274819",
                JobTitle = "Gerente",
                VendorId = 1
            };

            _vendorContactFormModel.VendorContactForm = newVendorContactModel;
            _vendorContactFormModel.Action = "create";

            var mappedVendorContact = new VendorContact
            {
                RowGuid = Guid.NewGuid(),
                Name = newVendorContactModel.Name,
                EmailAddress = newVendorContactModel.EmailAddress,
                PhoneNumber = newVendorContactModel.PhoneNumber,
                JobTitle = newVendorContactModel.JobTitle,
                VendorId = newVendorContactModel.VendorId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newVendorContactModel;
            var expectedMappedResult = mappedVendorContact;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map
            _mapperMock
                .Setup(mapper => mapper.Map<VendorContact>(sourceModel))
                .Returns(expectedMappedResult);

            _vendorContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorContactFormModel.OnPostAsync(newVendorContactModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./VendorContactForm?rowGuid={mappedVendorContact.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _vendorContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }


        [Test]
        public async Task OnPostAsync_EditarVendorContact()
        {
            // Arrange
            var existingVendorContact = new VendorContact
            {
                RowGuid = Guid.NewGuid(),
                Name = "Contacto Existente",
                EmailAddress = "existente@gmail.com",
                PhoneNumber = "123456789",
                JobTitle = "Director",
                VendorId = 1,
                IsNotDeleted = true
            };

            await _dbContext.VendorContact.AddAsync(existingVendorContact);
            await _dbContext.SaveChangesAsync();

            var editedVendorContactModel = new VendorContactFormModel.VendorContactModel
            {
                RowGuid = existingVendorContact.RowGuid,
                Name = "Contacto Editado",
                EmailAddress = "editado@gmail.com",
                PhoneNumber = "987654321",
                JobTitle = "Gerente",
                VendorId = 1
            };

            _vendorContactFormModel.VendorContactForm = editedVendorContactModel;
            _vendorContactFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedVendorContactModel, existingVendorContact))
                .Callback((VendorContactFormModel.VendorContactModel source, VendorContact destination) =>
                {
                    destination.Name = source.Name;
                    destination.EmailAddress = source.EmailAddress;
                    destination.PhoneNumber = source.PhoneNumber;
                    destination.JobTitle = source.JobTitle;
                });

            _vendorContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorContactFormModel.OnPostAsync(editedVendorContactModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");
            string expectedUrl = $"./VendorContactForm?rowGuid={editedVendorContactModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _vendorContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarVendorContact()
        {
            // Arrange
            var existingVendorContact = new VendorContact
            {
                RowGuid = Guid.NewGuid(),
                Name = "Contacto a Eliminar",
                EmailAddress = "eliminar@gmail.com",
                PhoneNumber = "123456789",
                JobTitle = "Asistente",
                VendorId = 1,
                IsNotDeleted = true
            };

            await _dbContext.VendorContact.AddAsync(existingVendorContact);
            await _dbContext.SaveChangesAsync();

            _vendorContactFormModel.VendorContactForm = new VendorContactFormModel.VendorContactModel
            {
                RowGuid = existingVendorContact.RowGuid
            };

            _vendorContactFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _vendorContactFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorContactFormModel.OnPostAsync(_vendorContactFormModel.VendorContactForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            string expectedUrl = "./VendorContactList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _vendorContactFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedVendorContact = await _dbContext.VendorContact.SingleOrDefaultAsync(u => u.RowGuid == existingVendorContact.RowGuid);
            Assert.IsNotNull(deletedVendorContact, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedVendorContact.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }

    }
}
