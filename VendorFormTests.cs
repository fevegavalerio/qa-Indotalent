using AutoMapper;
using Indotalent.Applications.Vendors;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.VendorGroups;
using Indotalent.Applications.VendorCategories;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.Vendors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Indotalent.Infrastructures.Countries;

namespace TestProject2
{
    [TestFixture]
    public class VendorFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private VendorService _vendorService;
        private NumberSequenceService _numberSequenceService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private VendorFormModel _vendorFormModel;
        private ApplicationDbContext _dbContext;
        private VendorGroupService _vendorGroupService;
        private VendorCategoryService _vendorCategoryService;
        private Mock<ICountryService> _countryServiceMock;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>();
            _countryServiceMock = new Mock<ICountryService>();

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando el dbContext real y los mocks de otras dependencias
            _vendorService = new VendorService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _vendorGroupService = new VendorGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _vendorCategoryService = new VendorCategoryService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _vendorFormModel = new VendorFormModel(
                _mapperMock.Object,
                _vendorService,
                _numberSequenceService,
                _vendorGroupService,
                _vendorCategoryService,
                _countryServiceMock.Object
            );

            // Configurar TempData para evitar errores de referencia nula
            _vendorFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _vendorFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoVendor()
        {
            // Arrange
            var newVendorModel = new VendorFormModel.VendorModel
            {
                Name = "Test Vendor",
                VendorGroupId = 1,
                VendorCategoryId = 1,
                Street = "123 Main St",
                PhoneNumber = "123456789",
                EmailAddress = "vendor@prueba.com"
            };

            _vendorFormModel.VendorForm = newVendorModel;
            _vendorFormModel.Action = "create";

            var mappedVendor = new Vendor
            {
                RowGuid = Guid.NewGuid(),
                Name = newVendorModel.Name,
                VendorGroupId = newVendorModel.VendorGroupId,
                VendorCategoryId = newVendorModel.VendorCategoryId,
                Street = newVendorModel.Street,
                PhoneNumber = newVendorModel.PhoneNumber,
                EmailAddress = newVendorModel.EmailAddress
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newVendorModel;
            var expectedMappedResult = mappedVendor;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map
            _mapperMock
                .Setup(mapper => mapper.Map<Vendor>(sourceModel))
                .Returns(expectedMappedResult);

            _vendorFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorFormModel.OnPostAsync(newVendorModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./VendorForm?rowGuid={mappedVendor.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _vendorFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarVendor()
        {
            // Arrange
            var existingVendor = new Vendor
            {
                RowGuid = Guid.NewGuid(),
                Name = "Vendor A",
                VendorGroupId = 1,
                VendorCategoryId = 1,
                Street = "123 Main St",
                PhoneNumber = "123456789",
                EmailAddress = "vendor@example.com",
                IsNotDeleted = true
            };

            await _dbContext.Vendor.AddAsync(existingVendor);
            await _dbContext.SaveChangesAsync();

            var editedVendorModel = new VendorFormModel.VendorModel
            {
                RowGuid = existingVendor.RowGuid,
                Name = "Vendor A Edited",
                VendorGroupId = existingVendor.VendorGroupId,
                VendorCategoryId = existingVendor.VendorCategoryId,
                Street = "456 Main St",
                PhoneNumber = "987654321",
                EmailAddress = "edited_vendor@example.com"
            };

            _vendorFormModel.VendorForm = editedVendorModel;
            _vendorFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedVendorModel, existingVendor))
                .Callback((VendorFormModel.VendorModel source, Vendor destination) =>
                {
                    destination.Name = source.Name;
                    destination.Street = source.Street;
                    destination.PhoneNumber = source.PhoneNumber;
                    destination.EmailAddress = source.EmailAddress;
                });

            _vendorFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorFormModel.OnPostAsync(editedVendorModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./VendorForm?rowGuid={editedVendorModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _vendorFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarVendor()
        {
            // Arrange
            var existingVendor = new Vendor
            {
                RowGuid = Guid.NewGuid(),
                Name = "Vendor A",
                VendorGroupId = 1,
                VendorCategoryId = 1,
                Street = "123 Main St",
                PhoneNumber = "123456789",
                EmailAddress = "vendor@example.com",
                IsNotDeleted = true
            };

            await _dbContext.Vendor.AddAsync(existingVendor);
            await _dbContext.SaveChangesAsync();

            _vendorFormModel.VendorForm = new VendorFormModel.VendorModel
            {
                RowGuid = existingVendor.RowGuid
            };

            _vendorFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _vendorFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorFormModel.OnPostAsync(_vendorFormModel.VendorForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./VendorList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _vendorFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedVendor = await _dbContext.Vendor.SingleOrDefaultAsync(u => u.RowGuid == existingVendor.RowGuid);
            Assert.IsNotNull(deletedVendor, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedVendor.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }
    }
}