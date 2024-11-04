using AutoMapper;
using Indotalent.Applications.AdjustmentPluss;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.PositiveAdjustments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;


namespace TestProject1
{
    [TestFixture]
    public class PositiveAdjustmentFormTests
    {
        private Mock<IMapper> _mapperMock;
        private AdjustmentPlusService _adjustmentPlusService;
        private NumberSequenceService _numberSequenceService;
        private WarehouseService _warehouseService;
        private ProductService _productService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private PositiveAdjustmentFormModel _positiveAdjustmentFormModel;
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
            _adjustmentPlusService = new AdjustmentPlusService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _positiveAdjustmentFormModel = new PositiveAdjustmentFormModel(_mapperMock.Object, _adjustmentPlusService, _numberSequenceService, _productService, _warehouseService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _positiveAdjustmentFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _positiveAdjustmentFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoPositiveAdjustment()
        {
            // Arrange
            var newPositiveAdjustmentModel = new PositiveAdjustmentFormModel.AdjustmentPlusModel
            {
                AdjustmentDate = DateTime.Now,
                Description = "Testing Functionality"
            };

            _positiveAdjustmentFormModel.AdjustmentPlusForm = newPositiveAdjustmentModel;
            _positiveAdjustmentFormModel.Action = "create";

            var mappedPositiveAdjustment = new AdjustmentPlus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = newPositiveAdjustmentModel.AdjustmentDate,
                Description = newPositiveAdjustmentModel.Description
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newPositiveAdjustmentModel;
            var expectedMappedResult = mappedPositiveAdjustment;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<AdjustmentPlus>(sourceModel))
                .Returns(expectedMappedResult);

            _positiveAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _positiveAdjustmentFormModel.OnPostAsync(newPositiveAdjustmentModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./PositiveAdjustmentForm?rowGuid={mappedPositiveAdjustment.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _positiveAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }
        [Test]
        public async Task OnPostAsync_EditarPositiveAdjustment()
        {
            // Arrange
            var existingPositiveAdjustment = new AdjustmentPlus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = DateTime.Now.AddDays(-1),
                Description = "Initial Description",
                IsNotDeleted = true
            };

            await _dbContext.AdjustmentPlus.AddAsync(existingPositiveAdjustment);
            await _dbContext.SaveChangesAsync();

            var editedPositiveAdjustmentModel = new PositiveAdjustmentFormModel.AdjustmentPlusModel
            {
                RowGuid = existingPositiveAdjustment.RowGuid,
                AdjustmentDate = DateTime.Now,
                Description = "Updated Description"
            };

            _positiveAdjustmentFormModel.AdjustmentPlusForm = editedPositiveAdjustmentModel;
            _positiveAdjustmentFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedPositiveAdjustmentModel, existingPositiveAdjustment))
                .Callback((PositiveAdjustmentFormModel.AdjustmentPlusModel source, AdjustmentPlus destination) =>
                {
                    destination.AdjustmentDate = source.AdjustmentDate;
                    destination.Description = source.Description;
                });

            _positiveAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _positiveAdjustmentFormModel.OnPostAsync(editedPositiveAdjustmentModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./PositiveAdjustmentForm?rowGuid={editedPositiveAdjustmentModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _positiveAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual("Updated Description", existingPositiveAdjustment.Description, "El campo Description debería haberse actualizado.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarPositiveAdjustment()
        {
            // Arrange
            var existingPositiveAdjustment = new AdjustmentPlus
            {
                RowGuid = Guid.NewGuid(),
                AdjustmentDate = DateTime.Now,
                Description = "To be deleted",
                IsNotDeleted = true
            };

            await _dbContext.AdjustmentPlus.AddAsync(existingPositiveAdjustment);
            await _dbContext.SaveChangesAsync();

            _positiveAdjustmentFormModel.AdjustmentPlusForm = new PositiveAdjustmentFormModel.AdjustmentPlusModel
            {
                RowGuid = existingPositiveAdjustment.RowGuid
            };

            _positiveAdjustmentFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _positiveAdjustmentFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _positiveAdjustmentFormModel.OnPostAsync(_positiveAdjustmentFormModel.AdjustmentPlusForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./PositiveAdjustmentList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _positiveAdjustmentFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedPositiveAdjustment = await _dbContext.AdjustmentPlus.SingleOrDefaultAsync(u => u.RowGuid == existingPositiveAdjustment.RowGuid);
            Assert.IsNotNull(deletedPositiveAdjustment, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedPositiveAdjustment.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}