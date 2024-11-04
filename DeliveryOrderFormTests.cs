using AutoMapper;
using Indotalent.Applications.SalesOrders;
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
using Indotalent.Pages.DeliveryOrders;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.InventoryTransactions;

namespace TestProject1
{
    [TestFixture]
    public class DeliveryOrderFormTests
    {
        private Mock<IMapper> _mapperMock;
        private DeliveryOrderService _deliveryOrderService;
        private SalesOrderService _salesOrderService;
        private NumberSequenceService _numberSequenceService;
        private ProductService _productService;
        private WarehouseService _warehouseService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private DeliveryOrderFormModel _deliveryOrderFormModel;
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
            _deliveryOrderService = new DeliveryOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _salesOrderService = new SalesOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _deliveryOrderFormModel = new DeliveryOrderFormModel(_mapperMock.Object, _deliveryOrderService, _numberSequenceService, _salesOrderService, _productService, _warehouseService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _deliveryOrderFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _deliveryOrderFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoDeliveryOrder()
        {
            // Arrange
            var newDeliveryOrderModel = new DeliveryOrderFormModel.DeliveryOrderModel
            {
                SalesOrderId = 1,
                DeliveryDate = DateTime.Now,
                Status = DeliveryOrderStatus.Draft
            };

            _deliveryOrderFormModel.DeliveryOrderForm = newDeliveryOrderModel;
            _deliveryOrderFormModel.Action = "create";

            var mappedDeliveryOrder = new DeliveryOrder
            {
                RowGuid = Guid.NewGuid(),
                SalesOrderId = newDeliveryOrderModel.SalesOrderId,
                DeliveryDate = newDeliveryOrderModel.DeliveryDate,
                Status = newDeliveryOrderModel.Status
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newDeliveryOrderModel;
            var expectedMappedResult = mappedDeliveryOrder;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<DeliveryOrder>(sourceModel))
                .Returns(expectedMappedResult);

            _deliveryOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _deliveryOrderFormModel.OnPostAsync(newDeliveryOrderModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./DeliveryOrderForm?rowGuid={mappedDeliveryOrder.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _deliveryOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarDeliveryOrder()
        {
            // Arrange
            var existingDeliveryOrder = new DeliveryOrder
            {
                RowGuid = Guid.NewGuid(),
                SalesOrderId = 1,
                DeliveryDate = DateTime.Now.AddDays(-1),
                Status = DeliveryOrderStatus.Draft,
                IsNotDeleted = true
            };

            await _dbContext.DeliveryOrder.AddAsync(existingDeliveryOrder);
            await _dbContext.SaveChangesAsync();

            var editedDeliveryOrderModel = new DeliveryOrderFormModel.DeliveryOrderModel
            {
                RowGuid = existingDeliveryOrder.RowGuid,
                SalesOrderId = existingDeliveryOrder.SalesOrderId,
                DeliveryDate = DateTime.Now,
                Status = DeliveryOrderStatus.Confirmed
            };

            _deliveryOrderFormModel.DeliveryOrderForm = editedDeliveryOrderModel;
            _deliveryOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedDeliveryOrderModel, existingDeliveryOrder))
                .Callback((DeliveryOrderFormModel.DeliveryOrderModel source, DeliveryOrder destination) =>
                {
                    destination.DeliveryDate = source.DeliveryDate;
                    destination.Status = source.Status;
                });

            _deliveryOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _deliveryOrderFormModel.OnPostAsync(editedDeliveryOrderModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./DeliveryOrderForm?rowGuid={editedDeliveryOrderModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _deliveryOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarDeliveryOrder()
        {
            // Arrange
            var existingDeliveryOrder = new DeliveryOrder
            {
                RowGuid = Guid.NewGuid(),
                SalesOrderId = 1,
                DeliveryDate = DateTime.Now.AddDays(-1),
                Status = DeliveryOrderStatus.Draft,
                IsNotDeleted = true
            };

            await _dbContext.DeliveryOrder.AddAsync(existingDeliveryOrder);
            await _dbContext.SaveChangesAsync();

            _deliveryOrderFormModel.DeliveryOrderForm = new DeliveryOrderFormModel.DeliveryOrderModel
            {
                RowGuid = existingDeliveryOrder.RowGuid
            };

            _deliveryOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _deliveryOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _deliveryOrderFormModel.OnPostAsync(_deliveryOrderFormModel.DeliveryOrderForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./DeliveryOrderList";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success delete existing data.", _deliveryOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            var deletedDeliveryOrder = await _dbContext.DeliveryOrder.SingleOrDefaultAsync(u => u.RowGuid == existingDeliveryOrder.RowGuid);
            Assert.IsNotNull(deletedDeliveryOrder, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedDeliveryOrder.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }
    }
}