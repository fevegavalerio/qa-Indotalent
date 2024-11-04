using AutoMapper;
using Indotalent.Applications.VendorGroups;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.VendorGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject1
{
    [TestFixture]
    public class VendorGroupFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private VendorGroupService _vendorGroupService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private VendorGroupFormModel _vendorGroupFormModel;
        private ApplicationDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            // Crear mocks de las dependencias
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _auditColumnTransformerMock = new Mock<IAuditColumnTransformer>(); // Crear el mock de IAuditColumnTransformer

            // Configurar DbContextOptions para ApplicationDbContext en modo de pruebas
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            // Crear instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando el dbContext real y los mocks de otras dependencias
            _vendorGroupService = new VendorGroupService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _vendorGroupFormModel = new VendorGroupFormModel(_mapperMock.Object, _vendorGroupService);

            // Configurar TempData para evitar errores de referencia nula
            _vendorGroupFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _vendorGroupFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoVendorGroup()
        {
            // Arrange
            var newVendorGroupModel = new VendorGroupFormModel.VendorGroupModel
            {
                Name = "Proveedor A",
                Description = "Grupo de proveedores de prueba"
            };

            _vendorGroupFormModel.VendorGroupForm = newVendorGroupModel;
            _vendorGroupFormModel.Action = "create";

            var mappedVendorGroup = new VendorGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = newVendorGroupModel.Name,
                Description = newVendorGroupModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newVendorGroupModel;
            var expectedMappedResult = mappedVendorGroup;

            // Configura el mock para devolver el objeto esperado cuando se llame al método Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<VendorGroup>(sourceModel))
                .Returns(expectedMappedResult);

            _vendorGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorGroupFormModel.OnPostAsync(newVendorGroupModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./VendorGroupForm?rowGuid={mappedVendorGroup.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert
            Assert.AreEqual("Success create new data.", _vendorGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarVendorGroup()
        {
            // Arrange
            var existingVendorGroup = new VendorGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = "Proveedor A",
                Description = "Grupo de proveedores",
                IsNotDeleted = true
            };

            await _dbContext.VendorGroup.AddAsync(existingVendorGroup);
            await _dbContext.SaveChangesAsync();

            var editedVendorGroupModel = new VendorGroupFormModel.VendorGroupModel
            {
                RowGuid = existingVendorGroup.RowGuid,
                Name = "Proveedor A Editado",
                Description = "Descripción actualizada"
            };

            _vendorGroupFormModel.VendorGroupForm = editedVendorGroupModel;
            _vendorGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedVendorGroupModel, existingVendorGroup))
                .Callback((VendorGroupFormModel.VendorGroupModel source, VendorGroup destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            _vendorGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorGroupFormModel.OnPostAsync(editedVendorGroupModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./VendorGroupForm?rowGuid={editedVendorGroupModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _vendorGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }


        [Test]
        public async Task OnPostAsync_EliminarVendorGroup()
        {
            // Arrange
            var existingVendorGroup = new VendorGroup
            {
                RowGuid = Guid.NewGuid(),
                Name = "Proveedor A",
                Description = "Grupo de proveedores",
                IsNotDeleted = true
            };

            await _dbContext.VendorGroup.AddAsync(existingVendorGroup);
            await _dbContext.SaveChangesAsync();

            _vendorGroupFormModel.VendorGroupForm = new VendorGroupFormModel.VendorGroupModel
            {
                RowGuid = existingVendorGroup.RowGuid
            };

            _vendorGroupFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _vendorGroupFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _vendorGroupFormModel.OnPostAsync(_vendorGroupFormModel.VendorGroupForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./VendorGroupList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _vendorGroupFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedVendorGroup = await _dbContext.VendorGroup.SingleOrDefaultAsync(u => u.RowGuid == existingVendorGroup.RowGuid);
            Assert.IsNotNull(deletedVendorGroup, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedVendorGroup.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }
    }
}

