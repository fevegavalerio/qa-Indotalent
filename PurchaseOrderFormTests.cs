using AutoMapper;
using Indotalent.Applications.PurchaseOrders;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Vendors;
using Indotalent.Applications.Taxes;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.PurchaseOrders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Indotalent.Applications.Products;
using Indotalent.Models.Enums;

namespace TestProject1
{
    [TestFixture]
    public class PurchaseOrderFormModelTests
    {
        private Mock<IMapper> _mapperMock;
        private PurchaseOrderService _purchaseOrderService;
        private NumberSequenceService _numberSequenceService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private PurchaseOrderFormModel _purchaseOrderFormModel;
        private ApplicationDbContext _dbContext;
        private VendorService _vendorService;
        private TaxService _taxService;
        private ProductService _productService;

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

            // Crear instancia de ApplicationDbContext
            _dbContext = new ApplicationDbContext(options);

            // Crear el servicio utilizando el dbContext real y los mocks de otras dependencias
            _purchaseOrderService = new PurchaseOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _vendorService = new VendorService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _taxService = new TaxService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _purchaseOrderFormModel = new PurchaseOrderFormModel(
                _mapperMock.Object,
                _purchaseOrderService,
                _numberSequenceService,
                _vendorService,
                _taxService,
                _productService
            );

            // Configurar TempData para evitar errores de referencia nula
            _purchaseOrderFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _purchaseOrderFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoPurchaseOrder()
        {
            var newPurchaseOrderModel = new PurchaseOrderFormModel.PurchaseOrderModel
            {
                VendorId = 1,
                OrderDate = DateTime.Now,
                TaxId = 1,
                OrderStatus = PurchaseOrderStatus.Draft
            };

            _purchaseOrderFormModel.PurchaseOrderForm = newPurchaseOrderModel;
            _purchaseOrderFormModel.Action = "create";

            var mappedPurchaseOrder = new PurchaseOrder
            {
                RowGuid = Guid.NewGuid(),
                VendorId = newPurchaseOrderModel.VendorId,
                OrderDate = newPurchaseOrderModel.OrderDate,
                TaxId = newPurchaseOrderModel.TaxId,
                OrderStatus = newPurchaseOrderModel.OrderStatus
            };

            var sourceModel = newPurchaseOrderModel;
            var expectedMappedResult = mappedPurchaseOrder;

            _mapperMock
                .Setup(mapper => mapper.Map<PurchaseOrder>(sourceModel))
                .Returns(expectedMappedResult);

            _purchaseOrderFormModel.TempData["StatusMessage"] = string.Empty;

            var result = await _purchaseOrderFormModel.OnPostAsync(newPurchaseOrderModel);

            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            string expectedUrl = $"./PurchaseOrderForm?rowGuid={mappedPurchaseOrder.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            Assert.AreEqual("Success create new data.", _purchaseOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarPurchaseOrder()
        {
            // Arrange
            var existingPurchaseOrder = new PurchaseOrder
            {
                RowGuid = Guid.NewGuid(),
                VendorId = 1,
                OrderDate = DateTime.Now.AddDays(-1),
                TaxId = 1,
                OrderStatus = PurchaseOrderStatus.Draft,
                IsNotDeleted = true
            };

            await _dbContext.PurchaseOrder.AddAsync(existingPurchaseOrder);
            await _dbContext.SaveChangesAsync();

            var editedPurchaseOrderModel = new PurchaseOrderFormModel.PurchaseOrderModel
            {
                RowGuid = existingPurchaseOrder.RowGuid,
                VendorId = 2,
                OrderDate = DateTime.Now,
                TaxId = 2,
                OrderStatus = PurchaseOrderStatus.Confirmed
            };

            _purchaseOrderFormModel.PurchaseOrderForm = editedPurchaseOrderModel;
            _purchaseOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            _mapperMock
                .Setup(mapper => mapper.Map(editedPurchaseOrderModel, existingPurchaseOrder))
                .Callback((PurchaseOrderFormModel.PurchaseOrderModel source, PurchaseOrder destination) =>
                {
                    destination.VendorId = source.VendorId;
                    destination.OrderDate = source.OrderDate;
                    destination.TaxId = source.TaxId;
                    destination.OrderStatus = source.OrderStatus;
                });

            _purchaseOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _purchaseOrderFormModel.OnPostAsync(editedPurchaseOrderModel);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");
            string expectedUrl = $"./PurchaseOrderForm?rowGuid={editedPurchaseOrderModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;
            Assert.AreEqual("Success update existing data.", _purchaseOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EliminarPurchaseOrder()
        {
            // Arrange
            var existingPurchaseOrder = new PurchaseOrder
            {
                RowGuid = Guid.NewGuid(),
                VendorId = 1,
                OrderDate = DateTime.Now,
                TaxId = 1,
                OrderStatus = PurchaseOrderStatus.Draft,
                IsNotDeleted = true
            };

            await _dbContext.PurchaseOrder.AddAsync(existingPurchaseOrder);
            await _dbContext.SaveChangesAsync();

            _purchaseOrderFormModel.PurchaseOrderForm = new PurchaseOrderFormModel.PurchaseOrderModel
            {
                RowGuid = existingPurchaseOrder.RowGuid
            };

            _purchaseOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            _purchaseOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _purchaseOrderFormModel.OnPostAsync(_purchaseOrderFormModel.PurchaseOrderForm);

            // Assert
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            string expectedUrl = "./PurchaseOrderList";
            string actualUrl = redirectResult.Url;

            Assert.AreEqual("Success delete existing data.", _purchaseOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedPurchaseOrder = await _dbContext.PurchaseOrder
                .SingleOrDefaultAsync(u => u.RowGuid == existingPurchaseOrder.RowGuid);

            Assert.IsNotNull(deletedPurchaseOrder, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedPurchaseOrder.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }
    }
}