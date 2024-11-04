using AutoMapper;
using Indotalent.Applications.StockCounts;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.Products;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.StockCounts;
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
    public class StockCountFormTests
    {
        private Mock<IMapper> _mapperMock;
        private StockCountService _stockCountService;
        private NumberSequenceService _numberSequenceService;
        private WarehouseService _warehouseService;
        private ProductService _productService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private StockCountFormModel _stockCountFormModel;
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
            _stockCountService = new StockCountService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _stockCountFormModel = new StockCountFormModel(_mapperMock.Object, _stockCountService, _numberSequenceService, _warehouseService, _productService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _stockCountFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _stockCountFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoStockCount()
        {
            // Arrange
            var newStockCountModel = new StockCountFormModel.StockCountModel
            {
                CountDate = DateTime.Now,
                WarehouseId = 1
            };

            _stockCountFormModel.StockCountForm = newStockCountModel;
            _stockCountFormModel.Action = "create";

            var mappedStockCount = new StockCount
            {
                RowGuid = Guid.NewGuid(),
                CountDate = newStockCountModel.CountDate,
                WarehouseId = newStockCountModel.WarehouseId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newStockCountModel;
            var expectedMappedResult = mappedStockCount;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<StockCount>(sourceModel))
                .Returns(expectedMappedResult);

            _stockCountFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _stockCountFormModel.OnPostAsync(newStockCountModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./StockCountForm?rowGuid={mappedStockCount.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _stockCountFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarStockCount()
        {
            // Arrange 
            var newStockCountModel = new StockCountFormModel.StockCountModel
            {
                CountDate = DateTime.Now,
                WarehouseId = 1
            };

            _stockCountFormModel.StockCountForm = newStockCountModel;


            _stockCountFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedStockCount = new StockCount
            {
                RowGuid = Guid.NewGuid(),
                CountDate = newStockCountModel.CountDate,
                WarehouseId = newStockCountModel.WarehouseId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newStockCountModel;
            var expectedMappedResult = mappedStockCount;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<StockCount>(sourceModel))
                .Returns(expectedMappedResult);

            _stockCountFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _stockCountFormModel.OnPostAsync(newStockCountModel);
           
            var createdRowGuid = mappedStockCount.RowGuid;

            var checkDate = DateTime.Now;

            // Arrange
            var editedStockCountModel = new StockCountFormModel.StockCountModel
            {
                RowGuid = createdRowGuid,
                CountDate = checkDate,
                WarehouseId = 2
            };

            _stockCountFormModel.StockCountForm = editedStockCountModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _stockCountFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedStockCountModel, mappedStockCount))
                .Callback((StockCountFormModel.StockCountModel source, StockCount destination) =>
                {
                    destination.CountDate = source.CountDate;
                    destination.WarehouseId = source.WarehouseId;
                });

            // Act 2
            var editResult = await _stockCountFormModel.OnPostAsync(editedStockCountModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./StockCountForm?rowGuid={editedStockCountModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _stockCountFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual(checkDate, mappedStockCount.CountDate, "Campo Count Date Actualizado.");
            Assert.AreEqual(2, mappedStockCount.WarehouseId, "Campo Warehouse ID Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarStockCount()
        {
            // Arrange 
            var newStockCountModel = new StockCountFormModel.StockCountModel
            {
                CountDate = DateTime.Now,
                WarehouseId = 1
            };

            _stockCountFormModel.StockCountForm = newStockCountModel;

          
            var mappedStockCount = new StockCount
            {
                RowGuid = Guid.NewGuid(),
                CountDate = newStockCountModel.CountDate,
                WarehouseId = newStockCountModel.WarehouseId,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.StockCount.AddAsync(mappedStockCount);
            await _dbContext.SaveChangesAsync();

            _stockCountFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _stockCountFormModel.StockCountForm = new StockCountFormModel.StockCountModel
            {
                RowGuid = mappedStockCount.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _stockCountFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _stockCountFormModel.OnPostAsync(_stockCountFormModel.StockCountForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./StockCountList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _stockCountFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedStockCount = await _dbContext.StockCount
                .SingleOrDefaultAsync(u => u.RowGuid == mappedStockCount.RowGuid);


            Assert.IsNotNull(deletedStockCount, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedStockCount.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}