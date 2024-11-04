using AutoMapper;
using Indotalent.Applications.Scrappings;
using Indotalent.Applications.NumberSequences;
using Indotalent.Applications.Warehouses;
using Indotalent.Applications.Products;
using Indotalent.Applications.InventoryTransactions;
using Indotalent.Data;
using Indotalent.Infrastructures.Repositories;
using Indotalent.Models.Entities;
using Indotalent.Pages.Scrappings;
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
    public class ScrappingFormTests
    {
        private Mock<IMapper> _mapperMock;
        private ScrappingService _scrappingService;
        private NumberSequenceService _numberSequenceService;
        private WarehouseService _warehouseService;
        private ProductService _productService;
        private InventoryTransactionService _inventoryTransactionService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<IAuditColumnTransformer> _auditColumnTransformerMock;
        private ScrappingFormModel _scrappingFormModel;
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
            _scrappingService = new ScrappingService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _numberSequenceService = new NumberSequenceService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _warehouseService = new WarehouseService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _productService = new ProductService(_dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);
            _inventoryTransactionService = new InventoryTransactionService(_warehouseService, _dbContext, _httpContextAccessorMock.Object, _auditColumnTransformerMock.Object);

            // Crear la instancia del modelo de formulario utilizando el servicio real
            _scrappingFormModel = new ScrappingFormModel(_mapperMock.Object, _scrappingService, _numberSequenceService, _warehouseService, _productService, _inventoryTransactionService);

            // Configurar TempData para evitar errores de referencia nula
            _scrappingFormModel.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Configurar HttpContext en PageContext directamente
            _scrappingFormModel.PageContext.HttpContext = new DefaultHttpContext();
        }

        [TearDown]
        public void TearDown()
        {
            // Disponer del contexto después de cada prueba
            _dbContext.Dispose();
        }

        [Test]
        public async Task OnPostAsync_AgregarNuevoScrapping()
        {
            // Arrange
            var newScrappingModel = new ScrappingFormModel.ScrappingModel
            {
                ScrappingDate = DateTime.Now,
                WarehouseId = 1
            };

            _scrappingFormModel.ScrappingForm = newScrappingModel;
            _scrappingFormModel.Action = "create";

            var mappedScrapping = new Scrapping
            {
                RowGuid = Guid.NewGuid(),
                ScrappingDate = newScrappingModel.ScrappingDate,
                WarehouseId = newScrappingModel.WarehouseId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newScrappingModel;
            var expectedMappedResult = mappedScrapping;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<Scrapping>(sourceModel))
                .Returns(expectedMappedResult);

            _scrappingFormModel.TempData["StatusMessage"] = string.Empty;

            // Act
            var result = await _scrappingFormModel.OnPostAsync(newScrappingModel);

            // Verifica si el `createResult` es de tipo RedirectResult y obtiene la URL
            var redirectResult = result as RedirectResult;
            Assert.IsNotNull(redirectResult, "Se esperaba un RedirectResult, pero fue nulo.");

            // Define los valores esperados y obtenidos
            string expectedUrl = $"./ScrappingForm?rowGuid={mappedScrapping.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;


            // Assert
            Assert.AreEqual("Success create new data.", _scrappingFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success create new data.");

            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");
        }

        [Test]
        public async Task OnPostAsync_EditarScrapping()
        {
            // Arrange 
            var newScrappingModel = new ScrappingFormModel.ScrappingModel
            {
                ScrappingDate = DateTime.Now,
                WarehouseId = 1
            };

            _scrappingFormModel.ScrappingForm = newScrappingModel;


            _scrappingFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "create" }
            });

            var mappedScrapping = new Scrapping
            {
                RowGuid = Guid.NewGuid(),
                ScrappingDate = newScrappingModel.ScrappingDate,
                WarehouseId = newScrappingModel.WarehouseId
            };

            // Define el modelo de entrada y el objeto esperado como resultado del mapeo
            var sourceModel = newScrappingModel;
            var expectedMappedResult = mappedScrapping;

            // Configura el mock para devolver el objeto esperado cuando se llame al m�todo Map con el modelo de entrada
            _mapperMock
                .Setup(mapper => mapper.Map<Scrapping>(sourceModel))
                .Returns(expectedMappedResult);

            _scrappingFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 1
            var createResult = await _scrappingFormModel.OnPostAsync(newScrappingModel);
           
            var createdRowGuid = mappedScrapping.RowGuid;

            var checkDate = DateTime.Now;

            // Arrange
            var editedScrappingModel = new ScrappingFormModel.ScrappingModel
            {
                RowGuid = createdRowGuid,
                ScrappingDate = checkDate,
                WarehouseId = 2
            };

            _scrappingFormModel.ScrappingForm = editedScrappingModel;

            // Cambia el `Request.Query["action"]` a "edit" para simular edici�n
            _scrappingFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "edit" }
            });

            // Configura el mock para mapear el modelo de entrada editado a la entidad
            _mapperMock
                .Setup(mapper => mapper.Map(editedScrappingModel, mappedScrapping))
                .Callback((ScrappingFormModel.ScrappingModel source, Scrapping destination) =>
                {
                    destination.ScrappingDate = source.ScrappingDate;
                    destination.WarehouseId = source.WarehouseId;
                });

            // Act 2
            var editResult = await _scrappingFormModel.OnPostAsync(editedScrappingModel);

            // Verifica si el `editResult` es de tipo RedirectResult y obt�n la URL
            var redirectResult = editResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");


            string expectedUrl = $"./ScrappingForm?rowGuid={editedScrappingModel.RowGuid}&action=edit";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success update existing data.", _scrappingFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success update existing data.");

            Assert.AreEqual(checkDate, mappedScrapping.ScrappingDate, "Campo Scrapping Date Actualizado.");
            Assert.AreEqual(2, mappedScrapping.WarehouseId, "Campo Warehouse ID Actualizado.");

            // Usa Assert.AreEqual para un mensaje de error detallado
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

        }

        [Test]
        public async Task OnPostAsync_EliminarScrapping()
        {
            // Arrange 
            var newScrappingModel = new ScrappingFormModel.ScrappingModel
            {
                ScrappingDate = DateTime.Now,
                WarehouseId = 1
            };

            _scrappingFormModel.ScrappingForm = newScrappingModel;

          
            var mappedScrapping = new Scrapping
            {
                RowGuid = Guid.NewGuid(),
                ScrappingDate = newScrappingModel.ScrappingDate,
                WarehouseId = newScrappingModel.WarehouseId,
                IsNotDeleted = true 
            };

            // Act 1
            await _dbContext.Scrapping.AddAsync(mappedScrapping);
            await _dbContext.SaveChangesAsync();

            _scrappingFormModel.TempData["StatusMessage"] = string.Empty;

            // Act 2
            _scrappingFormModel.ScrappingForm = new ScrappingFormModel.ScrappingModel
            {
                RowGuid = mappedScrapping.RowGuid
            };

            // Cambia el `Request.Query["action"]` a "delete" para simular la acción de eliminación
            _scrappingFormModel.PageContext.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "action", "delete" }
            });

            var deleteResult = await _scrappingFormModel.OnPostAsync(_scrappingFormModel.ScrappingForm);

            // Verifica que el resultado de `deleteResult` sea de tipo RedirectResult y obtiene la URL
            var redirectResult = deleteResult as RedirectResult;
            Assert.IsNotNull(redirectResult, "Expected a RedirectResult, but got null.");

            // Define la URL esperada y la URL obtenida
            string expectedUrl = $"./ScrappingList";
            string actualUrl = redirectResult.Url;

            // Assert 
            Assert.AreEqual("Success delete existing data.", _scrappingFormModel.TempData["StatusMessage"], "El mensaje debe ser: Success delete existing data.");
            Assert.AreEqual(expectedUrl, actualUrl, $"Expected: {expectedUrl}\nBut was: {actualUrl}");

            // Verifica que el registro se haya marcado como eliminado en la base de datos
            var deletedScrapping = await _dbContext.Scrapping
                .SingleOrDefaultAsync(u => u.RowGuid == mappedScrapping.RowGuid);


            Assert.IsNotNull(deletedScrapping, "El registro debe existir en la base de datos.");
            Assert.IsFalse(deletedScrapping.IsNotDeleted, "El registro debe estar marcado como eliminado (IsNotDeleted = false).");
        }







    }







}