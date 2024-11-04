using AutoMapper;
using Indotalent.Applications.Warehouses;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.Warehouses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace TestProject1
{
    [TestFixture]
    public class WarehousesFormTests
    {
        private Mock<IMapper> _mapperMock;
        private WarehouseService _warehouseService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private WarehouseFormModel _warehouseFormModel;
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
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _warehouseFormModel = new WarehouseFormModel(_mapperMock.Object, _warehouseService);

            // Configurar TempData para evitar errores de referencia nula
            _warehouseFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _warehouseFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoWarehouse()
        {
            // Arrange
            var newWarehouseModel = new WarehouseFormModel.WarehouseModel
            {
                Name = "Test",
                Description = "Testing Functionality"
            };

            _warehouseFormModel.WarehouseForm = newWarehouseModel;
            _warehouseFormModel.Action = "create";

            var mappedWarehouse = new Warehouse
            {
                RowGuid = Guid.NewGuid(),
                Name = newWarehouseModel.Name,
                Description = newWarehouseModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newWarehouseModel;
            var expectedMappedResult = mappedWarehouse;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<Warehouse>(sourceModel))
                .Returns(expectedMappedResult);

            _warehouseFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _warehouseFormModel.OnPostAsync(newWarehouseModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./WarehouseForm?rowGuid={mappedWarehouse.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _warehouseFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }
        [Test]
        public async Task OnPostAsync_EditarWarehouse()
        {
            // Arrange
            var existingWarehouse = new Warehouse
            {
                RowGuid = Guid.NewGuid(),
                Name = "Initial Warehouse",
                Description = "Initial Description",
                IsNotDeleted = true
            };

            await _dbContext.Warehouse.AddAsync(existingWarehouse);
            await _dbContext.SaveChangesAsync();

            var editedWarehouseModel = new WarehouseFormModel.WarehouseModel
            {
                RowGuid = existingWarehouse.RowGuid,
                Name = "Updated Warehouse",
                Description = "Updated Description"
            };

            _warehouseFormModel.WarehouseForm = editedWarehouseModel;
            _warehouseFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedWarehouseModel, existingWarehouse))
                .Callback((WarehouseFormModel.WarehouseModel source, Warehouse destination) =>
                {
                    destination.Name = source.Name;
                    destination.Description = source.Description;
                });

            _warehouseFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _warehouseFormModel.OnPostAsync(editedWarehouseModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./WarehouseForm?rowGuid={editedWarehouseModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _warehouseFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual("Updated Warehouse", existingWarehouse.Name, "El campo Name debería haberse actualizado.");
            Assert.AreEqual("Updated Description", existingWarehouse.Description, "El campo Description debería haberse actualizado.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }
        [Test]
        public async Task OnPostAsync_EliminarWarehouse()
        {
            // Arrange
            var existingWarehouse = new Warehouse
            {
                RowGuid = Guid.NewGuid(),
                Name = "Warehouse to be deleted",
                Description = "Description",
                IsNotDeleted = true
            };

            await _dbContext.Warehouse.AddAsync(existingWarehouse);
            await _dbContext.SaveChangesAsync();

            _warehouseFormModel.WarehouseForm = new WarehouseFormModel.WarehouseModel
            {
                RowGuid = existingWarehouse.RowGuid
            };

            _warehouseFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _warehouseFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _warehouseFormModel.OnPostAsync(_warehouseFormModel.WarehouseForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./WarehouseList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _warehouseFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedWarehouse = await _dbContext.Warehouse.SingleOrDefaultAsync(u => u.RowGuid == existingWarehouse.RowGuid);
            Assert.IsNotNull(deletedWarehouse, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedWarehouse.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }
    }
}
