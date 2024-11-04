using AutoMapper;
using Indotalent.Applications.SalesOrders;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Customers;
using Indotalent.Applications.Taxes;
using Indotalent.Applications.Products;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Models.Enums;
using Indotalent.Pages.SalesOrders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using OpenQA.Selenium.BiDi.Modules.BrowsingContext;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace TestProject1
{
    [TestFixture]
    public class SalesOrderFormTests
    {
        private Mock<IMapper> _mapperMock;
        private SalesOrderService _salesOrderService;
        private NumberSequenceService _numberSequenceService;
        private CustomerService _customerService;
        private TaxService _taxService;
        private ProductService _productService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private SalesOrderFormModel _salesOrderFormModel;
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
            _salesOrderService = new SalesOrderService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _customerService = new CustomerService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _taxService = new TaxService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _salesOrderFormModel = new SalesOrderFormModel(_mapperMock.Object, _salesOrderService, _numberSequenceService, _customerService, _taxService, _productService);

            // Configurar TempData para evitar errores de referencia nula
            _salesOrderFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _salesOrderFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoSalesOrder()
        {
            // Arrange
            var newSalesOrderModel = new SalesOrderFormModel.SalesOrderModel
            {
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TaxId = 1,
                OrderStatus = SalesOrderStatus.Draft
            };

            _salesOrderFormModel.SalesOrderForm = newSalesOrderModel;
            _salesOrderFormModel.Action = "create";

            var mappedSalesOrder = new SalesOrder
            {
                RowGuid = Guid.NewGuid(),
                CustomerId = newSalesOrderModel.CustomerId,
                OrderDate = newSalesOrderModel.OrderDate,
                TaxId = newSalesOrderModel.TaxId,
                OrderStatus = newSalesOrderModel.OrderStatus
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newSalesOrderModel;
            var expectedMappedResult = mappedSalesOrder;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<SalesOrder>(sourceModel))
                .Returns(expectedMappedResult);

            _salesOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _salesOrderFormModel.OnPostAsync(newSalesOrderModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./SalesOrderForm?rowGuid={mappedSalesOrder.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _salesOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarSalesOrder()
        {
            // Arrange 
            var newSalesOrderModel = new SalesOrderFormModel.SalesOrderModel
            {
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TaxId = 1,
                OrderStatus = SalesOrderStatus.Draft
            };

            _salesOrderFormModel.SalesOrderForm = newSalesOrderModel;


            _salesOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedSalesOrder = new SalesOrder
            {
                RowGuid = Guid.NewGuid(),
                CustomerId = newSalesOrderModel.CustomerId,
                OrderDate = newSalesOrderModel.OrderDate,
                TaxId = newSalesOrderModel.TaxId,
                OrderStatus = newSalesOrderModel.OrderStatus
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newSalesOrderModel;
            var expectedMappedResult = mappedSalesOrder;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<SalesOrder>(sourceModel))
                .Returns(expectedMappedResult);

            _salesOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _salesOrderFormModel.OnPostAsync(newSalesOrderModel);
           
            var createdRowGuid = mappedSalesOrder.RowGuid;

            var checkDate = DateTime.Now;

            // Arrange
            var editedSalesOrderModel = new SalesOrderFormModel.SalesOrderModel
            {
                RowGuid = createdRowGuid,
                CustomerId = 2,
                OrderDate = checkDate,
                TaxId = 2,
                OrderStatus = SalesOrderStatus.Confirmed
            };

            _salesOrderFormModel.SalesOrderForm = editedSalesOrderModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _salesOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedSalesOrderModel, mappedSalesOrder))
                .Callback((SalesOrderFormModel.SalesOrderModel source, SalesOrder destination) =>
                {
                    destination.CustomerId = source.CustomerId;
                    destination.OrderDate = source.OrderDate;
                    destination.TaxId = source.TaxId;
                    destination.OrderStatus = source.OrderStatus;
                });

            // Act 2
            var editResult = await _salesOrderFormModel.OnPostAsync(editedSalesOrderModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./SalesOrderForm?rowGuid={editedSalesOrderModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _salesOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual(2, mappedSalesOrder.CustomerId, "Campo Customer Actualizado.");
            Assert.AreEqual(checkDate, mappedSalesOrder.OrderDate, "Campo Order Date Actualizado.");
            Assert.AreEqual(2, mappedSalesOrder.TaxId, "Campo Tax Actualizado.");
            Assert.AreEqual(SalesOrderStatus.Confirmed, mappedSalesOrder.OrderStatus, "Campo Order Status Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarSalesOrder()
        {
            // Arrange 
            var newSalesOrderModel = new SalesOrderFormModel.SalesOrderModel
            {
                CustomerId = 1,
                OrderDate = DateTime.Now,
                TaxId = 1,
                OrderStatus = SalesOrderStatus.Draft
            };

            _salesOrderFormModel.SalesOrderForm = newSalesOrderModel;

          
            var mappedSalesOrder = new SalesOrder
            {
                RowGuid = Guid.NewGuid(),
                CustomerId = newSalesOrderModel.CustomerId,
                OrderDate = newSalesOrderModel.OrderDate,
                TaxId = newSalesOrderModel.TaxId,
                OrderStatus = newSalesOrderModel.OrderStatus,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.SalesOrder.AddAsync(mappedSalesOrder);
            await _dbContext.SaveChangesAsync();

            _salesOrderFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _salesOrderFormModel.SalesOrderForm = new SalesOrderFormModel.SalesOrderModel
            {
                RowGuid = mappedSalesOrder.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _salesOrderFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _salesOrderFormModel.OnPostAsync(_salesOrderFormModel.SalesOrderForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./SalesOrderList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _salesOrderFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedSalesOrder = await _dbContext.SalesOrder
                .SingleOrDefaultAsync(u => u.RowGuid == mappedSalesOrder.RowGuid);


            Assert.IsNotNull(deletedSalesOrder, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedSalesOrder.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}