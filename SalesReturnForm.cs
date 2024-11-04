using AutoMapper;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Products;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Indotalent.Applications.DeliveryOrders;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Applications.SalesReturns;
using Indotalent.Pages.SalesReturns;

namespace TestProject1
{
    [TestFixture]
    public class SalesReturnFormTests
    {
        private Mock<IMapper> _mapperMock;
        private SalesReturnService _salesReturnService;
        private DeliveryOrderService _deliveryOrderService;

        private NumberSequenceService _numberSequenceService;
        private ProductService _productService;
        private WarehouseService _warehouseService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private SalesReturnFormModel _salesReturnFormModel;
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
            _salesReturnService = new SalesReturnService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _deliveryOrderService = new DeliveryOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _salesReturnFormModel = new SalesReturnFormModel(_mapperMock.Object, _salesReturnService, _numberSequenceService, _deliveryOrderService, _productService, _warehouseService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _salesReturnFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _salesReturnFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoSalesReturn()
        {
            // Arrange
            var newSalesReturnModel = new SalesReturnFormModel.SalesReturnModel
            {
                DeliveryOrderId = 1,
                ReturnDate = DateTime.Now,
                Status = SalesReturnStatus.Draft
            };

            _salesReturnFormModel.SalesReturnForm = newSalesReturnModel;
            _salesReturnFormModel.Action = "create";

            var mappedSalesReturn = new SalesReturn
            {
                RowGuid = Guid.NewGuid(),
                DeliveryOrderId = newSalesReturnModel.DeliveryOrderId,
                ReturnDate = newSalesReturnModel.ReturnDate,
                Status = newSalesReturnModel.Status
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newSalesReturnModel;
            var expectedMappedResult = mappedSalesReturn;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<SalesReturn>(sourceModel))
                .Returns(expectedMappedResult);

            _salesReturnFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _salesReturnFormModel.OnPostAsync(newSalesReturnModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./SalesReturnForm?rowGuid={mappedSalesReturn.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _salesReturnFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarSalesReturn()
        {
            // Arrange
            var existingSalesReturn = new SalesReturn
            {
                RowGuid = Guid.NewGuid(),
                DeliveryOrderId = 1,
                ReturnDate = DateTime.Now.AddDays(-2),
                Status = SalesReturnStatus.Draft,
                IsNotDeleted = true
            };

            await _dbContext.SalesReturn.AddAsync(existingSalesReturn);
            await _dbContext.SaveChangesAsync();

            var editedSalesReturnModel = new SalesReturnFormModel.SalesReturnModel
            {
                RowGuid = existingSalesReturn.RowGuid,
                DeliveryOrderId = existingSalesReturn.DeliveryOrderId,
                ReturnDate = DateTime.Now,
                Status = SalesReturnStatus.Confirmed
            };

            _salesReturnFormModel.SalesReturnForm = editedSalesReturnModel;
            _salesReturnFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedSalesReturnModel, existingSalesReturn))
                .Callback((SalesReturnFormModel.SalesReturnModel source, SalesReturn destination) =>
                {
                    destination.ReturnDate = source.ReturnDate;
                    destination.Status = source.Status;
                });

            _salesReturnFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _salesReturnFormModel.OnPostAsync(editedSalesReturnModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./SalesReturnForm?rowGuid={editedSalesReturnModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _salesReturnFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }
    }
}